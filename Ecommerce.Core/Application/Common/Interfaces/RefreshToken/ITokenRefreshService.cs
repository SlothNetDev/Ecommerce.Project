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
    /// <param name="requestDto">The existing (expired or expiring) access token.</param>
    /// <returns>
    /// Returns a response containing the new tokens upon success, or an error with details on failure.
    /// </returns>
    Task<ResponseType<TokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto requestDto);
}