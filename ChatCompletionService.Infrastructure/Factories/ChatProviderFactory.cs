using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Domain.Enums;
using ChatCompletionService.Infrastructure.Configuration;
using ChatCompletionService.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatCompletionService.Infrastructure.Factories;

public class ChatProviderFactory : IProviderFactory
{
    private readonly ILogger<ChatProviderFactory> _logger;
    private readonly IOptions<ProviderSettings> _providerSettings;
    private readonly Dictionary<ProviderType, Func<string, IChatCompletionService>> _providerFactories;

    public ChatProviderFactory(IOptions<ProviderSettings> providerSettings, ILogger<ChatProviderFactory> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        var configurableLogger = loggerFactory.CreateLogger<ConfigurableOpenAIChatProvider>();

        _providerFactories = new Dictionary<ProviderType, Func<string, IChatCompletionService>>
        {
            {
                ProviderType.OpenRouter, (modelId) =>
                {
                    var config = providerSettings.Value.OpenRouter;
                    return new ConfigurableOpenAIChatProvider(config.ApiKey, modelId, config.Endpoint, "OpenRouter", configurableLogger);
                }
            },
            {
                ProviderType.NanoGPT, (modelId) =>
                {
                    var config = providerSettings.Value.NanoGPT;
                    return new ConfigurableOpenAIChatProvider(config.ApiKey, modelId, config.Endpoint, "NanoGpt", configurableLogger);
                }
            }
        };
    }

    public IChatCompletionService CreateProvider(string providerName, string modelId)
    {
        if (!Enum.TryParse<ProviderType>(providerName, true, out var providerType))
        {
            throw new NotSupportedException($"Provider '{providerName}' is not a valid provider type.");
        }

        if (_providerFactories.TryGetValue(providerType, out var factory))
        {
            try
            {
                return factory(modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create provider {Provider} with model {Model}", providerName, modelId);
                throw;
            }
        }

        throw new NotSupportedException($"Provider '{providerName}' is not supported.");
    }
}