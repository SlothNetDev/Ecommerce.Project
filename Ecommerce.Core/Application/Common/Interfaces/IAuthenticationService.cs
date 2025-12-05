using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

/// <summary>
/// Defines authentication operations required for user login and logout workflows in production environments.
/// Ensures proper handling of user credentials, authentication state, and audit logging of client activity.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates the user based on request credentials and records the originating IP address.
    /// </summary>
    /// <param name="request">The login request containing user credentials.</param>
    /// <param name="ipAddress">The client's IP address where the login attempt originated.</param>
    /// <returns>
    /// Returns an authentication response encapsulating tokens and user session details on successful login,
    /// or an error response on failure.
    /// </returns>
    Task<ResponseType<AuthenticationResponseDto>> LoginAsync(LoginRequestDto request, string ipAddress);

    /// <summary>
    /// Logs out the specified user and registers the client's IP address for audit and security tracking.
    /// </summary>
    /// <param name="userId">The unique identifier of the user requesting logout.</param>
    /// <param name="ipAddress">The client's IP address where the logout request originated.</param>
    /// <returns>
    /// Returns a response indicating the success or failure of the logout operation.
    /// </returns>
    Task<ResponseType<string>> LogoutAsync(string userId, string ipAddress);
}
