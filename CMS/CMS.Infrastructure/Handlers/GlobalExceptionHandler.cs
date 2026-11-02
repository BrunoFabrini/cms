using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;

public sealed class GlobalExceptionHandler(
    ILogger logger
) : IExceptionHandler
{
    private readonly ILogger _logger = logger.ForContext<GlobalExceptionHandler>();

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _logger.Error(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                StatusCode = 500,
                Message = "An unexpected error occurred.",
                TraceId = httpContext.TraceIdentifier
            },
            cancellationToken
        );

        return true;
    }
}