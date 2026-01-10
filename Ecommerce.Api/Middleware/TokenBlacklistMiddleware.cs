using System.Security.Claims;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Security;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ecommerce.Api.Middleware;
/// <summary>
/// This delegates will help to instantly check the tokens before reaching controllers
/// </summary>
/// <param name="next"></param>
public class TokenBlacklistMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext, ITokenBlacklistService service)
    {
        var jti = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (!string.IsNullOrWhiteSpace(jti))
        {
            if (await service.IsTokenBlacklistedAsync(jti))
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Token blacklisted"
                });
                return;
            }
        }
        await next(httpContext);
    }
}