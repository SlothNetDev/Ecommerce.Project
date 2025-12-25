using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Core.Application.Settings;

public class SecurityKeySettings
{
    [Required(ErrorMessage = "Security Key is required.")]
    public string SecurityKey { get; set; } = string.Empty;
}