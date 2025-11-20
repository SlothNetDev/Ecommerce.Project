
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Shared.CatalogDto.Product
{
    public record ProductUpdateRequestDto
    {
         public Guid Id { get; init; }
         public string? Name { get; init; } = string.Empty;
         public string? Description { get; init; }
         public decimal Price { get; init; }
         public int StockQuantity { get; init; }
         public IFormFile? ImageUrl { get; init; }
    }
}
