using Ecommerce.Shared.OrdersDto;
using FluentValidation;

namespace Ecommerce.Core.Domain.Validations.OrderValidation.OrdersValidation
{
    public class OrderRequestValidation : AbstractValidator<OrderRequestDto>
    {
        protected OrderRequestValidation()
        {
           // --- Rule for Price ---
            RuleFor(product => product.TotalAmount)
                .NotEmpty().WithMessage("Total Amount Price is required.")
                .GreaterThan(0).WithMessage("Total AmountTotal Amount must be greater than zero.")
                .Must(price => decimal.Round(price, 2) == price)
                .WithMessage("Total Amount can have a maximum of two decimal places.")
                .Must(price => price.ToString("F0").Length <= 10)
                .WithMessage("Total Amount cannot exceed 10 total digits.");
        }
    }
}
