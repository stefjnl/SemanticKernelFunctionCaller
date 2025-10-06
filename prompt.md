    # Semantic Kernel Integration Implementation Prompt

    ## Context
    You're implementing Phase 2 of SemanticKernelFunctionCaller, a Clean Architecture .NET 8 application currently using Microsoft.Extensions.AI for chat completions. The MVP is complete with working provider abstraction (OpenRouter, NanoGPT), streaming chat, and a vanilla JS frontend.

    ## Objective
    Add Semantic Kernel as an orchestration layer **above** Microsoft.Extensions.AI to enable advanced AI workflows, prompt templates, and multi-step orchestration while maintaining Clean Architecture principles.

    ## Architecture Principles
    - **Dependency Flow**: API → Application → Domain ← Infrastructure
    - **Semantic Kernel Location**: Application layer only (business logic orchestration)
    - **Microsoft.Extensions.AI**: Remains in Infrastructure layer (provider implementation detail)
    - **No Leakage**: Domain layer stays pure, Infrastructure never references Application interfaces

    ## Implementation Requirements

    ### 1. Application Layer - IAIOrchestrationService Interface

    **Create**: `ChatCompletionService.Application/Interfaces/IAIOrchestrationService.cs`

    Define interface with:
    - Signature for orchestrated chat with automatic function calling
    - Signature for prompt template execution with variable substitution
    - Signature for multi-step workflow execution (plan-and-execute pattern)
    - Return types using existing Application DTOs (ChatResponseDto, StreamingChatUpdate)
    - Support for CancellationToken on all async operations

    **Key Design Decision**: This interface abstracts Semantic Kernel concepts (plugins, prompts, planners) into domain-agnostic orchestration methods.

    ### 2. Application Layer - Prompt Template DTOs

    **Create**: `ChatCompletionService.Application/DTOs/PromptTemplateDto.cs`

    Define DTO structure for:
    - Template name/identifier
    - Template content with variable placeholders
    - Input variables dictionary (string key-value pairs)
    - Optional execution settings (temperature, max tokens)

    **Create**: `ChatCompletionService.Application/DTOs/WorkflowRequestDto.cs`

    Define DTO for multi-step workflows:
    - Goal/objective description
    - Context data for workflow
    - Available function/plugin names to use
    - Maximum steps allowed

    ### 3. Infrastructure Layer - Semantic Kernel Wrapper

    **Create**: `ChatCompletionService.Infrastructure/Orchestration/SemanticKernelOrchestrationService.cs`

    Implement IAIOrchestrationService with:

    **Constructor Dependencies**:
    - IProviderFactory (to get IChatCompletionService instances)
    - ILogger<SemanticKernelOrchestrationService>
    - Configuration for default provider/model selection

    **Core Responsibilities**:
    - Build Semantic Kernel instance wrapping Microsoft.Extensions.AI IChatClient
    - Register all kernel plugins (implemented separately)
    - Configure FunctionChoiceBehavior for automatic function calling
    - Handle prompt template rendering with variable substitution
    - Execute multi-step workflows using Semantic Kernel's planning capabilities
    - Convert between Semantic Kernel types and Application DTOs

    **Critical Implementation Detail**: Use Semantic Kernel's `kernel.CreateChatCompletionService()` or similar to wrap the existing Microsoft.Extensions.AI IChatClient from your providers. The chain should be: Semantic Kernel → Microsoft.Extensions.AI → OpenRouter/NanoGPT.

    ### 4. Infrastructure Layer - Kernel Plugin Architecture

    **Create**: `ChatCompletionService.Infrastructure/Plugins/` directory structure

    **Base Plugin Interface**:
    - Create `IKernelPluginProvider.cs` in Application/Interfaces
    - Define method to return plugin registration information
    - Keep Application layer unaware of Semantic Kernel specifics

    **Sample Plugin Implementation** (Weather example):
    - Create `WeatherPlugin.cs` with [KernelFunction] attributes
    - Include XML documentation on functions (Semantic Kernel uses these for LLM descriptions)
    - Implement simple mock weather data retrieval
    - Show parameter handling with [Description] attributes for the LLM

    **Plugin Registration Pattern**:
    - Create factory or registry pattern to discover and register plugins
    - Allow plugins to be enabled/disabled via configuration
    - Support dependency injection for plugin services

    ### 5. Infrastructure Layer - Prompt Template Management

    **Create**: `ChatCompletionService.Infrastructure/Orchestration/PromptTemplateManager.cs`

    Implement service for:
    - Loading prompt templates from configuration or embedded resources
    - Variable substitution using Semantic Kernel's template engine
    - Template validation (ensure all required variables provided)
    - Caching compiled templates for performance

    **Sample Templates to Include**:
    - "Summarize Conversation" - condenses conversation history
    - "Extract Entities" - pulls structured data from text
    - "Rewrite Tone" - adjusts message formality/style

    ### 6. Application Layer - Orchestration Use Cases

    **Create**: `ChatCompletionService.Application/UseCases/SendOrchestralChatMessageUseCase.cs`

    Implement use case for:
    - Receiving ChatRequestDto with optional workflow hints
    - Calling IAIOrchestrationService with automatic function selection
    - Handling multi-turn function execution loops
    - Returning ChatResponseDto with metadata about functions called

    **Create**: `ChatCompletionService.Application/UseCases/ExecutePromptTemplateUseCase.cs`

    Implement use case for:
    - Receiving PromptTemplateDto with template name and variables
    - Executing template through IAIOrchestrationService
    - Returning rendered result as ChatResponseDto

    ### 7. API Layer - New Controller Endpoints

    **Extend**: `ChatCompletionService.API/Controllers/ChatController.cs`

    Add endpoints:
    - `POST /api/chat/orchestrated` - sends message with automatic function calling
    - `POST /api/chat/orchestrated/stream` - streaming version with function execution
    - `POST /api/chat/prompt-template` - executes named prompt template
    - `GET /api/chat/templates` - returns available prompt templates
    - `POST /api/chat/workflow` - executes multi-step workflow (plan-and-execute)

    **Response Format Consideration**: Include metadata about function calls executed (function name, arguments, results) so frontend can display tool usage transparency.

    ### 8. Configuration Updates

    **Update**: `ChatCompletionService.API/appsettings.Development.json`

    Add section:
    ```
    "SemanticKernel": {
    "DefaultProvider": "OpenRouter",
    "DefaultModel": "google/gemini-2.5-flash-lite-preview-09-2025",
    "EnabledPlugins": ["Weather", "DateTime"],
    "PromptTemplates": {
        "Summarize": "...",
        "ExtractEntities": "..."
    }
    }
    ```

    ### 9. Dependency Injection Registration

    **Update**: `ChatCompletionService.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

    Add registration for:
    - IAIOrchestrationService → SemanticKernelOrchestrationService (Scoped)
    - Kernel plugin providers (Singleton)
    - PromptTemplateManager (Singleton)
    - Semantic Kernel NuGet package reference: `Microsoft.SemanticKernel`

    ### 10. Testing Strategy Placeholder

    **Document** (no implementation yet):
    - Unit tests for PromptTemplateManager variable substitution
    - Integration tests for plugin execution with mock LLM responses
    - End-to-end test for multi-step workflow with weather plugin

    ## Key Constraints

    1. **No Breaking Changes**: Existing `/api/chat/stream` endpoints must continue working unchanged
    2. **Provider Agnostic**: Semantic Kernel wrapper works with any provider implementing IChatCompletionService
    3. **Clean Architecture**: No Semantic Kernel types in Domain, no Application interfaces in Infrastructure
    4. **Gradual Adoption**: Frontend can use either traditional chat or new orchestrated endpoints
    5. **Error Handling**: Wrap Semantic Kernel exceptions into application-specific exceptions

    ## Success Criteria

    - [x] IAIOrchestrationService interface defined with clear abstractions
    - [x] Semantic Kernel successfully wraps Microsoft.Extensions.AI IChatClient
    - [x] At least one working plugin (Weather) with [KernelFunction] attributes
    - [x] Automatic function calling works in orchestrated endpoint
    - [x] One prompt template successfully executes with variable substitution
    - [x] Multi-step workflow endpoint demonstrates plan-and-execute pattern
    - [x] Clean Architecture maintained (dependency flow verified)
    - [x] Existing chat endpoints unaffected (regression test passes)