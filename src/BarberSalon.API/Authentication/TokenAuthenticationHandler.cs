using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using BarberSalon.Application.Staff.Interfaces;
using BarberSalon.Domain.Auth.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BarberSalon.API.Authentication;

/// <summary>
/// Custom authentication handler for validating BarberSalon session bearer tokens and mock test tokens.
/// </summary>
public sealed class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IStaffRepository _staffRepository;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IStaffRepository staffRepository)
        : base(options, logger, encoder)
    {
        _staffRepository = staffRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var headerValue = authHeader.ToString().Trim();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValue["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Format 1: bs_{userIdHex}_{base64(phone:role)}
            if (token.StartsWith("bs_", StringComparison.Ordinal))
            {
                var parts = token.Split('_');
                if (parts.Length >= 3)
                {
                    var userIdHex = parts[1];
                    var payloadBase64 = parts[2];
                    if (Guid.TryParseExact(userIdHex, "N", out var userId))
                    {
                        var decodedPayload = Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));
                        var subParts = decodedPayload.Split(':');
                        var phone = subParts.Length > 0 ? subParts[0] : "";
                        var roleStr = subParts.Length > 1 ? subParts[1] : "Customer";

                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.NameIdentifier, userId.ToString()),
                            new("sub", userId.ToString()),
                            new(ClaimTypes.MobilePhone, phone),
                            new(ClaimTypes.Role, roleStr),
                            new("role", roleStr)
                        };

                        if (Enum.TryParse<UserRole>(roleStr, true, out var role) && role == UserRole.Staff)
                        {
                            var staff = await _staffRepository.GetByUserIdAsync(userId)
                                ?? await _staffRepository.GetByPhoneNumberAsync(phone);
                            if (staff != null)
                            {
                                claims.Add(new Claim("StaffId", staff.Id.ToString()));
                                claims.Add(new Claim("staff_id", staff.Id.ToString()));
                            }
                        }

                        var identity = new ClaimsIdentity(claims, Scheme.Name);
                        var principal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(principal, Scheme.Name);

                        return AuthenticateResult.Success(ticket);
                    }
                }
            }

            // Format 2: mock / test tokens
            if (token.Equals("mock_admin_token", StringComparison.OrdinalIgnoreCase) || token.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminClaims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new(ClaimTypes.Role, "Admin"),
                    new("role", "Admin")
                };
                var identity = new ClaimsIdentity(adminClaims, Scheme.Name);
                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
            }

            if (token.StartsWith("mock_staff_", StringComparison.OrdinalIgnoreCase))
            {
                var staffIdStr = token["mock_staff_".Length..];
                var staffClaims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new(ClaimTypes.Role, "Staff"),
                    new("role", "Staff")
                };
                if (Guid.TryParse(staffIdStr, out var staffGuid))
                {
                    staffClaims.Add(new Claim("StaffId", staffGuid.ToString()));
                    staffClaims.Add(new Claim("staff_id", staffGuid.ToString()));
                }
                var identity = new ClaimsIdentity(staffClaims, Scheme.Name);
                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
            }

            return AuthenticateResult.Fail("Invalid or unsupported token format.");
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Token validation failed: {ex.Message}");
        }
    }
}
