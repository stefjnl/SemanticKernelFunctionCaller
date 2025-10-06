namespace SemanticKernelFunctionCaller.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Semantic Kernel integration
/// </summary>
public class SemanticKernelSettings
{
    /// <summary>
    /// Gets or sets the default provider to use for Semantic Kernel
    /// </summary>
    public string DefaultProvider { get; set; } = "OpenRouter";

    /// <summary>
    /// Gets or sets the default model to use for Semantic Kernel
    /// </summary>
    public string DefaultModel { get; set; } = "";

    /// <summary>
    /// Gets or sets the OpenRouter configuration for Semantic Kernel
    /// </summary>
    public required SemanticKernelProviderSettings OpenRouter { get; set; }

    /// <summary>
    /// Gets or sets the NanoGPT configuration for Semantic Kernel
    /// </summary>
    public required SemanticKernelProviderSettings NanoGPT { get; set; }
}

/// <summary>
/// Provider-specific settings for Semantic Kernel
/// </summary>
public class SemanticKernelProviderSettings
{
    /// <summary>
    /// Gets or sets the model ID for the provider
    /// </summary>
    public string ModelId { get; set; } = "";

    /// <summary>
    /// Gets or sets the API key for the provider
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Gets or sets the endpoint URL for the provider
    /// </summary>
    public string Endpoint { get; set; } = "";
}