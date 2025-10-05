using Microsoft.Extensions.Configuration;

namespace ChatCompletionService.Infrastructure.Configuration;

public class ProviderConfigurationManager
{
    private readonly ProviderSettings _providerSettings;
    private readonly IConfiguration _configuration;

    public ProviderConfigurationManager(IConfiguration configuration)
    {
        _configuration = configuration;

        Console.WriteLine($"[DEBUG] ProviderConfigurationManager constructor called");
        Console.WriteLine($"[DEBUG] Configuration section 'Providers' exists: {configuration.GetSection("Providers").Exists()}");

        // Debug: Check if we can read the API key directly
        var openRouterApiKey = configuration["Providers:OpenRouter:ApiKey"];
        Console.WriteLine($"[DEBUG] Direct config read - OpenRouter API Key: '{openRouterApiKey}' (length: {openRouterApiKey?.Length})");

        _providerSettings = configuration.GetSection("Providers").Get<ProviderSettings>()
                           ?? throw new InvalidOperationException("Provider settings not found in configuration.");

        Console.WriteLine($"[DEBUG] ProviderConfigurationManager initialized successfully");
        Console.WriteLine($"[DEBUG] OpenRouter API Key from settings: '{_providerSettings.OpenRouter.ApiKey}' (length: {_providerSettings.OpenRouter.ApiKey?.Length})");
    }

    public ProviderConfig GetProviderConfig(string providerName)
    {
        Console.WriteLine($"[DEBUG] ProviderConfigurationManager.GetProviderConfig called for: {providerName}");

        // Debug: Check raw configuration values
        var apiKey = _configuration[$"Providers:{providerName}:ApiKey"];
        Console.WriteLine($"[RAW] Config path: Providers:{providerName}:ApiKey");
        Console.WriteLine($"[RAW] Raw value from config: '{apiKey}'");
        Console.WriteLine($"[RAW] Raw value length: {apiKey?.Length}");
        Console.WriteLine($"[RAW] Raw value starts with 'sk-or-v1-': {apiKey?.StartsWith("sk-or-v1-")}");

        var config = providerName.ToLower() switch
        {
            "openrouter" => _providerSettings.OpenRouter,
            "nanogpt" => _providerSettings.NanoGPT,
            _ => throw new KeyNotFoundException($"Provider '{providerName}' not found in configuration.")
        };

        Console.WriteLine($"[DEBUG] Provider config retrieved - API Key: {(string.IsNullOrEmpty(config.ApiKey) ? "NULL or EMPTY" : $"{config.ApiKey.Substring(0, Math.Min(10, config.ApiKey.Length))}... (length: {config.ApiKey.Length})")}");
        Console.WriteLine($"[DEBUG] Provider config retrieved - Endpoint: {config.Endpoint}");

        return config;
    }

    public ProviderSettings GetAllProviderSettings()
    {
        return _providerSettings;
    }
}