using ChatCompletionService.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChatCompletionService.API.HealthChecks;

public class ProviderHealthCheck : IHealthCheck
{
    private readonly IProviderFactory _providerFactory;
    private readonly IProviderConfigurationReader _providerConfigurationReader;
    private readonly ILogger<ProviderHealthCheck> _logger;

    public ProviderHealthCheck(
        IProviderFactory providerFactory,
        IProviderConfigurationReader providerConfigurationReader,
        ILogger<ProviderHealthCheck> logger)
    {
        _providerFactory = providerFactory;
        _providerConfigurationReader = providerConfigurationReader;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = _providerConfigurationReader.GetProviders().ToList();
        var providerHealthData = new Dictionary<string, string>();
        var healthyProviders = 0;
        var totalProviders = providers.Count;

        if (totalProviders == 0)
        {
            return HealthCheckResult.Unhealthy(
                "No providers are configured.",
                data: new Dictionary<string, object>
                {
                    ["reason"] = "No providers found in configuration"
                });
        }

        foreach (var provider in providers)
        {
            try
            {
                // Try to create a provider instance to test availability
                // We'll use a dummy model ID since we're just testing connectivity
                var chatService = _providerFactory.CreateProvider(provider.Id, "test-model");

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
            return HealthCheckResult.Unhealthy(
                "All providers are unhealthy.",
                data: healthData);
        }
        else if (healthyProviders < totalProviders)
        {
            return HealthCheckResult.Degraded(
                $"{healthyProviders} of {totalProviders} providers are healthy.",
                data: healthData);
        }
        else
        {
            return HealthCheckResult.Healthy(
                "All providers are healthy.",
                data: healthData);
        }
    }
}