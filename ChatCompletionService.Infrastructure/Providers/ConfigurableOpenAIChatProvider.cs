using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Domain.Entities;
using ChatCompletionService.Infrastructure.Mappers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using System.Runtime.CompilerServices;

namespace ChatCompletionService.Infrastructure.Providers;

public class ConfigurableOpenAIChatProvider : BaseChatProvider
{
    public ConfigurableOpenAIChatProvider(
        string apiKey,
        string modelId,
        string endpoint,
        string providerName,
        ILogger logger) : base(providerName, logger, modelId)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(endpoint);

        // Using the static CreateChatClient method from the base class for consistency
        _chatClient = CreateChatClient(apiKey, modelId, endpoint);
    }

    // This method is now correctly overriding the base class method.
    public override async IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(
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
}