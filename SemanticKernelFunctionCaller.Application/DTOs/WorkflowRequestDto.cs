using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.DTOs;

public class WorkflowRequestDto
{
    /// <summary>
    /// Goal/objective description
    /// </summary>
    public required string Goal { get; set; }

    /// <summary>
    /// Context data for workflow
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Available function/plugin names to use
    /// </summary>
    public List<string> AvailableFunctions { get; set; } = new();

    /// <summary>
    /// Maximum steps allowed
    /// </summary>
    public int MaxSteps { get; set; } = 10;

    /// <summary>
    /// Optional execution settings
    /// </summary>
    public PromptExecutionSettings? ExecutionSettings { get; set; }
}