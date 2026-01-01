using System.Data.Common;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Common.Interfaces.Security;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.DevelopmentService.Notification;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Identity.Services.JwtTokenService;
using Ecommerce.Infrastructure.Identity.Services.Login;
using Ecommerce.Infrastructure.Identity.Services.Register;
using Ecommerce.Infrastructure.Security;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Infrastructure.Services.Notification;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Resend;

namespace Ecommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        //Add database
        GetConnectionDbContext(configuration, services);
        
        //add identity
        IdentitySetUp(services);
        
        //add services IOC container
        IocContainer(services);
        
        //Add Grid service
        ResendEmail(configuration, services);
        
        //add hangfire service
        HangFireService(configuration, services);
        return services;
    }
    
    private static void GetConnectionDbContext(IConfiguration configuration, IServiceCollection service)
    {
        service.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var env = serviceProvider.GetRequiredService<IHostEnvironment>();
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            Console.WriteLine($"Environment in AddPresentationService: {env.EnvironmentName}");

            if (env.IsEnvironment("Testing"))
            {
                Console.WriteLine("Using InMemoryDatabase for Testing");
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            }
            else
            {
                Console.WriteLine("Using SQL Lite for Production");

                options.UseSqlite(config.GetConnectionString("EcommerceDbConnection"));

            }
        });
    }
    
    private static void IdentitySetUp(IServiceCollection service)
    {
        service.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                //password
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireUppercase = false;
                //attemp to login
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                
                //confirm login email
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
    }
    
    private static void IocContainer(IServiceCollection service)
    {
        //JWT token
        service.AddScoped<ITokenService, TokenService>();
        service.AddScoped<IRefreshTokenService, RefreshTokenService>();
        service.AddScoped<ITokenRefreshService, TokenRefreshService>();
        service.AddScoped<IIpAdressService, IpAddressService>();

        //Authentication and user management
        service.AddScoped<IAuthenticationService, AuthenticationService>();
        service.AddScoped<IUserRegistrationService, UserRegistrationService>();

        //OTP and Email services
        service.AddScoped<IOtpService, OtpService>();
        
        //Security
        service.AddScoped<IHashService, HmacHashService>();
        
        //roles
        service.AddScoped<IRoleManagementService, RoleManagerService>();

        //seller application
        service.AddScoped<ISellerApplicationService, SellerApplicationService>();

       
    }
    private static void ResendEmail(IConfiguration configuration, IServiceCollection services)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            // This ensures the same list instance is shared across the entire application lifetime
            services.AddSingleton<DevEmailStore>();
            services.AddScoped<IEmailService, DevEmailService>();
        }
        else
        {
            services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("Resend"));
            services.AddHttpClient<ResendClient>();
            services.AddTransient<IResend, ResendClient>();
            
            services.AddScoped<IEmailService, EmailService>();   // for production
        }
    }
    
    private static void HangFireService(IConfiguration configuration, IServiceCollection service)
    {
        var serviceProvider = service.BuildServiceProvider();
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();
        
        if (!env.IsEnvironment("Testing"))
        {
            var hangfireConnectionString = configuration.GetConnectionString("HangfireDbConnection");

            if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
            {
                service.AddHangfire(config =>
                {
                    config.UseSQLiteStorage(hangfireConnectionString, new SQLiteStorageOptions
                    {
                        // This ensures the library handles the file correctly for background jobs
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        InvisibilityTimeout = TimeSpan.FromMinutes(5),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1)
                    });
                });

                service.AddHangfireServer();
            }
        }
    }
}

