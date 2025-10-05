using Microsoft.Extensions.Configuration;

namespace ChatCompletionService.Infrastructure.Configuration;

public class ProviderConfigurationManager
{
    private readonly ProviderSettings _providerSettings;
    private readonly IConfiguration _configuration;

    public ProviderConfigurationManager(IConfiguration configuration)
    {
        _configuration = configuration;

        _providerSettings = configuration.GetSection("Providers").Get<ProviderSettings>()
                            ?? throw new InvalidOperationException("Provider settings not found in configuration.");
    }

    public ProviderConfig GetProviderConfig(string providerName)
    {
        var config = providerName.ToLower() switch
        {
            "openrouter" => _providerSettings.OpenRouter,
            "nanogpt" => _providerSettings.NanoGPT,
            _ => throw new KeyNotFoundException($"Provider '{providerName}' not found in configuration.")
        };

        return config;
    }

    public ProviderSettings GetAllProviderSettings()
    {
        return _providerSettings;
    }
}