# Prompt Template Management System

## Overview
The prompt template management system handles loading, validating, and executing prompt templates within the Semantic Kernel orchestration layer. This system resides in the Infrastructure layer and provides services for template-based AI interactions.

## Components

### PromptTemplateManager (Infrastructure Layer)
The main service responsible for managing prompt templates.

#### Responsibilities
1. Loading prompt templates from configuration or embedded resources
2. Variable substitution using Semantic Kernel's template engine
3. Template validation to ensure all required variables are provided
4. Caching compiled templates for performance optimization
5. Managing template versioning and metadata

#### Interface Design
```csharp
using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Infrastructure.Orchestration
{
    public interface IPromptTemplateManager
    {
        /// <summary>
        /// Loads a prompt template by name.
        /// </summary>
        /// <param name="templateName">The name of the template to load.</param>
        /// <returns>The prompt template DTO.</returns>
        Task<PromptTemplateDto> LoadTemplateAsync(string templateName);

        /// <summary>
        /// Gets all available prompt templates.
        /// </summary>
        /// <returns>List of available template names.</returns>
        Task<IEnumerable<string>> GetAvailableTemplatesAsync();

        /// <summary>
        /// Validates that all required variables are provided for a template.
        /// </summary>
        /// <param name="template">The template to validate.</param>
        /// <param name="variables">The variables to substitute.</param>
        /// <returns>True if all required variables are provided, false otherwise.</returns>
        bool ValidateVariables(PromptTemplateDto template, Dictionary<string, string> variables);

        /// <summary>
        /// Renders a prompt template with variable substitution.
        /// </summary>
        /// <param name="template">The template to render.</param>
        /// <param name="variables">The variables to substitute.</param>
        /// <returns>The rendered prompt content.</returns>
        Task<string> RenderTemplateAsync(PromptTemplateDto template, Dictionary<string, string> variables);

        /// <summary>
        /// Saves a new prompt template.
        /// </summary>
        /// <param name="template">The template to save.</param>
        Task SaveTemplateAsync(PromptTemplateDto template);
    }
}
```

### PromptTemplateManager Implementation
```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Infrastructure.Orchestration
{
    public class PromptTemplateManager : IPromptTemplateManager
    {
        private readonly ILogger<PromptTemplateManager> _logger;
        private readonly IMemoryCache _cache;
        private readonly SemanticKernelSettings _settings;

        public PromptTemplateManager(
            ILogger<PromptTemplateManager> logger,
            IMemoryCache cache,
            IOptions<SemanticKernelSettings> settings)
        {
            _logger = logger;
            _cache = cache;
            _settings = settings.Value;
        }

        public async Task<PromptTemplateDto> LoadTemplateAsync(string templateName)
        {
            // Try to get from cache first
            if (_cache.TryGetValue(templateName, out PromptTemplateDto cachedTemplate))
            {
                return cachedTemplate;
            }

            // Load from configuration or embedded resources
            if (_settings.PromptTemplates?.ContainsKey(templateName) == true)
            {
                var templateContent = _settings.PromptTemplates[templateName];
                var template = new PromptTemplateDto
                {
                    TemplateName = templateName,
                    TemplateContent = templateContent
                };

                // Cache the template
                _cache.Set(templateName, template, TimeSpan.FromHours(1));
                return template;
            }

            throw new InvalidOperationException($"Prompt template '{templateName}' not found.");
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync()
        {
            return _settings.PromptTemplates?.Keys ?? Enumerable.Empty<string>();
        }

        public bool ValidateVariables(PromptTemplateDto template, Dictionary<string, string> variables)
        {
            // Extract required variables from template content
            var requiredVariables = ExtractVariablesFromTemplate(template.TemplateContent);
            
            // Check if all required variables are provided
            return requiredVariables.All(variable => variables.ContainsKey(variable));
        }

        public async Task<string> RenderTemplateAsync(PromptTemplateDto template, Dictionary<string, string> variables)
        {
            // Validate variables first
            if (!ValidateVariables(template, variables))
            {
                throw new InvalidOperationException("Not all required variables provided for template.");
            }

            // Use Semantic Kernel's template engine for rendering
            // This is a simplified representation - actual implementation will use SK's templating
            var renderedContent = template.TemplateContent;
            foreach (var variable in variables)
            {
                renderedContent = renderedContent.Replace($"{{$${variable.Key}}}", variable.Value);
            }

            return renderedContent;
        }

        public async Task SaveTemplateAsync(PromptTemplateDto template)
        {
            // In a more advanced implementation, this might save to a database
            // For now, we'll just update the cache
            _cache.Set(template.TemplateName, template, TimeSpan.FromHours(1));
        }

        private IEnumerable<string> ExtractVariablesFromTemplate(string templateContent)
        {
            // Simple regex to extract variable placeholders
            // In practice, this would use Semantic Kernel's template parsing
            var regex = new Regex(@"\{\{?\$(\w+)\}?\}");
            var matches = regex.Matches(templateContent);
            return matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct();
        }
    }
}
```

## Sample Prompt Templates

### Summarize Conversation
```
Summarize the following conversation in 2-3 sentences, highlighting the key points discussed:

{{$conversation}}

Summary:
```

### Extract Entities
```
Extract structured information from the following text. Identify and list:
- Names of people mentioned
- Organizations referenced
- Key dates
- Important facts or figures

Text:
{{$inputText}}

Extracted Information:
```

### Rewrite Tone
```
Rewrite the following message in a {{$tone}} tone:

Original message:
{{$originalMessage}}

Rewritten message:
```

## Configuration Structure
```json
{
  "SemanticKernel": {
    "PromptTemplates": {
      "Summarize": "Summarize the following conversation in 2-3 sentences, highlighting the key points discussed:\n\n{{$conversation}}\n\nSummary:",
      "ExtractEntities": "Extract structured information from the following text. Identify and list:\n- Names of people mentioned\n- Organizations referenced\n- Key dates\n- Important facts or figures\n\nText:\n{{$inputText}}\n\nExtracted Information:",
      "RewriteTone": "Rewrite the following message in a {{$tone}} tone:\n\nOriginal message:\n{{$originalMessage}}\n\nRewritten message:"
    }
  }
}
```

## Integration Points

1. **SemanticKernelOrchestrationService**: Uses the PromptTemplateManager to load and render templates during execution.

2. **ExecutePromptTemplateUseCase**: Consumes the PromptTemplateManager through the IAIOrchestrationService to execute templates.

3. **API Controller**: Exposes an endpoint to retrieve available templates.

4. **Caching**: Uses IMemoryCache to store compiled templates for performance.

5. **Configuration**: Reads template definitions from application settings.

## Design Considerations

1. **Performance**: Templates are cached to avoid repeated parsing and loading.

2. **Flexibility**: Supports both configuration-based and potentially database-based template storage.

3. **Validation**: Ensures all required variables are provided before template execution.

4. **Versioning**: Template names act as identifiers that could support versioning in future enhancements.

5. **Error Handling**: Provides clear error messages when templates are not found or variables are missing.

6. **Extensibility**: The interface design allows for different template storage mechanisms.

7. **Clean Architecture**: Resides in Infrastructure layer with no Domain dependencies.