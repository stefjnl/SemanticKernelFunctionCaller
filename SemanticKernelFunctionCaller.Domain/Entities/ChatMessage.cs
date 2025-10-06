namespace SemanticKernelFunctionCaller.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public required Enums.ChatRole Role { get; set; } // "User", "Assistant", "System"
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; }
}
