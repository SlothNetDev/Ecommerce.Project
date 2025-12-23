using Ecommerce.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IResult = Ecommerce.Shared.Wrapper.IResult;

namespace Ecommerce.Api.Filters;

public class ResultHttpFilter : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult &&
            objectResult.Value is IResult result &&
            !result.Success) // Use 'Succeeded' if that's the property name in your Wrapper
        {
            executedContext.Result = MapFailure(result);
        }
    }
    
    private static IActionResult MapFailure(IResult result)
    {
        // Determine both Status and Title at the same time
        (int status, string title) = result.FailureType switch
        {
            FailureType.Validation => (StatusCodes.Status400BadRequest, "Validation Error"),
            FailureType.Authentication => (StatusCodes.Status401Unauthorized, "Authentication Failed"),
            FailureType.Authorization => (StatusCodes.Status403Forbidden, "Access Denied"),
            FailureType.NotFound => (StatusCodes.Status404NotFound, "Resource Not Found"),
            FailureType.Conflict => (StatusCodes.Status409Conflict, "Conflict"), // This is your '4'
            FailureType.RateLimited => (StatusCodes.Status429TooManyRequests, "Too Many Requests"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        return new ObjectResult(new ProblemDetails
            {
                Title = title, // Use the human-readable string here
                Detail = result.Message ?? "An error occurred", 
                Status = status,
                Extensions = { ["errors"] = result.Errors }
            })
            { StatusCode = status };
    }
}