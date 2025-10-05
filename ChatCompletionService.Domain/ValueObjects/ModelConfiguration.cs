namespace ChatCompletionService.Domain.ValueObjects;

public class ModelConfiguration
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public int ContextWindow { get; set; }
}