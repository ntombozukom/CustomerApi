using CustomerApi.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CustomerApi.API.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException oce && oce.CancellationToken == context.RequestAborted)
        {
            logger.LogInformation("Request cancelled by client: {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499; // Client Closed Request (nginx convention)
            return true;
        }

        logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var problem = exception switch
        {
            CustomerNotFoundException => Problem(StatusCodes.Status404NotFound, exception.Message),
            DuplicateEmailException => Problem(StatusCodes.Status409Conflict, exception.Message),
            ValidationException ve => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Extensions =
                {
                    ["errors"] = ve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            },
            _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        problem.Instance = context.Request.Path;
        context.Response.StatusCode = problem.Status!.Value;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problem,
        });

        return true;
    }

    private static ProblemDetails Problem(int status, string detail) =>
        new() { Status = status, Title = ReasonPhrases.GetReasonPhrase(status), Detail = detail };
}
