namespace SemanticKernelFunctionCaller.Application.DTOs;

/// <summary>
/// Represents a streaming chat update
/// </summary>
public class StreamingChatUpdate
{
    /// <summary>
    /// The content of the update (for content updates)
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// Indicates if this is the final update in the stream
    /// </summary>
    public bool IsFinal { get; set; }
    
    /// <summary>
    /// The type of update (content, function_call_start, function_call_complete, etc.)
    /// </summary>
    public string Type { get; set; } = "content";
    
    /// <summary>
    /// The name of the function being executed (for function execution updates)
    /// </summary>
    public string? FunctionName { get; set; }
    
    /// <summary>
    /// Additional metadata for the update
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
