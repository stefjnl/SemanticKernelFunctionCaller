using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Domain.Enums;
using ChatCompletionService.Infrastructure.Configuration;
using ChatCompletionService.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;

namespace ChatCompletionService.Infrastructure.Factories;

public class ChatProviderFactory : IProviderFactory
{
    private readonly ProviderConfigurationManager _configManager;

    public ChatProviderFactory(IConfiguration configuration)
    {
        _configManager = new ProviderConfigurationManager(configuration);
    }

    public IChatCompletionService CreateProvider(ProviderType provider, string modelId)
    {
        var providerName = provider.ToString();
        var providerConfig = _configManager.GetProviderConfig(providerName);

        return provider switch
        {
            ProviderType.OpenRouter => new OpenRouterChatProvider(providerConfig.ApiKey, modelId),
            ProviderType.NanoGPT => new NanoGptChatProvider(providerConfig.ApiKey, modelId),
            _ => throw new NotSupportedException($"Provider {provider} is not supported.")
        };
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