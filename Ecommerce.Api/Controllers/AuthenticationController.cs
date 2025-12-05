using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth/action")]
public class AuthenticationController(
    UserManager<ApplicationUser> userManager,
    ITokenRefreshService refreshTokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        //1. check if user exist or not by email
        var user = await userManager.FindByEmailAsync(request.Email);
        if(user == null)
            return BadRequest(ResponseType<string>.Fail("Invalid name of password"));
        
        
        //2. check password if it's correct
        var isValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
        {
            return BadRequest(ResponseType<string>.Fail("Invalid password"));
        }
        
        return Ok(ResponseType<AuthenticationResponseDto>.SuccessResult(new AuthenticationResponseDto(
            )
        {
            UserName = user.UserName,
            Role = "Customer",
            BearerToken = "BearerToken",
            RefreshToken = "RefreshToken",
            ExpiresAt = DateTime.Now.AddDays(1)
        },
            "Authentication Successful"));
    }
    
    [HttpPost("RefreshToken")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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