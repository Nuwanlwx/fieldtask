using FieldTask.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTask.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IProblemDetailsService problemDetailsService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !context.Response.HasStarted)
        {
            var (status, title, detail, level) = Classify(ex);

            // The full exception goes to the log only, never to the client.
            logger.Log(level, ex, "{ExceptionType} while handling {Method} {Path}",
                ex.GetType().Name, context.Request.Method, context.Request.Path);

            context.Response.StatusCode = status;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = ex,
                ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
            });
        }
    }

    // One place that decides what each kind of failure means to the client.
    // AI failures are an upstream dependency misbehaving - expected from time to time, so they are
    // Warnings, not Errors, and they never say "something went wrong on our side".
    private static (int Status, string Title, string Detail, LogLevel Level) Classify(Exception ex) => ex switch
    {
        AiUnavailableException => (
            StatusCodes.Status503ServiceUnavailable,
            "AI suggestions are temporarily unavailable",
            "The AI service could not be reached or is out of capacity. You can still create the task manually.",
            LogLevel.Warning),

        AiResponseInvalidException => (
            StatusCodes.Status502BadGateway,
            "The AI returned an unusable suggestion",
            "The AI service returned a draft that failed validation. Try again, or create the task manually.",
            LogLevel.Warning),

        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            "Something went wrong on our side. Quote the correlationId if you contact support.",
            LogLevel.Error)
    };
}
