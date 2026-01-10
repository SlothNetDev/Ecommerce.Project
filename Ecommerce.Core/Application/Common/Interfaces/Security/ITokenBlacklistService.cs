namespace Ecommerce.Core.Application.Common.Interfaces.Security;

public interface ITokenBlacklistService
{
    /// <summary>
    /// Create a way to block or revoked user claims
    /// </summary>
    /// <param name="jti"></param>
    /// <param name="expiresIn"></param>
    /// <returns></returns>
    Task BlackListTokenAsync(string jti, TimeSpan expiresIn);
    Task<bool> IsTokenBlacklistedAsync(string jti);
}