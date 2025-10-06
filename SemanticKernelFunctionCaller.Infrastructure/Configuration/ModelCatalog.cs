using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.ValueObjects;
using SemanticKernelFunctionCaller.Infrastructure.Mappers;
using Microsoft.Extensions.Options;

namespace SemanticKernelFunctionCaller.Infrastructure.Configuration;

public class ModelCatalog : IModelCatalog
{
    private readonly ProviderSettings _providerSettings;

    public ModelCatalog(IOptions<ProviderSettings> providerSettings)
    {
        _providerSettings = providerSettings.Value ?? throw new InvalidOperationException("Provider settings cannot be null.");
    }

    public IEnumerable<ModelConfiguration> GetModels(string providerName)
    {
        var providerConfig = providerName.ToLower() switch
        {
            "openrouter" => _providerSettings.OpenRouter,
            "nanogpt" => _providerSettings.NanoGPT,
            _ => null
        };

        if (providerConfig?.Models == null)
        {
            return Enumerable.Empty<ModelConfiguration>();
        }

        return providerConfig.Models.Select(ModelConverter.ToModelConfiguration);
    }
}
