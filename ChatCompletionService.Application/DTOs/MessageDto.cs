using ChatCompletionService.Domain.Enums;

namespace ChatCompletionService.Application.DTOs;

public class MessageDto
{
    public ChatRole Role { get; set; }
    public required string Content { get; set; }
}