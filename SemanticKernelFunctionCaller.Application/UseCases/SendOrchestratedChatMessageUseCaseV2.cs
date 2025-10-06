using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Simplified use case for sending orchestrated chat messages with automatic function calling
/// </summary>
public class SendOrchestratedChatMessageUseCaseV2
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly ILogger<SendOrchestratedChatMessageUseCaseV2> _logger;

    public SendOrchestratedChatMessageUseCaseV2(
        IAIOrchestrationService orchestrationService,
        ILogger<SendOrchestratedChatMessageUseCaseV2> logger)
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
        try
        {
            // Map DTO to domain models
            var messages = request.Messages.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            // Call the orchestration service directly
            var result = await _orchestrationService.SendOrchestratedMessageAsync(messages, cancellationToken);

            _logger.LogInformation("Successfully completed orchestrated chat message execution");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrated chat message failed for user request");
            throw; // Let controller/middleware handle error response
        }
    }
}