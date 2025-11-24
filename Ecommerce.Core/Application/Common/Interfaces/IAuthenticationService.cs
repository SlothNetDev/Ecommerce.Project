using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user with the provided registration details
    /// </summary>
    /// <param name="registerRequestDto">The registration details containing username/email, password, and other necessary information</param>
    /// <returns>AuthenticationResponse containing registration result and token if successful</returns>
    Task<ResponseType<string>> Register(RegisterRequestDto registerRequestDto);
        
    /// <summary>
    /// Authenticates a user based on their login credentials and return authentication response
    /// </summary>
    /// <param name="loginRequestDto">The login credentials containing username/email and password</param>
    /// <returns>AuthenticationResponse containing authentication result and token if successful</returns>
    Task<ResponseType<AuthenticationResponseDto>> Login(LoginRequestDto loginRequestDto);

}