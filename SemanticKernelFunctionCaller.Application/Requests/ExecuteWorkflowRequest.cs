using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.Requests;

/// <summary>
/// Request for executing a workflow
/// </summary>
public class ExecuteWorkflowRequest : IRequest<ChatResponseDto>
{
    /// <summary>
    /// The workflow request
    /// </summary>
    public WorkflowRequestDto Request { get; set; } = new();
}