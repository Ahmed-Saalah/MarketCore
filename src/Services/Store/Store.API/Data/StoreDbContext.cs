using Microsoft.EntityFrameworkCore;

namespace Store.API.Data;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Store> Stores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OwnerIdentityId).IsUnique();

            entity.Property(e => e.OwnerName).HasMaxLength(100);
            entity.Property(e => e.OwnerEmail).HasMaxLength(150);
            entity.Property(e => e.OwnerPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });
    }
}
