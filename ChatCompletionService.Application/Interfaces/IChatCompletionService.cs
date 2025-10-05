using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.Entities;
using ChatCompletionService.Domain.ValueObjects;

namespace ChatCompletionService.Application.Interfaces;

public interface IChatCompletionService
{
    Task<Domain.Entities.ChatResponse> SendMessageAsync(
        ChatRequestDto request, 
        CancellationToken cancellationToken = default);
        
    IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(
        ChatRequestDto request, 
        CancellationToken cancellationToken = default);
        
    ProviderMetadata GetMetadata();
}