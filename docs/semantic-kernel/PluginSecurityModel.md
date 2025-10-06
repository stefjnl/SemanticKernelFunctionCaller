# Plugin Security Model and Boundaries

## Overview
This document defines the security model and boundaries for plugins in the Semantic Kernel integration. It addresses the critical need to prevent malicious or unintended actions while maintaining the flexibility and power of the plugin system.

## Security Principles

### 1. Principle of Least Privilege
Plugins operate with the minimum permissions necessary to perform their intended functions. No plugin has unrestricted access to the system.

### 2. Explicit Allowlisting
Only explicitly approved plugins can be registered and executed. Plugins must be listed in configuration to be available.

### 3. User Consent for Sensitive Operations
Destructive or sensitive operations require explicit user confirmation before execution.

### 4. Rate Limiting
Plugins are subject to rate limits to prevent abuse and resource exhaustion.

### 5. Audit Trail
All plugin executions are logged for security monitoring and auditing.

## Security Implementation

### Plugin Allowlist Configuration
```json
{
  "SemanticKernel": {
    "PluginSecurityPolicy": {
      "Allowlist": ["Weather", "DateTime", "WebSearch", "Summarize"],
      "RequireConfirmation": ["FileSystem", "Email", "Database"],
      "Disabled": ["SystemCommand", "NetworkAccess"],
      "RateLimits": {
        "Weather": "10/minute",
        "WebSearch": "5/minute",
        "FileSystem": "1/minute"
      }
    }
  }
}
```

### Security Policy Class
```csharp
namespace SemanticKernelFunctionCaller.Infrastructure.Security
{
    public class PluginSecurityPolicy
    {
        public List<string> Allowlist { get; set; } = new();
        public List<string> RequireConfirmation { get; set; } = new();
        public List<string> Disabled { get; set; } = new();
        public Dictionary<string, string> RateLimits { get; set; } = new();
    }
}
```

### Security Policy Validator
```csharp
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Infrastructure.Security;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins
{
    public interface IPluginSecurityValidator
    {
        /// <summary>
        /// Validates if a plugin can be executed based on security policy
        /// </summary>
        bool CanExecutePlugin(string pluginName);
        
        /// <summary>
        /// Determines if user confirmation is required for plugin execution
        /// </summary>
        bool RequiresConfirmation(string pluginName);
        
        /// <summary>
        /// Checks if the plugin execution is within rate limits
        /// </summary>
        bool IsWithinRateLimit(string pluginName);
        
        /// <summary>
        /// Records plugin execution for rate limiting
        /// </summary>
        void RecordExecution(string pluginName);
    }
    
    public class PluginSecurityValidator : IPluginSecurityValidator
    {
        private readonly PluginSecurityPolicy _policy;
        private readonly ILogger<PluginSecurityValidator> _logger;
        private readonly Dictionary<string, DateTime> _executionTimestamps = new();
        private readonly object _lock = new object();

        public PluginSecurityValidator(
            IOptions<PluginSecurityPolicy> policy,
            ILogger<PluginSecurityValidator> logger)
        {
            _policy = policy.Value;
            _logger = logger;
        }

        public bool CanExecutePlugin(string pluginName)
        {
            // Check if plugin is disabled
            if (_policy.Disabled.Contains(pluginName, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Plugin '{PluginName}' is disabled by security policy", pluginName);
                return false;
            }

            // Check if plugin is in allowlist
            if (!_policy.Allowlist.Contains(pluginName, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Plugin '{PluginName}' is not in the allowlist", pluginName);
                return false;
            }

            return true;
        }

        public bool RequiresConfirmation(string pluginName)
        {
            return _policy.RequireConfirmation.Contains(pluginName, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsWithinRateLimit(string pluginName)
        {
            if (!_policy.RateLimits.ContainsKey(pluginName))
                return true; // No rate limit defined

            var rateLimit = _policy.RateLimits[pluginName];
            var parts = rateLimit.Split('/');
            if (parts.Length != 2)
                return true; // Invalid format, allow execution

            if (!int.TryParse(parts[0], out var limit) || 
                !Enum.TryParse<TimeUnit>(parts[1], true, out var timeUnit))
                return true; // Invalid format, allow execution

            var timeWindow = GetTimeWindow(timeUnit);
            var now = DateTime.UtcNow;
            var windowStart = now - timeWindow;

            lock (_lock)
            {
                // Clean up old timestamps
                var keysToRemove = _executionTimestamps
                    .Where(kvp => kvp.Key.StartsWith($"{pluginName}_") && kvp.Value < windowStart)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _executionTimestamps.Remove(key);
                }

                // Count current executions
                var currentCount = _executionTimestamps
                    .Count(kvp => kvp.Key.StartsWith($"{pluginName}_") && kvp.Value >= windowStart);

                return currentCount < limit;
            }
        }

        public void RecordExecution(string pluginName)
        {
            lock (_lock)
            {
                var timestamp = DateTime.UtcNow;
                var key = $"{pluginName}_{timestamp.Ticks}";
                _executionTimestamps[key] = timestamp;
            }
        }

        private TimeSpan GetTimeWindow(TimeUnit timeUnit)
        {
            return timeUnit switch
            {
                TimeUnit.Second => TimeSpan.FromSeconds(1),
                TimeUnit.Minute => TimeSpan.FromMinutes(1),
                TimeUnit.Hour => TimeSpan.FromHours(1),
                TimeUnit.Day => TimeSpan.FromDays(1),
                _ => TimeSpan.FromMinutes(1)
            };
        }
    }

    public enum TimeUnit
    {
        Second,
        Minute,
        Hour,
        Day
    }
}
```

### Updated Plugin Registry with Security
```csharp
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins
{
    public class SecurePluginRegistry : IPluginRegistry
    {
        private readonly IEnumerable<IKernelPluginProvider> _pluginProviders;
        private readonly IPluginSecurityValidator _securityValidator;
        private readonly SemanticKernelSettings _settings;

        public SecurePluginRegistry(
            IEnumerable<IKernelPluginProvider> pluginProviders,
            IPluginSecurityValidator securityValidator,
            IOptions<SemanticKernelSettings> settings)
        {
            _pluginProviders = pluginProviders;
            _securityValidator = securityValidator;
            _settings = settings.Value;
        }

        public IEnumerable<IKernelPluginProvider> GetRegisteredPlugins()
        {
            // Get plugins from base registry
            var enabledPlugins = _settings.EnabledPlugins ?? new List<string>();
            var basePlugins = _pluginProviders
                .Where(p => enabledPlugins.Contains(p.PluginName, StringComparer.OrdinalIgnoreCase));

            // Apply security filtering
            return basePlugins.Where(p => _securityValidator.CanExecutePlugin(p.PluginName));
        }

        public IEnumerable<IKernelPluginProvider> GetPluginsByName(IEnumerable<string> pluginNames)
        {
            var nameSet = new HashSet<string>(pluginNames, StringComparer.OrdinalIgnoreCase);
            return GetRegisteredPlugins().Where(p => nameSet.Contains(p.PluginName));
        }
    }
}
```

## Security Boundaries by Plugin Type

### 1. Informational Plugins (Low Risk)
- **Examples**: Weather, DateTime, Summarize
- **Security**: No special restrictions
- **Rate Limit**: Moderate (10-50/minute)
- **Confirmation**: Not required

### 2. Data Access Plugins (Medium Risk)
- **Examples**: Database, File System (read-only)
- **Security**: Restricted to specific directories or database schemas
- **Rate Limit**: Conservative (1-10/minute)
- **Confirmation**: Required for sensitive data access

### 3. System Modification Plugins (High Risk)
- **Examples**: File System (write/delete), Email, System Commands
- **Security**: Sandboxed execution, explicit permissions
- **Rate Limit**: Very strict (1/minute)
- **Confirmation**: Always required

## Implementation in Semantic Kernel Orchestration

### Plugin Execution with Security Validation
```csharp
public partial class SemanticKernelOrchestrationService
{
    private readonly IPluginSecurityValidator _securityValidator;
    
    public async Task<ChatResponseDto> ExecuteWorkflowAsync(
        WorkflowRequestDto workflowRequest,
        CancellationToken cancellationToken = default)
    {
        // Validate workflow-level security
        if (workflowRequest.AvailableFunctions?.Any() == true)
        {
            foreach (var functionName in workflowRequest.AvailableFunctions)
            {
                // Check if plugin can be executed
                if (!_securityValidator.CanExecutePlugin(functionName))
                {
                    throw new SecurityException($"Plugin '{functionName}' is not allowed by security policy");
                }
                
                // Check rate limits
                if (!_securityValidator.IsWithinRateLimit(functionName))
                {
                    throw new SecurityException($"Rate limit exceeded for plugin '{functionName}'");
                }
            }
        }
        
        // Continue with workflow execution...
    }
}
```

### User Confirmation Flow
For plugins that require confirmation, the system would:
1. Detect the need for confirmation during workflow planning
2. Pause execution and request user confirmation
3. Resume only after explicit approval

```csharp
public async Task<ChatResponseDto> ExecuteWorkflowWithConfirmationAsync(
    WorkflowRequestDto workflowRequest,
    Func<string, Task<bool>> confirmationRequest,
    CancellationToken cancellationToken = default)
{
    // Check if any plugins require confirmation
    var pluginsRequiringConfirmation = workflowRequest.AvailableFunctions
        .Where(_securityValidator.RequiresConfirmation)
        .ToList();
    
    if (pluginsRequiringConfirmation.Any())
    {
        var confirmationMessage = $"This workflow will use the following plugins that require confirmation: {string.Join(", ", pluginsRequiringConfirmation)}. Do you approve?";
        var approved = await confirmationRequest(confirmationMessage);
        
        if (!approved)
        {
            return new ChatResponseDto
            {
                Content = "Workflow execution cancelled by user.",
                ProviderId = workflowRequest.ProviderId,
                ModelId = workflowRequest.ModelId
            };
        }
    }
    
    // Continue with workflow execution
    return await ExecuteWorkflowAsync(workflowRequest, cancellationToken);
}
```

## Security Event Logging

### Audit Trail Implementation
```csharp
public class PluginExecutionAudit
{
    public string PluginName { get; set; }
    public string FunctionName { get; set; }
    public string Arguments { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public bool RequiresConfirmation { get; set; }
    public bool WasConfirmed { get; set; }
    public string Result { get; set; }
}

public interface IPluginAuditLogger
{
    Task LogExecutionAsync(PluginExecutionAudit audit);
}

public class PluginAuditLogger : IPluginAuditLogger
{
    private readonly ILogger<PluginAuditLogger> _logger;
    
    public PluginAuditLogger(ILogger<PluginAuditLogger> logger)
    {
        _logger = logger;
    }
    
    public async Task LogExecutionAsync(PluginExecutionAudit audit)
    {
        // Log to structured logging
        _logger.LogInformation(
            "Plugin Execution: {PluginName}.{FunctionName} by user {UserId} at {Timestamp}. Confirmation: {RequiresConfirmation}/{WasConfirmed}",
            audit.PluginName, audit.FunctionName, audit.UserId, audit.Timestamp, 
            audit.RequiresConfirmation, audit.WasConfirmed);
        
        // In a production system, this would also log to a security information and event management (SIEM) system
        // await _siemLogger.LogAsync(audit);
    }
}
```

## Risk Mitigation Strategies

### 1. Sandboxing
- **File System Access**: Restrict to specific directories with read/write permissions
- **Network Access**: Limit to approved endpoints
- **System Commands**: Execute in isolated containers

### 2. Parameter Validation
- **Input Sanitization**: Validate all plugin parameters
- **Path Traversal Prevention**: Prevent directory traversal attacks in file operations
- **SQL Injection Prevention**: Use parameterized queries for database plugins

### 3. Timeout Protection
- **Execution Timeouts**: Limit plugin execution time
- **Resource Limits**: Restrict memory and CPU usage

### 4. Circuit Breaker Pattern
- **Failure Detection**: Monitor plugin failures
- **Automatic Disable**: Temporarily disable failing plugins
- **Gradual Re-enable**: Slowly re-enable after cooldown period

## Testing Security Features

### Unit Tests
```csharp
[Test]
public void CanExecutePlugin_DisabledPlugin_ReturnsFalse()
{
    // Arrange
    var policy = new PluginSecurityPolicy
    {
        Disabled = new List<string> { "FileSystem" }
    };
    var validator = new PluginSecurityValidator(Options.Create(policy), _logger);
    
    // Act
    var result = validator.CanExecutePlugin("FileSystem");
    
    // Assert
    Assert.False(result);
}
```

### Integration Tests
```csharp
[Test]
public async Task ExecuteWorkflowAsync_RateLimitedPlugin_ThrowsException()
{
    // Arrange
    var workflowRequest = new WorkflowRequestDto
    {
        AvailableFunctions = new List<string> { "FileSystem" }
        // Set up to exceed rate limit
    };
    
    // Act & Assert
    await Assert.ThrowsAsync<SecurityException>(() => 
        _orchestrationService.ExecuteWorkflowAsync(workflowRequest));
}
```

This security model provides a robust framework for safely executing plugins while maintaining the flexibility and power of the Semantic Kernel integration.