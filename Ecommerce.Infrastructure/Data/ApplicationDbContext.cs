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
using Microsoft.EntityFrameworkCore.Storage;


namespace Ecommerce.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
    {
        public virtual DbSet<ApplicationUser> ApplicationUsersDb { get; set; } = null!;
        public virtual DbSet<ApplicationRole> ApplicationRolesDb { get; set; } = null!;
        public virtual DbSet<ApplicationToken> RefreshToken { get; set; }
        
        public virtual DbSet<EmailOtp> EmailOtpDb { get; set; } = null!;
        
        public virtual DbSet<Category> CategoriesDb { get; set; } = null!;
        public virtual DbSet<Product> ProductsDb { get; set; } = null!;
        public virtual DbSet<Order> OrderDb { get; set; } = null!;
        public virtual DbSet<OrderItem> OrderItemDb { get; set; } = null!;
        public virtual DbSet<Review> ReviewDb { get; set; } = null!;
        public virtual DbSet<SellerApplication> SellerApplicationDb { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region Authentication
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationUserConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationRoleConfiguration).Assembly);
            
            builder.ApplyConfigurationsFromAssembly(typeof(EmailOtpConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationTokenConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(RoleHistoryConfiguration).Assembly);
            #endregion
           
            //Catalog
            builder.ApplyConfigurationsFromAssembly(typeof(CategoryConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(ProductConfiguration).Assembly);
            
            //Orders
            builder.ApplyConfigurationsFromAssembly(typeof(OrderItemConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(OrderConfiguration).Assembly);
            
            //Reviews
            builder.ApplyConfigurationsFromAssembly(typeof(ReviewConfiguration).Assembly);
            
        }
    }
}
