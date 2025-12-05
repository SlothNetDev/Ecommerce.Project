using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.RefreshToken;

public interface ITokenRefreshService
{
    Task<ResponseType<TokenResponseDto>> RefreshTokenAsync(
        string accessToken, 
        string refreshToken, 
        string ipAddress);
}   