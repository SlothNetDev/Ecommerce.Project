using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Identity.Services.Login;

public class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentitySettings> identity,
    ITokenService tokenService,
    ILogger<AuthenticationService>  logger,
    ApplicationDbContext dbContext,
    IRefreshTokenService refreshToken,
    IIpAdressService ipAdressService) : IAuthenticationService
{
    public async Task<ResponseType<AuthenticationResponseDto>> LoginAsync(LoginRequestDto request)
    {
        //1.Check if email is exist
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogError("LOG_002: Login attempt for non-existent email: {Email}", request.Email);
            return ResponseType<AuthenticationResponseDto>.Fail(
                "InvalidCredentials",
                "Login failed. Invalid email or password."
            );
        }
        
        //2. check if password correct
        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            logger.LogWarning("LOG_003: Invalid password attempt for user {UserId} (Email: {Email})",
                user.Id, request.Email);
            return ResponseType<AuthenticationResponseDto>.Fail(
                "InvalidCredentials",
                "Login failed. Invalid email or password."
            );
        }
        
        //3. get the roles Assigned to the user
        var roles = await userManager.GetRolesAsync(user);
        
        //4. assigning role as default which is costumer
        var userRole = roles.FirstOrDefault() ?? "Costumer"; //if no roles found, default role as Costumer
        
        //5. create  token for user
        var tokenUser = new TokenUserDto(
            user.Id.ToString(),
            user.UserName!,
            user.Email!,
            roles.ToList());
        
        //6. Generate JWT token claims and create token
        var claims = tokenService.BuildClaims(tokenUser); // create claims
        var jwtToken = tokenService.CreateJwtToken(claims); // make it token

        //7. Get client IP for refresh token
        var ipAdress = ipAdressService.GetClientIpAddress();
        
        //8. Generate refresh Token
        var refreshTokenResult = await refreshToken.GenerateRefreshTokenAsync(tokenUser.UserId,
            ipAdress);

        if (!refreshTokenResult.Success)
        {
            logger.LogWarning("LOG_004: Refresh token expired: {reason}", refreshTokenResult.Message);
            return ResponseType<AuthenticationResponseDto>.Fail("Refresh Token Failed.}");
        }
        
        //8. Calculate  expiration date of token
        var expirationTime = DateTime.UtcNow.AddMinutes(15);
        
        return ResponseType<AuthenticationResponseDto>.SuccessResult(new AuthenticationResponseDto()
            {
                UserName = user.UserName,
                Role = userRole,
                BearerToken = jwtToken,
                RefreshToken = refreshTokenResult.Data!.Token,
                ExpiresAt = expirationTime
            },
            "Login Sucessfully");

    }

    public async Task<ResponseType<string>> LogoutAsync(string userId)
    {
        throw new NotImplementedException();
    }
}