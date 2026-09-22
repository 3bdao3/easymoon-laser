using System.Text.Json;
using ErpClink.BuildingBlocks.Application.Common;

namespace ErpClink.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Application error {Code}", ex.Code);
            await WriteProblemAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var detail = _environment.IsDevelopment() || _environment.IsEnvironment("Testing")
                ? ex.Message
                : "An unexpected error occurred.";
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "server.error", detail);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string code, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var payload = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title = GetTitle(statusCode),
            status = statusCode,
            detail,
            code,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Error"
    };
}
