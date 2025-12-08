using Ecommerce.Shared.AuthenticationDTO;
using FluentValidation;
using Microsoft.AspNetCore.Identity.Data;

namespace Ecommerce.Core.Domain.Validations.AuthenticationValidation;

public class LoginRequestValidation : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidation()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Enter a valid email address");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}