using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth/action")]
public class AuthenticationController(
    ITokenRefreshService refreshTokenService,
    IAuthenticationService authenticationService,
    IUserRegistrationService userRegistrationService,
    ILogger<AuthenticationController> logger) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns an access token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>200 OK with access token on success, 401 Unauthorized on invalid credentials</returns>
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody]LoginRequestDto request)
    {
       var response = await authenticationService.LoginAsync(request);
       if (!response.Success)
       {
           logger.LogInformation("Processing login request for {Email}", request?.Email);
           
           if (!response.Success)
           {
               return BadRequest(new ProblemDetails
               {
                   Title = "Authentication Failed",
                   Detail = response.Message,
                   Status = StatusCodes.Status401Unauthorized
               });
           }
           logger.LogInformation("User {Email} logged in successfully", request.Email);
           
       }
       return Ok(response);

    }

    /// <summary>
    /// Registers a new user account with OTP email verification
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <returns>200 OK with verification status on success, 400 Bad Request on validation errors</returns>
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await userRegistrationService.RegisterAsync(request);

        if (!result.Success)
        {
            logger.LogInformation("Registration failed for {Email}: {Message}", request?.Email, result.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("User registered successfully: {Email}", request?.Email);
        return Ok(result); // 200 OK with registration status
    }

    /// <summary>
    /// Verifies user's email address using OTP code
    /// </summary>
    /// <param name="request">Email verification request with OTP code</param>
    /// <returns>200 OK on successful verification, 400 Bad Request on validation errors</returns>
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequestDto request)
    {
        var result = await userRegistrationService.VerifyEmailAsync(request);

        if (!result.Success)
        {
            logger.LogInformation("Email verification failed for {Email}: {Message}", request?.Email, result.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Email Verification Failed",
                Detail = result.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        logger.LogInformation("Email verified successfully for: {Email}", request?.Email);
        return Ok(result); // 200 OK with success message
    }

    [HttpPost("RefreshToken")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody]RefreshTokenRequestDto  request )
    {
        var response = await refreshTokenService.RefreshTokenAsync(request);

        if (!response.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Token Refresh Failed",
                Detail = response.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(response);
    }
}