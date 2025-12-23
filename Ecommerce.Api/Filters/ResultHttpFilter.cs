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
        var status = result.FailureType switch
        {
            FailureType.Validation => StatusCodes.Status400BadRequest,
            FailureType.Authentication => StatusCodes.Status401Unauthorized,
            FailureType.Authorization => StatusCodes.Status403Forbidden,
            FailureType.NotFound => StatusCodes.Status404NotFound,
            FailureType.Conflict => StatusCodes.Status409Conflict,
            FailureType.RateLimited => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ProblemDetails
            {
                Title = result.FailureType?.ToString() ?? "Error",
                Detail = result.Message ?? "An error occurred", 
                Status = status,
                Extensions =
                {
                    ["errors"] = result.Errors 
                }
            })
            { StatusCode = status };
    }
}