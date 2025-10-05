namespace ChatCompletionService.Application.DTOs;

public class StreamingChatUpdate
{
    public required string Content { get; set; }
    public bool IsFinal { get; set; }
    // Can be extended with token counts, etc. later
}