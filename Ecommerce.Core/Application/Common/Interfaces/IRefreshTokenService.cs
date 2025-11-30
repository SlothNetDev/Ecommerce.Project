using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generating Refresh Token for an Account
    /// </summary>
    /// <param name="token"></param>
    /// <param name="refreshToken"></param>
    /// <returns>AuthResponseDto </returns>
    Task<ResponseType<RefreshTokenResponseDto>> RefreshTokenAsync(string token, string refreshToken);
    /// <summary>
    /// Get all RefreshToken of Accounts
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>List of Account RefreshToken</returns>
    Task<ResponseType<List<RefreshTokenResponseDto>>> GetRefreshTokenAsync(Guid userId);

}