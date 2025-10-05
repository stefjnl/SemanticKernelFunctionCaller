using ChatCompletionService.Infrastructure.Configuration;

namespace ChatCompletionService.Infrastructure.Providers;

public class NanoGptChatProvider : BaseChatProvider
{
    public NanoGptChatProvider(ProviderConfig config, string modelId)
        : base(config, modelId, "NanoGPT")
    {
    }
}