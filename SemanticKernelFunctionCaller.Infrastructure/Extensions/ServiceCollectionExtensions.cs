using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Factories;
using SemanticKernelFunctionCaller.Infrastructure.Orchestration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;

namespace SemanticKernelFunctionCaller.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ProviderSettings>(configuration.GetSection("Providers"));
        services.Configure<SemanticKernelSettings>(configuration.GetSection("SemanticKernel"));
        
        // Register core services
        services.AddSingleton<IProviderConfigurationReader, ProviderConfigurationReader>();
        services.AddSingleton<IModelCatalog, ModelCatalog>();
        services.AddSingleton<IProviderFactory, ChatProviderFactory>();
        
        // Register Semantic Kernel services
        services.AddSingleton<PromptTemplateManager>();

        // Register the orchestration service
        services.AddScoped<IAIOrchestrationService, SemanticKernelOrchestrationService>();

        // Register simplified V2 use cases
        services.AddScoped<SendOrchestratedChatMessageUseCaseV2>();
        services.AddScoped<ExecutePromptTemplateUseCaseV2>();
        services.AddScoped<StreamOrchestratedChatMessageUseCaseV2>();
        
        // Configure IChatClient factory with telemetry
        services.AddSingleton<Func<string, string, string, IChatClient>>(serviceProvider =>
        {
            return (apiKey, modelId, endpoint) =>
            {
                var chatClient = new OpenAI.Chat.ChatClient(
                    modelId,
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

                return chatClient.AsIChatClient().AsBuilder()
                    .UseOpenTelemetry()
                    .Build();
            };
        });

        return services;
    }
}
