using Ecommerce.Shared.SellerApplication;
using FluentValidation;

namespace Ecommerce.Core.Domain.Validations.SellerApplicationValidation;

public class SubmitApplicationRequestValidation : AbstractValidator<SubmitApplicationRequest>
{
    public SubmitApplicationRequestValidation()
    {
        RuleFor(x => x.BusinessName)
            .MinimumLength(3)
            .MaximumLength(100)
            .NotEmpty()
            .WithMessage("The business name is required.");

        RuleFor(x => x.ApplicationReason)
            .MinimumLength(10)
            .MaximumLength(1000)
            .WithMessage("The application reason Cannot exceed to 1000 characters.");
    }
}