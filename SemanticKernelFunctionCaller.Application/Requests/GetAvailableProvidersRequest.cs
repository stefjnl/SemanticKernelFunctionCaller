using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.Requests;

/// <summary>
/// Request for getting available providers
/// </summary>
public class GetAvailableProvidersRequest : IRequest<IEnumerable<ProviderInfoDto>>
{
}