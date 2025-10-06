using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace SemanticKernelFunctionCaller.Infrastructure.Configuration;

public class ProviderConfigurationReader : IProviderConfigurationReader
{
    private readonly ProviderSettings _providerSettings;

    public ProviderConfigurationReader(IOptions<ProviderSettings> providerSettings)
    {
        _providerSettings = providerSettings.Value ?? throw new InvalidOperationException("Provider settings cannot be null.");
    }

    public IEnumerable<ProviderMetadata> GetProviders()
    {
        var providers = new List<ProviderMetadata>();

        if (_providerSettings.OpenRouter != null)
        {
            providers.Add(new ProviderMetadata
            {
                Id = "OpenRouter",
                DisplayName = "OpenRouter"
            });
        }

        if (_providerSettings.NanoGPT != null)
        {
            providers.Add(new ProviderMetadata
            {
                Id = "NanoGPT",
                DisplayName = "NanoGPT"
            });
        }

        return providers;
    }
}
