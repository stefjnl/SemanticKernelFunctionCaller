using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.Enums;
using System.Runtime.CompilerServices;

namespace ChatCompletionService.Application.UseCases;

public class StreamChatMessageUseCase
{
    private readonly IProviderFactory _providerFactory;

    public StreamChatMessageUseCase(IProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async IAsyncEnumerable<StreamingChatUpdate> ExecuteAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var providerType = Enum.Parse<ProviderType>(request.ProviderId);
        var provider = _providerFactory.CreateProvider(providerType.ToString(), request.ModelId);

        await foreach (var update in provider.StreamMessageAsync(request, cancellationToken))
        {
            yield return update;
        }
    }
}