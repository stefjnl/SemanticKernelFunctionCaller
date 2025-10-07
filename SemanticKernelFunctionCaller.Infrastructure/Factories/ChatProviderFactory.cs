using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.Enums;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Providers;

namespace SemanticKernelFunctionCaller.Infrastructure.Factories;

public class ChatProviderFactory : IProviderFactory
{
    private readonly ILogger<ChatProviderFactory> _logger;
    private readonly IOptions<ProviderSettings> _providerSettings;
    private readonly Func<string, string, string, IChatClient> _chatClientFactory;
        private readonly Dictionary<ProviderType, Func<string, ISemanticKernelFunctionCaller>> _providerFactories;
        private readonly IOptions<ChatSettings> _chatSettings;
    
        public ChatProviderFactory(
            IOptions<ProviderSettings> providerSettings,
            ILogger<ChatProviderFactory> logger,
            ILoggerFactory loggerFactory,
            Func<string, string, string, IChatClient> chatClientFactory,
            IOptions<ChatSettings> chatSettings)
        {
            _logger = logger;
            _providerSettings = providerSettings;
            _chatSettings = chatSettings;
            _chatClientFactory = chatClientFactory;
            
            var configurableLogger = loggerFactory.CreateLogger<ConfigurableOpenAIChatProvider>();

        _providerFactories = new Dictionary<ProviderType, Func<string, ISemanticKernelFunctionCaller>>
        {
            {
                ProviderType.OpenRouter, (modelId) =>
                {
                    var config = providerSettings.Value.OpenRouter;
                                        var systemPrompt = config.SystemPrompt
                                            ?? _chatSettings.Value.DefaultSystemPrompt;
                                        
                                        return new ConfigurableOpenAIChatProvider(
                                            config.ApiKey,
                                            modelId,
                                            config.Endpoint,
                                            "OpenRouter",
                                            configurableLogger,
                                            _chatClientFactory,
                                            _chatSettings.Value.EnableSystemPrompt ? systemPrompt : null);
                }
            },
            {
                ProviderType.NanoGPT, (modelId) =>
                {
                    var config = providerSettings.Value.NanoGPT;
                                        var systemPrompt = config.SystemPrompt
                                            ?? _chatSettings.Value.DefaultSystemPrompt;
                                        
                                        return new ConfigurableOpenAIChatProvider(
                                            config.ApiKey,
                                            modelId,
                                            config.Endpoint,
                                            "NanoGpt",
                                            configurableLogger,
                                            _chatClientFactory,
                                            _chatSettings.Value.EnableSystemPrompt ? systemPrompt : null);
                }
            }
        };
    }

    public ISemanticKernelFunctionCaller CreateProvider(string providerName, string modelId)
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
