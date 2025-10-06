using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface IGetAvailableProvidersUseCase
{
    IEnumerable<ProviderInfoDto> Execute();
}