using ChatCompletionService.Infrastructure.Configuration;

namespace ChatCompletionService.Infrastructure.Providers;

public class OpenRouterChatProvider : BaseChatProvider
{
    public OpenRouterChatProvider(ProviderConfig config, string modelId)
        : base(config, modelId, "OpenRouter")
    {
    }
}