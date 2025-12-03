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
    string Token,
    Guid UserId,
    DateTime Created,
    DateTime Expires,
    bool IsExpired,
    bool IsActive,
    string CreatedByIp,
    DateTime? Revoked,
    string? RevokedByIp,
    string? RevocationReason
);
