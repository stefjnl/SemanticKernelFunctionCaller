namespace SemanticKernelFunctionCaller.Domain.Entities;

public class ChatResponse
{
    public required ChatMessage Message { get; set; }
    public required string ModelUsed { get; set; }
    public required string ProviderUsed { get; set; }
    // Additional metadata like TokenUsage, ResponseTime can be added later.
}
