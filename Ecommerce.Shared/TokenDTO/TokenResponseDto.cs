namespace Ecommerce.Shared.TokenDTO;

public record TokenResponseDto(
    string AccessToken ,
    string RefreshToken,
    DateTime ExpiresAt );