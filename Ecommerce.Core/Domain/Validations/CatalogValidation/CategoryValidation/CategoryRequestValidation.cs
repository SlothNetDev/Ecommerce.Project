using Ecommerce.Shared.CatalogDto.Category;
using FluentValidation;


namespace Ecommerce.Core.Domain.Validations.CatalogValidation.CategoryValidation
{
    internal class CategoryRequestValidation : AbstractValidator<CategoryRequestDto>
    {
        protected CategoryRequestValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Category description must not exceed 500 characters.");
        }

    }
}
