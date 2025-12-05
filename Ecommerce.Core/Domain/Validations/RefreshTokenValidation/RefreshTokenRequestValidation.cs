using Ecommerce.Shared.TokenDTO;
using FluentValidation;

namespace Ecommerce.Core.Domain.Validations.RefreshTokenValidation;

public class RefreshTokenRequestValidation : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidation()
    {
        RuleFor(x => x.RefreshToken).NotNull().NotEmpty()
            .WithMessage("Bearer Token  is required");
        
        RuleFor(x => x.RefreshToken).NotNull().NotEmpty()
            .WithMessage("Refresh Token  is required");
    }
}