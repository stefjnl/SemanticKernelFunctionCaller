using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Application.Services;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Factories;
using SemanticKernelFunctionCaller.Infrastructure.Interfaces;
using SemanticKernelFunctionCaller.Infrastructure.Orchestration;
using SemanticKernelFunctionCaller.Infrastructure.Plugins;
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
        services.AddSingleton<IMediator, Mediator>();
        
        // Register plugin providers
        services.AddSingleton<IKernelPluginProvider, WeatherPluginProvider>();
        services.AddSingleton<IKernelPluginProvider, DateTimePluginProvider>();
        
        // Register the orchestration service with plugin providers
        services.AddScoped<IAIOrchestrationService>(serviceProvider =>
        {
            var providerFactory = serviceProvider.GetRequiredService<IProviderFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<SemanticKernelOrchestrationService>>();
            var semanticKernelSettings = serviceProvider.GetRequiredService<IOptions<SemanticKernelSettings>>();
            var pluginProviders = serviceProvider.GetServices<IKernelPluginProvider>();
            
            return new SemanticKernelOrchestrationService(
                providerFactory,
                logger,
                semanticKernelSettings,
                pluginProviders);
        });
        
        // Register use cases with logging
        services.AddScoped<SendOrchestratedChatMessageUseCase>();
        services.AddScoped<StreamOrchestratedChatMessageUseCase>();
        services.AddScoped<ExecutePromptTemplateUseCase>();
        services.AddScoped<ExecuteWorkflowUseCase>();
        
        // Register Mediator handlers
        services.AddScoped<IRequestHandler<GetAvailableProvidersRequest, IEnumerable<ProviderInfoDto>>, GetAvailableProvidersUseCase>();
        services.AddScoped<IRequestHandler<GetProviderModelsRequest, IEnumerable<ModelInfoDto>>, GetProviderModelsUseCase>();
        services.AddScoped<IRequestHandler<SendChatMessageRequest, ChatResponseDto>, SendChatMessageUseCase>();
        services.AddScoped<IRequestHandler<SendOrchestratedChatMessageRequest, ChatResponseDto>, SendOrchestratedChatMessageUseCase>();
        services.AddScoped<IRequestHandler<ExecutePromptTemplateRequest, ChatResponseDto>, ExecutePromptTemplateUseCase>();
        services.AddScoped<IRequestHandler<ExecuteWorkflowRequest, ChatResponseDto>, ExecuteWorkflowUseCase>();
        
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
