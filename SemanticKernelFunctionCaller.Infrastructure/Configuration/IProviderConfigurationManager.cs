using SemanticKernelFunctionCaller.Infrastructure.Configuration;

namespace SemanticKernelFunctionCaller.Infrastructure.Configuration;

public interface IProviderConfigurationManager
{
    ProviderConfig GetProviderConfig(string providerName);
    ProviderSettings GetAllProviderSettings();
}
