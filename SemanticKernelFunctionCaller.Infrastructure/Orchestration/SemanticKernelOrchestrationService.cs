using Microsoft.Extensions.Logging;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.Functions;
using DomainChatMessage = SemanticKernelFunctionCaller.Domain.Entities.ChatMessage;

namespace SemanticKernelFunctionCaller.Infrastructure.Orchestration;

public class SemanticKernelOrchestrationService : IAIOrchestrationService
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<SemanticKernelOrchestrationService> _logger;
    private readonly SemanticKernelSettings _semanticKernelSettings;

    public SemanticKernelOrchestrationService(
        IProviderFactory providerFactory,
        ILogger<SemanticKernelOrchestrationService> logger,
        IOptions<SemanticKernelSettings> semanticKernelSettings)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        _semanticKernelSettings = semanticKernelSettings.Value;
    }

    public async Task<ChatResponseDto> SendOrchestratedMessageAsync(
        IEnumerable<DomainChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create Semantic Kernel with our provider
            var kernel = CreateKernel();
            
            // Convert domain messages to Semantic Kernel chat history
            var chatHistory = ConvertToChatHistory(messages);

            // Configure execution settings for automatic function calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Track function calls for metadata
            var functionCalls = new List<FunctionCallMetadata>();
            
            // Add a filter to capture function invocation metadata
            kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilterAdapter(functionCalls, _logger));

            // Get response from Semantic Kernel with function calling
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletionService.GetChatMessageContentsAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: kernel,
                cancellationToken: cancellationToken);

            // No need to unsubscribe from filters

            return new ChatResponseDto
            {
                Content = result.FirstOrDefault()?.Content ?? string.Empty,
                ModelId = _semanticKernelSettings.DefaultModel,
                ProviderId = _semanticKernelSettings.DefaultProvider,
                FunctionsExecuted = functionCalls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendOrchestratedMessageAsync");
            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChatUpdate> StreamOrchestratedMessageAsync(
        IEnumerable<DomainChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Create Semantic Kernel with our provider
        var kernel = CreateKernel();
        
        // Convert domain messages to Semantic Kernel chat history
        var chatHistory = ConvertToChatHistory(messages);

        // Configure execution settings for automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        IAsyncEnumerable<StreamingChatMessageContent>? streamingResult = null;
        Exception? caughtException = null;

        try
        {
            // Get streaming response from Semantic Kernel with function calling
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            streamingResult = chatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: kernel,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StreamOrchestratedMessageAsync");
            caughtException = ex;
        }

        if (caughtException != null)
        {
            yield return new StreamingChatUpdate
            {
                Content = $"Error: {caughtException.Message}",
                IsFinal = true,
                Type = "error"
            };
            yield break;
        }

        if (streamingResult != null)
        {
            await foreach (var update in streamingResult.WithCancellation(cancellationToken))
            {
                // Check if this update has content
                if (!string.IsNullOrEmpty(update.Content))
                {
                    yield return new StreamingChatUpdate
                    {
                        Content = update.Content,
                        Type = "content",
                        IsFinal = false
                    };
                }
            }
        }

        yield return new StreamingChatUpdate { Content = string.Empty, IsFinal = true, Type = "content" };
    }

    public async Task<ChatResponseDto> ExecutePromptTemplateAsync(
        PromptTemplateDto templateRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create Semantic Kernel with our provider
            var kernel = CreateKernel();

            // Get the prompt template from settings
            var templateName = templateRequest.TemplateName ?? "default";
            var promptTemplate = _semanticKernelSettings.PromptTemplates.GetValueOrDefault(
                templateName,
                "{{input}}");

            // Replace variables in the template
            foreach (var variable in templateRequest.Variables)
            {
                promptTemplate = promptTemplate.Replace($"{{{{{variable.Key}}}}}", variable.Value.ToString());
            }

            // Configure execution settings
            var executionSettings = new OpenAIPromptExecutionSettings();
            if (templateRequest.ExecutionSettings?.Temperature.HasValue == true)
                executionSettings.Temperature = (float)templateRequest.ExecutionSettings.Temperature.Value;
            if (templateRequest.ExecutionSettings?.MaxTokens.HasValue == true)
                executionSettings.MaxTokens = templateRequest.ExecutionSettings.MaxTokens.Value;

            // Track function calls for metadata
            var functionCalls = new List<FunctionCallMetadata>();
            
            // Add a filter to capture function invocation metadata
            kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilterAdapter(functionCalls, _logger));

            // Execute the prompt template
            var arguments = new KernelArguments(executionSettings);
            var result = await kernel.InvokePromptAsync(promptTemplate, arguments);

            // No need to unsubscribe from filters

            return new ChatResponseDto
            {
                Content = result.ToString() ?? string.Empty,
                ModelId = _semanticKernelSettings.DefaultModel,
                ProviderId = _semanticKernelSettings.DefaultProvider,
                FunctionsExecuted = functionCalls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecutePromptTemplateAsync");
            throw;
        }
    }


    private Kernel CreateKernel()
    {
        // Create Semantic Kernel builder
        var builder = Kernel.CreateBuilder();

        // Get provider configuration
        var provider = _providerFactory.CreateProvider(_semanticKernelSettings.DefaultProvider, _semanticKernelSettings.DefaultModel);

        // Try to get the IChatClient from the provider
        if (provider is IChatClientProvider chatClientProvider)
        {
            var chatClient = chatClientProvider.GetChatClient();
            // Register the chat client directly with the kernel
            // Note: This is a simplified approach for the rollback
        }

        return builder.Build();
    }

    private ChatHistory ConvertToChatHistory(IEnumerable<DomainChatMessage> messages)
    {
        var chatHistory = new ChatHistory();
        
        foreach (var message in messages)
        {
            var role = message.Role.ToString().ToLowerInvariant() switch
            {
                "user" => AuthorRole.User,
                "assistant" => AuthorRole.Assistant,
                "system" => AuthorRole.System,
                _ => AuthorRole.User
            };
            
            chatHistory.Add(new Microsoft.SemanticKernel.ChatMessageContent(role, message.Content));
        }
        
        return chatHistory;
    }

}

/// <summary>
/// Adapter class to capture function invocation metadata using Semantic Kernel filters
/// </summary>
public class FunctionInvocationFilterAdapter : IFunctionInvocationFilter
{
    private readonly List<FunctionCallMetadata> _functionCalls;
    private readonly ILogger _logger;

    public FunctionInvocationFilterAdapter(List<FunctionCallMetadata> functionCalls, ILogger logger)
    {
        _functionCalls = functionCalls;
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(Microsoft.SemanticKernel.FunctionInvocationContext context, Func<Microsoft.SemanticKernel.FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Log function invocation
            _logger.LogInformation("Function invoked: {FunctionName}", context.Function.Name);
            
            // Call the next filter in the chain
            await next(context);
            
            stopwatch.Stop();
            
            // Capture function call metadata
            var metadata = new FunctionCallMetadata
            {
                FunctionName = $"{context.Function.PluginName}-{context.Function.Name}",
                Arguments = context.Arguments.ToDictionary(a => a.Key, a => (object)(a.Value?.ToString() ?? string.Empty)),
                Result = context.Result?.ToString() ?? string.Empty,
                ExecutionTime = stopwatch.Elapsed
            };
            
            lock (_functionCalls) // Thread-safe access
            {
                _functionCalls.Add(metadata);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during function invocation: {FunctionName}", context.Function.Name);
            throw;
        }
    }
}