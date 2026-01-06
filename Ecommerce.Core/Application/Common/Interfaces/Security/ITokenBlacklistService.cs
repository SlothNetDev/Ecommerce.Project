namespace Ecommerce.Core.Application.Common.Interfaces.Security;

public interface ITokenBlacklistService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="jti"></param>
    /// <param name="expiresIn"></param>
    /// <returns></returns>
    Task BlackListTokenAsync(string jti, TimeSpan expiresIn);
    Task<bool> IsTokenBlacklistedAsync(string jti);
}