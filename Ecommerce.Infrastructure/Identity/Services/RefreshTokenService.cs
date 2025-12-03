using System.Security.Cryptography;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Identity.Services;

public class RefreshTokenService(ApplicationDbContext dbContext)
{
    public ApplicationToken GenerateRefreshToken(string userId, string ipAddress)
    {
        return new ApplicationToken()
        {
            Id = Guid.NewGuid().ToString(),
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress,
            UserId = Guid.Parse(userId),
        };
    }

    public async Task SaveRefreshTokenAsync(ApplicationToken token) => await dbContext.RefreshToken.AddAsync(token);

    public async Task<ResponseType<RefreshTokenResponseDto>> GetStoredTokenAsync(string refreshToken)
    {
        var entity = await dbContext.RefreshToken.
            
            FirstOrDefaultAsync(x => x.Id == refreshToken);
        if (entity == null)
            return ResponseType<RefreshTokenResponseDto>.Fail("Refresh token not found");
        
        return ResponseType<RefreshTokenResponseDto>.SuccessResult(new RefreshTokenResponseDto(
            Id: entity.Id,
            Token: entity.Token,
            Expires:entity.Expires,
            Created:entity.Created,
            CreatedByIp:entity.CreatedByIp,
            Revoked:entity.Revoked,
            RevokedByIp:entity.RevokedByIp,
            IsActive:entity.IsActive,
            IsExpired:entity.Revoked == null && entity.Expires >  DateTime.UtcNow),
            "Refresh token successfully retrieved");
    }

    public async Task<string> RevokeTokenAsync(string refreshToken, string reason, string? replacedByToken = null)
    {
        var entity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(x => x.Id == refreshToken);
        
        if(entity == null)
            return "Refresh token not found";
        
        entity.Revoked = DateTime.UtcNow;
        entity.RevokedByIp = reason; // store reason field

        await dbContext.SaveChangesAsync();
        return "Refresh token revoked Successfully";
    }

    public async Task<ApplicationToken> RotateTokenAsync(ApplicationToken oldToken, string ipAddress)
    {
        //revoke old token
       await RevokeTokenAsync(oldToken.Id, ipAddress);
       
       //create new token
        var newToken = GenerateRefreshToken(oldToken.Id, ipAddress);
        
        //save
        await SaveRefreshTokenAsync(newToken);
        
        return newToken;
    }

    public async Task<bool> IsTokenValidAsync(ApplicationToken token)
    {
        var validToken = token.Revoked == null &&
                         token.Expires > DateTime.UtcNow &&
                         token.IsActive;

        return await Task.FromResult(validToken);
    }
}