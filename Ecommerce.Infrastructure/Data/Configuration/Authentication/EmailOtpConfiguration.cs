using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configuration.Authentication;

public class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
{
    public void Configure(EntityTypeBuilder<EmailOtp> builder)
    {
        builder.ToTable("Email_Otp");

        builder.HasKey(x => x.EmailOtpId);

        builder.Property(x => x.CodeHash)
            .HasMaxLength(256)
            .IsRequired();
        
        builder.Property(x => x.Attempts)
            .IsRequired()
            .HasDefaultValue(0);
        
        //relationship
        builder.HasOne(x => x.ApplicationUser)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        //Index
        builder.HasIndex(x => new {x.UserId, x.VerifiedAt});

        builder.HasIndex(x => x.ExpiresAt);
        
        builder.HasIndex(x => x.CreatedAt);
    }
}