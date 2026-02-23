namespace DDDworkshop.Dam.Rights.Api.Middleware;

using DDDworkshop.Dam.Rights.Domain.Exceptions;
using System.Text.Json;

/// <summary>
/// Global exception handler middleware.
/// 
/// Translates domain exceptions into appropriate HTTP status codes.
/// This keeps controllers clean — they don't need to catch every domain exception.
/// </summary>
public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    public DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (RightsViolationException ex)
        {
            _logger.LogWarning(ex, "Rights violation: {Reasons}", string.Join(", ", ex.Reasons));
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                reasons = ex.Reasons
            });
            await context.Response.WriteAsync(body);
        }
        catch (InvalidTimeWindowException ex)
        {
            _logger.LogWarning(ex, "Invalid time window");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new { error = ex.Message });
            await context.Response.WriteAsync(body);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad argument");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new { error = ex.Message });
            await context.Response.WriteAsync(body);
        }
    }
}
