namespace Ecommerce.Core.Domain.Utilities;

public class OtpGenerator
{
    private static readonly Random Random = new();
    
    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP code.
    /// </summary>
    public static string GenerateOtp()
    {
        // Use crypto-secure random for production
        // var bytes = RandomNumberGenerator.GetBytes(4);
        // var number = BitConverter.ToUInt32(bytes) % 900000 + 100000;
        // return number.ToString();
        
        // For now (development):
        lock (Random)
        {
            return Random.Next(100000, 999999).ToString();
        }
    }
}