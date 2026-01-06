using System.Security.Cryptography;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Identity.Services.JwtTokenService;

public class RefreshTokenService(ApplicationDbContext dbContext,
    ILogger<RefreshTokenService> logger,
    IIpAdressService ipAddressService,
    IOptions<JwtSettings> jwtSettings) :IRefreshTokenService
{
    public async Task<ResponseType<RefreshTokenResponseDto>> GenerateRefreshTokenAsync(
        string userId, 
        string ipAddress)
    {
        // 1. Validate and sanitize the IP address first
        var clientIp = ipAddressService.GetClientIpAddress();
        
        // 2. Check if IP is suspicious before generating token
        if (ipAddressService.IsSuspiciousIp(clientIp))
        {
            logger.LogWarning("Suspicious IP attempted to generate refresh token. UserId: {UserId}, IP: {IP}", 
                userId, clientIp);
            
            /*// OPTION A: Block completely (strict)
            return ResponseType<RefreshTokenResponseDto>.Fail("Token generation blocked due to security policy");*/
            // OPTION B: Allow but flag for review (recommended for e-commerce)
            // Continue with token generation but log for monitoring
        }
        var token = new ApplicationToken()
        {
            TokenId = Guid.NewGuid().ToString(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = Guid.Parse(userId),
            Created = DateTime.UtcNow,
            CreatedByIp = clientIp,
            Expires = DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes)
        };

        await dbContext.RefreshToken.AddAsync(token);
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Refresh token generated for user: {UserId}, IP: {IP}", 
            userId, clientIp);
        
        return await Task.FromResult(ResponseType<RefreshTokenResponseDto>.SuccessResult(MapToResponse(token),
            "Refresh token generated"));
    }

    public async Task<ResponseType<RefreshTokenResponseDto>> GetRefreshTokenAsync(string token)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);
        
        if (entity == null)
        {
            logger.LogWarning("Refresh token not found");
            return ResponseType<RefreshTokenResponseDto>.Fail("Token not found",
                FailureType.Authentication);
        }

        return ResponseType<RefreshTokenResponseDto>.SuccessResult(
            MapToResponse(entity),
            "Token retrieved");
    }

    public async Task<ResponseType<string>> RevokeRefreshTokenAsync(
        string token, 
        string ipAddress, 
        string? reason)
    {
        // 1. Get validated IP address
        var clientIp = ipAddressService.GetClientIpAddress();
        
        var storedToken = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);

        if (storedToken is null)
            return ResponseType<string>.Fail("Token not found",
                FailureType.Authentication);

        if (storedToken.Revoked.HasValue)
            return ResponseType<string>.SuccessResult("Already revoked", "Token already revoked");

        storedToken.Revoked = DateTime.UtcNow;
        storedToken.RevokedByIp = clientIp; 
        storedToken.RevocationReason = reason;

        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Token revoked: {TokenId}, Reason: {Reason}, IP: {IP}", 
            storedToken.TokenId, reason, clientIp);
            
        return ResponseType<string>.SuccessResult("Revoked", "Token revoked");
    }

    public async Task<ResponseType<RefreshTokenResponseDto>> RotateRefreshTokenAsync(
        string oldToken, 
        string ipAddress)
    {
        // 1. Get validated current IP address
        var clientIp = ipAddressService.GetClientIpAddress();
        
        // 2. Get and validate old token
        var oldTokenResult = await GetRefreshTokenAsync(oldToken);
        if (!oldTokenResult.Success)
            return ResponseType<RefreshTokenResponseDto>.Fail(oldTokenResult.Message,
                FailureType.Authentication);

        var oldTokenEntity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == oldToken);

        if (oldTokenEntity == null || !oldTokenEntity.IsActive)
            return ResponseType<RefreshTokenResponseDto>.Fail(
                "Invalid or expired token",
                FailureType.Authentication);

        // 3. SECURITY CHECK: Detect IP address changes (potential token theft)
        if (oldTokenEntity.CreatedByIp != clientIp)
        {
            logger.LogWarning(
                "IP address mismatch during token rotation. " +
                "UserId: {UserId}, Original IP: {OriginalIP}, Current IP: {CurrentIP}, " +
                "TokenId: {TokenId}",
                oldTokenEntity.UserId, 
                oldTokenEntity.CreatedByIp, 
                clientIp,
                oldTokenEntity.TokenId);

            // OPTION A: Block rotation (strict - banking apps)
            // return ResponseType<RefreshTokenResponseDto>.Fail("Security violation: IP address changed");
            
            // OPTION B: Allow but flag and notify user (recommended for e-commerce)
            // - Send email notification about login from new location
            // - Log for fraud detection team
            // - Continue with rotation
            
            // For Level 2 e-commerce: we'll allow but log heavily
        }

        // 4. Check if current IP is suspicious
        if (ipAddressService.IsSuspiciousIp(clientIp))
        {
            logger.LogWarning(
                "Suspicious IP attempting token rotation. " +
                "UserId: {UserId}, IP: {IP}, TokenId: {TokenId}",
                oldTokenEntity.UserId, 
                clientIp,
                oldTokenEntity.TokenId);
            
            // For e-commerce: allow but require re-authentication on next sensitive action
            // (like checkout or address change)
        }

        // 5. Revoke old token
        oldTokenEntity.Revoked = DateTime.UtcNow;
        oldTokenEntity.RevokedByIp = clientIp; // Use validated IP
        oldTokenEntity.RevocationReason = "Rotated";
        dbContext.RefreshToken.Update(oldTokenEntity);

        // 6. Generate new token with validated IP
        var newTokenResult = await GenerateRefreshTokenAsync(
            oldTokenEntity.UserId.ToString(), 
            clientIp); // Pass validated IP

        if (!newTokenResult.Success)
            return ResponseType<RefreshTokenResponseDto>.Fail("Failed to generate new token",
                FailureType.Authentication);

        // 7. Link old token to new one
        oldTokenEntity.ReplacedByToken = newTokenResult.Data.Token;

        // 8. Save changes
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Token rotated successfully. UserId: {UserId}, IP: {IP}", 
            oldTokenEntity.UserId, 
            clientIp);
        
        return newTokenResult;
    }

    public async Task<bool> IsRefreshTokenValidAsync(string token)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);

        return entity?.IsActive ?? false;
    }
    
    // Enhanced validation with IP checking
    public async Task<ResponseType<bool>> ValidateRefreshTokenWithIpCheckAsync(string token)
    {
        // 1. Get current IP
        var clientIp = ipAddressService.GetClientIpAddress();
        
        // 2. Get token from database
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);

        if (entity == null || !entity.IsActive)
        {
            return ResponseType<bool>.Fail("Invalid or expired token",
                FailureType.Authentication);
        }

        // 3. Check for IP address change
        if (entity.Revoked.HasValue  &&  entity.RevocationReason == "Rotated")
        {
            var secondsSinceRevocation = (DateTime.UtcNow - entity.Revoked.Value).TotalSeconds;
            if (secondsSinceRevocation < 30) 
            {
                logger.LogInformation("Grace period hit for Token: {TokenId}", entity.TokenId);
                return ResponseType<bool>.SuccessResult(true, "Grace period active");
            }
        }
        if (!entity.IsActive)
            return ResponseType<bool>.Fail("Token expired/revoked", FailureType.Authentication);
        
        // 4. Check if current IP is suspicious
        if (ipAddressService.IsSuspiciousIp(clientIp))
        {
            logger.LogWarning(
                "Token used from suspicious IP. UserId: {UserId}, IP: {IP}",
                entity.UserId, 
                clientIp);

            // Return success but with warning flag
            return ResponseType<bool>.SuccessResult(
                true, 
                "Token valid but from suspicious IP - enhanced verification may be required");
        }

        return ResponseType<bool>.SuccessResult(true, "Token valid");
    }
    #region Mapper 
    private RefreshTokenResponseDto MapToResponse(ApplicationToken token)
        => new(
            TokenId: token.TokenId,
            Token: token.Token,
            UserId: token.UserId,
            Created: token.Created,
            Expires: token.Expires,
            IsExpired: token.IsExpired,
            IsActive: token.IsActive,
            CreatedByIp: token.CreatedByIp,
            Revoked: token.Revoked,
            RevokedByIp: token.RevokedByIp,
            RevocationReason: token.RevocationReason,
            ReplacedByToken:  token.ReplacedByToken
        );
    #endregion
}