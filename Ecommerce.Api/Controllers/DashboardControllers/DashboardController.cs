using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.TokenDTO;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationService = Ecommerce.Core.Application.Common.Interfaces.Login.IAuthenticationService;

namespace Ecommerce.Api.Controllers.DashboardControllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IAuthenticationService authenticationService,
    IUserRegistrationService registrationService,
    ITokenRefreshService refreshToken,
    ILogger<DashboardController> logger): ControllerBase
{
    #region Authentication
    /// <summary>
    /// Authenticates a user and generates an access token upon successful login.
    /// </summary>
    /// <param name="request">The login request containing the user's email and password.</param>
    /// <returns>A 200 OK response with the authentication data on success, or an appropriate error response on failure.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await authenticationService.LoginAsync(request);
        logger.LogInformation("User {Email} logged in successfully", request.Email);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user and generates an access token upon successful login.
    /// </summary>
    /// <param name="token">The login request containing the user's credentials, such as email and password.</param>
    /// <returns>A response indicating the result of the login operation, including authentication data on success or an error message on failure.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Login([FromQuery] string token)
    {
        var response = await authenticationService.LogoutAsync(token);
        logger.LogInformation("User logged out successfully");
        return Ok(response);
    }
    #endregion

    #region Refresh token
    /// <summary>
    /// Refreshes an expired access token using a valid refresh token
    /// </summary>
    /// <param name="requestDto">Current access and refresh tokens</param>
    /// <returns>200 OK with new token pair on success, 400 Bad Request if refresh token is invalid</returns>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken([FromBody]RefreshTokenRequestDto requestDto)
    {
        logger.LogInformation("Processing token refresh request");

        var result = await refreshToken.RefreshTokenAsync(requestDto);
        logger.LogInformation("Token {token} refreshed successfully",result?.Data.AccessToken);
        return Ok(result);
    }
    #endregion
    
    #region Register
    /// <summary>
    /// Registers a new user with the provided registration details.
    /// </summary>
    /// <param name="request">The registration request containing the user's first name, last name, email, password, and password confirmation.</param>
    /// <returns>A 200 OK response with the registration result on success, or an appropriate error response on failure.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request)
    {
        var response = await registrationService.RegisterAsync(request);
        logger.LogInformation("User {Email} registered successfully", request.Email);
        return Ok(response);
    }

    /// <summary>
    /// Verifies the email address of a user by processing the provided email verification request.
    /// </summary>
    /// <param name="email">The email verification request containing the user's email and verification code.</param>
    /// <returns>An OK response with verification confirmation data on success, or an appropriate error response on failure.</returns>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailAsync(EmailVerificationRequestDto email)
    {
        var response = await registrationService.VerifyEmailAsync(email);
        logger.LogInformation("User {Email} verified successfully", email.Email);
        return Ok(response);
    }

    /// <summary>
    /// Initiates the password reset process by sending a password reset link to the provided email address.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset.</param>
    /// <returns>An HTTP response indicating the success or failure of the password reset request.</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgetPasswordAsync([FromRoute] string email)
    {
        var response = await registrationService.ForgotPasswordAsync(email);
        logger.LogInformation("Password reset link sent to {Email}", email);
        return Ok(response);
    }
    #endregion
}