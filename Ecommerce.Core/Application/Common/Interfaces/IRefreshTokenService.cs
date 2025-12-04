
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<ResponseType<RefreshTokenResponseDto>> GenerateRefreshToken(string userId, string ipAddress);
    Task SaveRefreshTokenAsync(RefreshTokenResponseDto refreshToken);
    Task<ResponseType<RefreshTokenResponseDto>> GetStoredTokenAsync(string refreshToken);
    Task<string> RevokeTokenAsync(string refreshToken, string ipAddress, string reason, string? replacedByToken = null);
    Task<ResponseType<RefreshTokenResponseDto>> RotateTokenAsync(string refreshToken, string ipAddress, string revocationReason);
    Task<bool> IsTokenValidAsync(string token);

}