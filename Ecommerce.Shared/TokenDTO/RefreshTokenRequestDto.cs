namespace Ecommerce.Shared.TokenDTO;

/// <summary>
/// Create refresh token request
/// </summary>
public record RefreshTokenDto(
    string BearerToken,
    string RefreshToken
);