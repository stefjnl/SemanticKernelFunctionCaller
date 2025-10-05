using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.ValueObjects;

namespace ChatCompletionService.Application.UseCases;

public class GetAvailableProvidersUseCase
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