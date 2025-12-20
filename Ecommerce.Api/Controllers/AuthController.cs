using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Shared.AuthenticationDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

public class AuthController(
    IAuthenticationService authenticationService,
    ILogger<AuthController> logger): Controller
{

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        return View();
    }
    
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
}