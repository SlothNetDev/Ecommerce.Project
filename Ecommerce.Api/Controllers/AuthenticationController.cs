using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(
    UserManager<ApplicationUser> userManager) : ControllerBase
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
    
    
}