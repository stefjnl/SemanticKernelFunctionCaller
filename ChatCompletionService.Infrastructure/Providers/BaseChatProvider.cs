using System.Runtime.CompilerServices;
using ChatCompletionService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using ChatCompletionService.Domain.Entities;
using ChatCompletionService.Domain.ValueObjects;
using ChatCompletionService.Infrastructure.Configuration;
using ChatCompletionService.Infrastructure.Mappers;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Diagnostics;
using ProviderChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ProviderChatResponse = Microsoft.Extensions.AI.ChatResponse;
using DomainChatMessage = ChatCompletionService.Domain.Entities.ChatMessage;
using DomainChatResponse = ChatCompletionService.Domain.Entities.ChatResponse;

namespace ChatCompletionService.Infrastructure.Providers;

public abstract class BaseChatProvider : IChatCompletionService
{
    protected IChatClient _chatClient;
    protected readonly string _modelId;
    protected readonly string _providerName;
    protected ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the BaseChatProvider class.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelId">The model identifier.</param>
    protected BaseChatProvider(string providerName, ILogger logger, string modelId)
    {
        _providerName = providerName;
        _logger = logger;
        _modelId = modelId;
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

        var providerMessages = messages.Select(ModelConverter.ToProviderMessage).ToList();
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

        var providerMessages = messages.Select(ModelConverter.ToProviderMessage).ToList();
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

}