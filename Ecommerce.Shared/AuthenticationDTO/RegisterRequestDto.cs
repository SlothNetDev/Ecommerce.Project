using System.Text.Json.Serialization;
using Ecommerce.Shared.Entities;

namespace Ecommerce.Shared.AuthenticationDTO;

public record RegisterRequestDto()
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string UserName { get; init; } =  string.Empty;
};