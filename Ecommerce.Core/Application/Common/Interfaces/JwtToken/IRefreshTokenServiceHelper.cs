using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.JwtToken;

/// <summary>
/// Provides methods for managing refresh tokens including generation, retrieval,
/// revocation, rotation, and validation. Intended for secure token lifecycle management
/// as part of authentication and session management.
/// </summary>
public interface IRefreshTokenServiceHelper
{
    /// <summary>
    /// Generates a new refresh token for the specified user and logs the originating IP address.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ipAddress">The client's IP address from which the request originated.</param>
    /// <returns>A response containing the refresh token details.</returns>
    Task<ResponseType<RefreshTokenResponseDto>> GenerateRefreshTokenAsync(string userId, string ipAddress);

    /// <summary>
    /// Retrieves an active refresh token by its string representation.
    /// </summary>
    /// <param name="token">The refresh token string.</param>
    /// <returns>A response with the refresh token details if found.</returns>
    Task<ResponseType<RefreshTokenResponseDto>> GetRefreshTokenAsync(string token);

    /// <summary>
    /// Revokes an existing refresh token and provides an audit reason.
    /// Typically required when a user initiates a logout or a suspected token compromise is detected.
    /// </summary>
    /// <param name="token">The refresh token string to revoke.</param>
    /// <param name="ipAddress">The IP address making the revocation request.</param>
    /// <param name="reason">The reason for token revocation for audit purposes.</param>
    /// <returns>A response containing a success message or error detail.</returns>
    Task<ResponseType<string>> RevokeRefreshTokenAsync(string token, string ipAddress, string reason);

    /// <summary>
    /// Rotates (exchanges) an existing valid refresh token for a new one,
    /// invalidating the old token. Used during token renewal operations.
    /// </summary>
    /// <param name="oldToken">The refresh token currently in use.</param>
    /// <param name="ipAddress">The IP address from which the rotation is requested.</param>
    /// <returns>A response containing the new refresh token details.</returns>
    Task<ResponseType<RefreshTokenResponseDto>> RotateRefreshTokenAsync(string oldToken, string ipAddress);

    /// <summary>
    /// Validates if the provided token is an active, not expired, and non-revoked refresh token.
    /// </summary>
    /// <param name="token">The refresh token string to validate.</param>
    /// <returns>True if the token is valid and usable; otherwise, false.</returns>
    Task<bool> IsRefreshTokenValidAsync(string token);

    /// <summary>
    /// Validate The refresh token with Ip 
    /// </summary>
    /// <param name="token"></param>
    /// <returns>True if refresh token was valid; Otherwise, return false</returns>
    Task<ResponseType<bool>> ValidateRefreshTokenWithIpCheckAsync(string token);
}