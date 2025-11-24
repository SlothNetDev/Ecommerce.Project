namespace Ecommerce.Shared.TokenDTO;

public record TokenUserDto(
    string UserId,
    string UserName,
    string Email,
    List<string> Roles
);