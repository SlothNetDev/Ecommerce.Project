using Ecommerce.Shared.OrdersDto.OrderItemDto;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Validations.OrderValidation.OrderItem
{
    internal class OrderItemUpdateValidation : AbstractValidator<OrderItemUpdateDto>
    {
        protected OrderItemUpdateValidation()
        {
            // --- Rule for Id ---
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Order Item ID is required.")
                .Must(id => id != Guid.Empty).WithMessage("Order Item ID must be a valid GUID.");

            // --- Rule for Quantity ---
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
                .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100 items.");

            // --- Rule for UnitPrice ---
            When(x => x.UnitPrice.HasValue, () =>
            {
                RuleFor(unitPrice => unitPrice.UnitPrice.Value).GreaterThan(0);
            });
               
        }
    }
}
