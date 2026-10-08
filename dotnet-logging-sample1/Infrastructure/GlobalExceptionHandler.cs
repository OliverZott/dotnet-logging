using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_logging_sample1.Infrastructure;

// Single place for unhandled request exceptions: logs once (structured, with
// stack trace) and returns a client-safe ProblemDetails response.
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            InvalidOperationException => (StatusCodes.Status503ServiceUnavailable, "Service temporarily unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        LogUnhandledException(exception, httpContext.Request.Method, httpContext.Request.Path, statusCode);

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title }
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Request {Method} {Path} failed with {StatusCode}")]
    private partial void LogUnhandledException(Exception exception, string method, string path, int statusCode);
}
