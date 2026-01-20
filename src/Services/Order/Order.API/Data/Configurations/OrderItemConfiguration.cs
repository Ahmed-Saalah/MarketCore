using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.API.Entities;

namespace Order.API.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductName).IsRequired().HasMaxLength(200);

        builder.Property(oi => oi.Sku).IsRequired().HasMaxLength(100);

        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);

        builder.Ignore(oi => oi.TotalPrice);
    }
}
