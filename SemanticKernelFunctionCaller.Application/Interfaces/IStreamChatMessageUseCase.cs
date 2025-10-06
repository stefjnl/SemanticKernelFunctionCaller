using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

public interface IStreamChatMessageUseCase
{
    IAsyncEnumerable<StreamingChatUpdate> ExecuteAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}