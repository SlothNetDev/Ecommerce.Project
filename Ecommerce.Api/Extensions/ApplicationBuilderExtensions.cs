using Ecommerce.Api.Middleware;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Hangfire;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> ConfigureApplication(this WebApplication app)
    {
        // 2. Configure HTTP Pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });
        }
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard();
        }

        // Seed Roles for all environments except Testing (handled by test factory)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
                    await IdentitySeeder.SeedRolesAsync(roleManager);
                }
                catch (Exception ex)
                {
                    // Good practice to log seeding failures
                    var logger = services.GetRequiredService<ILogger<WebApplication>>();
                    logger.LogError(ex, "An error occurred while seeding roles.");
                }
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

        await app.RunAsync();
        return app;
    }
}