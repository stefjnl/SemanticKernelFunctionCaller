using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Defines a contract for streaming chat messages with tool support.
/// </summary>
public interface IStreamWithToolsUseCase
{
    /// <summary>
    /// Executes a streaming chat request with automatic tool invocation.
    /// </summary>
    /// <param name="request">The chat request containing messages and provider information.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of streaming updates including tool calls.</returns>
    IAsyncEnumerable<ToolStreamingUpdate> ExecuteAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}