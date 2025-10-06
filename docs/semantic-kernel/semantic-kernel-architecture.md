# Semantic Kernel Integration Architecture

## Overview
This document describes the planned architecture for integrating Semantic Kernel into the existing Clean Architecture .NET 8 application. The integration adds an orchestration layer above Microsoft.Extensions.AI to enable advanced AI workflows, prompt templates, and multi-step orchestration while maintaining Clean Architecture principles.

## Architecture Layers

### Domain Layer (Unchanged)
- Contains core business rules and entities
- Remains completely independent with no dependencies on Semantic Kernel
- Entities: ChatMessage, ChatResponse, ConversationContext
- Enums: ChatRole, MessageType, ProviderType
- Value Objects: ModelConfiguration, ProviderMetadata

### Application Layer
- Contains use cases and orchestration interfaces
- New IAIOrchestrationService interface for Semantic Kernel orchestration
- New DTOs for prompt templates and workflows
- New use cases for orchestrated operations

### Infrastructure Layer
- Implements Semantic Kernel orchestration service
- Contains Semantic Kernel wrapper implementation
- Houses plugin implementations with [KernelFunction] attributes
- Manages prompt templates and workflow execution
- Maintains existing Microsoft.Extensions.AI provider implementations

### API Layer
- Exposes new endpoints for orchestrated operations
- Maintains backward compatibility with existing endpoints
- Maps between API models and Application DTOs

## Component Interactions

### Data Flow
1. API Controller receives request for orchestrated operation
2. New orchestrated use cases consume IAIOrchestrationService
3. IAIOrchestrationService implementation (SemanticKernelOrchestrationService) uses IProviderFactory to get IChatCompletionService instances
4. Semantic Kernel wraps Microsoft.Extensions.AI IChatClient instances
5. Plugins registered with Semantic Kernel provide extended functionality
6. Prompt templates managed by PromptTemplateManager
7. Responses converted from Semantic Kernel types to Application DTOs

### Dependency Flow
- API → Application → Domain ← Infrastructure
- Semantic Kernel positioned in Application layer (orchestration only)
- Microsoft.Extensions.AI remains in Infrastructure layer (provider implementation)
- No leakage of Semantic Kernel types to Domain layer
- Infrastructure references Application interfaces for plugin registration

## Key Components

### IAIOrchestrationService (Application Layer)
- Interface defining orchestration capabilities
- Abstracts Semantic Kernel concepts into domain-agnostic methods
- Methods for orchestrated chat, prompt templates, and workflows
- Returns existing Application DTOs (ChatResponseDto, StreamingChatUpdate)

### SemanticKernelOrchestrationService (Infrastructure Layer)
- Implementation of IAIOrchestrationService
- Wraps Microsoft.Extensions.AI IChatClient with Semantic Kernel
- Registers plugins and configures automatic function calling
- Handles prompt template rendering and workflow execution
- Converts between Semantic Kernel types and Application DTOs

### Plugin Architecture
- IKernelPluginProvider interface in Application layer
- Plugin implementations in Infrastructure layer with [KernelFunction] attributes
- Registration pattern allowing configuration-based enabling/disabling
- Sample WeatherPlugin for demonstration

### Prompt Template Management
- PromptTemplateManager service in Infrastructure layer
- Loads templates from configuration or embedded resources
- Handles variable substitution and template validation
- Caches compiled templates for performance

### New Use Cases (Application Layer)
- SendOrchestralChatMessageUseCase for orchestrated chat
- ExecutePromptTemplateUseCase for template execution
- Handle multi-turn function execution loops
- Return metadata about functions called

### API Endpoints
- POST /api/chat/orchestrated - orchestrated chat with function calling
- POST /api/chat/orchestrated/stream - streaming orchestrated chat
- POST /api/chat/prompt-template - execute named prompt template
- GET /api/chat/templates - list available prompt templates
- POST /api/chat/workflow - execute multi-step workflow

## Clean Architecture Compliance
- Domain layer remains pure with no external dependencies
- Application layer defines abstractions without Semantic Kernel specifics
- Infrastructure layer implements Semantic Kernel integration
- No direct references from Infrastructure to Application interfaces
- Dependency inversion through interfaces