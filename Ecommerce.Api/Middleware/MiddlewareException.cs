using System.Net;
using System.Text.Json;

namespace Ecommerce.Api.Middleware;

public class MiddlewareException(RequestDelegate next, ILogger<MiddlewareException> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Processing request for: {Path}", context.Request.Path);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🚨 Unhandled exception caught in middleware");

            if (context.Response.HasStarted)
            {
                logger.LogWarning("Response already started; skipping error middleware.");
                throw;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";

            int statusCode = ex switch
            {
                ArgumentNullException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var traceId = context.TraceIdentifier;

            var response = new
            {
                status = statusCode,
                traceId,
                message = "Sorry, something went wrong. Please try again later.",
#if DEBUG
                detail = ex.Message
#else
                 detail = statusCode == 500 ? "Internal server error." : ex.Message
#endif
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}

// Extension for easy registration
public static class MiddlewareExceptionExtensions
{
    public static IApplicationBuilder UseMiddlewareException(this IApplicationBuilder app)
        => app.UseMiddleware<MiddlewareException>();
}