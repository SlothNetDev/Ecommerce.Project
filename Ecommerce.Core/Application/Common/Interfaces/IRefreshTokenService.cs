
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    RefreshTokenResponseDto GenerateRefreshToken(string userId, string ipAddress);
    Task SaveRefreshTokenAsync(ApplicationTokenDto token);
    Task<ResponseType<RefreshTokenResponseDto>> GetStoredTokenAsync(string refreshToken);
    Task<string> RevokeTokenAsync(string refreshToken,string ipAdress,string revocationReason, string? replacedByToken = null);
    Task<RefreshTokenResponseDto> RotateTokenAsync(ApplicationTokenDto oldToken, string ipAddress, string revokedReason);
    Task<bool> IsTokenValidAsync(ApplicationTokenDto token);

}