using Ecommerce.Core.Domain.Entities.UserManagement;
using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configuration.Authentication
{
    internal class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("ApplicationUsers");

            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            // Configure relationship with Domain User (1:1)
            builder.HasOne(u => u.DomainUser)
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.DomainUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}