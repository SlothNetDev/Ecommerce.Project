using System.Text;
using Ecommerce.Api.Middleware;
using Ecommerce.Core;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Identity.Services;
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

            #region IOC container
            //JWT token
            builder.Services.AddScoped<ITokenService, TokenService>();   
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
             

            #endregion
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

            #region JWT token beaerer

            // First: Configure JwtSettings properly
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value); // Optional but safe

            //Then: configure JWT scheme
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    // Read settings from already-registered IOptions
                    var serviceProvider = builder.Services.BuildServiceProvider(); // temporary provider
                    var jwtSettings = serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

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
            
            //adding global middleware
            app.UseMiddleware<MiddlewareException>();
            
            app.UseAuthentication(); // Note: This should come BEFORE UseAuthorization
            app.UseAuthorization();
            app.MapControllers();
            
            app.Run();
        }
    }
}