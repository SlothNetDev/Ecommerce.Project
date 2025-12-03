using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configuration.Review
{
    internal class ReviewConfiguration : IEntityTypeConfiguration<Core.Domain.Entities.Reviews.Review>
    {
        public void Configure(EntityTypeBuilder<Core.Domain.Entities.Reviews.Review> builder)
        {
            builder.ToTable("Reviews");

            builder.HasKey(r => r.Id);

            builder.ToTable("Review",t => 
            t.HasCheckConstraint("CK_Reviews_Rating","[Rating] >= 1 AND [Rating] <= 5"));


            builder.Property(r => r.Comment)
                   .HasMaxLength(1000);
        }
    }
}
