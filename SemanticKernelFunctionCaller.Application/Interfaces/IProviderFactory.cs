using SemanticKernelFunctionCaller.Application.DTOs;
namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface IProviderFactory
{
    ISemanticKernelFunctionCaller CreateProvider(string providerName, string modelId);
}
