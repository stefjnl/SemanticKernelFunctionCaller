using System.Runtime.CompilerServices;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.ValueObjects;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Mappers;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Diagnostics;
using ProviderChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ProviderChatResponse = Microsoft.Extensions.AI.ChatResponse;
using DomainChatMessage = SemanticKernelFunctionCaller.Domain.Entities.ChatMessage;
using DomainChatResponse = SemanticKernelFunctionCaller.Domain.Entities.ChatResponse;

namespace SemanticKernelFunctionCaller.Infrastructure.Providers;

public abstract class BaseChatProvider : IChatClientProvider
{
    protected IChatClient _chatClient;
        protected readonly string _modelId;
        protected readonly string _providerName;
        protected readonly string? _systemPrompt;  // Add this
        protected ILogger _logger;
    
        /// <summary>
        /// Initializes a new instance of the BaseChatProvider class.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="systemPrompt">Optional system prompt to inject.</param>
        protected BaseChatProvider(string providerName, ILogger logger, string modelId, string? systemPrompt = null)  // Add parameter
        {
            _providerName = providerName;
            _logger = logger;
            _modelId = modelId;
            _systemPrompt = systemPrompt;
            _chatClient = null!;
        }

    /// <summary>
    /// Initialize the chat client using the injected factory.
    /// </summary>
    protected void InitializeChatClient(
        Func<string, string, string, IChatClient> chatClientFactory,
        string apiKey,
        string modelId,
        string endpoint)
    {
        ValidateApiKey(apiKey);
        _chatClient = chatClientFactory(apiKey, modelId, endpoint);
    }

    /// <summary>
    /// Sends a chat message to the provider and returns the response.
    /// </summary>
    /// <param name="messages">Collection of domain chat message entities to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The chat response containing the provider's reply message.</returns>
    public virtual async Task<DomainChatResponse> SendMessageAsync(
        IEnumerable<DomainChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var providerMessages = PrepareMessages(messages);
                var response = await _chatClient.GetResponseAsync(providerMessages, cancellationToken: cancellationToken);
        
                var domainMessage = ModelConverter.ToDomainMessage(response.Messages.First());
        return new DomainChatResponse
        {
            Message = domainMessage,
            ModelUsed = _modelId,
            ProviderUsed = _providerName
        };
    }

    /// <summary>
    /// Streams a chat message to the provider and yields response content as it arrives.
    /// </summary>
    /// <param name="messages">Collection of domain chat message entities to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Async enumerable of streaming content updates from the provider.</returns>
    public virtual async IAsyncEnumerable<string> StreamMessageAsync(
        IEnumerable<DomainChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var providerMessages = PrepareMessages(messages);
                var streamingResponse = _chatClient.GetStreamingResponseAsync(providerMessages, cancellationToken: cancellationToken);

        await foreach (var update in streamingResponse.WithCancellation(cancellationToken))
        {
            var content = ExtractStreamingContent(update);
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    /// <summary>
    /// Gets provider metadata information.
    /// </summary>
    /// <returns>Provider metadata containing provider name and display information.</returns>
    public virtual ProviderMetadata GetMetadata()
    {
        return new ProviderMetadata { Id = _providerName, DisplayName = _providerName };
    }

    /// <summary>
    /// Gets the underlying IChatClient instance.
    /// </summary>
    /// <returns>The IChatClient instance used by this provider.</returns>
    public IChatClient GetChatClient()
    {
        return _chatClient;
    }

    /// <summary>
    /// Extracts streaming content from provider response updates.
    /// </summary>
    /// <param name="update">The streaming update from the provider.</param>
    /// <returns>The extracted text content from the update.</returns>
    /// <remarks>Virtual method that providers can override for custom streaming logic.</remarks>
    protected virtual string ExtractStreamingContent(dynamic update)
    {
        return update.Text;
    }

    /// <summary>
    /// Validates the API key parameter.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the API key is null or empty.</exception>
    protected static void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
    }

    /// <summary>
    /// Prepares messages by optionally injecting system prompt
    /// </summary>
    protected virtual List<ProviderChatMessage> PrepareMessages(
        IEnumerable<DomainChatMessage> messages)
    {
        var messageList = messages.Select(ModelConverter.ToProviderMessage).ToList();

        // Remove any system messages from user conversation
                messageList.RemoveAll(m =>
                    m.Role.ToString().Equals("System", StringComparison.OrdinalIgnoreCase));
        
                // Then inject yours at position 0
                if (!string.IsNullOrWhiteSpace(_systemPrompt))
                {
                    messageList.Insert(0, new ProviderChatMessage(
                        new Microsoft.Extensions.AI.ChatRole("System"),
                        _systemPrompt
                    ));
                }

        return messageList;
    }

}
