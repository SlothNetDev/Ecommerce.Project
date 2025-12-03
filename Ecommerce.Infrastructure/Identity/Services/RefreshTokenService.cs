using System.ComponentModel.Design;
using System.Security.Cryptography;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

public class RefreshTokenService(ApplicationDbContext dbContext,
    ILogger<RefreshTokenService> logger) :IRefreshTokenService
{
    public RefreshTokenResponseDto GenerateRefreshToken(string userId, string ipAddress)
    {
        var token =  new ApplicationToken()
        {
            TokenId = Guid.NewGuid().ToString(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress,
            UserId = Guid.Parse(userId),
        };
        
        logger.LogInformation("Generated refresh token successfully");
        return new RefreshTokenResponseDto(
            TokenId: token.TokenId,
            Token: token.Token,
            Expires: token.Expires,
            IsExpired: token.IsExpired,
            CreatedByIp: token.CreatedByIp,
            Created: token.Created,
            Revoked: token.Revoked,
            RevokedByIp: token.RevokedByIp,
            IsActive: token.IsActive,
            RevocationReason: token.RevocationReason);
    }

    public async Task SaveRefreshTokenAsync(ApplicationTokenDto token)
    {
        var entity = new ApplicationToken()
        {
            TokenId = token.TokenId,
            Token = token.Token,
            Expires = token.Expires,
            Created = DateTime.UtcNow,
            CreatedByIp = token.CreatedByIp,
            Revoked = token.Revoked,
            RevokedByIp = token.RevokedByIp,
            RevocationReason = token.RevocationReason
        };
        
        await dbContext.RefreshToken.AddAsync(entity);
        
        logger.LogInformation("Saved refresh token successfully");
        await dbContext.SaveChangesAsync();
    }

    public async Task<ResponseType<RefreshTokenResponseDto>> GetStoredTokenAsync(string refreshToken)
    {
        var entity = await dbContext.RefreshToken.
            FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (entity == null)
            return ResponseType<RefreshTokenResponseDto>.Fail("Refresh token not found");
        
        logger.LogInformation("Stored refresh token successfully");
        return ResponseType<RefreshTokenResponseDto>.SuccessResult(new RefreshTokenResponseDto(
            TokenId: entity.TokenId,
            Token: entity.Token,
            Expires:entity.Expires,
            Created:entity.Created,
            CreatedByIp:entity.CreatedByIp,
            Revoked:entity.Revoked,
            RevokedByIp:entity.RevokedByIp,
            IsActive:entity.IsActive,
            IsExpired:entity.Revoked == null && entity.Expires <= DateTime.UtcNow,
            RevocationReason:entity.RevocationReason),
            "Refresh token successfully retrieved");
    }

    public async Task<string> RevokeTokenAsync(string refreshToken, string reason, string? replacedByToken = null)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == refreshToken);
        
        if(entity == null)
            return "Refresh token not found";
        
        entity.Revoked = DateTime.UtcNow;
        entity.RevocationReason = reason; // store reason field

        await dbContext.SaveChangesAsync();
        return "Refresh token revoked Successfully";
    }

    public async Task<RefreshTokenResponseDto> RotateTokenAsync(ApplicationTokenDto oldToken, string ipAddress)
    {
        logger.LogInformation("-----Rotating token-------\n");
        //revoke old token
       await RevokeTokenAsync(oldToken.Token, ipAddress);
       
       //create new token
        var newToken = GenerateRefreshToken(oldToken.TokenId, ipAddress);
        
        await SaveRefreshTokenAsync(new ApplicationTokenDto()
        {
            TokenId = newToken.TokenId,
            Token = newToken.Token,
            Expires = newToken.Expires,
            Created = newToken.Created,
            CreatedByIp = newToken.CreatedByIp,
            Revoked = newToken.Revoked,
            RevokedByIp = newToken.RevokedByIp,
            RevocationReason = newToken.RevocationReason
        });
        
        return newToken;
    }
    
    public async Task<bool> IsTokenValidAsync(ApplicationTokenDto token) => 
        await Task.FromResult(token.Revoked == null &&
        token.Expires > DateTime.UtcNow &&
        token.IsActive);
}