namespace ChatCompletionService.Infrastructure.Configuration;

public class ProviderSettings
{
    public required ProviderConfig OpenRouter { get; set; }
    public required ProviderConfig NanoGPT { get; set; }
}

public class ProviderConfig
{
    public required string ApiKey { get; set; }
    public required string Endpoint { get; set; }
    public required List<ModelInfo> Models { get; set; }
}

public class ModelInfo
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
}