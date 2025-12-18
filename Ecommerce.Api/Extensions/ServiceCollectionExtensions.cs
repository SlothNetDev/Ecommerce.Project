using System.Data.Common;
using System.Text;
using Ecommerce.Core;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Identity.Services;
using Ecommerce.Infrastructure.Identity.Services.JwtTokenService;
using Ecommerce.Infrastructure.Identity.Services.Login;
using Ecommerce.Infrastructure.Identity.Services.Register;
using Ecommerce.Infrastructure.Notification;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Ecommerce.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationService(this IServiceCollection services,
        IConfiguration configuration)
    {
        #region  Prerequisites for app to run
        // Add services to the container.

        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        //Add api docs
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Ecommerce API",
                Version = "v1",
                Description = "API For Ecommerce"
            });
        });
        #endregion
        
        //use the validation service
        services.AddInfrastructure();
        
        //Add database
        GetConnectionDbContext(configuration, services);
        
        //add identity
        IdentitySetUp(services);
        
        //add services IOC container
        IocContainer(services);
        
        //Add Grid service
        GridService(configuration, services);
        
        //add jwt bearer service
        JwtBearerService(configuration, services);
        
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
                Console.WriteLine("Using SQL Server for Production");
                options.UseSqlServer(configuration.GetConnectionString("EcommerceDbConnection"));
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

        //roles
        service.AddScoped<IRoleManagementService, RoleManagerService>();

        //seller application
        service.AddScoped<ISellerApplicationService, SellerApplicationService>();

        service.AddScoped<IEmailService, EmailService>();
    }
    private static void GridService(IConfiguration configuration, IServiceCollection service)
    {
        var serviceProvider = service.BuildServiceProvider();
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();
        
        if (!env.IsEnvironment("Testing"))
        {
            var sendGridApiKey = configuration["SendGrid:ApiKey"];
            var sendGridFromEmail = configuration["SendGrid:FromEmail"];
            var sendGridFromName = configuration["SendGrid:FromName"] ?? "Your App";

            if (string.IsNullOrWhiteSpace(sendGridApiKey))
            {
                throw new InvalidOperationException(
                    "SendGrid API key is not configured. Please set 'SendGrid:ApiKey' in your configuration.");
            }

            if (string.IsNullOrWhiteSpace(sendGridFromEmail))
            {
                throw new InvalidOperationException(
                    "SendGrid FromEmail is not configured. Please set 'SendGrid:FromEmail' in your configuration.");
            }

            service
                .AddFluentEmail(sendGridFromEmail, sendGridFromName)
                .AddSendGridSender(sendGridApiKey);
        }
    }
    
    private static void JwtBearerService(IConfiguration configuration, IServiceCollection service)
    {
        service.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        service.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings configuration section is missing.");
            
        service.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };
            });

    }
    
    private static void HangFireService(IConfiguration configuration, IServiceCollection service)
    {
        var serviceProvider = service.BuildServiceProvider();
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();
        
        if (!env.IsEnvironment("Testing"))
        {
            var hangfireConnectionString =
                configuration.GetConnectionString("HangfireDbConnection");

            if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
            {
                service.AddHangfire(config =>
                {
                    config.UseSqlServerStorage(hangfireConnectionString);
                });

                service.AddHangfireServer();
            }
        }
    }
    
}
public static class SerilogExtensions
{
    public static WebApplicationBuilder AddCleanSerilog(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)

                // Always good defaults
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
        });

        return builder;
    }
}