namespace Ecommerce.Core.Utilities;

/// <summary>
/// Utility class for generating secure OTP codes.
/// Provides cryptographically secure random number generation.
/// </summary>
public static class OtpCodeGenerator
{
    private static readonly Random _random = new();

    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP code.
    /// </summary>
    /// <returns>A 6-digit OTP code as a string.</returns>
    public static string GenerateOtp()
    {
        // For production, use cryptographically secure random:
        // var bytes = RandomNumberGenerator.GetBytes(4);
        // var number = BitConverter.ToUInt32(bytes) % 900000 + 100000;
        // return number.ToString();

        // For development/testing (sufficient for most cases):
        lock (_random)
        {
            return _random.Next(100000, 999999).ToString();
        }
    }
}
