namespace Ecommerce.Shared.TokenDTO;

/// <summary>
/// Represents the response for Refresh token Response
/// </summary>
/// <param name="Id"></param>
/// <param name="Token"></param>
/// <param name="Expires"></param>
/// <param name="IsExpired"></param>
/// <param name="Created"></param>
/// <param name="CreatedByIp"></param>
/// <param name="Revoked"></param>
/// <param name="RevokedByIp"></param>
/// <param name="IsActive"></param>
public record RefreshTokenResponseDto(
    string TokenId,
    string Token, //this includes token
    DateTime Expires,
    bool? IsExpired,
    DateTime Created,
    string? CreatedByIp,
    DateTime? Revoked,
    string? RevokedByIp,
    bool IsActive,
    string? RevocationReason
);
