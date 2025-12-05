using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Test.TestUtilities;

/// <summary>
/// Generates Test JWT Tokens: Creates valid tokens for test users
///Includes Proper Claims: Adds user ID, email, and roles to tokens
///Uses Test Configuration: Respects your test JWT settings
/// </summary>
public class TestUserHelper
{
    public static string GenerateJwtForUser(IServiceProvider services, string email)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var jwtSettings = services.GetRequiredService<JwtSettings>();

        var user = userManager.FindByEmailAsync(email).Result;

        if (user == null)
            throw new Exception($"Test user not found: {email}");

        var roles = userManager.GetRolesAsync(user).Result;

        // Build token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            // Standard identifiers
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            //human friendly name
            new Claim("username", user.UserName),

            //unique id
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}