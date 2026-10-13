using Microsoft.AspNetCore.Diagnostics;

namespace OneCover.Api.Errors;

/// <summary>
/// Turns any exception that escapes a controller into a consistent ProblemDetails response: 404, 400 or 500.
/// Unexpected errors are logged, and their details are never sent to the client.
/// </summary>
// IExceptionHandler is the interface ASP.NET Core looks for. implement it and the framework calls this
// class whenever an exception escapes a controller.
// both constructor params get injected by DI: problemDetails writes the standard error JSON, logger writes logs
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // gets the request (http) and the exception (ex). returns true if we handled it.
    // ValueTask is a lighter-weight Task, used here b/c the interface requires it
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        // "expected" = one of OUR exceptions, w/ a message that's written to be safe to show the user
        // 'is A or B' is pattern matching: is this exception either of these two types?
        var isExpected = ex is NotFoundException or BusinessRuleException;

        if (!isExpected) logger.LogError(ex, "Unhandled exception"); // full deets + stack trace, server side only

        // switch expression: picks the status code based on the exception's actual type, checked top to bottom.
        // this is polymorphism, ex is just "an Exception" here, but the result depends on what type it really is
        http.Response.StatusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessRuleException => StatusCodes.Status400BadRequest,
            // _ (underscore) is the "discard" pattern = anything else. like 'default:' in a regular switch.
            // has to go last since it matches everything
            _                 => StatusCodes.Status500InternalServerError 
        };

        // writes the standard ProblemDetails JSON (type, title, status, detail) using the status code set above.
        // only our own messages go to the client, anything unexpected gets a generic message instead
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = 
            {
                Detail = isExpected ? ex.Message : "An unexpected error occurred."    
            }    
        });
    }
    
}