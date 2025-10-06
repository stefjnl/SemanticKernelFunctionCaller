using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetProviderModelsUseCase
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

    private static ModelInfoDto MapToDto(ModelConfiguration configuration)
    {
        return new ModelInfoDto
        {
            Id = configuration.Id,
            DisplayName = configuration.DisplayName
        };
    }
}
