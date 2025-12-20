using Ecommerce.UI.Middleware;
using Hangfire;

namespace Ecommerce.UI.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> ConfigureApplication(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseHangfireDashboard();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        //adding global middleware
        app.UseMiddleware<MiddlewareException>();

        app.UseAuthentication(); // Note: This should come BEFORE UseAuthorization
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
        return app;
    }
}