using ChatCompletionService.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System.ClientModel;

namespace ChatCompletionService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenRouterChatClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection("Providers:OpenRouter");

        services.AddChatClient(services => {
            var chatClient = new OpenAI.Chat.ChatClient(
                config["DefaultModel"] ?? "openai/gpt-3.5-turbo",
                new ApiKeyCredential(config["ApiKey"] ?? ""),
                new OpenAIClientOptions { Endpoint = new Uri(config["Endpoint"] ?? "https://openrouter.ai/api/v1") });

            return chatClient.AsIChatClient().AsBuilder()
                .UseOpenTelemetry()
                .Build(services);
        });

        return services;
    }
}