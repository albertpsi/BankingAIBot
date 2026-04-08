using Microsoft.AspNetCore.Mvc;

namespace BankingAIBot.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleException(Exception exception, ILogger logger)
        => HandleExceptionCore(exception, logger);

    protected ActionResult<T> HandleException<T>(Exception exception, ILogger logger)
        => HandleExceptionCore(exception, logger);

    private ActionResult HandleExceptionCore(Exception exception, ILogger logger)
    {
        switch (exception)
        {
            case UnauthorizedAccessException:
                logger.LogWarning(exception, "Unauthorized access in {Controller}.", GetType().Name);
                return new UnauthorizedObjectResult(exception.Message);
            case ArgumentException:
                logger.LogWarning(exception, "Validation error in {Controller}.", GetType().Name);
                return new BadRequestObjectResult(exception.Message);
            default:
                logger.LogError(exception, "Unhandled exception in {Controller}.", GetType().Name);
                return new ObjectResult("An unexpected error occurred.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
        }
    }
}
