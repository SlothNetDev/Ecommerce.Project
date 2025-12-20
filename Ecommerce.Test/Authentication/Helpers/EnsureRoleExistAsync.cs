using Ecommerce.Api;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Test.TestUtilities;
using Ecommerce.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Test.Authentication.Helpers;

public  class RoleExist : TestBase
{
    public RoleExist(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
        EnsureRolesExistAsync().Wait();
    }

    public async Task EnsureRolesExistAsync()
    {
        var roleManager = _factory.Services.GetRequiredService<RoleManager<ApplicationRole>>();
        var roles = new[] { "Admin", "Costumer", "Seller" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }
}