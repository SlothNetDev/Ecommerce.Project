using Ecommerce.Shared.CatalogDto.Product;
using FluentValidation;

namespace Ecommerce.Core.Domain.Validations.CatalogValidation.ProductValidation
{
    internal class ProductRequestValidation : AbstractValidator<ProductRequestDto>
    {
        protected ProductRequestValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(150).WithMessage("Product name must not exceed 150 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Product description must not exceed 1000 characters.");

            // --- Rule for Price ---
            RuleFor(product => product.Price)
                .NotEmpty().WithMessage("Product price is required.")
                .GreaterThan(0).WithMessage("Price must be greater than zero.")
                .Must(price => decimal.Round(price, 2) == price)
                .WithMessage("Price can have a maximum of two decimal places.")
                .Must(price => price.ToString("F0").Length <= 10)
                .WithMessage("Price cannot exceed 10 total digits.");

            // --- Rule for StockQuantity ---
            RuleFor(product => product.StockQuantity)
                .NotEmpty().WithMessage("Stock quantity is required.")
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
                .LessThanOrEqualTo(1000).WithMessage("Stock quantity cannot exceed 1000 items.");

            // --- Rule for ImageUrl ---
            RuleFor(image => image.Image)
                .NotNull().WithMessage("Product image is required.")
                 .Must(file => file.Length > 0)
            .WithMessage("Image cannot be empty.")
            .Must(file => file.Length <= 2 * 1024 * 1024)
            .WithMessage("Image size cannot exceed 2MB.")
            .Must(file => 
                file.ContentType == "image/jpeg" || 
                file.ContentType == "image/png"
            )
            .WithMessage("Only JPG and PNG image formats are allowed.");


        }
    }
}
