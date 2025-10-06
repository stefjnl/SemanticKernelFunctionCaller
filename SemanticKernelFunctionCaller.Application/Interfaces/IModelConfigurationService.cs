using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Enums;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface IModelConfigurationService
{
    IEnumerable<ProviderInfoDto> GetAvailableProviders();
    IEnumerable<ModelInfoDto> GetModelsForProvider(ProviderType provider);
}
