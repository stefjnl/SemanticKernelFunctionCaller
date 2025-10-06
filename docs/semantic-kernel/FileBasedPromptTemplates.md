# File-Based Prompt Template Management

## Overview
This document specifies the implementation of file-based prompt template storage to replace the JSON configuration approach. This provides better scalability, version control, and maintainability for prompt templates.

## Storage Structure

### Directory Layout
```
ChatCompletionService.Infrastructure/
├── PromptTemplates/
│   ├── Summarize.prompt
│   ├── ExtractEntities.prompt
│   ├── RewriteTone.prompt
│   └── manifest.json
└── Orchestration/
    └── PromptTemplateManager.cs
```

### Template Files

#### Summarize.prompt
```
Summarize the following conversation in 2-3 sentences, highlighting the key points discussed:

{{$conversation}}

Summary:
```

#### ExtractEntities.prompt
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

#### RewriteTone.prompt
```
Rewrite the following message in a {{$tone}} tone:

Original message:
{{$originalMessage}}

Rewritten message:
```

### Manifest File
```json
{
  "templates": [
    {
      "name": "Summarize",
      "file": "Summarize.prompt",
      "description": "Summarizes conversations in 2-3 sentences"
    },
    {
      "name": "ExtractEntities",
      "file": "ExtractEntities.prompt",
      "description": "Extracts structured information from text"
    },
    {
      "name": "RewriteTone",
      "file": "RewriteTone.prompt",
      "description": "Rewrites messages in a specified tone"
    }
  ]
}
```

## Implementation Approach

### Updated PromptTemplateManager
```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Application.DTOs;
using System.Reflection;

namespace SemanticKernelFunctionCaller.Infrastructure.Orchestration
{
    public class PromptTemplateManager : IPromptTemplateManager
    {
        private readonly ILogger<PromptTemplateManager> _logger;
        private readonly IMemoryCache _cache;
        private readonly SemanticKernelSettings _settings;
        private readonly string _templatesDirectory;

        public PromptTemplateManager(
            ILogger<PromptTemplateManager> logger,
            IMemoryCache cache,
            IOptions<SemanticKernelSettings> settings)
        {
            _logger = logger;
            _cache = cache;
            _settings = settings.Value;
            
            // Determine templates directory
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            _templatesDirectory = Path.Combine(assemblyDirectory, "PromptTemplates");
        }

        public async Task<PromptTemplateDto> LoadTemplateAsync(string templateName)
        {
            // Try to get from cache first
            var cacheKey = $"prompt_template_{templateName}";
            if (_cache.TryGetValue(cacheKey, out PromptTemplateDto cachedTemplate))
            {
                return cachedTemplate;
            }

            try
            {
                // Load from file
                var templateFilePath = Path.Combine(_templatesDirectory, $"{templateName}.prompt");
                if (!File.Exists(templateFilePath))
                {
                    // Fallback to embedded resource
                    var resourceTemplate = await LoadFromEmbeddedResourceAsync(templateName);
                    if (resourceTemplate != null)
                    {
                        // Cache the template
                        _cache.Set(cacheKey, resourceTemplate, TimeSpan.FromHours(1));
                        return resourceTemplate;
                    }
                    
                    throw new FileNotFoundException($"Prompt template '{templateName}' not found.");
                }

                var templateContent = await File.ReadAllTextAsync(templateFilePath);
                var template = new PromptTemplateDto
                {
                    TemplateName = templateName,
                    TemplateContent = templateContent
                };

                // Cache the template
                _cache.Set(cacheKey, template, TimeSpan.FromHours(1));
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading prompt template '{TemplateName}'", templateName);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableTemplatesAsync()
        {
            try
            {
                // Check file system templates
                if (Directory.Exists(_templatesDirectory))
                {
                    var templateFiles = Directory.GetFiles(_templatesDirectory, "*.prompt");
                    var templateNames = templateFiles.Select(Path.GetFileNameWithoutExtension);
                    return templateNames;
                }

                // Fallback to embedded resources
                return await GetEmbeddedTemplateNamesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available prompt templates");
                return Enumerable.Empty<string>();
            }
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
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(_templatesDirectory);
                
                // Save to file
                var templateFilePath = Path.Combine(_templatesDirectory, $"{template.TemplateName}.prompt");
                await File.WriteAllTextAsync(templateFilePath, template.TemplateContent);
                
                // Update cache
                var cacheKey = $"prompt_template_{template.TemplateName}";
                _cache.Set(cacheKey, template, TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving prompt template '{TemplateName}'", template.TemplateName);
                throw;
            }
        }

        private IEnumerable<string> ExtractVariablesFromTemplate(string templateContent)
        {
            // Simple regex to extract variable placeholders
            // In practice, this would use Semantic Kernel's template parsing
            var regex = new Regex(@"\{\{?\$(\w+)\}?\}");
            var matches = regex.Matches(templateContent);
            return matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct();
        }

        private async Task<PromptTemplateDto?> LoadFromEmbeddedResourceAsync(string templateName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"SemanticKernelFunctionCaller.Infrastructure.PromptTemplates.{templateName}.prompt";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return null;
                    
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                
                return new PromptTemplateDto
                {
                    TemplateName = templateName,
                    TemplateContent = content
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load template '{TemplateName}' from embedded resources", templateName);
                return null;
            }
        }

        private async Task<IEnumerable<string>> GetEmbeddedTemplateNamesAsync()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                var templateNames = resourceNames
                    .Where(name => name.EndsWith(".prompt"))
                    .Select(name => Path.GetFileNameWithoutExtension(name.Split('.').Last()));
                return templateNames;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve embedded template names");
                return Enumerable.Empty<string>();
            }
        }
    }
}
```

## Configuration Updates

### Updated Configuration Structure
```json
{
  "SemanticKernel": {
    "DefaultProvider": "OpenRouter",
    "DefaultModel": "google/gemini-2.5-flash-lite-preview-09-2025",
    "EnabledPlugins": ["Weather", "DateTime"],
    "PromptTemplateDirectory": "./PromptTemplates/",
    "TemplateCacheDurationMinutes": 60
  }
}
```

### SemanticKernelSettings Update
```csharp
namespace SemanticKernelFunctionCaller.Infrastructure.Configuration
{
    public class SemanticKernelSettings
    {
        public const string SectionName = "SemanticKernel";

        public string DefaultProvider { get; set; } = "OpenRouter";
        public string DefaultModel { get; set; } = "google/gemini-2.5-flash-lite-preview-09-2025";
        public List<string> EnabledPlugins { get; set; } = new();
        public string PromptTemplateDirectory { get; set; } = "./PromptTemplates/";
        public int TemplateCacheDurationMinutes { get; set; } = 60;
        public int MaxWorkflowSteps { get; set; } = 10;
    }
}
```

## Deployment Considerations

### File Copy Settings
In the `.csproj` file:
```xml
<ItemGroup>
  <None Remove="PromptTemplates\*.prompt" />
</ItemGroup>

<ItemGroup>
  <Content Include="PromptTemplates\**\*.prompt">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>

<ItemGroup>
  <EmbeddedResource Include="PromptTemplates\**\*.prompt" />
</ItemGroup>
```

## Benefits of File-Based Approach

1. **Scalability**: Templates can be hundreds of lines without cluttering configuration files
2. **Version Control**: Better diffing and merge capabilities for template content
3. **IDE Support**: Syntax highlighting and editing features for prompt files
4. **Runtime Updates**: Ability to modify templates without redeployment
5. **Organization**: Logical grouping of related templates in directories
6. **Fallback Mechanism**: Embedded resources provide backup if files are missing

## Migration Path

### From JSON Configuration
1. Extract existing templates from `appsettings.json`
2. Create corresponding `.prompt` files
3. Update configuration to remove `PromptTemplates` section
4. Verify loading works through file system
5. Remove JSON template configuration

### Backward Compatibility
The implementation maintains backward compatibility by:
1. First checking file system for templates
2. Falling back to embedded resources if files are missing
3. Providing the same interface for template loading

## Testing Strategy

### Unit Tests
```csharp
[Test]
public async Task LoadTemplateAsync_FileExists_ReturnsTemplate()
{
    // Arrange
    var templateName = "Summarize";
    
    // Act
    var result = await _promptTemplateManager.LoadTemplateAsync(templateName);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(templateName, result.TemplateName);
    Assert.Contains("Summarize the following conversation", result.TemplateContent);
}
```

### Integration Tests
```csharp
[Test]
public async Task LoadTemplateAsync_MissingFile_FallsBackToEmbedded()
{
    // Arrange
    var templateName = "TestTemplate";
    // Ensure file doesn't exist but embedded resource does
    
    // Act
    var result = await _promptTemplateManager.LoadTemplateAsync(templateName);
    
    // Assert
    Assert.NotNull(result);
    // Verify content matches embedded resource
}
```

This file-based approach provides a more maintainable and scalable solution for prompt template management while maintaining the flexibility of runtime updates and deployment-time defaults.