using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.Enums;
using ChatCompletionService.Domain.Entities;

namespace ChatCompletionService.Application.UseCases;

public class SendChatMessageUseCase
{
    private readonly IProviderFactory _providerFactory;

    public SendChatMessageUseCase(IProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task<ChatResponseDto> ExecuteAsync(ChatRequestDto request)
    {
        var providerType = Enum.Parse<ProviderType>(request.ProviderId);
        var provider = _providerFactory.CreateProvider(providerType.ToString(), request.ModelId);
        var response = await provider.SendMessageAsync(request);
        return MapToDto(response);
    }

    private static ChatResponseDto MapToDto(ChatResponse response)
    {
        return new ChatResponseDto
        {
            Content = response.Message.Content,
            ModelId = response.ModelUsed,
            ProviderId = response.ProviderUsed
        };
    }
}