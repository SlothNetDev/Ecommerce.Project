
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    RefreshTokenResponseDto GenerateRefreshToken(string userId, string ipAddress);
    Task SaveRefreshTokenAsync(RefreshTokenResponseDto token);
    Task<RefreshTokenResponseDto> GetStoredTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken, string reason, string? replacedByToken = null);
    Task<RefreshTokenResponseDto> RotateTokenAsync(RefreshTokenResponseDto oldToken, string ipAddress);
    Task<bool> IsTokenValidAsync(RefreshTokenResponseDto token);

}