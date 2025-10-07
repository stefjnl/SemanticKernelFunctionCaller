using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetAvailableProvidersUseCase(IProviderConfigurationReader providerReader) : IGetAvailableProvidersUseCase
{
    public IEnumerable<ProviderInfoDto> Execute()
    {
        return providerReader.GetProviders().Select(MapToDto);
    }

    private static ProviderInfoDto MapToDto(ProviderMetadata metadata)
    {
        return new ProviderInfoDto
        {
            Id = metadata.Id,
            DisplayName = metadata.DisplayName
        };
    }
}