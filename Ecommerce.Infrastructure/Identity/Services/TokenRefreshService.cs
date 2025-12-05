using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

public class TokenRefreshService(
    IRefreshTokenService refreshTokenService,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<TokenRefreshService> logger) : ITokenRefreshService
{
    public async Task<ResponseType<TokenResponseDto>> RefreshTokenAsync(string accessToken, string refreshToken, string ipAddress)
    {
            //1. Validate refresh token exist or is active
            var isValid = await refreshTokenService.IsRefreshTokenValidAsync(refreshToken);
            if (!isValid)
            {
                logger.LogWarning("Refresh token {refreshToken} is invalid",refreshToken);
                return ResponseType<TokenResponseDto>.Fail("Invalid refresh token");
            }
                
            //2. Perform the enhanced Validation with IP address 
            var validationResult = await refreshTokenService.ValidateRefreshTokenWithIpCheckAsync(refreshToken);
            if (!validationResult.Success)
            {
                logger.LogWarning("Refresh token {refreshToken} is invalid", refreshToken);
                return ResponseType<TokenResponseDto>.Fail("Invalid refresh token");
            }
            
            //3. Check if IP changed address or Suspicious
            if (validationResult.Message.Contains("suspicious"))
            {
                // Optional: Send email notification(will implement soon)
                /*await _emailService.SendSecurityAlertAsync(
                    userId, 
                    "Your account was accessed from a new location");*/
                logger.LogWarning("Refresh token {refreshToken} is suspicious", refreshToken);
            }
            
            //2. Get the stored token with user data
            var storedToken = await dbContext.RefreshToken
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (storedToken is null)
            {
                logger.LogWarning("Refresh token {token} not found in User {user}",storedToken.Token,storedToken.User.Email);
                return ResponseType<TokenResponseDto>.Fail("Refresh token not found");
            }
            
            //3. rotate refresh token (one-time use)
            var rotationToken = await refreshTokenService.RotateRefreshTokenAsync(storedToken.Token,ipAddress);

            if (!rotationToken.Success)
            {
                logger.LogWarning("Refresh Token {token} Rotation Failed",  rotationToken.Data.Token);
                return ResponseType<TokenResponseDto>.Fail("Refresh Token Rotation Failed");
            }
            
            //4. Generate new JWT Token 
            var roles = await userManager.GetRolesAsync(storedToken.User);

            var tokenUser = new TokenUserDto(
                storedToken.UserId.ToString(),
                storedToken.User.UserName!,
                storedToken.User.Email!,
                roles.ToList());

            var claims = tokenService.BuildClaims(tokenUser);
            var newAccessToken = tokenService.CreateJwtToken(claims);
            
            //5. Return new Token pair
            var tokenResponse = new TokenResponseDto(
                newAccessToken,
                rotationToken.Data!.Token,
                DateTime.UtcNow.AddMinutes(15));
            
            logger.LogInformation("Token refreshed for user: {UserId}", storedToken.UserId);
            
            return ResponseType<TokenResponseDto>.SuccessResult(tokenResponse,
                "Token refreshed Successfully");
    }
}