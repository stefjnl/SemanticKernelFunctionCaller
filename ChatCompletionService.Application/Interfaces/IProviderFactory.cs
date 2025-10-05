using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.Enums;

namespace ChatCompletionService.Application.Interfaces;

public interface IProviderFactory
{
    IChatCompletionService CreateProvider(ProviderType provider, string modelId);
    IEnumerable<ProviderInfoDto> GetAvailableProviders();
    IEnumerable<ModelInfoDto> GetModelsForProvider(ProviderType provider);
}