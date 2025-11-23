using Ecommerce.Core.Domain.Entities.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Entities.Orders
{
    public class Order
    {
         public Guid Id { get; set; } = Guid.NewGuid();
         public Guid UserId { get; set; }
         public string Status { get; set; } = "Pending"; // Pending, Confirmed, Shipped, Delivered, Cancelled
         public decimal TotalAmount { get; set; }
         public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
         
         // Navigation
         public virtual User User { get; set; } = null!;
         public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
