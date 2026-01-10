using Customer.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Data;

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdentityId).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasMaxLength(50);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .HasOne(a => a.Customer)
                .WithMany(c => c.Addresses)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
