using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth/action")]
public class AuthenticationController(
    UserManager<ApplicationUser> userManager,
    ITokenRefreshService refreshTokenService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenServiceDirect,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        //1. check if user exist or not by email
        var user = await userManager.FindByEmailAsync(request.Email);
        if(user == null)
            return BadRequest(ResponseType<string>.Fail("Invalid credentials"));

        //2. check password if it's correct
        var isValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
        {
            return BadRequest(ResponseType<string>.Fail("Invalid credentials"));
        }

        //3. Get user roles
        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Costumer";

        //4. Create token user data
        var tokenUser = new TokenUserDto(
            user.Id.ToString(),
            user.UserName!,
            user.Email!,
            roles.ToList());

        //5. Generate JWT token claims and create token
        var claims = tokenService.BuildClaims(tokenUser);
        var jwtToken = tokenService.CreateJwtToken(claims);

        //6. Get client IP for refresh token
        var clientIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        //7. Generate refresh token
        var refreshTokenResult = await refreshTokenServiceDirect.GenerateRefreshTokenAsync(user.Id.ToString(), clientIp);
        if (!refreshTokenResult.Success)
        {
            return BadRequest(ResponseType<string>.Fail("Failed to generate refresh token"));
        }

        //8. Calculate consistent expiration time
        var expirationTime = DateTime.UtcNow.AddMinutes(15);

        //9. Return successful authentication response
        return Ok(ResponseType<AuthenticationResponseDto>.SuccessResult(new AuthenticationResponseDto
        {
            UserName = user.UserName,
            Role = primaryRole,
            BearerToken = jwtToken,
            RefreshToken = refreshTokenResult.Data!.Token,
            ExpiresAt = expirationTime
        }, "Authentication Successful"));
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