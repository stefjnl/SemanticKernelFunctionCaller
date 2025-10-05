using System.Runtime.CompilerServices;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Domain.Entities;
using ChatCompletionService.Domain.ValueObjects;
using ChatCompletionService.Infrastructure.Configuration;
using ChatCompletionService.Infrastructure.Mappers;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace ChatCompletionService.Infrastructure.Providers;

public abstract class BaseChatProvider : IChatCompletionService
{
    protected readonly IChatClient _chatClient;
    protected readonly string _modelId;
    protected readonly string _providerName;

    protected BaseChatProvider(ProviderConfig config, string modelId, string providerName)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(modelId);
        ArgumentNullException.ThrowIfNull(providerName);

        _modelId = modelId;
        _providerName = providerName;

        ValidateApiKey(config.ApiKey);
        _chatClient = CreateChatClient(config.ApiKey, modelId, config.Endpoint);
    }

    public virtual async Task<Domain.Entities.ChatResponse> SendMessageAsync(
        ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var providerMessages = request.Messages.Select(ModelConverter.ToProviderMessage).ToList();
        var response = await _chatClient.GetResponseAsync(providerMessages, cancellationToken: cancellationToken);

        var domainMessage = ModelConverter.ToDomainMessage(response.Messages.First());
        return new Domain.Entities.ChatResponse
        {
            Message = domainMessage,
            ModelUsed = _modelId,
            ProviderUsed = _providerName
        };
    }

    public virtual async IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(
        ChatRequestDto request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var providerMessages = request.Messages.Select(ModelConverter.ToProviderMessage).ToList();
        var streamingResponse = _chatClient.GetStreamingResponseAsync(providerMessages, cancellationToken: cancellationToken);

        await foreach (var update in streamingResponse.WithCancellation(cancellationToken))
        {
            var content = ExtractStreamingContent(update);
            if (!string.IsNullOrEmpty(content))
            {
                yield return new StreamingChatUpdate { Content = content, IsFinal = false };
            }
        }

        yield return new StreamingChatUpdate { Content = string.Empty, IsFinal = true };
    }

    public virtual ProviderMetadata GetMetadata()
    {
        return new ProviderMetadata { ProviderName = _providerName };
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

    protected static void ValidateRequest(ChatRequestDto request)
    {
        if (request?.Messages == null)
            throw new ArgumentNullException(nameof(request.Messages));
    }

    private static IChatClient CreateChatClient(string apiKey, string modelId, string endpoint)
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