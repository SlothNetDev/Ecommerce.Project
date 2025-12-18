using System.Text;
using Ecommerce.Api.Middleware;
using Ecommerce.Core;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.JwtToken;
using Ecommerce.Core.Application.Common.Interfaces.Login;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using FluentEmail.Core;
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

namespace Ecommerce.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Add api docs
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Ecommerce API",
                    Version = "v1",
                    Description = "API For Ecommerce"
                });
            });

            //use the validation
            builder.Services.AddInfrastructure();

            builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
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
                    options.UseSqlServer(builder.Configuration.GetConnectionString("EcommerceDbConnection"));
                }
            });

            #region Identity setUp
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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
            #endregion

            #region IOC container
            //JWT token
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();
            builder.Services.AddScoped<IIpAdressService, IpAddressService>();

            //Authentication and user management
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();

            //OTP and Email services
            builder.Services.AddScoped<IOtpService, OtpService>();

            //roles
            builder.Services.AddScoped<IRoleManagementService, RoleManagerService>();

            //seller application
            builder.Services.AddScoped<ISellerApplicationService, SellerApplicationService>();

            builder.Services.AddScoped<IEmailService, EmailService>();
            #endregion

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"];
                var sendGridFromEmail = builder.Configuration["SendGrid:FromEmail"];
                var sendGridFromName = builder.Configuration["SendGrid:FromName"] ?? "Your App";

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

                builder.Services
                    .AddFluentEmail(sendGridFromEmail, sendGridFromName)
                    .AddSendGridSender(sendGridApiKey);
            }

            #region JWT token bearer

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value); // Optional but safe

            // Read JWT settings directly from configuration (NO temporary ServiceProvider)
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
                             ?? throw new InvalidOperationException("JwtSettings configuration section is missing.");

            builder.Services.AddAuthentication(options =>
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

            #endregion

            #region Calling for background job hangfire

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                // 1. Get the dedicated connection string
                var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireDbConnection");

                if (string.IsNullOrWhiteSpace(hangfireConnectionString))
                {
                    throw new NullReferenceException("Hangfire connection string is missing. Please set 'HangfireDbConnection'.");
                }

                builder.Services.AddHangfire(config =>
                {
                    // 2. Use the dedicated connection string for Hangfire storage
                    config.UseSqlServerStorage(hangfireConnectionString);
                });

                builder.Services.AddHangfireServer();
            }

            #endregion

            var app = builder.Build();

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

            //call hangfire
            app.UseHangfireDashboard(); //add hangfire url(background url)
            //adding global middleware
            app.UseMiddleware<MiddlewareException>();

            app.UseAuthentication(); // Note: This should come BEFORE UseAuthorization
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}