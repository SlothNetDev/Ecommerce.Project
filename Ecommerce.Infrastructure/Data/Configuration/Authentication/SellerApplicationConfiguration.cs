using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configuration.Authentication;

public class SellerApplicationConfiguration : IEntityTypeConfiguration<SellerApplication>
{
    public void Configure(EntityTypeBuilder<SellerApplication> builder)
    {
        builder.ToTable("Seller-Applications",
            t => t.HasCheckConstraint("CK_Application_Status", 
                "[Status] IN ('Pending', 'Approved', 'Rejected')"));

        builder.HasKey(x => x.SellerId);

        builder.Property(x => x.BusinessName)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(x => x.ApplicationReason)
            .HasMaxLength(1000)
            .IsRequired();
        
        builder.HasOne(x => x.ReviewByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ReviewComments)
            .HasMaxLength(1000);
        
        //index
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.ReviewBy);
        builder.HasIndex(x => x.SubmittedAt);
        builder.HasIndex(x => x.BusinessName);
        builder.HasIndex(x => x.ApplicationReason);
        builder.HasIndex(x => x.Status);
    }
}