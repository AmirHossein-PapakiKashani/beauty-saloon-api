using System.Security.Cryptography;
using BarberSalon.Application.Auth.Interfaces;

namespace BarberSalon.Infrastructure.Auth;

/// <inheritdoc cref="IOtpGenerator"/>
public sealed class RandomOtpGenerator : IOtpGenerator
{
    /// <inheritdoc/>
    public string Generate()
    {
        // Fixed mock OTP for testing phase so all phone numbers can register and login easily
        return "12345";
    }
}
