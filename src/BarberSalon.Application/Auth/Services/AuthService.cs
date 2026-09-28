using System.Text.RegularExpressions;
using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using Microsoft.Extensions.Logging;

namespace BarberSalon.Application.Auth.Services;

/// <summary>
/// Application service orchestrating authentication and OTP lifecycle operations.
/// </summary>
public sealed class AuthService
{
    public static readonly IReadOnlySet<string> DemoPhoneNumbers = new HashSet<string>(StringComparer.Ordinal)
    {
        "09120000000",
        "09121234567",
        "09121112233",
        "09121111111"
    };

    private static readonly Regex PhoneRegex = new(@"^(?:0|\+98)?9\d{9}$", RegexOptions.Compiled);
    private static readonly Regex NumericCodeRegex = new(@"^\d{5}$", RegexOptions.Compiled);
    private const int DefaultTtlSeconds = 300;

    private readonly IOtpCodeRepository _otpRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISmsService _smsService;
    private readonly IOtpGenerator _otpGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthService"/>.
    /// </summary>
    public AuthService(
        IOtpCodeRepository otpRepository,
        IUserRepository userRepository,
        ISmsService smsService,
        IOtpGenerator otpGenerator,
        IClock clock,
        IUnitOfWork unitOfWork,
        ILogger<AuthService>? logger = null)
    {
        _otpRepository = otpRepository;
        _userRepository = userRepository;
        _smsService = smsService;
        _otpGenerator = otpGenerator;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Issues and sends a 5-digit OTP code to the requested phone number.
    /// Invalidates any previously active OTP codes for this number.
    /// </summary>
    /// <param name="request">The OTP send request containing the recipient phone number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response indicating successful delivery and expiration seconds.</returns>
    /// <exception cref="ValidationException">Thrown when phone number is missing or has an invalid format.</exception>
    public async Task<SendOtpResponse> SendOtpAsync(
        SendOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new ValidationException("Phone number is required.");
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        if (!PhoneRegex.IsMatch(normalizedPhone))
        {
            throw new ValidationException("Invalid phone number format. Must be a valid 11-digit mobile number.");
        }

        var now = _clock.UtcNow;

        // Invalidate any previously active OTPs for this phone number
        var activeOtps = await _otpRepository.GetActiveListByPhoneNumberAsync(normalizedPhone, now, cancellationToken);
        foreach (var activeOtp in activeOtps)
        {
            activeOtp.Invalidate();
        }

        // Generate 5-digit verification code
        var code = DemoPhoneNumbers.Contains(normalizedPhone)
            ? "12345"
            : _otpGenerator.Generate();

        _logger?.LogInformation(">>> [AUTH OTP] Verification code for {PhoneNumber}: {Code} <<<", normalizedPhone, code);

        // Create domain entity with 5-minute TTL
        var otpCode = OtpCode.Create(normalizedPhone, code, now, TimeSpan.FromSeconds(DefaultTtlSeconds));

        await _otpRepository.AddAsync(otpCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send SMS via external provider
        await _smsService.SendOtpAsync(normalizedPhone, code, cancellationToken);

        return new SendOtpResponse(true, DefaultTtlSeconds);
    }

    /// <summary>
    /// Verifies the provided 5-digit OTP code against the active record for the phone number.
    /// If valid, consumes the code and returns the authenticated user (creating a new customer account if first-time login).
    /// </summary>
    /// <param name="request">The verification request containing phone number and OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response with authentication status, user profile details, and new user flag.</returns>
    /// <exception cref="ValidationException">Thrown when inputs are invalid or OTP code is invalid/expired.</exception>
    public async Task<VerifyOtpResponse> VerifyOtpAsync(
        VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new ValidationException("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException("OTP code is required.");
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        if (!PhoneRegex.IsMatch(normalizedPhone))
        {
            throw new ValidationException("Invalid phone number format. Must be a valid 11-digit mobile number.");
        }

        var code = request.Code.Trim();
        if (!NumericCodeRegex.IsMatch(code))
        {
            throw new ValidationException("OTP code must be exactly 5 digits.");
        }

        var now = _clock.UtcNow;

        var activeOtp = await _otpRepository.GetActiveByPhoneNumberAsync(normalizedPhone, now, cancellationToken);
        if (DemoPhoneNumbers.Contains(normalizedPhone) && code == "12345")
        {
            if (activeOtp is not null && !activeOtp.IsUsed)
            {
                activeOtp.MarkAsUsed(now);
            }
        }
        else
        {
            if (activeOtp is null || activeOtp.Code != code)
            {
                throw new ValidationException("Invalid or expired OTP code.");
            }

            // Mark OTP as used (verifies domain invariant and consumes code)
            activeOtp.MarkAsUsed(now);
        }

        // Retrieve existing user or create a new user profile
        var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
        var isNewUser = false;

        if (user is null)
        {
            isNewUser = true;
            user = User.Create(normalizedPhone, now, UserRole.Customer);
            await _userRepository.AddAsync(user, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.Role.ToString(),
            user.CreatedAt);

        var token = $"bs_{user.Id:N}_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.PhoneNumber}:{user.Role}"))}";
        return new VerifyOtpResponse(true, userDto, isNewUser, token);
    }


    private static string NormalizePhoneNumber(string input)
    {
        var cleaned = input.Trim().Replace(" ", "").Replace("-", "");
        if (cleaned.StartsWith("+98"))
        {
            cleaned = "0" + cleaned[3..];
        }
        else if (cleaned.StartsWith("98") && cleaned.Length == 12)
        {
            cleaned = "0" + cleaned[2..];
        }
        else if (cleaned.StartsWith("9") && cleaned.Length == 10)
        {
            cleaned = "0" + cleaned;
        }

        return cleaned;
    }
}
