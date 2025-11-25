using Ecommerce.Api.Middleware;
using Ecommerce.Core;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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