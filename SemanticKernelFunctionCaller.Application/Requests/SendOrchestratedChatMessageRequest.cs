using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.Requests;

/// <summary>
/// Request for sending an orchestrated chat message
/// </summary>
public class SendOrchestratedChatMessageRequest : IRequest<ChatResponseDto>
{
    /// <summary>
    /// The chat request
    /// </summary>
    public ChatRequestDto Request { get; set; } = new();
}