namespace ChatCompletionService.Domain.Entities;

public class ConversationContext
{
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    // Business logic for context management will be added later.
}