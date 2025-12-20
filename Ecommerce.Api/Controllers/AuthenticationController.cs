using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Identity.Services.Register;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(
    ITokenRefreshService refreshTokenService,
    IAuthenticationService authenticationService,
    IUserRegistrationService userRegistrationService,
    ILogger<AuthenticationController> logger,
    IOtpService otpService) : ControllerBase
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
        return Accepted(result); // 200 OK with registration status
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