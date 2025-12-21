using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Use RoleSeeder constants for consistency
        string[] roles = new[] { RoleSeeder.Admin, RoleSeeder.Customer, RoleSeeder.Seller };

        // 1. Ensure roles exist
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role
                });
            }
        }

        #region 2. Create admin user

        var admin = await userManager.FindByEmailAsync("admin@test.com");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin@test.com",
                Email = "admin@test.com"
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, RoleSeeder.Admin); // Use constant
        }

        #endregion

        #region 3. Create normal seller

        var seller = await userManager.FindByEmailAsync("seller123@test.com");
        if (seller == null)
        {
            seller = new ApplicationUser
            {
                UserName = "seller123@test.com",
                Email = "seller123@test.com"
            };

            await userManager.CreateAsync(seller, "User123!");
            await userManager.AddToRoleAsync(seller, RoleSeeder.Seller);
        }

        #endregion

        #region 4. Create normal customer

        var customer = await userManager.FindByEmailAsync("customer23@test.com");
        if (customer == null)
        {
            customer = new ApplicationUser
            {
                UserName = "customer23@test.com",
                Email = "customer23@test.com"
            };

            await userManager.CreateAsync(customer, "customer23123!");
            await userManager.AddToRoleAsync(customer, RoleSeeder.Customer);

            #endregion
        }
    }
}