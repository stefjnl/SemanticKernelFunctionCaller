# IAIOrchestrationService Interface Design

## Overview
The IAIOrchestrationService interface defines the contract for the Semantic Kernel orchestration layer in the Application layer. It abstracts Semantic Kernel concepts (plugins, prompts, planners) into domain-agnostic orchestration methods.

## Interface Definition

```csharp
using SemanticKernelFunctionCaller.Application.DTOs;
using System.Runtime.CompilerServices;

namespace SemanticKernelFunctionCaller.Application.Interfaces
{
    public interface IAIOrchestrationService
    {
        /// <summary>
        /// Sends a chat message with automatic function calling through Semantic Kernel orchestration.
        /// </summary>
        /// <param name="request">Chat request containing messages and provider information.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Chat response with content and metadata about function calls executed.</returns>
        Task<ChatResponseDto> SendOrchestratedMessageAsync(
            ChatRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams a chat message with automatic function calling through Semantic Kernel orchestration.
        /// </summary>
        /// <param name="request">Chat request containing messages and provider information.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Async enumerable of streaming chat updates with content and metadata.</returns>
        IAsyncEnumerable<StreamingChatUpdate> StreamOrchestratedMessageAsync(
            ChatRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a prompt template with variable substitution.
        /// </summary>
        /// <param name="templateRequest">Prompt template request with name and variables.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Chat response with rendered template result and metadata.</returns>
        Task<ChatResponseDto> ExecutePromptTemplateAsync(
            PromptTemplateDto templateRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a multi-step workflow using the plan-and-execute pattern.
        /// </summary>
        /// <param name="workflowRequest">Workflow request with goal and context.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Chat response with final result and metadata about steps executed.</returns>
        Task<ChatResponseDto> ExecuteWorkflowAsync(
            WorkflowRequestDto workflowRequest,
            CancellationToken cancellationToken = default);
    }
}
```

## Supporting DTOs

### PromptTemplateDto
```csharp
namespace SemanticKernelFunctionCaller.Application.DTOs
{
    public class PromptTemplateDto
    {
        /// <summary>
        /// Template name/identifier
        /// </summary>
        public required string TemplateName { get; set; }

        /// <summary>
        /// Template content with variable placeholders
        /// </summary>
        public required string TemplateContent { get; set; }

        /// <summary>
        /// Input variables dictionary (string key-value pairs)
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new();

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

### WorkflowRequestDto
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

## Design Considerations

1. **Clean Architecture Compliance**: The interface is defined in the Application layer without any Semantic Kernel specific types or dependencies.

2. **Domain-Agnostic Methods**: The interface abstracts Semantic Kernel concepts into business-oriented method names:
   - `SendOrchestratedMessageAsync` for orchestrated chat
   - `ExecutePromptTemplateAsync` for prompt template execution
   - `ExecuteWorkflowAsync` for multi-step workflows

3. **Consistent Return Types**: All methods return existing Application DTOs (ChatResponseDto, StreamingChatUpdate) to maintain consistency with the rest of the application.

4. **CancellationToken Support**: All async methods support cancellation for better resource management.

5. **Metadata Inclusion**: Return types should include metadata about function calls executed so the frontend can display tool usage transparency.

6. **Backward Compatibility**: The interface is designed to work alongside existing interfaces without breaking changes.

7. **Extensibility**: The design allows for future extensions to support additional Semantic Kernel features without changing the core interface.

## Integration Points

1. **Use Cases**: New orchestrated use cases will consume this interface:
   - SendOrchestralChatMessageUseCase
   - ExecutePromptTemplateUseCase

2. **Implementation**: The SemanticKernelOrchestrationService in the Infrastructure layer will implement this interface.

3. **Dependency Injection**: The interface will be registered in the service collection for dependency injection.

4. **API Layer**: New controller endpoints will use use cases that consume this interface.