using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Shared.TokenDTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Identity.Services.JwtTokenService;

public class GenerateTokenService(IOptions<JwtSettings> jwtSettings, 
    ILogger<GenerateTokenService> logger)
    : IGenerateTokenService
{
    
    public string CreateJwtToken(IEnumerable<Claim> claims)
    {
        logger.LogInformation("Creating JWT Token");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Value.Key));
       
        //Cryptographic algorithm that ensures token integrity
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        //create JWT token
        var jwt = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.Value.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        //Converts the JWT object into a compact string format
        // Result is a base64-encoded string that can be sent in HTTP headers
        logger.LogInformation("JWT Token created successfully");
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public IEnumerable<Claim> BuildClaims(TokenUserDto user)
    {
        var claims = new List<Claim>()
        {
            // Standard identifiers
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            
            //human friendly name
            new Claim("username", user.UserName),

            //unique id
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        //add roles
        if (user.Roles != null)
        {
            claims.AddRange(
                user.Roles.Select(role => new Claim("role", role)));
        }

        return claims;
    }
}