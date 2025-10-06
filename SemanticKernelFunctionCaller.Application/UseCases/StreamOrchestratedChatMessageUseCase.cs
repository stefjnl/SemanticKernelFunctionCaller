using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Application.Exceptions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Use case for streaming orchestrated chat messages with automatic function calling
/// </summary>
public class StreamOrchestratedChatMessageUseCase
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly ILogger<StreamOrchestratedChatMessageUseCase> _logger;

    public StreamOrchestratedChatMessageUseCase(
        IAIOrchestrationService orchestrationService,
        ILogger<StreamOrchestratedChatMessageUseCase> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the use case to stream an orchestrated chat message
    /// </summary>
    /// <param name="request">Chat request DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming updates</returns>
    public async IAsyncEnumerable<StreamingChatUpdate> ExecuteAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Add correlation ID for logging
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = "unknown" // In a real implementation, this would come from the request context
        });

        _logger.LogInformation("Starting orchestrated chat message streaming with correlation ID: {CorrelationId}", correlationId);

        // Map DTO to domain models
        var messages = request.Messages.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        // Call the orchestration service
        await foreach (var update in _orchestrationService.StreamOrchestratedMessageAsync(messages, cancellationToken))
        {
            yield return update;
        }
        _logger.LogInformation("Successfully completed orchestrated chat message streaming with correlation ID: {CorrelationId}", correlationId);
    }
}