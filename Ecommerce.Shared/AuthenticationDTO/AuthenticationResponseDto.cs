using System.Text.Json.Serialization;

namespace Ecommerce.Shared.AuthenticationDTO;

public record AuthenticationResponseDto()
{
    [property: JsonPropertyName("bearerToken")]
    public string BearerToken { get; init; } = string.Empty;
    
    [property: JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; init; }
    
    [property: JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
    
    [property: JsonPropertyName("userName")]
    public string UserName { get; init; } = string.Empty;
    
    [property: JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;
}