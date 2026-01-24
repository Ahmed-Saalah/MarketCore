using Cart.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cart.API.Data;

public class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entities.Cart>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.UserId);

            entity
                .HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);

            entity.Property(e => e.PictureUrl).IsRequired(false).HasMaxLength(500);

            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });
    }
}
