using System.Text.Json;
using FieldTask.Api.Middleware;
using FieldTask.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FieldTask.Tests;

// The middleware is the single place that decides what each failure means to the client.
// These tests pin that mapping down: AI unavailable -> 503, AI garbage -> 502, our bugs -> 500.
public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task AiUnavailable_Returns503()
    {
        var (status, title) = await RunWithException(new AiUnavailableException("quota exhausted"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("AI suggestions are temporarily unavailable", title);
    }

    [Fact]
    public async Task AiResponseInvalid_Returns502()
    {
        var (status, title) = await RunWithException(new AiResponseInvalidException("priority 99"));

        Assert.Equal(StatusCodes.Status502BadGateway, status);
        Assert.Equal("The AI returned an unusable suggestion", title);
    }

    [Fact]
    public async Task AnyOtherException_Returns500()
    {
        var (status, title) = await RunWithException(new InvalidOperationException("our bug"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("An unexpected error occurred", title);
    }

    [Fact]
    public async Task ExceptionMessage_NeverLeaksToClient()
    {
        var context = BuildContext();
        var middleware = BuildMiddleware(_ => throw new InvalidOperationException("SECRET connection string"));

        await middleware.InvokeAsync(context);

        Assert.DoesNotContain("SECRET", ReadBody(context));
    }

    [Fact]
    public async Task Cancellation_IsNotTurnedIntoAnError()
    {
        // A client disconnecting is not a server failure - it must propagate, not become a 500.
        var middleware = BuildMiddleware(_ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(BuildContext()));
    }

    // ---- helpers ----

    private static async Task<(int Status, string? Title)> RunWithException(Exception ex)
    {
        var context = BuildContext();
        await BuildMiddleware(_ => throw ex).InvokeAsync(context);

        using var doc = JsonDocument.Parse(ReadBody(context));
        return (context.Response.StatusCode, doc.RootElement.GetProperty("title").GetString());
    }

    private static ExceptionHandlingMiddleware BuildMiddleware(RequestDelegate next)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        return new ExceptionHandlingMiddleware(
            next,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            services.GetRequiredService<IProblemDetailsService>());
    }

    private static DefaultHttpContext BuildContext()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }
}
