using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Generating Token
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<ResponseType<AuthenticationResponseDto>> GenerateTokenAsync(TokenUserDto user); 
    
    ResponseType<RefreshTokenResponseDto> GenerateRefreshToken(string userId, string ipAddress);
}