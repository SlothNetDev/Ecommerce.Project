using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Generating Token for Users
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<ResponseType<AuthenticationResponseDto>> GenerateTokenAsync(TokenUserDto user); 
    
    /// <summary>
    ///  Generating the refreshtoken that will use to extend the login period or token that will be revoked
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    ResponseType<RefreshTokenResponseDto> GenerateRefreshToken(string userId, string ipAddress);
}