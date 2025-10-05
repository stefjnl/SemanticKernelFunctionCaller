using ChatCompletionService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatCompletionService.Infrastructure.Configuration;

public class ProviderConfigurationManager : IProviderConfigurationManager
{
    private readonly ProviderSettings _settings;
    private readonly ILogger<ProviderConfigurationManager> _logger;

    public ProviderConfigurationManager(
        IOptions<ProviderSettings> options,
        ILogger<ProviderConfigurationManager> logger)
    {
        _settings = options.Value; // Throws if configuration invalid
        _logger = logger;
    }

    public ProviderConfig GetProviderConfig(string providerName)
    {
        return providerName.ToLower() switch
        {
            "openrouter" => _settings.OpenRouter,
            "nanogpt" => _settings.NanoGPT,
            _ => throw new KeyNotFoundException($"Provider '{providerName}' not found")
        };
    }

    public ProviderSettings GetAllProviderSettings() => _settings;
}