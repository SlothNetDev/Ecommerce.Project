using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Shared.CatalogDto.Product
{
    public record ProductResponseDto
    {
         public Guid Id { get; init; }
         public string? Name { get; init; } = string.Empty;
         public string? Description { get;  init; }
         public decimal Price { get; init; }
         public int StockQuantity { get; init; }
         public string? ImageUrl { get; init; }
    }
}
