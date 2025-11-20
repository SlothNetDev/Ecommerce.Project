using Ecommerce.Core.Domain.Entities.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Domain.Entities.Catalog
{
    public class Category
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        //For admin or seller categories
        public Guid? SellerId { get; set; } // Null if created by Admin
        public User? Seller { get; set; }


        // Self-referencing for subcategories
        public Guid? ParentCategoryId { get; set; } 
        public Category? ParentCategory { get; set; }

        public ICollection<Category> SubCategories { get; set; } = new List<Category>();


        // Products in this category
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
