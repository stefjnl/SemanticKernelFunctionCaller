# Streaming Behavior During Function Execution

## Overview
This document clarifies the streaming behavior during function execution in orchestrated operations. It addresses how the system handles the inherent conflict between streaming tokens and waiting for function call results.

## Technical Challenge

### The Streaming Conflict
When using Semantic Kernel with function calling in streaming mode, there's a fundamental tension:
1. **Streaming Expectation**: Users expect a continuous flow of tokens as the response is generated
2. **Function Call Reality**: When the LLM decides to call a function, streaming must pause while the function executes
3. **User Experience Gap**: Users may perceive the pause as a system hang or failure

### Sequence of Events
```
1. User sends message → LLM
2. LLM begins streaming response: "Let me check the weather..."
3. LLM encounters need for WeatherPlugin call → streaming pauses
4. Plugin executes (may take 1-5 seconds)
5. Plugin result sent back to LLM
6. LLM resumes streaming: "The weather is 75°F..."
```

## Implementation Approach

### Solution: Transparent State Communication
Rather than hiding the pause, we inform the user about what's happening during the function execution:

1. **Pre-Function Indication**: Stream a message indicating a function will be called
2. **Function Execution Notification**: Send a special streaming update indicating function execution
3. **Post-Function Continuation**: Resume normal streaming after function completes
4. **Timeout Handling**: Handle cases where functions take too long

### Streaming Response Format
```csharp
public class StreamingChatUpdate
{
    public required string Content { get; set; }
    public bool IsFinal { get; set; }
    
    // NEW: Additional fields for orchestrated streaming
    public bool IsFunctionCall { get; set; }
    public string FunctionName { get; set; }
    public Dictionary<string, object> FunctionArguments { get; set; }
    public bool IsFunctionExecuting { get; set; }
    public bool IsFunctionComplete { get; set; }
    public string FunctionResult { get; set; }
}
```

## Detailed Implementation

### Semantic Kernel Streaming with Function Calls
```csharp
public partial class SemanticKernelOrchestrationService
{
    public async IAsyncEnumerable<StreamingChatUpdate> StreamOrchestratedMessageAsync(
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Create kernel with function calling enabled
        var kernel = await CreateKernelAsync(request.ProviderId, request.ModelId);
        
        // Register plugins
        var plugins = _pluginRegistry.GetRegisteredPlugins();
        foreach (var plugin in plugins)
        {
            var pluginRegistration = plugin.GetPluginRegistration();
            if (pluginRegistration is KernelPlugin kernelPlugin)
            {
                kernel.Plugins.Add(kernelPlugin);
            }
        }
        
        // Convert messages to Semantic Kernel format
        var chatHistory = ConvertToChatHistory(request.Messages);
        
        // Get streaming response
        var result = kernel.InvokeStreamingAsync<StreamingChatContent>(
            kernel.Plugins["ChatCompletion"].FirstOrDefault(),
            new() { 
                { "messages", chatHistory },
                { "settings", new PromptExecutionSettings() }
            });
        
        var functionCallBuffer = new List<StreamingChatContent>();
        var hasActiveFunctionCall = false;
        
        await foreach (var update in result.WithCancellation(cancellationToken))
        {
            if (update is StreamingFunctionCallUpdate functionCallUpdate)
            {
                // Handle function call updates
                if (!hasActiveFunctionCall)
                {
                    hasActiveFunctionCall = true;
                    // Send notification that function call is starting
                    yield return new StreamingChatUpdate
                    {
                        Content = $" [{functionCallUpdate.FunctionName}]",
                        IsFunctionCall = true,
                        FunctionName = functionCallUpdate.FunctionName,
                        FunctionArguments = new Dictionary<string, object>()
                    };
                }
                
                functionCallBuffer.Add(update);
            }
            else if (hasActiveFunctionCall && update is StreamingFunctionResult functionResult)
            {
                // Function execution completed
                yield return new StreamingChatUpdate
                {
                    Content = "",
                    IsFunctionExecuting = false,
                    IsFunctionComplete = true,
                    FunctionResult = functionResult.Result?.ToString() ?? ""
                };
                
                hasActiveFunctionCall = false;
                functionCallBuffer.Clear();
            }
            else if (hasActiveFunctionCall)
            {
                // Accumulate function call information
                functionCallBuffer.Add(update);
            }
            else if (update is StreamingTextContent textContent)
            {
                // Normal text streaming
                yield return new StreamingChatUpdate
                {
                    Content = textContent.Text ?? "",
                    IsFinal = false
                };
            }
        }
        
        // Final update
        yield return new StreamingChatUpdate
        {
            Content = "",
            IsFinal = true
        };
    }
}
```

### Enhanced Chat Controller for Streaming
```csharp
[HttpPost("orchestrated/stream")]
public async Task StreamOrchestratedMessage(ChatRequestDto request)
{
    Response.ContentType = "text/event-stream";
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("Connection", "keep-alive");

    try
    {
        var stream = _aiOrchestrationService.StreamOrchestratedMessageAsync(request, HttpContext.RequestAborted);

        await foreach (var update in stream)
        {
            var jsonUpdate = JsonSerializer.Serialize(update, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            await Response.WriteAsync($"data: {jsonUpdate}\n\n");
            await Response.Body.FlushAsync();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "An error occurred during orchestrated streaming.");
        var jsonError = JsonSerializer.Serialize(new { error = $"An error occurred: {ex.Message}" });
        await Response.WriteAsync($"data: {jsonError}\n\n");
        await Response.Body.FlushAsync();
    }
}
```

## User Experience Patterns

### Pattern 1: Function Call Visibility
```
Stream 1: "Let me check the current weather for you"
Stream 2: " [GetCurrentWeather]"
Stream 3: "" (Function executing indicator)
Stream 4: "" (Function completed indicator)
Stream 5: "The current weather in Seattle is sunny with a temperature of 72°F."
Stream 6: "" (Final indicator)
```

### Pattern 2: Multi-Function Execution
```
Stream 1: "I'll research that for you"
Stream 2: " [WebSearch]"
Stream 3: "" (Search executing)
Stream 4: "" (Search completed)
Stream 5: " [Summarize]"
Stream 6: "" (Summarization executing)
Stream 7: "" (Summarization completed)
Stream 8: "Here's what I found: Recent advancements in quantum computing..."
Stream 9: "" (Final indicator)
```

## Frontend Implementation Guidance

### JavaScript/TypeScript Client
```javascript
class OrchestratedChatClient {
    async streamOrchestratedMessage(request, onUpdate, onComplete, onError) {
        const eventSource = new EventSource('/api/chat/orchestrated/stream', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });
        
        let accumulatedContent = "";
        let isFunctionExecuting = false;
        
        eventSource.onmessage = (event) => {
            const update = JSON.parse(event.data);
            
            if (update.error) {
                onError(update.error);
                eventSource.close();
                return;
            }
            
            if (update.isFunctionCall) {
                // Show function call indicator
                onUpdate({
                    type: 'functionCall',
                    functionName: update.functionName,
                    content: `Calling ${update.functionName}...`
                });
                return;
            }
            
            if (update.isFunctionExecuting) {
                // Show executing state
                isFunctionExecuting = true;
                onUpdate({
                    type: 'functionExecuting',
                    content: 'Executing function...'
                });
                return;
            }
            
            if (update.isFunctionComplete) {
                // Show completion
                isFunctionExecuting = false;
                onUpdate({
                    type: 'functionComplete',
                    content: 'Function completed'
                });
                return;
            }
            
            if (update.content) {
                accumulatedContent += update.content;
                onUpdate({
                    type: 'content',
                    content: accumulatedContent,
                    isFinal: update.isFinal
                });
            }
            
            if (update.isFinal) {
                onComplete(accumulatedContent);
                eventSource.close();
            }
        };
        
        eventSource.onerror = (error) => {
            onError(error);
            eventSource.close();
        };
    }
}
```

### UI Component States
1. **Normal Streaming**: Display text as it arrives
2. **Function Call Detected**: Show function name and "calling..." indicator
3. **Function Executing**: Show spinner or progress indicator
4. **Function Complete**: Brief confirmation message
5. **Resume Streaming**: Continue displaying text

## Timeout Handling

### Function Execution Timeouts
```csharp
public async IAsyncEnumerable<StreamingChatUpdate> StreamOrchestratedMessageAsync(
    ChatRequestDto request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30-second timeout for function calls
    
    try
    {
        // ... streaming implementation ...
        
        // When detecting function call
        if (update is StreamingFunctionCallUpdate functionCallUpdate)
        {
            var timeoutToken = cts.Token;
            
            // Start timeout timer
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), timeoutToken);
            
            // Race function execution against timeout
            var functionTask = ExecuteFunctionAsync(functionCallUpdate);
            
            var completedTask = await Task.WhenAny(functionTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                yield return new StreamingChatUpdate
                {
                    Content = " [Function timed out]",
                    IsFunctionCall = true,
                    FunctionName = functionCallUpdate.FunctionName
                };
                
                // Continue with LLM response without function result
            }
            else
            {
                var result = await functionTask;
                // Process function result normally
            }
        }
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
        yield return new StreamingChatUpdate
        {
            Content = " [Function execution timed out]",
            IsFunctionCall = true
        };
    }
}
```

## Error Handling in Streaming

### Function Failure During Streaming
```csharp
// When function execution fails
yield return new StreamingChatUpdate
{
    Content = $" [Error calling {functionName}: {errorMessage}]",
    IsFunctionCall = true,
    FunctionName = functionName
};

// Let LLM decide how to respond to the error
yield return new StreamingChatUpdate
{
    Content = " I apologize, but I encountered an issue while trying to get that information. "
};
```

## Performance Considerations

### Buffering Strategy
1. **Small Responses**: Buffer and send immediately for responsiveness
2. **Large Responses**: Send in chunks to maintain streaming feel
3. **Function Calls**: Send immediate notification without buffering

### Connection Management
1. **Keep-Alive**: Send periodic empty updates during long function calls
2. **Graceful Closure**: Ensure connections close properly on completion or error
3. **Cancellation Handling**: Properly handle client disconnections

This streaming behavior implementation provides transparency during function execution while maintaining a smooth user experience.