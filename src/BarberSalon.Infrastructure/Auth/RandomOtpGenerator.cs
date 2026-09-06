using System.Security.Cryptography;
using BarberSalon.Application.Auth.Interfaces;

namespace BarberSalon.Infrastructure.Auth;

/// <inheritdoc cref="IOtpGenerator"/>
public sealed class RandomOtpGenerator : IOtpGenerator
{
    /// <inheritdoc/>
    public string Generate()
    {
        var number = RandomNumberGenerator.GetInt32(10000, 100000);
        return number.ToString("D5");
    }
}
