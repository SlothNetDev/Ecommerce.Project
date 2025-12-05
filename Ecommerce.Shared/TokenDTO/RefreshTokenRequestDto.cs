namespace Ecommerce.Shared.TokenDTO;

public record RefreshTokenRequestDto(
    string BearerToken,
    string RefreshToken
);