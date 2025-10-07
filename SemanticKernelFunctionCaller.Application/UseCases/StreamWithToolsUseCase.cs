using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.Enums;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Implements the use case for streaming chat messages with automatic tool invocation.
/// </summary>
public class StreamWithToolsUseCase : IStreamWithToolsUseCase
{
    private readonly Kernel _kernel;
    private readonly ILogger<StreamWithToolsUseCase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamWithToolsUseCase"/> class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance.</param>
    /// <param name="logger">The logger instance.</param>
    public StreamWithToolsUseCase(Kernel kernel, ILogger<StreamWithToolsUseCase> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a streaming chat request with automatic tool invocation.
    /// </summary>
    /// <param name="request">The chat request containing messages and provider information.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of streaming updates including tool calls.</returns>
    public async IAsyncEnumerable<ToolStreamingUpdate> ExecuteAsync(
        ChatRequestDto request, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting stream-with-tools request");

        // Convert request messages to ChatHistory
        var chatHistory = new ChatHistory();
        foreach (var msg in request.Messages)
        {
            var role = msg.Role == ChatRole.User
                ? AuthorRole.User
                : AuthorRole.Assistant;
            chatHistory.Add(new ChatMessageContent(role, msg.Content));
        }

        // Configure execution settings for tool calling
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        // Stream the response with tool calling
        await foreach (var update in _kernel.GetRequiredService<IChatCompletionService>().GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel,
            cancellationToken: cancellationToken))
        {
            // Show tool invocation in the stream
            if (update.Role == AuthorRole.Tool && update.Content != null)
            {
                _logger.LogInformation("Tool invocation: {FunctionName}", update.Content);
                
                yield return new ToolStreamingUpdate
                {
                    Type = "tool_call",
                    FunctionName = update.Content,
                    Content = $"🔧 Calling {update.Content}...",
                    IsFinal = false
                };
            }
            else if (!string.IsNullOrEmpty(update.Content))
            {
                yield return new ToolStreamingUpdate
                {
                    Type = "content",
                    Content = update.Content,
                    IsFinal = false
                };
            }
        }

        _logger.LogInformation("Completed stream-with-tools request successfully");
        
        // Send final update
        yield return new ToolStreamingUpdate
        {
            Type = "content",
            IsFinal = true
        };
    }
}