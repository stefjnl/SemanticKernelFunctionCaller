namespace SemanticKernelFunctionCaller.Application.DTOs;

/// <summary>
/// Represents a chat response with optional function call metadata
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// The content of the chat response
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// The model ID used for the response
    /// </summary>
    public required string ModelId { get; set; }
    
    /// <summary>
    /// The provider ID used for the response
    /// </summary>
    public required string ProviderId { get; set; }
    
    /// <summary>
    /// Metadata about functions executed during the orchestration
    /// </summary>
    public List<FunctionCallMetadata>? FunctionsExecuted { get; set; }
}

/// <summary>
/// Metadata about a function call executed during orchestration
/// </summary>
public class FunctionCallMetadata
{
    /// <summary>
    /// The name of the function that was executed
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    
    /// <summary>
    /// The arguments passed to the function
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();
    
    /// <summary>
    /// The result returned by the function
    /// </summary>
    public string Result { get; set; } = string.Empty;
    
    /// <summary>
    /// The execution time of the function
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
}
