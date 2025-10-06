using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;

namespace SemanticKernelFunctionCaller.Infrastructure.Orchestration;

/// <summary>
/// Manages prompt templates for the application
/// </summary>
public class PromptTemplateManager
{
    private readonly ILogger<PromptTemplateManager> _logger;
    private readonly SemanticKernelSettings _settings;
    private readonly ConcurrentDictionary<string, PromptTemplate> _templateCache;

    public PromptTemplateManager(
        ILogger<PromptTemplateManager> logger,
        IOptions<SemanticKernelSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _templateCache = new ConcurrentDictionary<string, PromptTemplate>();
    }

    /// <summary>
    /// Loads a prompt template by name
    /// </summary>
    /// <param name="templateName">The name of the template to load</param>
    /// <returns>The prompt template</returns>
    public async Task<PromptTemplate> LoadTemplateAsync(string templateName)
    {
        // Check cache first
        if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        // Try to load from embedded resources first
        var template = await LoadFromEmbeddedResourceAsync(templateName);
        
        // If not found in embedded resources, try to load from file system
        if (template == null)
        {
            template = await LoadFromFileSystemAsync(templateName);
        }

        // If still not found, try to load from configuration
        if (template == null)
        {
            template = await LoadFromConfigurationAsync(templateName);
        }

        // If still not found, throw an exception
        if (template == null)
        {
            throw new InvalidOperationException($"Prompt template '{templateName}' not found.");
        }

        // Cache the template
        _templateCache.TryAdd(templateName, template);
        
        return template;
    }

    /// <summary>
    /// Renders a prompt template with the provided variables
    /// </summary>
    /// <param name="template">The template to render</param>
    /// <param name="variables">The variables to substitute</param>
    /// <returns>The rendered prompt</returns>
    /// <exception cref="ArgumentException">Thrown when required variables are missing</exception>
    public async Task<string> RenderTemplateAsync(PromptTemplate template, IDictionary<string, object?> variables)
    {
        try
        {
            // Validate template variables before rendering
            ValidateTemplateVariables(template, variables);
            
            // Simple template rendering - replace variables directly
            var result = template.Content;
            foreach (var variable in variables.Where(v => v.Value != null))
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                result = result.Replace(placeholder, variable.Value.ToString());
            }
            return Task.FromResult(result);
        }
        catch (ArgumentException)
        {
            // Re-throw argument exceptions as-is (they're validation errors)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template");
            throw;
        }
    }

    /// <summary>
    /// Validates that all required variables are provided for a template
    /// </summary>
    /// <param name="template">The template to validate</param>
    /// <param name="variables">The variables to check</param>
    /// <exception cref="ArgumentException">Thrown when required variables are missing</exception>
    public void ValidateTemplateVariables(PromptTemplate template, IDictionary<string, object?> variables)
    {
        // Extract variable names from the template content
        var requiredVariables = ExtractRequiredVariables(template.Content);
        
        // Check if all required variables are provided
        var missingVariables = requiredVariables.Except(variables.Keys).ToList();
        
        if (missingVariables.Any())
        {
            throw new ArgumentException($"Missing required variables: {string.Join(", ", missingVariables)}");
        }
    }

    /// <summary>
    /// Extracts required variable names from template content
    /// </summary>
    /// <param name="templateContent">The template content to analyze</param>
    /// <returns>List of required variable names</returns>
    private IEnumerable<string> ExtractRequiredVariables(string templateContent)
    {
        // Simple regex to find variable placeholders like {{$variableName}}
        var variableRegex = new Regex(@"\{\{\$(\w+)\}\}");
        var matches = variableRegex.Matches(templateContent);
        
        return matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct();
    }

    /// <summary>
    /// Gets a list of available template names
    /// </summary>
    /// <returns>List of template names</returns>
    public async Task<IEnumerable<string>> GetAvailableTemplatesAsync()
    {
        var templates = new List<string>();
        
        // Add embedded resource templates
        var embeddedTemplates = await GetEmbeddedResourceTemplatesAsync();
        templates.AddRange(embeddedTemplates);
        
        // Add file system templates
        var fileTemplates = await GetFileSystemTemplatesAsync();
        templates.AddRange(fileTemplates);
        
        // Add configuration templates
        var configTemplates = _settings.PromptTemplates?.Keys ?? Enumerable.Empty<string>();
        templates.AddRange(configTemplates);
        
        return templates.Distinct();
    }

    private async Task<PromptTemplate?> LoadFromEmbeddedResourceAsync(string templateName)
    {
        try
        {
            // In a full implementation, this would load from embedded resources
            // For now, we'll return null to indicate not found
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error loading template from embedded resources: {TemplateName}", templateName);
            return null;
        }
    }

    private async Task<PromptTemplate?> LoadFromFileSystemAsync(string templateName)
    {
        try
        {
            // Construct file path
            var templatePath = Path.Combine("PromptTemplates", $"{templateName}.prompt");
            
            // Check if file exists
            if (!File.Exists(templatePath))
            {
                return null;
            }

            // Read template content
            var content = await File.ReadAllTextAsync(templatePath);
            
            // Create and return prompt template
            return new PromptTemplate(content);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error loading template from file system: {TemplateName}", templateName);
            return null;
        }
    }

    private Task<PromptTemplate?> LoadFromConfigurationAsync(string templateName)
    {
        try
        {
            // Check if template exists in configuration
            if (_settings.PromptTemplates?.ContainsKey(templateName) == true)
            {
                var content = _settings.PromptTemplates[templateName];
                return Task.FromResult<PromptTemplate?>(new PromptTemplate(content));
            }
            
            return Task.FromResult<PromptTemplate?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error loading template from configuration: {TemplateName}", templateName);
            return Task.FromResult<PromptTemplate?>(null);
        }
    }

    private async Task<IEnumerable<string>> GetEmbeddedResourceTemplatesAsync()
    {
        // In a full implementation, this would list embedded resource templates
        await Task.CompletedTask;
        return new List<string>();
    }

    private async Task<IEnumerable<string>> GetFileSystemTemplatesAsync()
    {
        try
        {
            var templatesDir = "PromptTemplates";
            
            if (!Directory.Exists(templatesDir))
            {
                return new List<string>();
            }

            var templateFiles = Directory.GetFiles(templatesDir, "*.prompt");
            var templateNames = templateFiles.Select(Path.GetFileNameWithoutExtension);
            
            return templateNames;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting file system templates");
            return new List<string>();
        }
    }
}

public class PromptTemplate
{
    public string Content { get; }
    
    public PromptTemplate(string content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}