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

public class OpenRouterChatProvider : IChatCompletionService
{
    private readonly IChatClient _chatClient;
    private readonly string _modelId;

    public OpenRouterChatProvider(string apiKey, string modelId)
    {
        _modelId = modelId;

        // Debug logging - check if API key is being passed correctly
        Console.WriteLine($"[DEBUG] OpenRouterChatProvider constructor called");
        Console.WriteLine($"[DEBUG] ModelId: {_modelId}");
        Console.WriteLine($"[DEBUG] API Key provided: {(string.IsNullOrEmpty(apiKey) ? "NULL or EMPTY" : $"{apiKey.Substring(0, Math.Min(10, apiKey.Length))}... (length: {apiKey.Length})")}");
        Console.WriteLine($"[DEBUG] API Key starts with 'sk-or-v1-': {apiKey?.StartsWith("sk-or-v1-")}");
        Console.WriteLine($"[DEBUG] API Key length: {apiKey?.Length}");

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
                Endpoint = new Uri("https://openrouter.ai/api/v1/")
            });

        // Convert to IChatClient abstraction
        _chatClient = chatClient.AsIChatClient();

        Console.WriteLine($"[DEBUG] OpenRouterChatProvider initialized successfully");
    }

    public async Task<ChatCompletionService.Domain.Entities.ChatResponse> SendMessageAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var providerMessages = request.Messages.Select(ModelConverter.ToProviderMessage).ToList();
        var response = await _chatClient.GetResponseAsync(providerMessages, cancellationToken: cancellationToken);

        var domainMessage = ModelConverter.ToDomainMessage(response.Messages.First());
        return new ChatCompletionService.Domain.Entities.ChatResponse
        {
            Message = domainMessage,
            ModelUsed = _modelId,
            ProviderUsed = "OpenRouter"
        };
    }

    public async IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(ChatRequestDto request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Add null checks as suggested for debugging
        if (_chatClient == null) throw new InvalidOperationException("Chat client not initialized");
        if (request?.Messages == null) throw new ArgumentNullException(nameof(request.Messages));

        var providerMessages = request.Messages.Select(ModelConverter.ToProviderMessage).ToList();
        var streamingResponse = _chatClient.GetStreamingResponseAsync(providerMessages, cancellationToken: cancellationToken);

        await foreach (var update in streamingResponse.WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))  // Check for actual content
            {
                yield return new StreamingChatUpdate
                {
                    Content = update.Text,  // Use .Text property
                    IsFinal = false
                };
            }
        }

        yield return new StreamingChatUpdate { Content = string.Empty, IsFinal = true };
    }

    public ProviderMetadata GetMetadata()
    {
        // This will be implemented later with configuration reading.
        return new ProviderMetadata { ProviderName = "OpenRouter" };
    }
}