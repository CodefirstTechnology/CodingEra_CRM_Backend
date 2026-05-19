using CRM.Business.Common;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceStatus.Success => new OkObjectResult(result.Value),
            ServiceStatus.NotFound => new NotFoundResult(),
            ServiceStatus.BadRequest => new BadRequestObjectResult(result.Message ?? "Bad request."),
            ServiceStatus.Conflict => new ConflictObjectResult(result.Message),
            ServiceStatus.Unauthorized => new UnauthorizedObjectResult(result.Message),
            _ => new BadRequestResult(),
        };
    }

    public static IActionResult ToActionResult(this ServiceResult result, object? successBody = null)
    {
        return result.Status switch
        {
            ServiceStatus.Success => successBody == null ? new OkResult() : new OkObjectResult(successBody),
            ServiceStatus.NotFound => new NotFoundResult(),
            ServiceStatus.BadRequest => new BadRequestObjectResult(result.Message ?? "Bad request."),
            ServiceStatus.Conflict => new ConflictObjectResult(result.Message),
            ServiceStatus.Unauthorized => new UnauthorizedObjectResult(result.Message),
            _ => new BadRequestResult(),
        };
    }
}
