using Ecommerce.Shared.SellerApplication;
using FluentValidation;

namespace Ecommerce.Core.Domain.Validations.SellerApplicationValidation;

public class SubmitApplicationRequestValidation : AbstractValidator<SubmitApplicationRequest>
{
    public SubmitApplicationRequestValidation()
    {
        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("Seller ID is required.");
        
        RuleFor(x => x.BusinessName)
            .MinimumLength(3).WithMessage("Cannot be less than 3 characters")
            .MaximumLength(100).WithMessage("Cannot be more than 100 characters")
            .NotEmpty().WithMessage("The business name is required.");

        RuleFor(x => x.ApplicationReason)
            .MinimumLength(10).WithMessage("Cannot be less than 10 characters")
            .MaximumLength(1000).WithMessage("Cannot be more than 1000 characters");
    }
}