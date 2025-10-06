using ChatCompletionService.Domain.Entities;
using ChatCompletionService.Domain.ValueObjects;

namespace ChatCompletionService.Application.Interfaces;

public interface IChatCompletionService
{
    Task<ChatResponse> SendMessageAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
        
    IAsyncEnumerable<string> StreamMessageAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
        
    ProviderMetadata GetMetadata();
}