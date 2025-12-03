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
    ILogger<RefreshTokenService>  logger) : IRefreshTokenService
{
    public RefreshTokenResponseDto GenerateRefreshToken(string userId, string ipAddress)
    {
        var token =  new ApplicationToken()
        {
            Id = Guid.NewGuid().ToString(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress,
            UserId = Guid.Parse(userId),
        };

        return new RefreshTokenResponseDto(
            Id: token.Id,
            Token: token.Token,
            Expires:  token.Expires,
            IsExpired: token.IsExpired,
            Created: token.Created,
            CreatedByIp: token.CreatedByIp,
            Revoked: token.Revoked,
            RevokedByIp: token.RevokedByIp,
            IsActive: token.IsActive,
            RevocationReason: token.RevocationReason);
    }

    public async Task SaveRefreshTokenAsync(ApplicationTokenDto dto)
    {
        var entity = new ApplicationToken
        {
            Id = dto.UserId,
            Token = dto.Token,
            Expires = dto.Expires,
            Created = dto.Created,
            CreatedByIp = dto.CreatedByIp,
            Revoked = dto.Revoked,
            RevokedByIp = dto.RevokedByIp,
            RevocationReason = dto.RevocationReason
        };
        await dbContext.AddAsync(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<ResponseType<RefreshTokenResponseDto>> GetStoredTokenAsync(string refreshToken)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (entity == null)
        {
            logger.LogWarning($"Refresh token not found: {refreshToken}");
            return ResponseType<RefreshTokenResponseDto>.Fail("Refresh token not found");
        }

        var dto = new RefreshTokenResponseDto(
            Id: entity.Id,
            Token: entity.Token,
            Expires: entity.Expires,
            Created: entity.Created,
            CreatedByIp: entity.CreatedByIp,
            Revoked: entity.Revoked,
            RevokedByIp: entity.RevokedByIp,
            IsActive: entity.IsActive,
            IsExpired: entity.Revoked == null && entity.Expires <= DateTime.UtcNow,
            RevocationReason:entity.RevocationReason
        );

        return ResponseType<RefreshTokenResponseDto>.SuccessResult(dto,"Successfully stored token");
    }


    public async Task<string> RevokeTokenAsync(string refreshToken, string ipAdress, string revocationReason, string? replacedByToken = null)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (entity == null)
        {
            logger.LogWarning($"Refresh token not found for refresh token: {refreshToken}");
            return "Refresh token not found";
        }
        
        entity.Revoked = DateTime.UtcNow;
        entity.RevokedByIp = ipAdress; // store reason field
        
        entity.RevocationReason = revocationReason; //reason
        
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation($"Refresh token revoked: {entity.Revoked}");
        return "Refresh token revoked Successfully";
    }
    
    public async Task<RefreshTokenResponseDto> RotateTokenAsync(ApplicationTokenDto oldToken, string ipAddress, string revokedReason)
    {
        //revoke old token
       logger.LogInformation($"Revoking old token: {oldToken.Token}\n" +
                             $"Reason: {ipAddress}"); 
       
       await RevokeTokenAsync(oldToken.Token, ipAddress,revokedReason);
       
       //create new token
       logger.LogInformation($"Replacing token with new token: {oldToken.Token}");
       var newToken = GenerateRefreshToken(oldToken.UserId, ipAddress);
        
        //save
        var getNewToken = new ApplicationTokenDto()
        {
            UserId = newToken.Id,
            Token = newToken.Token,
            Expires = newToken.Expires,
            Created = newToken.Created,
            CreatedByIp = newToken.CreatedByIp,
            Revoked = newToken.Revoked,
            RevokedByIp = newToken.RevokedByIp,
            RevocationReason = newToken.RevocationReason
        };
        await SaveRefreshTokenAsync(getNewToken);
        
        return newToken;
    }

    public Task<bool> IsTokenValidAsync(ApplicationTokenDto token) => 
        Task.FromResult(token.Revoked == null && token.Expires > DateTime.UtcNow &&
        token.IsActive);
}