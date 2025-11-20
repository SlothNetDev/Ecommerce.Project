using Ecommerce.Core.Domain.Entities.Orders;
using Ecommerce.Core.Domain.Entities.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Entities.Catalog
{
    public class Product
    {
          public Guid Id { get; set; }
          public string Name { get; set; } = string.Empty;
          public string? Description { get; set; }
          public decimal Price { get; set; }
          public int StockQuantity { get; set; }
          public string? ImageUrl { get; set; }
          public Guid CategoryId { get; set; }
          public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
          public bool IsActive { get; set; } = true;
          
          // Navigation
          public  Category Category { get; set; } = null!;
          public  ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
          public  ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
