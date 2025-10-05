using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Infrastructure.Configuration;
using ChatCompletionService.Infrastructure.Factories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;

namespace ChatCompletionService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure ProviderSettings from appsettings.json
        services.Configure<ProviderSettings>(configuration.GetSection("Providers"));

        // Register the new services
        services.AddSingleton<IProviderConfigurationReader, ProviderConfigurationReader>();
        services.AddSingleton<IModelCatalog, ModelCatalog>();
        services.AddSingleton<IProviderFactory, ChatProviderFactory>();

        // Add a default ChatClient using OpenRouter for now.
        // This can be expanded to support more clients dynamically.
        services.AddHttpClient();
        services.AddChatClient(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderSettings>>().Value;
            var openRouterSettings = options.OpenRouter;

            if (openRouterSettings == null)
            {
                throw new InvalidOperationException("OpenRouter settings are not configured.");
            }

            var chatClient = new OpenAI.Chat.ChatClient(
                openRouterSettings.Models.FirstOrDefault()?.Id ?? "default-model",
                new ApiKeyCredential(openRouterSettings.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(openRouterSettings.Endpoint) });

            return chatClient.AsIChatClient().AsBuilder()
                .UseOpenTelemetry()
                .Build(serviceProvider);
        });


        return services;
    }
}