using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.Enums;

namespace ChatCompletionService.Application.Interfaces;

public interface IModelConfigurationService
{
    IEnumerable<ProviderInfoDto> GetAvailableProviders();
    IEnumerable<ModelInfoDto> GetModelsForProvider(ProviderType provider);
}