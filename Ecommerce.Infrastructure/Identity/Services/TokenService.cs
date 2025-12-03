using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Infrastructure.Identity.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings  _jwtSettings;
    private readonly ILogger<TokenService> _logger;
    public TokenService(IOptions<JwtSettings> jwtSettings,  ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }
    public string CreateJwtToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));
        
        //Cryptographic algorithm that ensures token integrity
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //create JWT token
        var jwt = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        //Converts the JWT object into a compact string format
        // Result is a base64-encoded string that can be sent in HTTP headers
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
            _logger.LogInformation("Creating roles Claims");
            claims.AddRange(
                user.Roles.Select(role => new Claim("role", role)));
        }

        return claims;
    }
}