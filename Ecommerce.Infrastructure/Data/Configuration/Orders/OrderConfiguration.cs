using Ecommerce.Core.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Data.Configuration.Orders
{
    internal class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Order Table",
                t => t.HasCheckConstraint("CK_YourEntity_Status", 
                "[Status] IN ('Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled')"));

            builder.Property(x => x.Status)
                .HasMaxLength(20);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();
        }
    }
}
