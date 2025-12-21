using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.UI.Middleware;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.UI.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> ConfigureApplication(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard();
            
            //Seed Roles for request (skip in Testing environment - handled by test factory)
            using(var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

                await IdentitySeeder.SeedRolesAsync(roleManager);
            }
        }
        

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        

        //adding global middleware
        app.UseMiddleware<MiddlewareException>();

        app.UseAuthentication(); // Note: This should come BEFORE UseAuthorization
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
        return app;
    }
}