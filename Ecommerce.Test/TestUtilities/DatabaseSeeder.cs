using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Test.TestUtilities;

/// <summary>
/// Creates Roles: Ensures "Admin" and "User" roles exist
/// Creates Test Users:
/// admin@test.com with "Admin" role
/// user@test.com with "User" role
/// Uses Real Identity: Works with ASP.NET Core Identity's UserManager and RoleManager
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = new[] { "Admin", "Costumer", "Seller" };
        
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
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        

        #endregion


        #region 3. Create normal seller
        var seller = await userManager.FindByEmailAsync("serller123@test.com");
        if (seller == null)
        {
            seller = new ApplicationUser
            {
                UserName = "serller123@test.com",
                Email = "serller123@test.com"
            };

            await userManager.CreateAsync(seller, "User123!");
            await userManager.AddToRoleAsync(seller, "Seller");
        }
        #endregion 
        
        #region 3. Create normal seller
        var costumer = await userManager.FindByEmailAsync("costumer23@test.com");
        if (costumer == null)
        {
            costumer = new ApplicationUser
            {
                UserName = "costumer23@test.com",
                Email = "costumer23@test.com"
            };

            await userManager.CreateAsync(costumer, "costumer23123!");
            await userManager.AddToRoleAsync(costumer, "Costumer");
        }
        #endregion 
        
    }
}