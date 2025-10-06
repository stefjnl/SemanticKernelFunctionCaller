# Configuration Updates for Semantic Kernel Integration

## Overview
Configuration updates are required to support the Semantic Kernel integration, including default provider/model selection, enabled plugins, and prompt templates.

## Configuration Structure

### appsettings.json
```json
{
  "SemanticKernel": {
    "DefaultProvider": "OpenRouter",
    "DefaultModel": "google/gemini-2.5-flash-lite-preview-09-2025",
    "EnabledPlugins": ["Weather", "DateTime"],
    "PromptTemplates": {
      "Summarize": "Summarize the following conversation in 2-3 sentences, highlighting the key points discussed:\n\n{{$conversation}}\n\nSummary:",
      "ExtractEntities": "Extract structured information from the following text. Identify and list:\n- Names of people mentioned\n- Organizations referenced\n- Key dates\n- Important facts or figures\n\nText:\n{{$inputText}}\n\nExtracted Information:",
      "RewriteTone": "Rewrite the following message in a {{$tone}} tone:\n\nOriginal message:\n{{$originalMessage}}\n\nRewritten message:"
    },
    "MaxWorkflowSteps": 10
  }
}
```

### appsettings.Development.json
```json
{
  "SemanticKernel": {
    "DefaultProvider": "OpenRouter",
    "DefaultModel": "google/gemini-2.5-flash-lite-preview-09-2025",
    "EnabledPlugins": ["Weather", "DateTime"],
    "PromptTemplates": {
      "Summarize": "Summarize the following conversation in 2-3 sentences, highlighting the key points discussed:\n\n{{$conversation}}\n\nSummary:",
      "ExtractEntities": "Extract structured information from the following text. Identify and list:\n- Names of people mentioned\n- Organizations referenced\n- Key dates\n- Important facts or figures\n\nText:\n{{$inputText}}\n\nExtracted Information:",
      "RewriteTone": "Rewrite the following message in a {{$tone}} tone:\n\nOriginal message:\n{{$originalMessage}}\n\nRewritten message:"
    },
    "MaxWorkflowSteps": 15
  }
}
```

## Configuration Classes

### SemanticKernelSettings
```csharp
namespace SemanticKernelFunctionCaller.Infrastructure.Configuration
{
    public class SemanticKernelSettings
    {
        public const string SectionName = "SemanticKernel";

        public string DefaultProvider { get; set; } = "OpenRouter";
        public string DefaultModel { get; set; } = "google/gemini-2.5-flash-lite-preview-09-2025";
        public List<string> EnabledPlugins { get; set; } = new();
        public Dictionary<string, string> PromptTemplates { get; set; } = new();
        public int MaxWorkflowSteps { get; set; } = 10;
    }
}
```

## Configuration Registration

### ServiceCollectionExtensions
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Orchestration;
using SemanticKernelFunctionCaller.Infrastructure.Plugins;

namespace SemanticKernelFunctionCaller.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            services.Configure<SemanticKernelSettings>(configuration.GetSection(SemanticKernelSettings.SectionName));

            // Register orchestration service
            services.AddScoped<IAIOrchestrationService, SemanticKernelOrchestrationService>();

            // Register prompt template manager
            services.AddSingleton<IPromptTemplateManager, PromptTemplateManager>();

            // Register plugin implementations
            services.AddScoped<WeatherPlugin>();

            // Register plugin providers
            services.AddSingleton<IKernelPluginProvider, WeatherPluginProvider>();

            // Register plugin registry
            services.AddSingleton<IPluginRegistry, PluginRegistry>();

            // Register use cases
            services.AddScoped<ISendOrchestralChatMessageUseCase, SendOrchestralChatMessageUseCase>();
            services.AddScoped<IExecutePromptTemplateUseCase, ExecutePromptTemplateUseCase>();

            return services;
        }
    }
}
```

### Program.cs (API Layer)
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SemanticKernelFunctionCaller.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register existing services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Semantic Kernel services
builder.Services.AddSemanticKernelServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Configuration Usage

### SemanticKernelOrchestrationService
```csharp
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;

public class SemanticKernelOrchestrationService : IAIOrchestrationService
{
    private readonly SemanticKernelSettings _settings;

    public SemanticKernelOrchestrationService(
        IOptions<SemanticKernelSettings> settings)
    {
        _settings = settings.Value;
    }

    // Use _settings.DefaultProvider and _settings.DefaultModel when provider/model are not specified
    // Use _settings.MaxWorkflowSteps to limit workflow iterations
}
```

### PluginRegistry
```csharp
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;

public class PluginRegistry : IPluginRegistry
{
    private readonly IEnumerable<IKernelPluginProvider> _pluginProviders;
    private readonly SemanticKernelSettings _settings;

    public PluginRegistry(
        IEnumerable<IKernelPluginProvider> pluginProviders,
        IOptions<SemanticKernelSettings> settings)
    {
        _pluginProviders = pluginProviders;
        _settings = settings.Value;
    }

    public IEnumerable<IKernelPluginProvider> GetRegisteredPlugins()
    {
        // Filter by enabled plugins from configuration
        var enabledPlugins = _settings.EnabledPlugins ?? new List<string>();
        return _pluginProviders.Where(p => enabledPlugins.Contains(p.PluginName, StringComparer.OrdinalIgnoreCase));
    }
}
```

### PromptTemplateManager
```csharp
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;

public class PromptTemplateManager : IPromptTemplateManager
{
    private readonly SemanticKernelSettings _settings;

    public PromptTemplateManager(
        IOptions<SemanticKernelSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<PromptTemplateDto> LoadTemplateAsync(string templateName)
    {
        // Load from configuration
        if (_settings.PromptTemplates?.ContainsKey(templateName) == true)
        {
            var templateContent = _settings.PromptTemplates[templateName];
            return new PromptTemplateDto
            {
                TemplateName = templateName,
                TemplateContent = templateContent
            };
        }

        throw new InvalidOperationException($"Prompt template '{templateName}' not found.");
    }

    public async Task<IEnumerable<string>> GetAvailableTemplatesAsync()
    {
        return _settings.PromptTemplates?.Keys ?? Enumerable.Empty<string>();
    }
}
```

## Environment-Specific Configuration

### Development
- Higher MaxWorkflowSteps for testing
- More verbose logging
- Development-specific prompt templates

### Production
- Lower MaxWorkflowSteps for performance
- Production-specific prompt templates
- Stricter plugin restrictions

## Configuration Validation

### SemanticKernelSettingsValidator
```csharp
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;

namespace SemanticKernelFunctionCaller.Infrastructure.Validation
{
    public class SemanticKernelSettingsValidator : IValidateOptions<SemanticKernelSettings>
    {
        public ValidateOptionsResult Validate(string name, SemanticKernelSettings options)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.DefaultProvider))
            {
                errors.Add("DefaultProvider must be provided.");
            }

            if (string.IsNullOrWhiteSpace(options.DefaultModel))
            {
                errors.Add("DefaultModel must be provided.");
            }

            if (options.MaxWorkflowSteps <= 0)
            {
                errors.Add("MaxWorkflowSteps must be greater than zero.");
            }

            if (options.MaxWorkflowSteps > 50)
            {
                errors.Add("MaxWorkflowSteps should not exceed 50 for performance reasons.");
            }

            return errors.Any() ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
        }
    }
}
```

### Registration in ServiceCollectionExtensions
```csharp
public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
{
    // Register configuration
    services.Configure<SemanticKernelSettings>(configuration.GetSection(SemanticKernelSettings.SectionName));
    
    // Register configuration validation
    services.AddSingleton<IValidateOptions<SemanticKernelSettings>, SemanticKernelSettingsValidator>();

    // ... other registrations
}
```

## Integration Points

1. **Dependency Injection**: Configuration is registered and injected where needed.

2. **Service Registration**: Configuration is used during service registration.

3. **Runtime Usage**: Services access configuration values at runtime.

4. **Validation**: Configuration is validated at startup.

5. **Environment Specific**: Different settings for different environments.

## Design Considerations

1. **Flexibility**: Configuration allows for easy customization without code changes.

2. **Security**: Sensitive settings are kept in user secrets or environment variables.

3. **Performance**: Reasonable limits are enforced through configuration.

4. **Maintainability**: Clear structure and naming conventions.

5. **Extensibility**: Configuration structure can be extended with new features.

6. **Validation**: Configuration validation prevents runtime errors.

7. **Documentation**: Clear documentation of all configuration options.

## Future Enhancements

1. **Database Configuration**: Store some configuration in a database for dynamic updates.

2. **Hot Reload**: Support for configuration changes without application restart.

3. **Configuration API**: REST API for managing configuration at runtime.

4. **Configuration Versioning**: Support for versioned configuration.

5. **Configuration Auditing**: Track changes to configuration over time.