namespace SemanticKernelFunctionCaller.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Semantic Kernel
/// </summary>
public class SemanticKernelSettings
{
    /// <summary>
    /// Default provider to use for Semantic Kernel operations
    /// </summary>
    public string DefaultProvider { get; set; } = "OpenRouter";

    /// <summary>
    /// Default model to use for Semantic Kernel operations
    /// </summary>
    public string DefaultModel { get; set; } = "google/gemini-2.5-flash-lite-preview-09-2025";

    /// <summary>
    /// List of enabled plugins
    /// </summary>
    public List<string> EnabledPlugins { get; set; } = new();

    /// <summary>
    /// Plugin criticality configuration
    /// </summary>
    public PluginCriticalitySettings PluginCriticality { get; set; } = new();

    /// <summary>
    /// Prompt templates configuration
    /// </summary>
    public Dictionary<string, string> PromptTemplates { get; set; } = new();
}

/// <summary>
/// Plugin criticality settings
/// </summary>
public class PluginCriticalitySettings
{
    /// <summary>
    /// List of critical plugins that will cause failure if they fail
    /// </summary>
    public List<string> Critical { get; set; } = new();
    
    /// <summary>
    /// List of non-critical plugins that can fail without causing overall failure
    /// </summary>
    public List<string> NonCritical { get; set; } = new();
}