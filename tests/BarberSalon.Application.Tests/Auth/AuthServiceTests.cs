using BarberSalon.Application.Auth.DTOs;
using BarberSalon.Application.Auth.Interfaces;
using BarberSalon.Application.Auth.Services;
using BarberSalon.Application.Common.Exceptions;
using BarberSalon.Application.Common.Interfaces;
using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BarberSalon.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly IOtpCodeRepository _otpRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISmsService _smsService;
    private readonly IOtpGenerator _otpGenerator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthService _sut;

    private readonly DateTime _utcNow = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public AuthServiceTests()
    {
        _otpRepository = Substitute.For<IOtpCodeRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _smsService = Substitute.For<ISmsService>();
        _otpGenerator = Substitute.For<IOtpGenerator>();
        _clock = Substitute.For<IClock>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _clock.UtcNow.Returns(_utcNow);
        _otpGenerator.Generate().Returns("54321");
        _otpRepository.GetActiveListByPhoneNumberAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<OtpCode>());

        _sut = new AuthService(
            _otpRepository,
            _userRepository,
            _smsService,
            _otpGenerator,
            _clock,
            _unitOfWork);
    }

    [Fact]
    public async Task SendOtpAsync_WithValidPhoneNumber_ShouldGenerateSaveAndDispatchSms()
    {
        // Arrange
        var request = new SendOtpRequest("09123456789");

        // Act
        var response = await _sut.SendOtpAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Sent.Should().BeTrue();
        response.ExpiresInSeconds.Should().Be(300);

        _otpGenerator.Received(1).Generate();
        await _otpRepository.Received(1).AddAsync(
            Arg.Is<OtpCode>(o => o.PhoneNumber == "09123456789" && o.Code == "54321" && o.ExpiresAt == _utcNow.AddMinutes(5)),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _smsService.Received(1).SendOtpAsync("09123456789", "54321", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("09123456789", "09123456789")]
    [InlineData("+989123456789", "09123456789")]
    [InlineData("9123456789", "09123456789")]
    [InlineData(" 0912 345 6789 ", "09123456789")]
    public async Task SendOtpAsync_WithDifferentPhoneFormats_ShouldNormalizeCorrectly(string inputPhone, string expectedPhone)
    {
        // Arrange
        var request = new SendOtpRequest(inputPhone);

        // Act
        var response = await _sut.SendOtpAsync(request);

        // Assert
        response.Sent.Should().BeTrue();
        await _otpRepository.Received(1).AddAsync(
            Arg.Is<OtpCode>(o => o.PhoneNumber == expectedPhone),
            Arg.Any<CancellationToken>());
        await _smsService.Received(1).SendOtpAsync(expectedPhone, "54321", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpAsync_WhenPreviousActiveOtpsExist_ShouldInvalidateAllBeforeCreatingNew()
    {
        // Arrange
        var oldOtp1 = OtpCode.Create("09123456789", "11111", _utcNow.AddMinutes(-2));
        var oldOtp2 = OtpCode.Create("09123456789", "22222", _utcNow.AddMinutes(-1));

        _otpRepository.GetActiveListByPhoneNumberAsync("09123456789", _utcNow, Arg.Any<CancellationToken>())
            .Returns(new List<OtpCode> { oldOtp1, oldOtp2 });

        var request = new SendOtpRequest("09123456789");

        // Act
        var response = await _sut.SendOtpAsync(request);

        // Assert
        response.Sent.Should().BeTrue();
        oldOtp1.IsUsed.Should().BeTrue();
        oldOtp2.IsUsed.Should().BeTrue();
        await _otpRepository.Received(1).AddAsync(
            Arg.Is<OtpCode>(o => o.PhoneNumber == "09123456789" && o.Code == "54321" && !o.IsUsed),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SendOtpAsync_WithNullOrEmptyPhoneNumber_ShouldThrowValidationException(string? invalidPhone)
    {
        // Arrange
        var request = new SendOtpRequest(invalidPhone!);

        // Act
        var act = () => _sut.SendOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Phone number is required*");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("0912345")]
    [InlineData("091234567890123")]
    [InlineData("08123456789")]
    [InlineData("0912abc6789")]
    public async Task SendOtpAsync_WithInvalidPhoneFormat_ShouldThrowValidationException(string invalidFormat)
    {
        // Arrange
        var request = new SendOtpRequest(invalidFormat);

        // Act
        var act = () => _sut.SendOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid phone number format*");
    }

    // ─── VerifyOtpAsync Tests ───────────────────────────────────────────────

    [Fact]
    public async Task VerifyOtpAsync_WithValidOtpAndNewUser_ShouldMarkOtpUsed_CreateUser_AndReturnIsNewUserTrue()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _utcNow.AddMinutes(-1));
        _otpRepository.GetActiveByPhoneNumberAsync("09123456789", _utcNow, Arg.Any<CancellationToken>())
            .Returns(otp);
        _userRepository.GetByPhoneNumberAsync("09123456789", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var request = new VerifyOtpRequest("09123456789", "12345");

        // Act
        var response = await _sut.VerifyOtpAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.IsNewUser.Should().BeTrue();
        response.User.Should().NotBeNull();
        response.User!.PhoneNumber.Should().Be("09123456789");
        response.User.Role.Should().Be("Customer");

        otp.IsUsed.Should().BeTrue();
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.PhoneNumber == "09123456789" && u.Role == UserRole.Customer),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtpAsync_WithValidOtpAndExistingUser_ShouldMarkOtpUsed_AndReturnExistingUserWithIsNewUserFalse()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _utcNow.AddMinutes(-1));
        var existingUser = User.Create("09123456789", _utcNow.AddDays(-10), UserRole.Admin, "مدیر سالن");

        _otpRepository.GetActiveByPhoneNumberAsync("09123456789", _utcNow, Arg.Any<CancellationToken>())
            .Returns(otp);
        _userRepository.GetByPhoneNumberAsync("09123456789", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var request = new VerifyOtpRequest("09123456789", "12345");

        // Act
        var response = await _sut.VerifyOtpAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.IsNewUser.Should().BeFalse();
        response.User.Should().NotBeNull();
        response.User!.Id.Should().Be(existingUser.Id);
        response.User.PhoneNumber.Should().Be("09123456789");
        response.User.Name.Should().Be("مدیر سالن");
        response.User.Role.Should().Be("Admin");

        otp.IsUsed.Should().BeTrue();
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtpAsync_WithInvalidCode_ShouldThrowValidationException()
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "54321", _utcNow.AddMinutes(-1));
        _otpRepository.GetActiveByPhoneNumberAsync("09123456789", _utcNow, Arg.Any<CancellationToken>())
            .Returns(otp);

        var request = new VerifyOtpRequest("09123456789", "12345");

        // Act
        var act = () => _sut.VerifyOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid or expired OTP code*");

        otp.IsUsed.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyOtpAsync_WhenNoActiveOtpFound_ShouldThrowValidationException()
    {
        // Arrange
        _otpRepository.GetActiveByPhoneNumberAsync("09123456789", _utcNow, Arg.Any<CancellationToken>())
            .Returns((OtpCode?)null);

        var request = new VerifyOtpRequest("09123456789", "12345");

        // Act
        var act = () => _sut.VerifyOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Invalid or expired OTP code*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task VerifyOtpAsync_WithNullOrEmptyPhoneNumber_ShouldThrowValidationException(string? invalidPhone)
    {
        // Arrange
        var request = new VerifyOtpRequest(invalidPhone!, "12345");

        // Act
        var act = () => _sut.VerifyOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Phone number is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task VerifyOtpAsync_WithNullOrEmptyCode_ShouldThrowValidationException(string? invalidCode)
    {
        // Arrange
        var request = new VerifyOtpRequest("09123456789", invalidCode!);

        // Act
        var act = () => _sut.VerifyOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*OTP code is required*");
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("abcde")]
    [InlineData("12a45")]
    public async Task VerifyOtpAsync_WithInvalidCodeFormat_ShouldThrowValidationException(string invalidCode)
    {
        // Arrange
        var request = new VerifyOtpRequest("09123456789", invalidCode);

        // Act
        var act = () => _sut.VerifyOtpAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*OTP code must be exactly 5 digits*");
    }

    [Theory]
    [InlineData("+989123456789")]
    [InlineData("9123456789")]
    [InlineData(" 0912 345 6789 ")]
    public async Task VerifyOtpAsync_WithDifferentPhoneFormats_ShouldNormalizeAndSucceed(string inputPhone)
    {
        // Arrange
        var otp = OtpCode.Create("09123456789", "12345", _utcNow.AddMinutes(-1));
        _otpRepository.GetActiveByPhoneNumberAsync("09123456789", _utcNow, Arg.Any<CancellationToken>())
            .Returns(otp);
        _userRepository.GetByPhoneNumberAsync("09123456789", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var request = new VerifyOtpRequest(inputPhone, "12345");

        // Act
        var response = await _sut.VerifyOtpAsync(request);

        // Assert
        response.Success.Should().BeTrue();
        response.User!.PhoneNumber.Should().Be("09123456789");
    }

    [Fact]
    public async Task VerifyOtpAsync_WhenValidCode_ShouldReturnAuthenticationToken()
    {
        // Arrange
        var phone = "09123456789";
        var code = "54321";
        var request = new VerifyOtpRequest(phone, code);
        var otp = OtpCode.Create(phone, code, _utcNow, TimeSpan.FromMinutes(5));

        _otpRepository.GetActiveByPhoneNumberAsync(phone, _utcNow, Arg.Any<CancellationToken>())
            .Returns(otp);
        _userRepository.GetByPhoneNumberAsync(phone, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var response = await _sut.VerifyOtpAsync(request);

        // Assert
        response.Success.Should().BeTrue();
        response.Token.Should().NotBeNullOrWhiteSpace();
    }
}

