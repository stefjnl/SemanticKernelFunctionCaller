using System.Runtime.CompilerServices;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Domain.Entities;
using ChatCompletionService.Domain.ValueObjects;
using ChatCompletionService.Infrastructure.Mappers;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace ChatCompletionService.Infrastructure.Providers;

public class NanoGptChatProvider : IChatCompletionService
{
    private readonly IChatClient _chatClient;
    private readonly string _modelId;

    public NanoGptChatProvider(string apiKey, string modelId)
    {
        _modelId = modelId;

        // Validate API key is not null or empty
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        // Create ChatClient directly (NOT OpenAIClient) - following the working example
        var chatClient = new ChatClient(
            _modelId,
            new System.ClientModel.ApiKeyCredential(apiKey!),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.nanogpt.com/v1/chat/completions")
            });

        // Convert to IChatClient abstraction
        _chatClient = chatClient.AsIChatClient();
    }

    public async Task<Domain.Entities.ChatResponse> SendMessageAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var providerMessages = request.Messages.Select(ModelConverter.ToProviderMessage).ToList();
        var response = await _chatClient.GetResponseAsync(providerMessages, cancellationToken: cancellationToken);
        
        var domainMessage = ModelConverter.ToDomainMessage(response.Messages.First());
        return new Domain.Entities.ChatResponse
        {
            Message = domainMessage,
            ModelUsed = _modelId,
            ProviderUsed = "NanoGPT"
        };
    }

    public async IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(ChatRequestDto request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var providerMessages = request.Messages.Select(ModelConverter.ToProviderMessage).ToList();
        var streamingResponse = _chatClient.GetStreamingResponseAsync(providerMessages, cancellationToken: cancellationToken);

        await foreach (var update in streamingResponse.WithCancellation(cancellationToken))
        {
            yield return new StreamingChatUpdate { Content = update.ToString(), IsFinal = false };
        }
        
        yield return new StreamingChatUpdate { Content = string.Empty, IsFinal = true };
    }

    public ProviderMetadata GetMetadata()
    {
        // This will be implemented later with configuration reading.
        return new ProviderMetadata { ProviderName = "NanoGPT" };
    }
}