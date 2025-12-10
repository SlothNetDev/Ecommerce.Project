using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configuration.Authentication;

public class RoleHistoryConfiguration : IEntityTypeConfiguration<RoleChangeHistory>
{
    public void Configure(EntityTypeBuilder<RoleChangeHistory> builder)
    {
        builder.ToTable("Role_Change_History");
        builder.HasKey(x => x.ChangeHistoryId);
        
        //The user whose role was changed.
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
        builder.Property(x => x.Reason)
            .HasMaxLength(1000);
        
        builder.HasOne(x => x.OldRole)
            .WithMany()
            .HasForeignKey(x => x.OldRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NewRole)
            .WithMany()
            .HasForeignKey(x => x.NewRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChangeByUser)
            .WithMany()
            .HasForeignKey(k => k.ChangeBy)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);
    
        
        //Index
        builder.HasIndex(x => x.ChangeHistoryId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.NewRoleId);
        builder.HasIndex(x => x.OldRoleId);
        
    }
}