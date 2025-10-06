using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Simplified use case for streaming orchestrated chat messages
/// </summary>
public class StreamOrchestratedChatMessageUseCaseV2
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly ILogger<StreamOrchestratedChatMessageUseCaseV2> _logger;

    public StreamOrchestratedChatMessageUseCaseV2(
        IAIOrchestrationService orchestrationService,
        ILogger<StreamOrchestratedChatMessageUseCaseV2> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the use case to stream an orchestrated chat message
    /// </summary>
    /// <param name="request">Chat request DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming chat updates</returns>
    public async IAsyncEnumerable<StreamingChatUpdate> ExecuteAsync(ChatRequestDto request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Map DTO to domain models
        var messages = request.Messages.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        // Stream the response
        var stream = _orchestrationService.StreamOrchestratedMessageAsync(messages, cancellationToken);

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            yield return update;
        }

        _logger.LogInformation("Successfully completed orchestrated chat message streaming");
    }
}