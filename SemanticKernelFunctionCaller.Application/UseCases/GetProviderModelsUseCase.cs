using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class GetProviderModelsUseCase(IModelCatalog modelCatalog) : IGetProviderModelsUseCase
{
    public IEnumerable<ModelInfoDto> Execute(string providerId)
    {
        return modelCatalog.GetModels(providerId).Select(MapToDto);
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