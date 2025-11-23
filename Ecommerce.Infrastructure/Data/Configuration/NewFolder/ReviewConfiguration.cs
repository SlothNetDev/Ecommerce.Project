using Ecommerce.Core.Domain.Entities.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Data.Configuration.NewFolder
{
    internal class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
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
