using Ecommerce.Core.Domain.Entities.Catalog;
using Ecommerce.Core.Domain.Entities.Orders;
using Ecommerce.Core.Domain.Entities.Reviews;
using Ecommerce.Infrastructure.Data.Configuration.Authentication;
using Ecommerce.Infrastructure.Data.Configuration.Catalog;
using Ecommerce.Infrastructure.Data.Configuration.Orders;
using Ecommerce.Infrastructure.Data.Configuration.Review;
using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Ecommerce.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
    {
        public virtual DbSet<ApplicationUser> ApplicationUsersDb { get; set; } = null!;
        public virtual DbSet<ApplicationToken> RefreshToken { get; set; }
        
        public virtual DbSet<Category> CategoriesDb { get; set; } = null!;
        public virtual DbSet<Product> ProductsDb { get; set; } = null!;
        public virtual DbSet<Order> OrderDb { get; set; } = null!;
        public virtual DbSet<OrderItem> OrderItemDb { get; set; } = null!;
        public virtual DbSet<Review> ReviewDb { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationTokenConfiguration).Assembly);

            builder.ApplyConfigurationsFromAssembly(typeof(CategoryConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(ProductConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(OrderItemConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(OrderConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(ReviewConfiguration).Assembly);
        }
    }
}
