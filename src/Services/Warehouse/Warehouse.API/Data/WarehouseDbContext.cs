using Microsoft.EntityFrameworkCore;
using Warehouse.API.Entities;

namespace Warehouse.API.Data;

public class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Inventory> Inventory { get; set; }
    public DbSet<StockTransaction> StockTransaction { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.StoreId, e.ProductId }).IsUnique();

            entity.Property(e => e.Sku).HasMaxLength(50).IsRequired(false);

            entity.Property(e => e.QuantityOnHand).IsRequired();

            entity.Property(e => e.ReservedQuantity).HasDefaultValue(0);

            entity.Ignore(e => e.AvailableStock);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReferenceId).HasMaxLength(100);

            entity.Property(e => e.Type).HasConversion<string>();

            entity
                .HasOne(t => t.Inventory)
                .WithMany()
                .HasForeignKey(t => t.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
