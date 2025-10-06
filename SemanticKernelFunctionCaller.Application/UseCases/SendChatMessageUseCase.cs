using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Enums;
using SemanticKernelFunctionCaller.Domain.Entities;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class SendChatMessageUseCase : ISendChatMessageUseCase
{
    private readonly IProviderFactory _providerFactory;

    public SendChatMessageUseCase(IProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task<ChatResponseDto> ExecuteAsync(ChatRequestDto request)
    {
        // Map DTO to domain models
        var messages = request.Messages.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content,
            // Note: We're not setting Id and Timestamp as they're not in the DTO
            // In a real implementation, we might want to generate these
        }).ToList();

        var providerType = Enum.Parse<ProviderType>(request.ProviderId);
        var provider = _providerFactory.CreateProvider(providerType.ToString(), request.ModelId);
        var response = await provider.SendMessageAsync(messages);
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