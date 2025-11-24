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

            //Add sql
            builder.Services.AddDbContext<ApplicationDbContext>(temp =>
            {
                temp.UseSqlServer(builder
                    .Configuration.GetConnectionString("EcommerceDbConnection"));
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

            
            app.MapControllers();
            
            app.Run();
        }
    }
}