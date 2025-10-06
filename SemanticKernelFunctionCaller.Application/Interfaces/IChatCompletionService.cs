using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.ValueObjects;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface ISemanticKernelFunctionCaller
{
    Task<ChatResponse> SendMessageAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
        
    IAsyncEnumerable<string> StreamMessageAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
        
    ProviderMetadata GetMetadata();
}
