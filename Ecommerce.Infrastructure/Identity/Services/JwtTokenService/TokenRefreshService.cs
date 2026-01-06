using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Identity.Services.JwtTokenService;

public class TokenRefreshService(
    IRefreshTokenService refreshTokenService,
    IIpAdressService ipAdressService,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<TokenRefreshService> logger,
    IOptions<JwtSettings> jwtSettings) : ITokenRefreshService
{
    public async Task<ResponseType<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        // 1. Get and validate current IP address 
        var currentIp = ipAdressService.GetClientIpAddress();
        if (currentIp.Equals("Unknown"))
        {
            logger.LogWarning("Refresh token attempt with unknown IP. Token: {Token}", 
                request.RefreshToken);
            return ResponseType<TokenResponseDto>.Fail(
                "Unable to verify request origin",
                FailureType.Validation);
        }
        
        //2. Validate refresh token exist or is active
        var isValid = await refreshTokenService.IsRefreshTokenValidAsync(request.RefreshToken);
        if (!isValid)
        {
            logger.LogWarning("Invalid refresh token used: {Token}, IP: {IP}", 
                request.RefreshToken, currentIp);
            return ResponseType<TokenResponseDto>.Fail(
                "Invalid refresh token",
                FailureType.Validation);
        }
            
        //3. Perform the enhanced Validation with IP address 
        var validationResult = await refreshTokenService
            .ValidateRefreshTokenWithIpCheckAsync(request.RefreshToken);
        
        if (!validationResult.Success)
        {
            logger.LogWarning("Refresh token validation failed: {Token}, IP: {IP}, Reason: {Reason}", 
                request.RefreshToken, currentIp, validationResult.Message);
            return ResponseType<TokenResponseDto>.Fail("Invalid refresh token",
                FailureType.Validation, validationResult.Message);
        }
        
        
        //4. Get the stored token with user data
        var storedToken = await dbContext.RefreshToken
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (storedToken is null)
        {
            logger.LogError("Refresh token not found in database: {Token}", request.RefreshToken);
            return ResponseType<TokenResponseDto>.Fail("Refresh token not found",
                FailureType.Authentication);
        }
        
        // 5. Security check: IP address changed (potential token theft)
        if (storedToken.CreatedByIp != currentIp)
        {
            logger.LogWarning(
                "IP address changed during token refresh. " +
                "UserId: {UserId}, Email: {Email}, " +
                "Original IP: {OriginalIP}, Current IP: {CurrentIP}",
                storedToken.UserId, 
                storedToken.User.Email,
                storedToken.CreatedByIp, 
                currentIp);

            // TODO: Send email notification when email service is ready
            // await _emailService.SendSecurityAlertAsync(
            //     storedToken.User.Email,
            //     $"New login detected from {await ipAddressService.GetLocationAsync(currentIp)}");
        }
        
        //6. Check if IP changed address or Suspicious
        if (validationResult.Message.Contains("suspicious"))
        {
            // Optional: Send email notification(will implement soon)
            /*await _emailService.SendSecurityAlertAsync(
                userId,
                "Your account was accessed from a new location");*/
            logger.LogWarning("Refresh token {refreshToken} is suspicious", request.RefreshToken);
            // TODO: Flag user account for enhanced security on checkout
            // await FlagAccountForReview(storedToken.UserId);
        }
        
        
        //7. rotate refresh token (one-time use)
        var rotationToken = await refreshTokenService
            .RotateRefreshTokenAsync(storedToken.Token
                ,currentIp);

        if (!rotationToken.Success)
        {
            logger.LogError(
                "Token rotation failed for user: {UserId}, Email: {Email}, IP: {IP}",
                storedToken.UserId, 
                storedToken.User.Email, 
                currentIp);
            
            return ResponseType<TokenResponseDto>.Fail(
                "Refresh Token Rotation Failed",
                FailureType.Authentication, 
                rotationToken.Message);
        }
        if (storedToken.CreatedByIp != currentIp)
        {
            logger.LogWarning(
                "IP address changed during token refresh. " +
                "UserId: {UserId}, Email: {Email}, " +
                "Original IP: {OriginalIP}, Current IP: {CurrentIP}",
                storedToken.UserId, 
                storedToken.User.Email,
                storedToken.CreatedByIp, 
                currentIp);

            // TODO: Send email notification when email service is ready
            // await _emailService.SendSecurityAlertAsync(
            //     storedToken.User.Email,
            //     $"New login detected from {await ipAddressService.GetLocationAsync(currentIp)}");
        }
        
        //8. Generate new JWT Token 
        var roles = await userManager.GetRolesAsync(storedToken.User);

        var tokenUser = new TokenUserDto(
            storedToken.UserId.ToString(),
            storedToken.User.UserName!,
            storedToken.User.Email!,
            roles.ToList());

        var claims = tokenService.BuildClaims(tokenUser);
        var newAccessToken = tokenService.CreateJwtToken(claims);
        
        //9. Return new Token pair
        var tokenResponse = new TokenResponseDto(
            newAccessToken,
            rotationToken.Data!.Token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(jwtSettings.Value.RefreshTokenExpiryDays)
        );
        
        logger.LogInformation("Token refreshed for user: {UserId}", storedToken.UserId);
        
        return ResponseType<TokenResponseDto>.SuccessResult(tokenResponse,
            "Token refreshed Successfully"); 
            
    }
}