using System.ComponentModel.Design;
using System.Security.Cryptography;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

public class RefreshTokenService(ApplicationDbContext dbContext,
    ILogger<RefreshTokenService> logger,
    IIpAdressService ipAddressService) :IRefreshTokenService
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
            CreatedByIp = ipAddress,
            Expires = DateTime.UtcNow.AddDays(7),
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
            return ResponseType<RefreshTokenResponseDto>.Fail("Token not found");
        }

        return ResponseType<RefreshTokenResponseDto>.SuccessResult(
            MapToResponse(entity),
            "Token retrieved");
    }

    public async Task<ResponseType<string>> RevokeRefreshTokenAsync(
        string token, 
        string ipAddress, 
        string reason)
    {
        // 1. Get validated IP address
        var clientIp = ipAddressService.GetClientIpAddress();
        
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);

        if (entity == null)
            return ResponseType<string>.Fail("Token not found");

        if (entity.Revoked.HasValue)
            return ResponseType<string>.SuccessResult("Already revoked", "Token already revoked");

        entity.Revoked = DateTime.UtcNow;
        entity.RevokedByIp = clientIp; 
        entity.RevocationReason = reason;

        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("Token revoked: {TokenId}, Reason: {Reason}, IP: {IP}", 
            entity.TokenId, reason, clientIp);
            
        return ResponseType<string>.SuccessResult("Revoked", "Token revoked");
    }

    public async Task<ResponseType<RefreshTokenResponseDto>> RotateRefreshTokenAsync(string oldToken, string ipAddress)
    {
        // 1. Get and validate old token
        var oldTokenResult = await GetRefreshTokenAsync(oldToken);
        if (!oldTokenResult.Success)
            return ResponseType<RefreshTokenResponseDto>.Fail(oldTokenResult.Message);

        var oldTokenEntity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == oldToken);

        if (oldTokenEntity == null || !oldTokenEntity.IsActive)
            return ResponseType<RefreshTokenResponseDto>.Fail("Invalid or expired token");

        // 2. Revoke old token
        oldTokenEntity.Revoked = DateTime.UtcNow;
        oldTokenEntity.RevokedByIp = ipAddress;
        oldTokenEntity.RevocationReason = "Rotated";
        dbContext.RefreshToken.Update(oldTokenEntity);

        // 3. Generate new token
        var newTokenResult = await GenerateRefreshTokenAsync(
            oldTokenEntity.UserId.ToString(), ipAddress);

        if (!newTokenResult.Success)
            return ResponseType<RefreshTokenResponseDto>.Fail("Failed to generate new token");

        // 4. Link old token to new one
        oldTokenEntity.ReplacedByToken = newTokenResult.Data.Token;

        // 5. Save new token to database
        var newTokenEntity = new ApplicationToken
        {
            TokenId = newTokenResult.Data.TokenId,
            Token = newTokenResult.Data.Token,
            UserId = oldTokenEntity.UserId,
            Created = newTokenResult.Data.Created,
            Expires = newTokenResult.Data.Expires,
            CreatedByIp = newTokenResult.Data.CreatedByIp
        };

        await dbContext.RefreshToken.AddAsync(newTokenEntity);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Token rotated for user: {UserId}", oldTokenEntity.UserId);
        
        return newTokenResult;
    }

    public async Task<bool> IsRefreshTokenValidAsync(string token)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);

        return entity?.IsActive ?? false;
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