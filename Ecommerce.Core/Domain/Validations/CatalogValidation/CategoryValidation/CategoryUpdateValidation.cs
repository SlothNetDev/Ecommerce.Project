using Ecommerce.Shared.CatalogDto.Category;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Validations.CatalogValidation.CategoryValidation
{
    internal class CategoryUpdateValidation : AbstractValidator<CategoryUpdateRequestDto>
    {
        protected CategoryUpdateValidation()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Category ID is required.")
                .Must(id => id != Guid.Empty).WithMessage("Category ID must be a valid GUID.");

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Category description must not exceed 500 characters.");

        }
    }
}
