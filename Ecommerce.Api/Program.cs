
using Ecommerce.Api.Extensions;

namespace Ecommerce.Api
{
    public abstract class Program
    {
        public static async Task  Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

          
          
          
            
            var app = builder.Build();

           
            
            await app.RunAsync();
        }
    }
}