using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.Enums;
using System.Runtime.CompilerServices;

namespace SemanticKernelFunctionCaller.Application.UseCases;

public class StreamChatMessageUseCase(IProviderFactory providerFactory) : IStreamChatMessageUseCase
{
    public async IAsyncEnumerable<StreamingChatUpdate> ExecuteAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Map DTO to domain models
        var messages = request.Messages.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content,
            // Note: We're not setting Id and Timestamp as they're not in the DTO
        }).ToList();

        var providerType = Enum.Parse<ProviderType>(request.ProviderId);
        var provider = providerFactory.CreateProvider(providerType.ToString(), request.ModelId);

        await foreach (var content in provider.StreamMessageAsync(messages, cancellationToken))
        {
            yield return new StreamingChatUpdate { Content = content, IsFinal = false };
        }

        yield return new StreamingChatUpdate { Content = string.Empty, IsFinal = true };
    }
}