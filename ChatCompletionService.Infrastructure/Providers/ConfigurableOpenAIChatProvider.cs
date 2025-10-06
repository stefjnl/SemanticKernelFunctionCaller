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
        ILogger logger) : base(apiKey, modelId, endpoint, providerName, logger)
    {
        // Base class handles all common initialization
    }
}