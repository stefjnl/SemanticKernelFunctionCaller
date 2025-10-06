using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface ISendChatMessageUseCase
{
    Task<ChatResponseDto> ExecuteAsync(ChatRequestDto request);
}