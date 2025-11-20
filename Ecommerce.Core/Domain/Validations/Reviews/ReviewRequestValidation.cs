using Ecommerce.Shared.Reviews;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Validations.Reviews
{
    internal class ReviewRequestValidation : AbstractValidator<ReviewRequestDto>
    {
        protected ReviewRequestValidation()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters.");
        }
    }
}
