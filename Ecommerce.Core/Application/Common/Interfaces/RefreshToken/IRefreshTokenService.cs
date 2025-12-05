using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.RefreshToken;

public interface IRefreshTokenService
{
    Task<ResponseType<RefreshTokenResponseDto>> GenerateRefreshTokenAsync(string userId, string ipAddress);
    Task<ResponseType<RefreshTokenResponseDto>> GetRefreshTokenAsync(string token);
    Task<ResponseType<string>> RevokeRefreshTokenAsync(string token, string ipAddress, string reason);
    Task<ResponseType<RefreshTokenResponseDto>> RotateRefreshTokenAsync(string oldToken, string ipAddress);
    Task<bool> IsRefreshTokenValidAsync(string token);

}