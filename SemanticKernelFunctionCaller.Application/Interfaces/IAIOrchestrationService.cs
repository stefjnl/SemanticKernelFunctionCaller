using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Entities;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface IAIOrchestrationService
{
    /// <summary>
    /// Sends a chat message with automatic function calling orchestration
    /// </summary>
    /// <param name="messages">Collection of chat messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response with function call metadata</returns>
    Task<ChatResponseDto> SendOrchestratedMessageAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a chat message with automatic function calling orchestration
    /// </summary>
    /// <param name="messages">Collection of chat messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming updates</returns>
    IAsyncEnumerable<StreamingChatUpdate> StreamOrchestratedMessageAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a prompt template with variable substitution
    /// </summary>
    /// <param name="templateRequest">Prompt template request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response with rendered template result</returns>
    Task<ChatResponseDto> ExecutePromptTemplateAsync(
        PromptTemplateDto templateRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a multi-step workflow using plan-and-execute pattern
    /// </summary>
    /// <param name="workflowRequest">Workflow request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response with workflow execution result</returns>
    Task<ChatResponseDto> ExecuteWorkflowAsync(
        WorkflowRequestDto workflowRequest,
        CancellationToken cancellationToken = default);
}