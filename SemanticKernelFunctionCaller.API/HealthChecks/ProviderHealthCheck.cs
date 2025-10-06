using SemanticKernelFunctionCaller.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SemanticKernelFunctionCaller.API.HealthChecks;

public class ProviderHealthCheck : IHealthCheck
{
    private readonly IProviderFactory _providerFactory;
    private readonly IProviderConfigurationReader _providerConfigurationReader;
    private readonly IModelCatalog _modelCatalog;
    private readonly ILogger<ProviderHealthCheck> _logger;

    public ProviderHealthCheck(
        IProviderFactory providerFactory,
        IProviderConfigurationReader providerConfigurationReader,
        IModelCatalog modelCatalog,
        ILogger<ProviderHealthCheck> logger)
    {
        _providerFactory = providerFactory;
        _providerConfigurationReader = providerConfigurationReader;
        _modelCatalog = modelCatalog;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = _providerConfigurationReader.GetProviders().ToList();
        var providerHealthData = new Dictionary<string, string>();
        var healthyProviders = 0;
        var totalProviders = providers.Count;

        if (totalProviders == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "No providers are configured.",
                data: new Dictionary<string, object>
                {
                    ["reason"] = "No providers found in configuration"
                }));
        }

        foreach (var provider in providers)
        {
            try
            {
                // Get available models for this provider
                var models = _modelCatalog.GetModels(provider.Id).ToList();

                if (!models.Any())
                {
                    providerHealthData[provider.Id] = "Unhealthy";
                    _logger.LogWarning("Provider {ProviderId} has no available models", provider.Id);
                    continue;
                }

                // Try to create a provider instance to test availability
                var chatService = _providerFactory.CreateProvider(provider.Id, models.First().Id);

                if (chatService != null)
                {
                    providerHealthData[provider.Id] = "Healthy";
                    healthyProviders++;
                    _logger.LogDebug("Provider {ProviderId} is healthy", provider.Id);
                }
                else
                {
                    providerHealthData[provider.Id] = "Unhealthy";
                    _logger.LogWarning("Provider {ProviderId} returned null service", provider.Id);
                }
            }
            catch (Exception ex)
            {
                providerHealthData[provider.Id] = "Unhealthy";
                _logger.LogWarning(ex, "Provider {ProviderId} health check failed", provider.Id);
            }
        }

        var healthData = new Dictionary<string, object>
        {
            ["providers"] = providerHealthData,
            ["healthyCount"] = healthyProviders,
            ["totalCount"] = totalProviders
        };

        if (healthyProviders == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "All providers are unhealthy.",
                data: healthData));
        }
        else if (healthyProviders < totalProviders)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"{healthyProviders} of {totalProviders} providers are healthy.",
                data: healthData));
        }
        else
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "All providers are healthy.",
                data: healthData));
        }
    }
}
