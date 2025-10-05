using ChatCompletionService.Domain.ValueObjects;

namespace ChatCompletionService.Application.Interfaces;

/// <summary>
/// Defines a contract for retrieving model information for a given provider.
/// </summary>
public interface IModelCatalog
{
    /// <summary>
    /// Gets the available models for a specific provider.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>An enumerable of <see cref="ModelConfiguration"/>.</returns>
    IEnumerable<ModelConfiguration> GetModels(string providerName);
}