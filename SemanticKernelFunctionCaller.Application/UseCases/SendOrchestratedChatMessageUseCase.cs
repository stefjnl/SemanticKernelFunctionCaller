using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Requests;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Use case for sending orchestrated chat messages with automatic function calling
/// </summary>
public class SendOrchestratedChatMessageUseCase : IRequestHandler<SendOrchestratedChatMessageRequest, ChatResponseDto>
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly ILogger<SendOrchestratedChatMessageUseCase> _logger;

    public SendOrchestratedChatMessageUseCase(
        IAIOrchestrationService orchestrationService,
        ILogger<SendOrchestratedChatMessageUseCase> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the use case to send an orchestrated chat message
    /// </summary>
    /// <param name="request">Chat request DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response DTO</returns>
    public async Task<ChatResponseDto> ExecuteAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        // Add correlation ID for logging
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = "unknown" // In a real implementation, this would come from the request context
        });

        _logger.LogInformation("Starting orchestrated chat message execution with correlation ID: {CorrelationId}", correlationId);

        // Map DTO to domain models
        var messages = request.Messages.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        try
        {
            // Call the orchestration service
            var result = await _orchestrationService.SendOrchestratedMessageAsync(messages, cancellationToken);
            _logger.LogInformation("Successfully completed orchestrated chat message execution with correlation ID: {CorrelationId}", correlationId);
            return result;
        }
        catch (PluginExecutionException ex) when (ex.IsTransient)
        {
            _logger.LogWarning("Plugin {PluginName} failed transiently, retrying... Correlation ID: {CorrelationId}", ex.PluginName, correlationId);
            return await RetryWithExponentialBackoff(messages, cancellationToken, ex);
        }
        catch (PluginExecutionException ex)
        {
            _logger.LogError("Plugin {PluginName} failed permanently. Correlation ID: {CorrelationId}", ex.PluginName, correlationId);
            return await FallbackResponse(messages, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SendOrchestratedChatMessageUseCase. Correlation ID: {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<ChatResponseDto> Handle(SendOrchestratedChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(request.Request, cancellationToken);
    }

    private async Task<ChatResponseDto> RetryWithExponentialBackoff(
        List<ChatMessage> messages,
        CancellationToken cancellationToken,
        PluginExecutionException originalException)
    {
        var retryCount = 0;
        var maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);

        while (retryCount < maxRetries)
        {
            try
            {
                retryCount++;
                _logger.LogInformation("Retry attempt {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                
                // Exponential backoff
                if (retryCount > 1)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(delay.Ticks * 2); // Double the delay
                }

                return await _orchestrationService.SendOrchestratedMessageAsync(messages, cancellationToken);
            }
            catch (PluginExecutionException ex) when (ex.IsTransient && retryCount < maxRetries)
            {
                _logger.LogWarning("Retry {RetryCount} failed with transient error: {Message}", retryCount, ex.Message);
                // Continue to next retry
            }
            catch (PluginExecutionException ex)
            {
                _logger.LogError("Retry {RetryCount} failed with permanent error: {Message}", retryCount, ex.Message);
                return await FallbackResponse(messages, ex);
            }
        }

        // If we've exhausted retries, return fallback
        _logger.LogError("All retries exhausted for transient error: {Message}", originalException.Message);
        return await FallbackResponse(messages, originalException);
    }

    private Task<ChatResponseDto> FallbackResponse(List<ChatMessage> messages, Exception ex)
    {
        _logger.LogInformation("Generating fallback response due to error: {Message}", ex.Message);
        
        // Create a fallback response that acknowledges the error but provides helpful information
        var fallbackContent = $"I apologize, but I encountered an issue while processing your request: {ex.Message}. " +
                             "Please try rephrasing your request or try again later.";

        var response = new ChatResponseDto
        {
            Content = fallbackContent,
            ModelId = "fallback",
            ProviderId = "system",
            FunctionsExecuted = new List<FunctionCallMetadata>()
        };
        
        return Task.FromResult(response);
    }
}