using ChatCompletionService.Domain.ValueObjects;

namespace ChatCompletionService.Application.Interfaces;

/// <summary>
/// Defines a contract for reading provider configurations.
/// </summary>
public interface IProviderConfigurationReader
{
    /// <summary>
    /// Retrieves a list of all available provider configurations.
    /// </summary>
    /// <returns>An enumerable of <see cref="ProviderMetadata"/>.</returns>
    IEnumerable<ProviderMetadata> GetProviders();
}