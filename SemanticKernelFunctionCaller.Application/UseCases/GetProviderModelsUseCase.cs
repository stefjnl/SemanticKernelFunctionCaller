using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Requests;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetProviderModelsUseCase : IGetProviderModelsUseCase, IRequestHandler<GetProviderModelsRequest, IEnumerable<ModelInfoDto>>
{
    private readonly IModelCatalog _modelCatalog;

    public GetProviderModelsUseCase(IModelCatalog modelCatalog)
    {
        _modelCatalog = modelCatalog;
    }

    public IEnumerable<ModelInfoDto> Execute(string providerId)
    {
        return _modelCatalog.GetModels(providerId).Select(MapToDto);
    }

    public Task<IEnumerable<ModelInfoDto>> Handle(GetProviderModelsRequest request, CancellationToken cancellationToken = default)
    {
        var result = Execute(request.ProviderId);
        return Task.FromResult(result);
    }

    private static ModelInfoDto MapToDto(ModelConfiguration config)
    {
        return new ModelInfoDto
        {
            Id = config.Id,
            DisplayName = config.DisplayName
        };
    }
}