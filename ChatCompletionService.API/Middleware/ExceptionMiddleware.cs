using System.Net;
using System.Text.Json;
using ChatCompletionService.API.Models;
using Microsoft.AspNetCore.Http;

namespace ChatCompletionService.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var statusCode = GetStatusCode(exception);
        var message = GetErrorMessage(exception);

        _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);

        var errorResponse = new ErrorResponse
        {
            TraceId = traceId,
            Timestamp = DateTime.UtcNow,
            Message = message,
            Details = exception.InnerException?.Message
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(jsonResponse);
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Invalid request parameters provided.",
            UnauthorizedAccessException => "Authentication required.",
            KeyNotFoundException => "The requested resource was not found.",
            InvalidOperationException => "The operation could not be completed.",
            _ => "An internal server error occurred."
        };
    }
}