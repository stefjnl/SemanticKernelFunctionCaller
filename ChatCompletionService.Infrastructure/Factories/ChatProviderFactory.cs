using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Domain.Enums;
using ChatCompletionService.Infrastructure.Configuration;
using ChatCompletionService.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatCompletionService.Infrastructure.Factories;

public class ChatProviderFactory : IProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatProviderFactory> _logger;
    private readonly ProviderConfigurationManager _configManager;

    public ChatProviderFactory(IConfiguration configuration, ILogger<ChatProviderFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _configManager = new ProviderConfigurationManager(configuration);
    }

    public IChatCompletionService CreateProvider(ProviderType provider, string modelId)
    {
        try
        {
            var providerName = provider.ToString();
            var providerConfig = _configManager.GetProviderConfig(providerName);

            return provider switch
            {
                ProviderType.OpenRouter => new OpenRouterChatProvider(providerConfig, modelId),
                ProviderType.NanoGPT => new NanoGptChatProvider(providerConfig, modelId),
                _ => throw new NotSupportedException($"Provider {provider} is not supported.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create provider {Provider} with model {Model}", provider, modelId);
            throw;
        }
    }

    public IEnumerable<ProviderInfoDto> GetAvailableProviders()
    {
        var settings = _configManager.GetAllProviderSettings();
        var providers = new List<ProviderInfoDto>();

        if (settings.OpenRouter != null)
        {
            providers.Add(new ProviderInfoDto { Id = "OpenRouter", DisplayName = "OpenRouter" });
        }
        if (settings.NanoGPT != null)
        {
            providers.Add(new ProviderInfoDto { Id = "NanoGPT", DisplayName = "NanoGPT" });
        }

        return providers;
    }

    public IEnumerable<ModelInfoDto> GetModelsForProvider(ProviderType provider)
    {
        var providerName = provider.ToString();
        var providerConfig = _configManager.GetProviderConfig(providerName);

        return providerConfig.Models.Select(m => new ModelInfoDto { Id = m.Id, DisplayName = m.DisplayName });
    }
}