namespace SemanticKernelFunctionCaller.Application.DTOs;

/// <summary>
/// Represents a streaming update for chat responses with tool support.
/// </summary>
public class ToolStreamingUpdate
{
    /// <summary>
    /// Gets or sets the type of update ("content" or "tool_call").
    /// </summary>
    public string Type { get; set; } = "content";

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the function name for tool calls.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Gets or sets whether this is the final update.
    /// </summary>
    public bool IsFinal { get; set; }
}