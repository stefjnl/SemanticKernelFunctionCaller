# Error Handling Strategy for Function Call Failures

## Overview
This document defines the error handling strategy for plugin failures during Semantic Kernel orchestration. It addresses how the system gracefully handles various failure scenarios while maintaining a good user experience.

## Error Handling Principles

### 1. Graceful Degradation
When a plugin fails, the system should attempt to provide a meaningful response using available information rather than failing completely.

### 2. Retry with Exponential Backoff
Transient failures should be retried with increasing delays to allow for temporary issues to resolve.

### 3. User Transparency
Users should be informed about failures in a clear, non-technical manner, with options to retry or proceed.

### 4. Detailed Logging
All failures should be logged with sufficient detail for debugging while protecting sensitive information.

### 5. Circuit Breaker Pattern
Repeated failures should temporarily disable problematic plugins to prevent cascading failures.

## Error Categories and Handling

### 1. Transient Failures
- **Examples**: Network timeouts, temporary API unavailability, rate limiting
- **Handling**: Retry with exponential backoff (1s, 2s, 4s, 8s, max 3 attempts)
- **Fallback**: If retries fail, ask LLM to respond without the plugin data

### 2. Permanent Failures
- **Examples**: Invalid API keys, malformed requests, unsupported operations
- **Handling**: No retry, immediate fallback
- **Fallback**: Ask LLM to respond explaining the limitation

### 3. Plugin Not Found
- **Examples**: Requested plugin not registered or disabled
- **Handling**: Immediate error response
- **Fallback**: Ask LLM to respond explaining unavailable functionality

### 4. Security Violations
- **Examples**: Attempted execution of disabled plugin, rate limit exceeded
- **Handling**: Immediate rejection with security exception
- **Fallback**: Inform user of security policy restriction

## Implementation Approach

### Error Handling Cascade
```csharp
public partial class SemanticKernelOrchestrationService
{
    private readonly ILogger<SemanticKernelOrchestrationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<ChatResponseDto> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<object>> operation,
        string operationName,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var delay = TimeSpan.FromSeconds(1);
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await operation(cancellationToken);
                return CreateSuccessResponse(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Operation {OperationName} was cancelled", operationName);
                throw;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Transient error in {OperationName}, attempt {Attempt}/{MaxRetries}", 
                    operationName, attempt + 1, maxRetries + 1);
                
                // Exponential backoff
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromTicks(delay.Ticks * 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permanent error in {OperationName} after {Attempts} attempts", 
                    operationName, attempt + 1);
                
                // Handle permanent failure
                return await HandlePermanentFailureAsync(operationName, ex);
            }
        }
        
        // This should never be reached
        throw new InvalidOperationException("Unexpected code path in retry logic");
    }
    
    private bool IsTransientError(Exception ex)
    {
        return ex is TimeoutException ||
               ex is HttpRequestException ||
               ex is TaskCanceledException ||
               (ex is InvalidOperationException && ex.Message.Contains("rate limit"));
    }
    
    private async Task<ChatResponseDto> HandlePermanentFailureAsync(string operationName, Exception ex)
    {
        // Log detailed error for diagnostics
        _logger.LogError(ex, "Permanent failure in {OperationName}: {ErrorMessage}", 
            operationName, ex.Message);
        
        // Create fallback response using LLM
        var fallbackPrompt = $@"
An error occurred while trying to execute '{operationName}':
Error: {ex.Message}

Please provide a helpful response to the user that acknowledges the issue without exposing technical details.
If there's useful context from previous messages, incorporate that into your response.
";
        
        // Use a basic chat completion without plugins for fallback
        var fallbackResponse = await GetFallbackResponseAsync(fallbackPrompt);
        
        return new ChatResponseDto
        {
            Content = fallbackResponse,
            ProviderId = _settings.DefaultProvider,
            ModelId = _settings.DefaultModel,
            Metadata = new Dictionary<string, object>
            {
                ["Error"] = new
                {
                    Operation = operationName,
                    Message = "An error occurred while processing your request",
                    Type = ex.GetType().Name
                }
            }
        };
    }
    
    private async Task<string> GetFallbackResponseAsync(string prompt)
    {
        try
        {
            // Get a basic chat client without Semantic Kernel orchestration
            var chatProvider = _providerFactory.CreateProvider(_settings.DefaultProvider, _settings.DefaultModel);
            var messages = new[]
            {
                new ChatMessage(ChatRole.User, prompt)
            };
            
            var response = await chatProvider.SendMessageAsync(messages);
            return response.Message.Content;
        }
        catch (Exception fallbackEx)
        {
            _logger.LogError(fallbackEx, "Fallback response generation failed");
            return "Sorry, I'm experiencing technical difficulties. Please try again later.";
        }
    }
}
```

### Plugin-Specific Error Handling
```csharp
public class WeatherPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherPlugin> _logger;
    private readonly CircuitBreaker _circuitBreaker;
    
    public WeatherPlugin(HttpClient httpClient, ILogger<WeatherPlugin> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _circuitBreaker = new CircuitBreaker(
            failureThreshold: 5,
            recoveryTimeout: TimeSpan.FromMinutes(1));
    }
    
    [KernelFunction]
    [Description("Gets the current weather for a specified location.")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("The location to get weather for (e.g., Seattle, WA)")] string location)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(location))
                {
                    throw new ArgumentException("Location cannot be empty");
                }
                
                var response = await _httpClient.GetAsync(
                    $"https://api.weather.com/v1/current?location={Uri.EscapeDataString(location)}");
                
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Weather service rate limit exceeded");
                }
                
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                // Parse and return weather information
                return ParseWeatherResponse(content);
            }
            catch (HttpRequestException ex) when (ex.StatusCode.HasValue && 
                (ex.StatusCode == HttpStatusCode.NotFound || ex.StatusCode == HttpStatusCode.BadRequest))
            {
                // Handle invalid location
                _logger.LogWarning("Invalid location requested: {Location}", location);
                return $"I couldn't find weather information for '{location}'. Please check the location name and try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for location: {Location}", location);
                throw; // Let the orchestration service handle retries
            }
        });
    }
    
    private string ParseWeatherResponse(string jsonResponse)
    {
        try
        {
            var doc = JsonDocument.Parse(jsonResponse);
            var temperature = doc.RootElement.GetProperty("temperature").GetString();
            var conditions = doc.RootElement.GetProperty("conditions").GetString();
            
            return $"The current weather is {conditions} with a temperature of {temperature}°F.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing weather response");
            throw new InvalidOperationException("Failed to parse weather data");
        }
    }
}
```

### Circuit Breaker Implementation
```csharp
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _recoveryTimeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;
    
    public CircuitBreaker(int failureThreshold, TimeSpan recoveryTimeout)
    {
        _failureThreshold = failureThreshold;
        _recoveryTimeout = recoveryTimeout;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime < _recoveryTimeout)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
            
            _state = CircuitState.HalfOpen;
        }
        
        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }
    
    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
    }
    
    private void OnFailure(Exception ex)
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
        
        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitState.Open;
        }
        
        // In Half-Open state, any failure returns to Open
        if (_state == CircuitState.HalfOpen)
        {
            _state = CircuitState.Open;
        }
    }
}

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

## Workflow-Level Error Handling

### Error Recovery in Multi-Step Workflows
```csharp
public async Task<ChatResponseDto> ExecuteWorkflowAsync(
    WorkflowRequestDto workflowRequest,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Create a Semantic Kernel instance for this workflow
        var kernel = await CreateKernelAsync(workflowRequest.ProviderId, workflowRequest.ModelId);

        // Register specified plugins for this workflow
        if (workflowRequest.AvailableFunctions?.Any() == true)
        {
            var plugins = _pluginRegistry.GetPluginsByName(workflowRequest.AvailableFunctions);
            foreach (var plugin in plugins)
            {
                var pluginRegistration = plugin.GetPluginRegistration();
                if (pluginRegistration is KernelPlugin kernelPlugin)
                {
                    kernel.Plugins.Add(kernelPlugin);
                }
            }
        }

        // Create a plan using Semantic Kernel's planning capabilities
        var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions
        {
            MaxIterations = workflowRequest.MaxSteps
        });

        // Execute the plan with error handling
        var goal = workflowRequest.Context != null 
            ? $"{workflowRequest.Goal}\n\nContext: {workflowRequest.Context}" 
            : workflowRequest.Goal;

        try
        {
            var planResult = await planner.ExecuteAsync(kernel, goal, cancellationToken);
            
            // Successful execution
            return CreateWorkflowResponse(planResult, workflowRequest);
        }
        catch (Exception ex) when (IsRecoverableWorkflowError(ex))
        {
            _logger.LogWarning(ex, "Recoverable error in workflow execution, attempting fallback");
            
            // Try to get a partial result or explanation from the LLM
            return await GetWorkflowFallbackResponseAsync(goal, ex, workflowRequest);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fatal error in workflow execution");
        throw; // Let higher-level error handling take over
    }
}

private bool IsRecoverableWorkflowError(Exception ex)
{
    // Define which errors we can recover from
    return ex is CircuitBreakerOpenException ||
           ex is TimeoutException ||
           (ex is InvalidOperationException && ex.Message.Contains("rate limit"));
}

private async Task<ChatResponseDto> GetWorkflowFallbackResponseAsync(
    string goal, Exception error, WorkflowRequestDto request)
{
    var fallbackPrompt = $@"
I was trying to help with this request: "{goal}"

But I encountered an issue: {error.Message}

Please provide a helpful response that:
1. Acknowledges the issue without exposing technical details
2. Offers alternatives or suggestions if possible
3. Maintains a helpful tone
";

    var fallbackResponse = await GetFallbackResponseAsync(fallbackPrompt);
    
    return new ChatResponseDto
    {
        Content = fallbackResponse,
        ProviderId = request.ProviderId,
        ModelId = request.ModelId,
        Metadata = new Dictionary<string, object>
        {
            ["Error"] = new
            {
                Message = "An error occurred while processing your request",
                Type = error.GetType().Name,
                Recovered = true
            }
        }
    };
}
```

## User Experience Considerations

### Error Response Examples

#### Transient Error
```
User: "What's the weather in Tokyo?"

System: (After retry failure)
Response: "I'm having trouble getting the current weather information for Tokyo right now. This might be due to a temporary connection issue. Would you like me to try again, or perhaps I can help with something else?"
```

#### Invalid Input
```
User: "What's the weather in XYZ123?"

System:
Response: "I couldn't find weather information for 'XYZ123'. This location doesn't appear to be valid. Could you please provide a city name or zip code?"
```

#### Rate Limiting
```
User: (Rapid requests)

System:
Response: "I'm receiving your requests a bit too quickly. To ensure fair usage, I need to pause briefly before processing more requests. Please wait a moment and try again."
```

#### Plugin Disabled
```
User: "Delete my files"

System:
Response: "I don't have permission to perform file deletion operations for security reasons. Is there something else I can help you with today?"
```

## Monitoring and Alerting

### Error Metrics Collection
```csharp
public class ErrorMetricsCollector
{
    private readonly ILogger<ErrorMetricsCollector> _logger;
    
    public async Task RecordErrorAsync(string operation, Exception ex, bool recovered)
    {
        // Log structured error data
        _logger.LogError(new EventId(1001, "PluginError"), ex, 
            "Plugin error: {Operation}, Recovered: {Recovered}, Type: {ExceptionType}", 
            operation, recovered, ex.GetType().Name);
        
        // In a production system, this would also send metrics to a monitoring system
        // await _metricsClient.IncrementCounterAsync("plugin_errors", 
        //     new[] { 
        //         new Tag("operation", operation),
        //         new Tag("type", ex.GetType().Name),
        //         new Tag("recovered", recovered.ToString())
        //     });
    }
}
```

This error handling strategy ensures that plugin failures are gracefully managed while providing informative feedback to users and detailed diagnostics for developers.