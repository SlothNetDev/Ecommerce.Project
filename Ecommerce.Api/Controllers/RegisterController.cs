using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Shared.RegisterDto;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;
[ApiController]
[Route("api/[controller]/[action]")]
public class RegisterController(
    IUserRegistrationService registrationService,
    ILogger<RegisterController> logger) : ControllerBase
{

    [HttpPost]
   public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
   {
       var response = await registrationService.RegisterAsync(request);
       if (!response.Success)
       {
           logger.LogInformation("Processing registration request for {Email}", request?.Email);
           
           if (!response.Success)
           {
               return BadRequest(new ProblemDetails
               {
                   Title = "Registration Failed",
                   Detail = response.Message,
                   Status = StatusCodes.Status400BadRequest
               });
           }
           logger.LogInformation("User {Email} registered successfully", request.Email);
           
       }
       return Ok(response);
    }
}