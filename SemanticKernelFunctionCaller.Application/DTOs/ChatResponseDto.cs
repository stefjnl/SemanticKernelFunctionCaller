namespace SemanticKernelFunctionCaller.Application.DTOs;

public class ChatResponseDto
{
    public required string Content { get; set; }
    public required string ModelId { get; set; }
    public required string ProviderId { get; set; }
}
