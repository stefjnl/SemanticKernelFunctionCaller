using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetAvailableProvidersUseCase : IGetAvailableProvidersUseCase
{
    private readonly IProviderConfigurationReader _providerReader;

    public GetAvailableProvidersUseCase(IProviderConfigurationReader providerReader)
    {
        _providerReader = providerReader;
    }

    public IEnumerable<ProviderInfoDto> Execute()
    {
        return _providerReader.GetProviders().Select(MapToDto);
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