using System.Security.Cryptography;

namespace Ecommerce.Core.Domain.Utilities;

public static class OtpGenerator
{
    private static readonly Random Random = new();
    
    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP code.
    /// </summary>
    public static string GenerateOtp()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var number = BitConverter.ToUInt32(bytes) % 900000 + 100000;
        return number.ToString();
    }
}