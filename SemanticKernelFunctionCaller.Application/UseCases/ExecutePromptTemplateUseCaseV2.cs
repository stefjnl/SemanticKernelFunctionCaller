using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Simplified use case for executing prompt templates
/// </summary>
public class ExecutePromptTemplateUseCaseV2
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly ILogger<ExecutePromptTemplateUseCaseV2> _logger;

    public ExecutePromptTemplateUseCaseV2(
        IAIOrchestrationService orchestrationService,
        ILogger<ExecutePromptTemplateUseCaseV2> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a prompt template with the provided variables
    /// </summary>
    /// <param name="templateRequest">Template request DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response DTO</returns>
    public async Task<ChatResponseDto> ExecuteAsync(PromptTemplateDto templateRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            // Call the orchestration service directly
            var result = await _orchestrationService.ExecutePromptTemplateAsync(templateRequest, cancellationToken);

            _logger.LogInformation("Successfully executed prompt template: {TemplateName}", templateRequest.TemplateName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prompt template execution failed for template: {TemplateName}", templateRequest.TemplateName);
            throw; // Let controller/middleware handle error response
        }
    }
}