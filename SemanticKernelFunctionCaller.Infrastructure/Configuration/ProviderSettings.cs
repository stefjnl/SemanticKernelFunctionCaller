namespace SemanticKernelFunctionCaller.Infrastructure.Configuration;

public class ProviderSettings
{
    public required ProviderConfig OpenRouter { get; set; }
    public required ProviderConfig NanoGPT { get; set; }
}

public class ProviderConfig
{
    public required string ApiKey { get; set; }
    public required string Endpoint { get; set; }
    public string? SystemPrompt { get; set; }  // Add this
    public required List<ModelInfo> Models { get; set; }
}

// Add new settings class
public class ChatSettings
{
    public string DefaultSystemPrompt { get; set; } = "You are a helpful assistant.";
    public bool EnableSystemPrompt { get; set; } = true;
}

public class ModelInfo
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
}
