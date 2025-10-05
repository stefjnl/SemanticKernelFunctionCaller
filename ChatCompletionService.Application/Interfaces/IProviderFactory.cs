using ChatCompletionService.Application.DTOs;
namespace ChatCompletionService.Application.Interfaces;

public interface IProviderFactory
{
    IChatCompletionService CreateProvider(string providerName, string modelId);
}