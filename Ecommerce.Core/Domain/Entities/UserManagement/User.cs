using Ecommerce.Core.Domain.Entities.Catalog;
using Ecommerce.Core.Domain.Entities.Orders;
using Ecommerce.Core.Domain.Entities.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Entities.UserManagement
{
    public class User
    {
         public Guid UserId { get; set; }
        
         public bool IsActive { get; set; } = true;

        //navigation properties
        // Navigation
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Seller-specific
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
