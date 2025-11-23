using Ecommerce.Core;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseHttpsRedirection();
            
            app.UseAuthorization();
            
            app.MapControllers();
            
            app.Run();

        }
    }
}