using ChatCompletionService.Domain.Enums;
using ChatCompletionService.Application.DTOs;
using Microsoft.Extensions.AI;
using System.Linq;
using Azure.AI.OpenAI;
using DomainChatMessage = ChatCompletionService.Domain.Entities.ChatMessage;
using ProviderChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace ChatCompletionService.Infrastructure.Mappers;

public static class ModelConverter
{
    public static ProviderChatMessage ToProviderMessage(DomainChatMessage domainMessage)
    {
        return new ProviderChatMessage(new Microsoft.Extensions.AI.ChatRole(domainMessage.Role.ToString()), domainMessage.Content);
    }

    public static ProviderChatMessage ToProviderMessage(MessageDto dtoMessage)
    {
        return new ProviderChatMessage(new Microsoft.Extensions.AI.ChatRole(dtoMessage.Role.ToString()), dtoMessage.Content);
    }

    public static DomainChatMessage ToDomainMessage(ProviderChatMessage providerMessage)
    {
        // Handle different content types safely
        var content = providerMessage.Contents.FirstOrDefault();
        string textContent = string.Empty;

        if (content is TextContent textContentPart)
        {
            textContent = textContentPart.Text ?? string.Empty;
        }
        else if (content != null)
        {
            // For other content types, convert to string representation
            textContent = content.ToString() ?? string.Empty;
        }

        return new DomainChatMessage
        {
            Id = Guid.NewGuid(),
            Role = (Domain.Enums.ChatRole)Enum.Parse(typeof(Domain.Enums.ChatRole), providerMessage.Role.ToString(), true),
            Content = textContent,
            Timestamp = DateTime.UtcNow
        };
    }
}