using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Core.Application.Settings;

public class JwtSettings
{
    [Required(ErrorMessage = "Jwt Key is required.")]
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; }
    
    public int RefreshTokenExpiryDays { get; set; }
    
}