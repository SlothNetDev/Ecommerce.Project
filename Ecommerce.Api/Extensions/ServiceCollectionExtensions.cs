using System.Reflection;
using System.Text;
using Ecommerce.Core;

using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Resend;
using Serilog;

namespace Ecommerce.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationService(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment env)
    {
        #region  Prerequisites for app to run
        // Add services to the container.

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Ecommerce API",
                Version = "v1",
                Description = "API For Ecommerce Application",
                License = new OpenApiLicense()
                {
                    Name ="Ecommerce Federation"
                },
                Contact = new  OpenApiContact()
                {
                    Email = "ecommerce.sample.com",
                    Name = "bubena"
                }
                
            });
            
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });

        
        #endregion
        //fluent api
        services.AddFluentValidationAutoValidation();
        
        //dependency injection in core/domain use the validation service
        services.AddCore();
        
       //dependency injection in infrastructure use the data context
       services.AddInfrastructure(
           configuration, env);
       
        //add jwt bearer service
        JwtBearerService(configuration, services);
        
        
        //Validate security key for OTP
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(nameof(SecurityKeySettings)))
            .ValidateDataAnnotations()
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.Key),
                "Otp Key must be provided"
            )
            .ValidateOnStart();
        
        //safely delete db files
        SafeCleanupDatabaseFiles();
        
        //Add ImemoryCache
        services.AddMemoryCache();
        return services;
    }
    
    private static void JwtBearerService(IConfiguration configuration, IServiceCollection service)
    {
        service
            .AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"))
            .ValidateDataAnnotations()
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.Key),
                "Jwt Key must be provided"
            )
            .ValidateOnStart();
            
        service.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = configuration
                    .GetRequiredSection("JwtSettings")
                    .Get<JwtSettings>();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings!.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Key)
                    )
                };
            });

    }
    
    static void SafeCleanupDatabaseFiles(string basePath = null)
    {
        try
        {
            // Ensure all SQLite connections are closed
            GC.Collect();
            GC.WaitForPendingFinalizers();
        
            // Use provided path or current directory
            basePath = basePath ?? Directory.GetCurrentDirectory();
        
            var filesToDelete = new[]
            {
                "Ecommerce.db-shm",
                "Ecommerce.db-wal", 
                "Hangfire.db-shm",
                "Hangfire.db-wal",
                "*.db-shm",  // Pattern for any SQLite SHM files
                "*.db-wal"   // Pattern for any SQLite WAL files
            };
        
            foreach (var pattern in filesToDelete)
            {
                var files = Directory.GetFiles(basePath, pattern);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted: {file}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not delete {file}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cleanup error: {ex.Message}");
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
                .Enrich.FromLogContext()
                // These will stay in the JSON file but won't clutter the Console anymore
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            // Optional: You can force clear properties for the console here if you prefer 
            // but the appsettings template change is usually enough.
        });

        return builder;
    }
}