using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface IGetProviderModelsUseCase
{
    IEnumerable<ModelInfoDto> Execute(string providerId);
}