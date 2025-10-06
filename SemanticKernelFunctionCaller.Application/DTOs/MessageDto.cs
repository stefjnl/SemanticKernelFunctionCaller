using SemanticKernelFunctionCaller.Domain.Enums;

namespace SemanticKernelFunctionCaller.Application.DTOs;

public class MessageDto
{
    public ChatRole Role { get; set; }
    public required string Content { get; set; }
}
