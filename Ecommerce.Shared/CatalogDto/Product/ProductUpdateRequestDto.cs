
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Shared.CatalogDto.Product
{
    public class ProductUpdateRequestDto
    {
         public Guid Id { get; set; }
         public string? Name { get; set; } = string.Empty;
         public string? Description { get; set; }
         public decimal Price { get; set; }
         public int StockQuantity { get; set; }
         public IFormFile? ImageUrl { get; set; }
    }
}
