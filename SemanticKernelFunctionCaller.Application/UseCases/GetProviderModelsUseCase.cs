using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetProviderModelsUseCase : IGetProviderModelsUseCase
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

    private static ModelInfoDto MapToDto(ModelConfiguration config)
    {
        return new ModelInfoDto
        {
            Id = config.Id,
            DisplayName = config.DisplayName
        };
    }
}