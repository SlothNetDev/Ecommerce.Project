using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configuration.Authentication;

public class ApplicationTokenConfiguration :IEntityTypeConfiguration<ApplicationToken>
{
    
    public void Configure(EntityTypeBuilder<ApplicationToken> builder)
    {
        builder.ToTable("RefrehTokens");
        
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}