using Ecommerce.Core.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Data.Configuration.Orders
{
    internal class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
             // Add a check constraint to restrict the database values to ONLY valid enum strings
            builder.ToTable("Order_Items", 
            t => t.HasCheckConstraint("CK_YourEntity_Status", 
                "[Status] IN ('Pending', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled')"));

            builder.HasKey(oi => oi.Id);

      
            builder.Property(x => x.Status)
                .HasMaxLength(10);
        }
    }
}
