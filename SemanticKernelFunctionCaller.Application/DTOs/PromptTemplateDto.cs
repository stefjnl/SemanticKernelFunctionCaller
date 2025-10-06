namespace SemanticKernelFunctionCaller.Application.DTOs;

public class PromptTemplateDto
{
    /// <summary>
    /// Template name/identifier
    /// </summary>
    public required string TemplateName { get; set; }

    /// <summary>
    /// Input variables dictionary (string key-value pairs)
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Optional execution settings (temperature, max tokens)
    /// </summary>
    public PromptExecutionSettings? ExecutionSettings { get; set; }
}

public class PromptExecutionSettings
{
    /// <summary>
    /// Temperature setting for the prompt execution
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Top-P sampling value
    /// </summary>
    public double? TopP { get; set; }
}