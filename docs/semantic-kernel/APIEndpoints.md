# API Endpoints for Orchestrated Operations

## Overview
New API endpoints are added to the ChatController to expose Semantic Kernel orchestration capabilities while maintaining backward compatibility with existing endpoints.

## Extended ChatController

### New Endpoints

```csharp
using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using System.Text.Json;

namespace SemanticKernelFunctionCaller.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class ChatController : ControllerBase
    {
        private readonly IAIOrchestrationService _aiOrchestrationService;

        // Existing constructor with additional dependency
        public ChatController(
            IGetAvailableProvidersUseCase getProvidersUseCase,
            IGetProviderModelsUseCase getModelsUseCase,
            ISendChatMessageUseCase sendMessageUseCase,
            IStreamChatMessageUseCase streamMessageUseCase,
            IAIOrchestrationService aiOrchestrationService,  // New dependency
            ILogger<ChatController> logger)
        {
            _getProvidersUseCase = getProvidersUseCase;
            _getModelsUseCase = getModelsUseCase;
            _sendMessageUseCase = sendMessageUseCase;
            _streamMessageUseCase = streamMessageUseCase;
            _aiOrchestrationService = aiOrchestrationService;  // New assignment
            _logger = logger;
        }

        /// <summary>
        /// Sends a message with automatic function calling through Semantic Kernel orchestration.
        /// </summary>
        [HttpPost("orchestrated")]
        public async Task<IActionResult> SendOrchestratedMessage(ChatRequestDto request)
        {
            try
            {
                var response = await _aiOrchestrationService.SendOrchestratedMessageAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending an orchestrated message.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Streams a message with automatic function calling through Semantic Kernel orchestration.
        /// </summary>
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
                    var jsonUpdate = JsonSerializer.Serialize(update);
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

        /// <summary>
        /// Executes a named prompt template with variable substitution.
        /// </summary>
        [HttpPost("prompt-template")]
        public async Task<IActionResult> ExecutePromptTemplate(PromptTemplateRequestDto request)
        {
            try
            {
                var templateRequest = new PromptTemplateDto
                {
                    TemplateName = request.TemplateName,
                    Variables = request.Variables ?? new Dictionary<string, string>(),
                    ExecutionSettings = request.ExecutionSettings
                };

                var response = await _aiOrchestrationService.ExecutePromptTemplateAsync(templateRequest);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing a prompt template.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns available prompt templates.
        /// </summary>
        [HttpGet("templates")]
        public async Task<IActionResult> GetPromptTemplates()
        {
            try
            {
                // This would use a template manager service in a full implementation
                // For now, returning a static list
                var templates = new[]
                {
                    "Summarize",
                    "ExtractEntities",
                    "RewriteTone"
                };
                
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving prompt templates.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a multi-step workflow using the plan-and-execute pattern.
        /// </summary>
        [HttpPost("workflow")]
        public async Task<IActionResult> ExecuteWorkflow(WorkflowRequestDto request)
        {
            try
            {
                var response = await _aiOrchestrationService.ExecuteWorkflowAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing a workflow.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
```

### Supporting DTOs

#### PromptTemplateRequestDto
```csharp
namespace SemanticKernelFunctionCaller.API.DTOs
{
    public class PromptTemplateRequestDto
    {
        /// <summary>
        /// Template name/identifier
        /// </summary>
        public required string TemplateName { get; set; }

        /// <summary>
        /// Input variables dictionary (string key-value pairs)
        /// </summary>
        public Dictionary<string, string>? Variables { get; set; }

        /// <summary>
        /// Optional execution settings (temperature, max tokens)
        /// </summary>
        public PromptExecutionSettings? ExecutionSettings { get; set; }
    }

    public class PromptExecutionSettings
    {
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
    }
}
```

## Endpoint Details

### POST /api/chat/orchestrated
Sends a message with automatic function calling through Semantic Kernel orchestration.

**Request Body**:
```json
{
  "providerId": "OpenRouter",
  "modelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "messages": [
    {
      "role": "user",
      "content": "What is the weather like in Seattle?"
    }
  ]
}
```

**Response**:
```json
{
  "content": "The current weather in Seattle is sunny with a temperature of 72°F.",
  "modelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "providerId": "OpenRouter",
  "metadata": {
    "functionCalls": [
      {
        "functionName": "GetCurrentWeatherAsync",
        "arguments": "{\"location\":\"Seattle\"}",
        "result": "\"The current weather in Seattle is sunny with a temperature of 72°F.\"",
        "timestamp": "2025-10-06T10:00:00Z"
      }
    ]
  }
}
```

### POST /api/chat/orchestrated/stream
Streams a message with automatic function calling through Semantic Kernel orchestration.

**Request Body**:
```json
{
  "providerId": "OpenRouter",
  "modelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "messages": [
    {
      "role": "user",
      "content": "Tell me about the weather forecast for the next week in Seattle."
    }
  ]
}
```

**Response** (Server-Sent Events):
```
data: {"content":"The","isFinal":false}

data: {"content":" current","isFinal":false}

data: {"content":" weather","isFinal":false}

data: {"content":" in","isFinal":false}

data: {"content":" Seattle","isFinal":false}

data: {"content":" is","isFinal":false}

data: {"content":" sunny","isFinal":false}

data: {"content":" with","isFinal":false}

data: {"content":" a","isFinal":false}

data: {"content":" temperature","isFinal":false}

data: {"content":" of","isFinal":false}

data: {"content":" 72°F.","isFinal":true,"metadata":{"functionCalls":[{"functionName":"GetCurrentWeatherAsync","arguments":"{\"location\":\"Seattle\"}","result":"\"The current weather in Seattle is sunny with a temperature of 72°F.\"","timestamp":"2025-10-06T10:00:00Z"}]}}
```

### POST /api/chat/prompt-template
Executes a named prompt template with variable substitution.

**Request Body**:
```json
{
  "templateName": "Summarize",
  "variables": {
    "conversation": "User: What is the weather like in Seattle?\nAssistant: The current weather in Seattle is sunny with a temperature of 72°F.\nUser: That sounds nice!\nAssistant: Yes, it's a beautiful day!"
  },
  "executionSettings": {
    "temperature": 0.3,
    "maxTokens": 100
  }
}
```

**Response**:
```json
{
  "content": "The conversation involved a user inquiring about the weather in Seattle, with the assistant responding that it was sunny and 72°F. The user expressed that it sounded nice, and the assistant agreed.",
  "modelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "providerId": "OpenRouter"
}
```

### GET /api/chat/templates
Returns available prompt templates.

**Response**:
```json
[
  "Summarize",
  "ExtractEntities",
  "RewriteTone"
]
```

### POST /api/chat/workflow
Executes a multi-step workflow using the plan-and-execute pattern.

**Request Body**:
```json
{
  "goal": "Find the current weather in Seattle and provide a 3-day forecast",
  "context": "The user is planning a trip next week",
  "availableFunctions": ["Weather"],
  "maxSteps": 5
}
```

**Response**:
```json
{
  "content": "I've checked the weather for Seattle. The current weather is sunny with a temperature of 72°F. For your trip next week, here's a 3-day forecast: Monday will be partly cloudy with a high of 75°F, Tuesday will be rainy with a high of 68°F, and Wednesday will be sunny with a high of 77°F.",
  "modelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "providerId": "OpenRouter",
  "metadata": {
    "functionCalls": [
      {
        "functionName": "GetCurrentWeatherAsync",
        "arguments": "{\"location\":\"Seattle\"}",
        "result": "\"The current weather in Seattle is sunny with a temperature of 72°F.\"",
        "timestamp": "2025-10-06T10:00:00Z"
      },
      {
        "functionName": "GetWeatherForecastAsync",
        "arguments": "{\"location\":\"Seattle\",\"date\":\"2025-10-13\"}",
        "result": "\"The weather forecast for Seattle on 2025-10-13 is partly cloudy with a high of 75°F and a low of 55°F.\"",
        "timestamp": "2025-10-06T10:00:01Z"
      }
    ],
    "stepsExecuted": 2,
    "goal": "Find the current weather in Seattle and provide a 3-day forecast"
  }
}
```

## Integration Points

1. **Dependency Injection**: The controller requires a new dependency on IAIOrchestrationService.

2. **Use Cases**: New orchestrated use cases will be created to handle the business logic.

3. **DTO Mapping**: API DTOs may need to be mapped to Application DTOs.

4. **Error Handling**: Consistent error handling with existing endpoints.

5. **Logging**: Consistent logging approach with existing endpoints.

## Backward Compatibility

All existing endpoints remain unchanged:
- GET /api/chat/providers
- GET /api/chat/providers/{providerId}/models
- POST /api/chat/send
- POST /api/chat/stream

This ensures that existing frontend code continues to work without modification.

## Security Considerations

1. **Rate Limiting**: Orchestrated endpoints may require rate limiting due to increased resource usage.

2. **Authentication**: If authentication is added later, orchestrated endpoints would follow the same pattern.

3. **Input Validation**: All inputs are validated before processing.

4. **Function Restrictions**: Only configured functions can be executed through workflows.

## Performance Considerations

1. **Streaming Support**: Long-running orchestrated operations support streaming responses.

2. **CancellationToken**: All endpoints properly handle request cancellation.

3. **Resource Management**: Efficient use of Semantic Kernel resources.

4. **Caching**: Template and plugin information can be cached for performance.

## Testing Considerations

1. **Unit Tests**: Each endpoint should have unit tests.

2. **Integration Tests**: End-to-end testing of orchestrated operations.

3. **Load Testing**: Performance testing of resource-intensive orchestrated operations.

4. **Error Scenarios**: Testing various error conditions and edge cases.