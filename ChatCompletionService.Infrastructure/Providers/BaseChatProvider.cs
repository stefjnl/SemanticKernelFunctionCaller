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

    protected BaseChatProvider(ProviderConfig config, string modelId, string providerName, ILogger logger) : this(providerName, logger, modelId)
    {
        ArgumentNullException.ThrowIfNull(config);
        ValidateApiKey(config.ApiKey);
        _chatClient = CreateChatClient(config.ApiKey, modelId, config.Endpoint);
    }
    
    protected BaseChatProvider(string providerName, ILogger logger, string modelId)
    {
        _providerName = providerName;
        _logger = logger;
        _modelId = modelId;
        _chatClient = null!;
    }

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

    public virtual ProviderMetadata GetMetadata()
    {
        return new ProviderMetadata { Id = _providerName, DisplayName = _providerName };
    }

    // Virtual with default - providers can override if needed
    protected virtual string ExtractStreamingContent(dynamic update)
    {
        return update.Text;
    }

    protected static void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
    }

    protected static IChatClient CreateChatClient(string apiKey, string modelId, string endpoint)
    {
        var chatClient = new OpenAI.Chat.ChatClient(
            modelId,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return chatClient.AsIChatClient().AsBuilder()
            .UseOpenTelemetry()
            .Build();
    }
}