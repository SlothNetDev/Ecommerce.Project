using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.DevelopmentService.Notification;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.TokenDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationService = Ecommerce.Core.Application.Common.Interfaces.Login.IAuthenticationService;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthenticationService authenticationService,
    IUserRegistrationService registrationService,
    IRefreshRotateTokenService refreshRotateToken,
    ILogger<AuthController> logger,
    DevEmailStore fakeEmailService): ControllerBase
{
    #region Authentication
    /// <summary>
    /// Authenticates a user and generates an access token upon successful login.
    /// </summary>
    /// <param name="request">The login request containing the user's email and password.</param>
    /// <returns>A 200 OK response with the authentication data on success, or an appropriate error response on failure.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request)
    {
        var response = await authenticationService.LoginAsync(request);
        logger.LogInformation(
            response.Success? "User {Email} logged in successfully":
            "User {Email} failed to send in {Email}", request.Email);
        return Ok(response);
    }

    /// <summary>
    /// Log out your Account by providing the valid refresh Token
    /// </summary>
    /// <param name="token"></param>
    /// <param name="reason"></param>
    /// <returns>Return a message indicating you successfully log-out</returns>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> LogOutAsync([FromQuery] string token, string? reason)
    {
        var response = await authenticationService.LogoutAsync(token, reason);
        logger.LogInformation(
            response.Success?"User Token {token} logged out successfully":
                "User failed to logout");
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
    [Authorize]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken([FromBody]RefreshTokenRequestDto requestDto)
    {
        var response = await refreshRotateToken.RefreshTokenAsync(requestDto);
        logger.LogInformation(
            response.Success? "Token {token} refreshed successfully" :
            "Token {Token} Failed to refresh",response?.Data.AccessToken);
        return Ok(response);
    }
    #endregion
    
    #region Register
    /// <summary>
    /// Registers a new user with the provided registration details.
    /// </summary>
    /// <param name="request">The registration request containing the user's first name, last name, email, password, and password confirmation.</param>
    /// <returns>A 200 OK response with the registration result on success, or an appropriate error response on failure.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request)
    {
        var response = await registrationService.RegisterAsync(request);
        logger.LogInformation(
            response.Success ? "User {Email} registered successfully" : "User {Email} registration failed", 
            request.Email);
        return Ok(response);
    }

    /// <summary>
    /// Verifies the email address of a user by processing the provided email verification request.
    /// </summary>
    /// <param name="email">The email verification request containing the user's email and verification code.</param>
    /// <returns>An OK response with verification confirmation data on success, or an appropriate error response on failure.</returns>
    [HttpPost("verify-email")]
    [Authorize]
    public async Task<IActionResult> VerifyEmailAsync(EmailVerificationRequestDto email)
    {
        var response = await registrationService.VerifyEmailAsync(email);
        logger.LogInformation(
            response.Success? "User {Email} verified successfully {email.Email}" :
                "User {Email} verification failed", email.Email);
        return Ok(response);
    }

    
    /// <summary>
    /// Initiates the password reset process by sending a password reset link to the provided email address.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset.</param>
    /// <returns>An HTTP response indicating the success or failure of the password reset request.</returns>
    [HttpPost("forgot-password")]
    [Authorize]
    public async Task<IActionResult> ForgetPasswordAsync([FromRoute] string email)
    {
        var response = await registrationService.ForgotPasswordAsync(email);
        logger.LogInformation(
            response.Success? "Password reset link sent to {Email} Successfully" :
        "Password reset was failed",email);
        return Ok(response);
    }

    /// <summary>
    /// Taking the specific JTI and put in cache and revoked that jti
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="token"></param>
    /// <param name="reason"></param>
    /// <returns>An Http indicating the success or failure of revoking all session of user token</returns>
    [Authorize(Roles = "Admin")]
    [HttpPost("revoke-session-Token")]
    public async Task<IActionResult> RevokeAllOtherSessionsAsync([FromQuery] string userId, string  token, string reason)
    {
        var response = await authenticationService.RevokeAllOtherSessionsAsync(userId, token, reason);
        logger.LogInformation(
            response.Success ? $"All session token of user {userId} revoked successfully":
                $"Cannot revoke all other sessions of user {userId}"
        );
        return Ok(response);
    }
    #endregion
    
    #if DEBUG
    [HttpGet("GetEmails")]
    public IActionResult Get()
        => Ok(fakeEmailService.GetAllEmails());
    #endif
}