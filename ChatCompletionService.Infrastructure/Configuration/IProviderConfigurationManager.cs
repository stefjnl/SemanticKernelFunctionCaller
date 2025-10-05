using ChatCompletionService.Infrastructure.Configuration;

namespace ChatCompletionService.Infrastructure.Configuration;

public interface IProviderConfigurationManager
{
    ProviderConfig GetProviderConfig(string providerName);
    ProviderSettings GetAllProviderSettings();
}