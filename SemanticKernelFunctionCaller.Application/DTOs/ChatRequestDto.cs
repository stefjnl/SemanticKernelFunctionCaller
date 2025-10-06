namespace SemanticKernelFunctionCaller.Application.DTOs;

public class ChatRequestDto
{
    public required string ProviderId { get; set; }
    public required string ModelId { get; set; }
    public required List<MessageDto> Messages { get; set; }
}
