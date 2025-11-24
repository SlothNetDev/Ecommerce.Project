namespace Ecommerce.Shared.AuthenticationDTO;

public record LoginRequestDto()
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
};