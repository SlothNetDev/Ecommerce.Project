using Ecommerce.Shared.OrdersDto.OrderDto;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Validations.OrderValidation.OrdersValidation
{
    internal class OrderUpdateValidation :AbstractValidator<OrderUpdateDto>
    {
        protected OrderUpdateValidation()
        {
            RuleFor(status => status.Status)
                .IsInEnum().WithMessage("Invalid order status.");

            // --- Rule for Price ---
            RuleFor(product => product.TotalAmount)
                .GreaterThan(0)
                .WithMessage("Total AmountTotal Amount must be greater than zero.")
                .LessThanOrEqualTo(1_000_000_000)
                .WithMessage("Total Amount cannot exceed 10 digits");


        }
    }
}
