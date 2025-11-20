using Ecommerce.Core.Domain.Entities.Catalog;
using Ecommerce.Core.Domain.Entities.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Entities.Reviews
{
    public class Review
    {
         public Guid Id { get; set; }
         public Guid ProductId { get; set; }
         public Guid UserId { get; set; }
         public int Rating { get; set; } // 1-5
         public string Comment { get; set; } = string.Empty;
         public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
         
         // Navigation
         public virtual Product Product { get; set; } = null!;
         public virtual User User { get; set; } = null!;
    }
}
