using Ecommerce.Shared.OrdersDto.OrderItemDto;
using FluentValidation;


namespace Ecommerce.Core.Domain.Validations.OrderValidation.OrderItem
{
    internal class OrderItemRequestValidation : AbstractValidator<OrderItemRequestDto>
    {
        protected OrderItemRequestValidation()
        {
            // --- Rule for Quantity ---
            RuleFor(item => item.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.")
                .LessThanOrEqualTo(100)
                .WithMessage("Quantity cannot exceed 100 items.");

            // --- Rule for UnitPrice ---
            RuleFor(item => item.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Unit Price must be greater than zero.")
                .Must(price => decimal.Round(price, 2) == price)
                .WithMessage("Unit Price can have a maximum of two decimal places.")
                .Must(price => price.ToString("F0").Length <= 10)
                .WithMessage("Unit Price cannot exceed 10 total digits.");
        }
    }
}
