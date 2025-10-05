using ChatCompletionService.Domain.Entities;

namespace ChatCompletionService.Application.DTOs;

public class ChatRequestDto
{
    public required string ProviderId { get; set; }
    public required string ModelId { get; set; }
    public required List<ChatMessage> Messages { get; set; }
}