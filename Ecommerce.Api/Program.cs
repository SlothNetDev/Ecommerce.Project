using Ecommerce.Api.Extensions;

namespace Ecommerce.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.AddCleanSerilog()
            .Services
            .AddPresentationService(builder.Configuration);

        var app = builder.Build();

        await app.ConfigureApplication();

       await app.RunAsync();
    }
}