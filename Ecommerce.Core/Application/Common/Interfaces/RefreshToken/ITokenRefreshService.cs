using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.RefreshToken;

/// <summary>
/// Defines a contract for refreshing JWT tokens in production authentication workflows.
/// Ensures secure issuance of new access tokens using a valid refresh token and logs the client's IP address.
/// </summary>
public interface ITokenRefreshService
{
    /// <summary>
    /// Generates a new access token based on the provided refresh token, validating both tokens
    /// and recording the operation for audit and security purposes.
    /// </summary>
    /// <param name="accessToken">The existing (expired or expiring) access token.</param>
    /// <param name="refreshToken">A valid refresh token tied to the user session.</param>
    /// <param name="ipAddress">The IP address from which the refresh request originates.</param>
    /// <returns>
    /// Returns a response containing the new tokens upon success, or an error with details on failure.
    /// </returns>
    Task<ResponseType<TokenResponseDto>> RefreshTokenAsync(
        string accessToken, 
        string refreshToken, 
        string ipAddress);
}