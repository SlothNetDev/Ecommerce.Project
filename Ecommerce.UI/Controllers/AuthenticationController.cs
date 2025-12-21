using Ecommerce.Shared.AuthenticationDTO;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationService = Ecommerce.Core.Application.Common.Interfaces.Login.IAuthenticationService;

namespace Ecommerce.UI.Controllers;

public class AuthenticationController(
    IAuthenticationService authenticationService,
    ILogger<AuthenticationController> logger): ControllerBase
{
    
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