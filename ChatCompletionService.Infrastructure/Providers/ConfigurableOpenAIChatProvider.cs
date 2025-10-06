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
            ILogger logger,
            Func<string, string, string, IChatClient> chatClientFactory,
            string? systemPrompt = null)  // Add parameter
            : base(providerName, logger, modelId, systemPrompt)  // Pass to base
        {
            ArgumentNullException.ThrowIfNull(apiKey);
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(chatClientFactory);
    
            InitializeChatClient(chatClientFactory, apiKey, modelId, endpoint);
        }
}