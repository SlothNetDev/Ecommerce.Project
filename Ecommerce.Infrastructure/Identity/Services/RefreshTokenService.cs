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
    public async Task<ResponseType<RefreshTokenResponseDto>> GenerateRefreshToken(string userId, string ipAddress)
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
        
        logger.LogInformation("Refresh token generated for UserId: {UserId}", userId);

        return await Task.FromResult(
            ResponseType<RefreshTokenResponseDto>.SuccessResult(MapToResponse(token),
                "Refresh token generated for UserId"));
    }
    

    public async Task SaveRefreshTokenAsync(RefreshTokenResponseDto dto)
    {
        var entity = new ApplicationToken
        {
            TokenId = dto.TokenId,
            Token = dto.Token,
            UserId = dto.UserId,
            Created = dto.Created,
            Expires = dto.Expires,
            CreatedByIp = dto.CreatedByIp,
            Revoked = dto.Revoked,
            RevokedByIp = dto.RevokedByIp,
            RevocationReason = dto.RevocationReason
        };

        await dbContext.RefreshToken.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Refresh token saved. TokenId: {TokenId}", dto.TokenId);
    }

    public async Task<ResponseType<RefreshTokenResponseDto>> GetStoredTokenAsync(string refreshToken)
    {
        var entity = await dbContext.RefreshToken.
            FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (entity == null)
        {
            logger.LogWarning("Refresh token not found. RefreshToken: {refreshToken}", refreshToken);
            return ResponseType<RefreshTokenResponseDto>.Fail("Refresh token not found");
        }
        
        return ResponseType<RefreshTokenResponseDto>.SuccessResult(
            MapToResponse(entity),
            "Refresh token retrieved");
    }

    public async Task<string> RevokeTokenAsync(string refreshToken,
        string ipAddress,
        string reason, 
        string? replacedByToken = null)
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
    
    public async Task<ResponseType<RefreshTokenResponseDto>> RotateTokenAsync(
        string refreshToken,
        string ipAddress,
        string revocationReason)
    {
       var existing = await GetStoredTokenAsync(refreshToken);
       if (!existing.Success)
           return ResponseType<RefreshTokenResponseDto>.Fail(existing.Message ?? "Refresh token not found");
       
       await RevokeTokenAsync(refreshToken, ipAddress, revocationReason);
       
       var newToken = GenerateRefreshToken(existing.Data.UserId.ToString(),ipAddress );

       await SaveRefreshTokenAsync(new RefreshTokenResponseDto(TokenId: newToken.TokenId,
           Token: newToken.Token,
           UserId: newToken.UserId,
           Created: newToken.Created,
           Expires: newToken.Expires,
           IsExpired: newToken.IsExpired,
           IsActive: newToken.IsActive,
           CreatedByIp: newToken.CreatedByIp,
           Revoked: newToken.Revoked,
           RevokedByIp: newToken.RevokedByIp,
           RevocationReason: newToken.RevocationReason
       ));
       
       return ResponseType<RefreshTokenResponseDto>.SuccessResult(new RefreshTokenResponseDto(
            TokenId:  newToken.TokenId,
            Token:  newToken.Token,
            UserId:newToken.UserId,
            Created:newToken.Created,
            Expires:newToken.Expires,
            IsExpired:newToken.IsExpired,
            IsActive:newToken.IsActive,
            CreatedByIp:newToken.CreatedByIp,
            Revoked:newToken.Revoked,
            RevokedByIp:newToken.RevokedByIp,
            RevocationReason:newToken.RevocationReason
           ),"Refresh token revoked successfully");
    }
    
    public async Task<bool> IsTokenValidAsync(string token)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token);
        
        if (entity == null)
            return false;
        
        return entity.IsActive;
    }

    #region Mapper 
    private static RefreshTokenResponseDto MapToResponse(ApplicationToken token)
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
            RevocationReason: token.RevocationReason
        );
    #endregion
  
}