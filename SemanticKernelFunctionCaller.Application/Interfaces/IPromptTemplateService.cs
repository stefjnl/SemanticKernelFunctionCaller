namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Service for managing prompt templates
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>
    /// Gets a list of available template names
    /// </summary>
    /// <returns>List of template names</returns>
    Task<IEnumerable<string>> GetAvailableTemplatesAsync();
}