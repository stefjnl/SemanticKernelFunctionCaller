namespace ChatCompletionService.Domain.ValueObjects;

public class ProviderMetadata
{
    public required string ProviderName { get; set; }
    public List<ModelConfiguration> Models { get; set; } = new List<ModelConfiguration>();
}