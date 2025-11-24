namespace Ecommerce.Shared.TokenDTO;

/// <summary>
/// Create refresh token request
/// </summary>
public record RefreshTokenRequestDto(
    string BearerToken,
    string RefreshToken
);