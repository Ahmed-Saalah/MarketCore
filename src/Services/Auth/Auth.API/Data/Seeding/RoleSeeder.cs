using Auth.API.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Data.Seeding;

public static class RoleSeeder
{
    public static void SeedRolesAndAdminAsync(this ModelBuilder builder)
    {
        builder
            .Entity<IdentityRole<int>>()
            .HasData(
                new IdentityRole<int>
                {
                    Id = 1,
                    Name = Roles.Admin,
                    NormalizedName = Roles.Admin.ToUpper(),
                },
                new IdentityRole<int>
                {
                    Id = 2,
                    Name = Roles.Customer,
                    NormalizedName = Roles.Customer.ToUpper(),
                },
                new IdentityRole<int>
                {
                    Id = 3,
                    Name = Roles.Seller,
                    NormalizedName = Roles.Seller.ToUpper(),
                }
            );
    }
}
