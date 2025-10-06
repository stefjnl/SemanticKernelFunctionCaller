using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Requests;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetAvailableProvidersUseCase : IGetAvailableProvidersUseCase, IRequestHandler<GetAvailableProvidersRequest, IEnumerable<ProviderInfoDto>>
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

    public Task<IEnumerable<ProviderInfoDto>> Handle(GetAvailableProvidersRequest request, CancellationToken cancellationToken = default)
    {
        var result = Execute();
        return Task.FromResult(result);
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