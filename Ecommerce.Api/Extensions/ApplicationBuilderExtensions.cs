using Ecommerce.Api.Middleware;
using Hangfire;

namespace Ecommerce.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> ConfigureApplication(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce v1");
                options.RoutePrefix = "api-docs"; // Optional: Changes the URL from /swagger to /api-docs
            });
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard();
        }

        //adding global middleware
        app.UseMiddleware<MiddlewareException>();

        app.UseAuthentication(); // Note: This should come BEFORE UseAuthorization
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
        return app;
    }
}