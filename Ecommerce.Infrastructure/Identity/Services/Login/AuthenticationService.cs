using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Identity.Services.Login;

public class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    ILogger<AuthenticationService>  logger,
    ApplicationDbContext dbContext,
    IRefreshTokenService refreshToken,
    IIpAdressService ipAdressService,
    IOptions<JwtSettings> jwtSettings) : IAuthenticationService
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
                FailureType.Authentication,
                "Login failed. Invalid email or password."
            );
        }
        
        //2. check if password correct
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogWarning("LOG_003: Invalid password attempt for user {UserId} (Email: {Email})",
                user.Id, request.Email);
            return ResponseType<AuthenticationResponseDto>.Fail(
                "InvalidCredentials",
                FailureType.Authentication,
                "Login failed. Invalid email or password."
            );
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("LOG_004: User {UserId} is locked out", user.Id);
            return ResponseType<AuthenticationResponseDto>.Fail(
                "AccountLocked",
                FailureType.Authentication, 
                "Account locked due to too many attempts. Please try again in 5 minutes."
            );
        }
        
        //3. check if email was confirmed and valid
        var isValidEmail =  await userManager.IsEmailConfirmedAsync(user);
        if (!isValidEmail)
        {
            logger.LogWarning("LOG_004: Email: {Email} not confirmed", request.Email);
            return ResponseType<AuthenticationResponseDto>.Fail("Email was not Verified yet",
                FailureType.Authentication);
        }
        
        //3. get the roles Assigned to the user
        var roles = await userManager.GetRolesAsync(user);
        
        //4. assigning role as default which is costumer
        var userRole = roles.FirstOrDefault() ?? RoleSeeder.Customer; //if no roles found, default role as Costumer
        
        //5. create  token for user
        var tokenUser = new TokenUserDto(
            user.Id.ToString(),
            user.UserName!,
            user.Email!,
            roles.ToList());
        
        //6. Generate JWT token claims and create token
        var claims = tokenService.BuildClaims(tokenUser); // create claims

        //7. Get client IP for refresh token
        var ipAdress = ipAdressService.GetClientIpAddress();
        
        //8. Generate refresh Token
        var refreshTokenResult = await refreshToken.GenerateRefreshTokenAsync(tokenUser.UserId,
            ipAdress);

        if (!refreshTokenResult.Success)
        {
            logger.LogWarning("LOG_004: Refresh token expired: {reason}", refreshTokenResult.Message);
            return ResponseType<AuthenticationResponseDto>.Fail(
                "Refresh Token Failed.}",
                FailureType.Authentication,
                refreshTokenResult.Message);
        }
        
        return ResponseType<AuthenticationResponseDto>.SuccessResult(new AuthenticationResponseDto()
            {
                UserName = user.UserName,
                Role = userRole,
                BearerToken = tokenService.CreateJwtToken(claims), // make it token
                RefreshToken = refreshTokenResult.Data!.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes)
            },
            "Login Successful");

    }

    public async Task<ResponseType<string>> LogoutAsync(string refreshTokenId)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenId))
        {
            logger.LogInformation("refresh Token cannot be empty");
            return ResponseType<string>.Fail("Refresh Token cannot be Empty",
                FailureType.Validation);
        }
        //check if refresh token exist
        var isValid =  await refreshToken.IsRefreshTokenValidAsync(refreshTokenId);
        if (!isValid)
        {
            return ResponseType<string>.Fail("Refresh Token is Invalid",
                FailureType.Validation);
        }
        //get ip adress
        var ipAdress = ipAdressService.GetClientIpAddress();
        
        //get the 
        //break the refresh token
        await refreshToken.RevokeRefreshTokenAsync(refreshTokenId, ipAdress, null);
        
        await dbContext.SaveChangesAsync();
        return ResponseType<string>.SuccessResult("Logout Successfully");
    }
}