using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Requests;
using SemanticKernelFunctionCaller.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Use case for executing prompt templates with variable substitution
/// </summary>
public class ExecutePromptTemplateUseCase : IRequestHandler<ExecutePromptTemplateRequest, ChatResponseDto>
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly ILogger<ExecutePromptTemplateUseCase> _logger;

    public ExecutePromptTemplateUseCase(
        IAIOrchestrationService orchestrationService,
        ILogger<ExecutePromptTemplateUseCase> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the use case to run a prompt template
    /// </summary>
    /// <param name="templateRequest">Prompt template request DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response DTO with template result</returns>
    public async Task<ChatResponseDto> ExecuteAsync(
        PromptTemplateDto templateRequest,
        CancellationToken cancellationToken = default)
    {
        // Add correlation ID for logging
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = "unknown" // In a real implementation, this would come from the request context
        });

        _logger.LogInformation("Starting prompt template execution with correlation ID: {CorrelationId}", correlationId);

        try
        {
            // Call the orchestration service
            var result = await _orchestrationService.ExecutePromptTemplateAsync(templateRequest, cancellationToken);
            _logger.LogInformation("Successfully completed prompt template execution with correlation ID: {CorrelationId}", correlationId);
            return result;
        }
        catch (PluginExecutionException ex) when (ex.IsTransient)
        {
            _logger.LogWarning("Plugin {PluginName} failed transiently during prompt template execution, retrying... Correlation ID: {CorrelationId}", ex.PluginName, correlationId);
            return await RetryWithExponentialBackoff(templateRequest, cancellationToken, ex);
        }
        catch (PluginExecutionException ex)
        {
            _logger.LogError("Plugin {PluginName} failed permanently during prompt template execution. Correlation ID: {CorrelationId}", ex.PluginName, correlationId);
            return await FallbackResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during prompt template execution. Correlation ID: {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<ChatResponseDto> Handle(ExecutePromptTemplateRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(request.Request, cancellationToken);
    }

    private async Task<ChatResponseDto> RetryWithExponentialBackoff(
        PromptTemplateDto templateRequest,
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
                _logger.LogInformation("Retry attempt {RetryCount}/{MaxRetries} for prompt template execution", retryCount, maxRetries);
                
                // Exponential backoff
                if (retryCount > 1)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(delay.Ticks * 2); // Double the delay
                }

                return await _orchestrationService.ExecutePromptTemplateAsync(templateRequest, cancellationToken);
            }
            catch (PluginExecutionException ex) when (ex.IsTransient && retryCount < maxRetries)
            {
                _logger.LogWarning("Retry {RetryCount} failed with transient error: {Message}", retryCount, ex.Message);
                // Continue to next retry
            }
            catch (PluginExecutionException ex)
            {
                _logger.LogError("Retry {RetryCount} failed with permanent error: {Message}", retryCount, ex.Message);
                return await FallbackResponse(ex);
            }
        }

        // If we've exhausted retries, return fallback
        _logger.LogError("All retries exhausted for transient error: {Message}", originalException.Message);
        return await FallbackResponse(originalException);
    }

    private Task<ChatResponseDto> FallbackResponse(Exception ex)
    {
        _logger.LogInformation("Generating fallback response due to error: {Message}", ex.Message);
        
        // Create a fallback response that acknowledges the error but provides helpful information
        var fallbackContent = $"I apologize, but I encountered an issue while executing the prompt template: {ex.Message}. " +
                             "Please check your template and variables or try again later.";

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