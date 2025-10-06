using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.Requests;

/// <summary>
/// Request for executing a prompt template
/// </summary>
public class ExecutePromptTemplateRequest : IRequest<ChatResponseDto>
{
    /// <summary>
    /// The prompt template request
    /// </summary>
    public PromptTemplateDto Request { get; set; } = new();
}