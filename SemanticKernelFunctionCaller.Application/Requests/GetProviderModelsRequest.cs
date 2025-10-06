using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.Requests;

/// <summary>
/// Request for getting provider models
/// </summary>
public class GetProviderModelsRequest : IRequest<IEnumerable<ModelInfoDto>>
{
    /// <summary>
    /// The provider ID
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;
}