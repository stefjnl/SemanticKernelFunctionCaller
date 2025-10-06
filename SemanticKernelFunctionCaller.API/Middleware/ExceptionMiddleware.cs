using System.Net;
using System.Text.Json;
using SemanticKernelFunctionCaller.API.Models;
using SemanticKernelFunctionCaller.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace SemanticKernelFunctionCaller.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
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

        // Enhanced logging for DI issues
        if (exception is InvalidOperationException invalidOpEx &&
            (invalidOpEx.Message.Contains("Unable to resolve service") ||
             invalidOpEx.Message.Contains("No service for type")))
        {
            _logger.LogError(exception, "Dependency Injection error detected. TraceId: {TraceId}. Service registration issue: {ExceptionMessage}",
                traceId, invalidOpEx.Message);
            
            // Log all registered services for debugging
            LogRegisteredServices(context);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
        }

        var errorResponse = new ErrorResponse
        {
            TraceId = traceId,
            Timestamp = DateTime.UtcNow,
            Message = message,
            Details = _environment.IsDevelopment() ? exception.ToString() : null
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
            InvalidOperationException opEx when opEx.Message.Contains("Unable to resolve service") => HttpStatusCode.InternalServerError,
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
            InvalidOperationException opEx when opEx.Message.Contains("Unable to resolve service") =>
                $"Service registration error: {opEx.Message}",
            InvalidOperationException => "The operation could not be completed.",
            _ => "An internal server error occurred."
        };
    }

    private void LogRegisteredServices(HttpContext context)
    {
        try
        {
            var services = context.RequestServices;
            _logger.LogInformation("=== Registered Services Debug Info ===");
            
            // Log key service registrations
            var keyServices = new[]
            {
                typeof(IGetAvailableProvidersUseCase),
                typeof(IGetProviderModelsUseCase),
                typeof(ISendChatMessageUseCase),
                typeof(IStreamChatMessageUseCase),
                typeof(IProviderConfigurationReader)
            };

            foreach (var serviceType in keyServices)
            {
                var service = services.GetService(serviceType);
                _logger.LogInformation("Service {ServiceType}: {Status}",
                    serviceType.Name,
                    service != null ? "Registered" : "NOT REGISTERED");
            }
            
            _logger.LogInformation("=== End Service Debug Info ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log registered services");
        }
    }
}
