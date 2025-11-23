using Ecommerce.Core.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Data.Configuration.Catalog
{
    internal class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(p => p.Description)
                     .HasMaxLength(1000);

            builder.Property(p => p.Price)
                     .IsRequired()
                     .HasColumnType("decimal(18,2)");

            builder.Property(p => p.StockQuantity)
                        .IsRequired();
                        
            builder.Property(p => p.ImageUrl)
                .HasMaxLength(1_048_576)
                .IsRequired();


            //relationships
            builder.HasOne(p => p.Category) 
                   .WithMany(c => c.Products) // category has many products
                   .HasForeignKey(p => p.CategoryId) // foreign key in products table
                   .OnDelete(DeleteBehavior.Cascade); //when category is deleted, delete products too
        }
    }
}
