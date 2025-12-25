using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Ecommerce.Core.Application.Common.Interfaces.Security;

namespace Ecommerce.Infrastructure.Security;

public class HmacHashService : IHashService
{
    private readonly byte[] _key;

    public HmacHashService(IConfiguration configuration)
    {
        var secret = configuration["Security:OtpHashKey"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("OTP hash key is not configured.");

        _key = Encoding.UTF8.GetBytes(secret);
    }
    public string Hash(string input)
    {
        using var hmac = new HMACSHA256(_key);
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(hmac.ComputeHash(bytes));
    }

    public bool Verify(string input, string hashedInput)
    {
        var computed = Hash(input);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computed),
            Convert.FromBase64String(hashedInput));
    }
}