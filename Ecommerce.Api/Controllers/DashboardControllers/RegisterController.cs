using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Shared.RegisterDto;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers.DashboardControllers;
[ApiController]
[Route("api/dashboard")]
public class RegisterController(
    IUserRegistrationService registrationService,
    ILogger<RegisterController> logger) : ControllerBase
{

    [HttpPost("register")]
   public async Task<IActionResult> SignInAsync([FromBody] RegisterRequestDto request)
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
       }
       logger.LogInformation("User {Email} registered successfully", request.Email);
       return StatusCode(StatusCodes.Status201Created, response);
    }
}