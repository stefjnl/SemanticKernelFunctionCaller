using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.Requests;

/// <summary>
/// Request for sending a chat message
/// </summary>
public class SendChatMessageRequest : IRequest<ChatResponseDto>
{
    /// <summary>
    /// The chat request
    /// </summary>
    public ChatRequestDto Request { get; set; } = new();
}