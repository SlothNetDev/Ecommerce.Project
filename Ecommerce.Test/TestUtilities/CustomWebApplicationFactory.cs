using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ecommerce.Test.TestUtilities;

/// <summary>
/// Replaces Real Database: Removes the real SQL Server context and uses isolated in-memory databases
/// Configures Test JWT Settings: Sets up test-specific token settings
///Ensures Clean Environment: Each test gets a fresh database (via Guid.NewGuid())
///Pre-seeds Data: Automatically creates roles and test users
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 1. Remove real SQL Server DbContext
            RemoveApplicationDbContext(services);

            // 2. Add isolated in-memory DB
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("RefreshTokenTestDb");
            });

            // 3. Add Test JWT settings
            services.Configure<JwtSettings>(options =>
            {
                options.Key = "TEST_KEY_256BIT_LONG_ABCDEFG1234567890";
                options.Issuer = "Ecommerce.Test";
                options.Audience = "Ecommerce.Test.Users";
                options.ExpiryMinutes = 60;
            });

            services.AddSingleton(sp =>
                sp.GetRequiredService<IOptions<JwtSettings>>().Value);

            // 4. Override IP address service for testing
            services.AddScoped<IIpAdressService, TestIpAddressService>();
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            });
        });
    }
    
    //Program.cs 
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();

        //register for IOC container
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        db.Database.EnsureCreated();
        DatabaseSeeder.SeedAsync(userManager, roleManager).Wait();

        return host;
    }
    
    //remove our real dbContext
    private void RemoveApplicationDbContext(IServiceCollection services)
    {
        var descriptors = services
            .Where(s =>
                s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                s.ServiceType == typeof(ApplicationDbContext))
            .ToList();

        foreach (var d in descriptors)
            services.Remove(d);
    }
}

/// <summary>
/// Test implementation of IP address service that returns a valid IP for testing
/// </summary>
public class TestIpAddressService : IIpAdressService
{
    public string GetClientIpAddress() => "127.0.0.1";

    public bool IsSuspiciousIp(string ip) => false;

    public Task<string> GetLocationAsync(string ip) => Task.FromResult("Test Location");
}