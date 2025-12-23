using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Shared.TokenDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers.DashboardControllers;

[Authorize]
[ApiController]
[Route("api/dashboard")]
public class RefreshTokenController(
   ITokenRefreshService refreshToken,
   ILogger<RefreshTokenController> logger) : ControllerBase
{
   /// <summary>
   /// Refreshes an expired access token using a valid refresh token
   /// </summary>
   /// <param name="requestDto">Current access and refresh tokens</param>
   /// <returns>200 OK with new token pair on success, 400 Bad Request if refresh token is invalid</returns>
   [HttpPost("refresh")]
   public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken([FromBody]RefreshTokenRequestDto requestDto)
   {
      logger.LogInformation("Processing token refresh request");

      var result = await refreshToken.RefreshTokenAsync(requestDto);
      logger.LogInformation("Token {token} refreshed successfully",result?.Data.AccessToken);
      return Ok(result);
   }
}