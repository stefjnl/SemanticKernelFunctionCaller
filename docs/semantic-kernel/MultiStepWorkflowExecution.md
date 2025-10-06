# Multi-Step Workflow Execution Approach

## Overview
The multi-step workflow execution approach implements a plan-and-execute pattern using Semantic Kernel's planning capabilities. This allows for complex, multi-step operations to be executed automatically to achieve a specified goal.

## Workflow Execution Design

### WorkflowRequestDto (Application Layer)
Defines the structure for requesting a multi-step workflow execution.

```csharp
namespace SemanticKernelFunctionCaller.Application.DTOs
{
    public class WorkflowRequestDto
    {
        /// <summary>
        /// Goal/objective description
        /// </summary>
        public required string Goal { get; set; }

        /// <summary>
        /// Context data for workflow
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Available function/plugin names to use
        /// </summary>
        public List<string> AvailableFunctions { get; set; } = new();

        /// <summary>
        /// Maximum steps allowed
        /// </summary>
        public int MaxSteps { get; set; } = 10;
    }
}
```

### Workflow Execution Process

1. **Goal Definition**: User specifies a goal and optional context information
2. **Planning Phase**: Semantic Kernel creates a plan to achieve the goal
3. **Execution Phase**: The plan is executed step-by-step with monitoring
4. **Result Compilation**: Final result and execution metadata are compiled
5. **Response Generation**: ChatResponseDto is created with results and metadata

### Implementation in SemanticKernelOrchestrationService

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using SemanticKernelFunctionCaller.Application.DTOs;

public partial class SemanticKernelOrchestrationService
{
    public async Task<ChatResponseDto> ExecuteWorkflowAsync(
        WorkflowRequestDto workflowRequest,
        CancellationToken cancellationToken = default)
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

        // Execute the plan
        var goal = workflowRequest.Context != null 
            ? $"{workflowRequest.Goal}\n\nContext: {workflowRequest.Context}" 
            : workflowRequest.Goal;

        var planResult = await planner.ExecuteAsync(kernel, goal, cancellationToken);

        // Compile execution metadata
        var functionCalls = new List<FunctionCallMetadata>();
        if (planResult.ChatHistory != null)
        {
            foreach (var message in planResult.ChatHistory)
            {
                // Extract function call information from chat history
                // This is a simplified representation - actual implementation would parse the history
                if (message.Content?.Contains("\"function_call\"") == true)
                {
                    functionCalls.Add(new FunctionCallMetadata
                    {
                        FunctionName = "parsed_from_message",
                        Arguments = "parsed_from_message",
                        Result = "parsed_from_message"
                    });
                }
            }
        }

        // Create response with result and metadata
        return new ChatResponseDto
        {
            Content = planResult.FinalAnswer,
            ProviderId = workflowRequest.ProviderId,
            ModelId = workflowRequest.ModelId,
            Metadata = new Dictionary<string, object>
            {
                ["FunctionCalls"] = functionCalls,
                ["StepsExecuted"] = planResult.Iterations,
                ["Goal"] = workflowRequest.Goal
            }
        };
    }
}
```

### FunctionCallMetadata DTO
Represents metadata about function calls executed during workflow execution.

```csharp
namespace SemanticKernelFunctionCaller.Application.DTOs
{
    public class FunctionCallMetadata
    {
        public string FunctionName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
```

### Enhanced ChatResponseDto
Updated to include metadata about function calls and workflow execution.

```csharp
namespace SemanticKernelFunctionCaller.Application.DTOs
{
    public class ChatResponseDto
    {
        public required string Content { get; set; }
        public required string ModelId { get; set; }
        public required string ProviderId { get; set; }
        
        /// <summary>
        /// Additional metadata about the response, including function calls executed
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
```

## Plan-and-Execute Pattern Details

### Planning Phase
1. **Goal Analysis**: The LLM analyzes the goal and context to understand requirements
2. **Capability Assessment**: Available functions/plugins are evaluated for relevance
3. **Plan Generation**: A sequence of steps is created to achieve the goal
4. **Validation**: The plan is validated for feasibility and safety

### Execution Phase
1. **Step Execution**: Each step in the plan is executed sequentially
2. **Result Monitoring**: Outcomes of each step are monitored and recorded
3. **Adaptation**: If a step fails, the plan may be adapted or replanned
4. **Progress Tracking**: Execution progress is tracked against the maximum steps allowed

### Error Handling and Safety
1. **Iteration Limits**: Maximum steps prevent infinite loops
2. **Function Restrictions**: Only specified functions can be used
3. **CancellationToken Support**: Operations can be cancelled
4. **Error Propagation**: Errors are properly propagated with context

## Integration Points

1. **IAIOrchestrationService**: Defines the ExecuteWorkflowAsync method contract
2. **SemanticKernelOrchestrationService**: Implements the workflow execution logic
3. **Plugin System**: Workflows can utilize registered plugins
4. **API Layer**: Exposes workflow execution through a dedicated endpoint
5. **Use Cases**: SendOrchestralChatMessageUseCase can trigger workflows based on request hints

## Configuration Considerations

```json
{
  "SemanticKernel": {
    "DefaultProvider": "OpenRouter",
    "DefaultModel": "google/gemini-2.5-flash-lite-preview-09-2025",
    "MaxWorkflowSteps": 15
  }
}
```

## Design Considerations

1. **Scalability**: The approach can handle complex workflows with multiple steps
2. **Transparency**: Detailed metadata is provided about function calls executed
3. **Control**: Users can limit available functions and maximum steps
4. **Flexibility**: Workflows can be adapted based on intermediate results
5. **Monitoring**: Execution progress and results are tracked
6. **Safety**: Iteration limits and function restrictions prevent runaway execution
7. **Extensibility**: The design allows for different planning algorithms in the future

## Sample Workflow Execution

### Request
```json
{
  "Goal": "Find the current weather in Seattle and provide a 3-day forecast",
  "Context": "The user is planning a trip next week",
  "AvailableFunctions": ["Weather"],
  "MaxSteps": 5
}
```

### Execution Process
1. Planner identifies that the Weather plugin is needed
2. Planner creates a two-step plan:
   - Step 1: Call GetCurrentWeatherAsync for Seattle
   - Step 2: Call GetWeatherForecastAsync for Seattle for the next 3 days
3. Steps are executed sequentially
4. Results are compiled into a coherent response

### Response
```json
{
  "Content": "I've checked the weather for Seattle. The current weather is sunny with a temperature of 72°F. For your trip next week, here's a 3-day forecast: Monday will be partly cloudy with a high of 75°F, Tuesday will be rainy with a high of 68°F, and Wednesday will be sunny with a high of 77°F.",
  "ModelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "ProviderId": "OpenRouter",
  "Metadata": {
    "FunctionCalls": [
      {
        "FunctionName": "GetCurrentWeatherAsync",
        "Arguments": "{\"location\":\"Seattle\"}",
        "Result": "\"The current weather in Seattle is sunny with a temperature of 72°F.\"",
        "Timestamp": "2025-10-06T10:00:00Z"
      },
      {
        "FunctionName": "GetWeatherForecastAsync",
        "Arguments": "{\"location\":\"Seattle\",\"date\":\"2025-10-13\"}",
        "Result": "\"The weather forecast for Seattle on 2025-10-13 is partly cloudy with a high of 75°F and a low of 55°F.\"",
        "Timestamp": "2025-10-06T10:00:01Z"
      }
    ],
    "StepsExecuted": 2,
    "Goal": "Find the current weather in Seattle and provide a 3-day forecast"
  }
}
```

## Future Enhancements

1. **Parallel Execution**: Support for executing independent steps in parallel
2. **Conditional Logic**: More sophisticated conditional workflow execution
3. **State Management**: Persistent state management for long-running workflows
4. **Human-in-the-Loop**: Integration points for human approval during execution
5. **Workflow Visualization**: Tools to visualize and debug workflow execution
6. **Advanced Planning**: Integration with more sophisticated planning algorithms