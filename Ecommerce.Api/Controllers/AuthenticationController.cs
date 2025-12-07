using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
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