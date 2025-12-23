using Ecommerce.Shared.AuthenticationDTO;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationService = Ecommerce.Core.Application.Common.Interfaces.Login.IAuthenticationService;

namespace Ecommerce.Api.Controllers.DashboardControllers;

[ApiController]
[Route("api/dashboard")]
public class AuthenticationController(
    IAuthenticationService authenticationService,
    ILogger<AuthenticationController> logger): ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody]LoginRequestDto request)
    {
        var response = await authenticationService.LoginAsync(request);
        logger.LogInformation("User {Email} logged in successfully", request.Email);
        return Ok(response);
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Login([FromQuery]string request)
    {
        var response = await authenticationService.LogoutAsync(request);
        logger.LogInformation("User logged out successfully");
        return Ok(response);
    }
    
}