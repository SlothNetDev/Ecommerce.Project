using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Data.Seeders;

public class IdentitySeeder
{
    /// <summary>
    /// Seeds predefined roles to the database if they don't exist.
    /// </summary>
    public static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = new[] { "Admin", "Costumer", "Seller" };

        foreach (var role in roles)
        {
            // Check if role exists
            if (!await roleManager.RoleExistsAsync(role))
            {
                // Create it if not
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }
}