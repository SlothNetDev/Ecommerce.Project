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
   public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request)
   {
       var response = await registrationService.RegisterAsync(request);
       logger.LogInformation("User {Email} registered successfully", request.Email);
       return Ok(response);
   }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailAsync(EmailVerificationRequestDto email)
    {
        var response = await registrationService.VerifyEmailAsync(email);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgetPasswordAsync([FromRoute] string email)
    {
        var response = await registrationService.ForgotPasswordAsync(email);
        return Ok(response);
    }
}