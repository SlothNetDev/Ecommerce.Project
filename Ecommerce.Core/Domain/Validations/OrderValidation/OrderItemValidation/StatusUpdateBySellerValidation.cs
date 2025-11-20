using Ecommerce.Shared.OrdersDto.OrderItemDto;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Validations.OrderValidation.OrderItemValidation
{
    internal class StatusUpdateBySellerValidation : AbstractValidator<StatusUpdateBySellerDto>
    {
        private readonly string[] Status = { "Confirmed", "Shipped", "Delivered", "Cancelled" };

        protected StatusUpdateBySellerValidation()
        {
            RuleFor(id => id.OrderItemId)
                .NotEmpty().WithMessage("Category ID is required.")
                .Must(id => id != Guid.Empty).WithMessage("Category ID must be a valid GUID.");

            RuleFor(status => status.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(status => Status.Contains(status))
                .WithMessage($"Status must be one of the following: {string.Join(", ", Status)}");
        }
    }
}
