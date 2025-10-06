# Product Requirements Document (PRD)

## SemanticKernelFunctionCaller - Enterprise AI Chat Application

**Version:** 1.0 (Phase 1 - MVP)  
**Date:** October 2025  
**Author:** Portfolio Project  
**Status:** Draft for Implementation

---

## 1. Executive Summary

SemanticKernelFunctionCaller is an enterprise-grade AI chat application built to demonstrate mastery of .NET 9, Clean Architecture principles, and Microsoft's AI ecosystem (Microsoft.Extensions.AI with future Semantic Kernel integration). The application provides a unified interface for interacting with multiple AI providers while maintaining clean separation of concerns and scalability for future enhancements.

**Phase 1 Goal:** Build a solid foundation with provider abstraction, streaming chat, and clean architecture that can be extended in Phase 2 with Semantic Kernel, function calling, and advanced features.

---

## 2. Business Objectives

### Primary Objectives

- **Portfolio Showcase**: Demonstrate Clean Architecture, SOLID/DRY principles, and modern .NET practices
- **Learning Platform**: Foundation for exploring Microsoft.Extensions.AI and Semantic Kernel
- **Personal Tool**: Unified interface for multiple AI providers/models

### Success Criteria

- Clean, maintainable codebase adhering to Clean Architecture
- Seamless provider/model switching via UI
- Working streaming chat with multiple providers
- Scalable architecture ready for Phase 2 enhancements

---

## 3. Architecture Overview

### 3.1 Clean Architecture Layers

**Domain Layer** (Core Business Logic)

- **Entities**: `ChatMessage`, `ChatResponse`, `ConversationContext`
- **Enums**: `ChatRole`, `ProviderType`, `MessageType`
- **Value Objects**: `ModelConfiguration`, `ProviderMetadata`
- **Domain Interfaces**: No external dependencies

**Application Layer** (Use Cases & Business Rules)

- **Interfaces**:
    - `ISemanticKernelFunctionCaller` - Core chat abstraction
    - `IProviderFactory` - Creates provider instances
    - `IModelConfigurationService` - Manages model metadata
- **DTOs**: `ChatRequestDto`, `ChatResponseDto`, `ModelInfoDto`, `ProviderInfoDto`
- **Use Cases**:
    - `SendChatMessageUseCase` - Orchestrates chat flow
    - `GetAvailableProvidersUseCase` - Returns provider list
    - `GetProviderModelsUseCase` - Returns models for selected provider

**Infrastructure Layer** (External Concerns)

- **Provider Implementations**:
    - `OpenRouterChatProvider : ISemanticKernelFunctionCaller`
    - `NanoGptChatProvider : ISemanticKernelFunctionCaller`
- **Factories**: `ChatProviderFactory : IProviderFactory`
- **Configuration**: `ProviderConfigurationManager`
- **Microsoft.Extensions.AI Integration**: Wrapper around `IChatClient`

**API Layer** (Presentation)

- **Controllers**: `ChatController` (streaming endpoints)
- **Middleware**: Basic exception handling (enhanced in Phase 2)
- **Static Files**: wwwroot (HTML/JS/CSS frontend)
- **DTOs**: API-specific request/response models

**AppHost Layer** (.NET Aspire)

- Service discovery
- Configuration management
- Local development orchestration

### 3.2 Dependency Flow

```
API → Application → Domain ← Infrastructure
                      ↑
                   AppHost
```

**Key Principle**: Dependencies point inward. Domain has zero external dependencies.

---

## 4. Microsoft.Extensions.AI Integration Strategy

### 4.1 Architecture Pattern

**Provider Implementation Wrapper Pattern**: Each provider (OpenRouter, NanoGPT) implements `ISemanticKernelFunctionCaller` (Application layer interface) but internally uses Microsoft.Extensions.AI's `IChatClient`.

**Why This Approach:**

- Application layer remains provider-agnostic
- Infrastructure layer handles provider-specific details
- Easy to swap/add providers without touching Application/Domain
- Microsoft.Extensions.AI provides standardized streaming, error handling, and future middleware support

### 4.2 Provider Implementation Structure

**OpenRouterChatProvider Example:**

```csharp
// Infrastructure/Providers/OpenRouterChatProvider.cs
public class OpenRouterChatProvider : ISemanticKernelFunctionCaller
{
    private readonly IChatClient _chatClient;
    
    public OpenRouterChatProvider(string apiKey, string defaultModel)
    {
        var client = new ChatClient(
            defaultModel,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions 
            { 
                Endpoint = new Uri("https://openrouter.ai/api/v1/") 
            });
        
        _chatClient = client.AsIChatClient();
    }
    
    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        // Convert domain ChatMessage[] to Microsoft.Extensions.AI ChatMessage[]
        // Call _chatClient.GetResponseAsync()
        // Convert back to domain ChatResponse
    }
    
    public IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(ChatRequest request)
    {
        // Use _chatClient.GetStreamingResponseAsync()
    }
}
```

**Key Points:**

- Microsoft.Extensions.AI's `IChatClient` is an infrastructure detail
- Application layer only knows about `ISemanticKernelFunctionCaller`
- Clean conversion between domain models and Microsoft.Extensions.AI types
- Provider-specific configuration (endpoints, headers) isolated in Infrastructure

### 4.3 Factory Pattern for Provider Selection

**ChatProviderFactory:**

- Reads configuration from `appsettings.json`
- Instantiates correct provider based on runtime selection
- Uses Strategy Pattern for provider switching

**Configuration Structure:**

```json
{
  "ChatProviders": {
    "OpenRouter": {
      "ApiKey": "sk-or-v1-xxx",
      "Endpoint": "https://openrouter.ai/api/v1/",
      "Models": [
        {
          "Id": "anthropic/claude-3.5-sonnet",
          "DisplayName": "Claude 3.5 Sonnet",
          "ContextWindow": 200000
        },
        {
          "Id": "google/gemini-2.5-flash-lite-preview",
          "DisplayName": "Gemini 2.5 Flash Lite",
          "ContextWindow": 1000000
        }
      ]
    },
    "NanoGPT": {
      "ApiKey": "nano-xxx",
      "Endpoint": "https://nano-gpt.com/api/v1/",
      "Models": [
        {
          "Id": "chatgpt-4o-latest",
          "DisplayName": "GPT-4o Latest",
          "ContextWindow": 128000
        },
        {
          "Id": "chatgpt-4o-latest:online",
          "DisplayName": "GPT-4o with Web Search",
          "ContextWindow": 128000
        }
      ]
    }
  }
}
```

---

## 5. Functional Requirements

### 5.1 Core Features (Phase 1)

**FR-001: Provider Selection**

- User selects provider from dropdown (OpenRouter, NanoGPT)
- Selection triggers model dropdown population
- Provider/model selection persists in browser (localStorage)

**FR-002: Model Selection**

- Cascading dropdown: Provider selection determines available models
- Models loaded from appsettings.json configuration
- Display names shown to user, model IDs sent to API

**FR-003: Chat Messaging**

- User sends text messages
- System streams responses token-by-token (SSE)
- Client maintains conversation history locally
- Each request includes full conversation context

**FR-004: Conversation Management**

- Client-side history storage (JavaScript array)
- Clear conversation button (resets local state)
- No server-side persistence (Phase 2)

**FR-005: Response Streaming**

- Server-Sent Events (SSE) for streaming
- Token-by-token display in UI
- Graceful handling of connection interruptions

### 5.2 API Endpoints

**POST /api/chat/send**

- Request: `{ providerId, modelId, messages: ChatMessage[] }`
- Response: Complete `ChatResponse` object
- Use case: Non-streaming requests (if needed)

**POST /api/chat/stream**

- Request: Same as /send
- Response: SSE stream (text/event-stream)
- Streams tokens as they arrive from provider

**GET /api/providers**

- Response: `ProviderInfo[]` (available providers)
- Reads from configuration

**GET /api/providers/{providerId}/models**

- Response: `ModelInfo[]` (models for provider)
- Reads from configuration

---

## 6. Technical Architecture Details

### 6.1 Domain Models

**ChatMessage (Domain Entity):**

- Properties: `Role` (System/User/Assistant), `Content`, `Timestamp`, `Id`
- Immutable value object
- No dependencies on external libraries

**ChatResponse (Domain Entity):**

- Properties: `Message` (ChatMessage), `ModelUsed`, `ProviderUsed`, `TokenUsage`, `ResponseTime`
- Metadata about the response

**ConversationContext (Domain Aggregate):**

- Collection of `ChatMessage` objects
- Validation rules (max messages, token limits)
- Business logic for context management

### 6.2 Application Layer Interfaces

**ISemanticKernelFunctionCaller:**

```csharp
public interface ISemanticKernelFunctionCaller
{
    Task<ChatResponse> SendMessageAsync(
        ChatRequest request, 
        CancellationToken cancellationToken = default);
        
    IAsyncEnumerable<StreamingChatUpdate> StreamMessageAsync(
        ChatRequest request, 
        CancellationToken cancellationToken = default);
        
    ProviderMetadata GetMetadata();
}
```

**IProviderFactory:**

```csharp
public interface IProviderFactory
{
    ISemanticKernelFunctionCaller CreateProvider(ProviderType provider, string modelId);
    IEnumerable<ProviderInfo> GetAvailableProviders();
    IEnumerable<ModelInfo> GetModelsForProvider(ProviderType provider);
}
```

### 6.3 Infrastructure Implementation

**Microsoft.Extensions.AI Integration Points:**

- Each provider creates `ChatClient` from OpenAI.Chat namespace
- Calls `.AsIChatClient()` extension method (from Microsoft.Extensions.AI.OpenAI)
- Uses `GetResponseAsync()` for non-streaming
- Uses `GetStreamingResponseAsync()` for streaming
- Converts between domain models and Microsoft.Extensions.AI types

**Conversion Layer:** Provider implementations contain private methods:

- `ToDomainMessage(Microsoft.Extensions.AI.ChatMessage)` → `Domain.ChatMessage`
- `ToProviderMessage(Domain.ChatMessage)` → `Microsoft.Extensions.AI.ChatMessage`
- `ToDomainResponse(ChatResponse)` → `Domain.ChatResponse`

### 6.4 API Layer Implementation

**ChatController Streaming Endpoint:**

- Sets response headers: `Content-Type: text/event-stream`
- Calls `ISemanticKernelFunctionCaller.StreamMessageAsync()`
- Writes SSE format: `data: {json}\n\n`
- Handles client disconnection gracefully
- Flushes response stream after each token

**Exception Handling (Phase 1 - Basic):**

- Try/catch in controllers
- Return standardized error format
- Log errors with ILogger
- Phase 2: Global exception middleware

---

## 7. Frontend Requirements

### 7.1 UI Components

**Provider/Model Selector:**

- Two cascading dropdowns (Provider → Model)
- Loads providers from `/api/providers`
- Loads models from `/api/providers/{id}/models`
- Stores selection in localStorage

**Chat Interface:**

- Message list (scrollable, auto-scroll to bottom)
- Input textarea with Send button
- Clear conversation button
- Loading indicator during streaming

**Message Display:**

- User messages: right-aligned, blue background
- Assistant messages: left-aligned, gray background
- Markdown rendering support (Phase 2)
- Token-by-token animation during streaming

### 7.2 Frontend Technology Stack

- **HTML5**: Semantic markup
- **Vanilla JavaScript**: No framework dependencies (Phase 1)
- **Tailwind CSS**: Utility-first styling
- **EventSource API**: Server-Sent Events for streaming
- **LocalStorage**: Provider/model selection, conversation history (optional)

### 7.3 Client-Side State Management

**Conversation State:**

```javascript
const conversationState = {
    messages: [], // Array of {role, content, timestamp}
    selectedProvider: 'OpenRouter',
    selectedModel: 'anthropic/claude-3.5-sonnet',
    isStreaming: false
};
```

**State Persistence:**

- Provider/model selection → localStorage
- Conversation history → in-memory only (Phase 1)
- Phase 2: Optional conversation persistence

---

## 8. Configuration Management

### 8.1 AppSettings Structure

Detailed in Section 4.3 above. Key aspects:

**Provider Configuration:**

- API keys (user secrets in development)
- Endpoints
- Model lists with metadata (display name, context window)

**Application Settings:**

- Logging levels
- CORS policies
- Aspire service discovery

### 8.2 Environment-Specific Configuration

**Development:**

- `appsettings.Development.json`
- User secrets for API keys
- Verbose logging

**Production (Phase 2):**

- Environment variables
- Azure Key Vault integration
- Structured logging

---

## 9. .NET Aspire Integration

### 9.1 AppHost Configuration

**Service Registration:**

- API project registered as Aspire service
- Configuration propagation from AppHost to API
- Local development dashboard

**Service Discovery:**

- Phase 1: Single API service
- Phase 2: Could add background services, message queues, etc.

### 9.2 Development Experience

**Aspire Dashboard:**

- View logs from API service
- Monitor HTTP requests
- Configuration inspection
- Health checks (Phase 2)

---

## 10. Phase 2 Preparation (Future Enhancements)

### 10.1 Semantic Kernel Integration

**When to Add Semantic Kernel:**

- Function calling/tool orchestration needed
- Multi-step AI workflows
- Memory/RAG patterns
- Prompt templates and planning

**Integration Strategy:**

- Semantic Kernel sits in Application layer
- Wraps Microsoft.Extensions.AI providers
- New interface: `IAIOrchestrationService`
- Kernel plugins for tools (web search, file system)

**Architecture Update:**

```
Semantic Kernel (Application)
        ↓
Microsoft.Extensions.AI (Infrastructure)
        ↓
Provider APIs (OpenRouter, NanoGPT)
```

### 10.2 Function Calling Architecture

**Planned Tools:**

- **Web Search**: Tavily/Serper API integration
- **File System**: Safe file search with security boundaries
- **Database Queries**: (Future consideration)

**Implementation Pattern:**

- Semantic Kernel plugins (`KernelFunction` attributes)
- Automatic function discovery
- Tool calling via `FunctionChoiceBehavior.Auto()`

### 10.3 Advanced Features (Phase 2+)

- **Conversation Persistence**: PostgreSQL storage
- **User Authentication**: Identity framework
- **RAG Patterns**: Vector embeddings, Qdrant integration
- **Global Exception Middleware**: Centralized error handling
- **Structured Logging**: Serilog with Application Insights
- **Caching**: Distributed cache for expensive operations
- **Rate Limiting**: Protect against abuse

---

## 11. Development Principles

### 11.1 SOLID Principles Application

**Single Responsibility:**

- Each provider implementation handles one provider only
- Controllers handle HTTP concerns only
- Services handle business logic only

**Open/Closed:**

- New providers added without modifying existing code
- Factory pattern enables extension

**Liskov Substitution:**

- All providers implement `ISemanticKernelFunctionCaller`
- Interchangeable at runtime

**Interface Segregation:**

- Focused interfaces (`ISemanticKernelFunctionCaller`, `IProviderFactory`)
- No bloated interfaces with unused methods

**Dependency Inversion:**

- Depend on abstractions (`ISemanticKernelFunctionCaller`), not concretions
- Infrastructure depends on Application, not vice versa

### 11.2 DRY Principles

- **Model Conversion**: Shared utilities for domain ↔ Microsoft.Extensions.AI conversions
- **Configuration Reading**: Centralized `ProviderConfigurationManager`
- **Error Handling**: Reusable error response DTOs
- **Validation**: Shared validation logic in Application layer

### 11.3 Code Quality Standards

- XML documentation on public APIs
- Unit tests for Application layer (Phase 2)
- Integration tests for API endpoints (Phase 2)
- Clean, self-documenting code
- Consistent naming conventions

---

## 12. Success Metrics

### 12.1 Technical Metrics

- **Architecture**: Clean separation of concerns, no circular dependencies
- **Performance**: < 500ms initial response time for streaming
- **Code Quality**: Zero critical SonarQube issues
- **Testability**: Application layer 100% mockable

### 12.2 Portfolio Metrics

- **Demonstrates**: Clean Architecture, SOLID, DRY, modern .NET
- **Scalability**: Ready for Phase 2 enhancements
- **Professional Quality**: Enterprise-ready patterns
- **Documentation**: Clear README, architecture diagrams

---

## 13. Risks and Mitigations

### 13.1 Technical Risks

|Risk|Impact|Mitigation|
|---|---|---|
|Provider API changes|High|Abstraction layer isolates changes to Infrastructure|
|Streaming complexity|Medium|Use proven Microsoft.Extensions.AI patterns|
|Token limit handling|Medium|Client-side truncation in Phase 2|
|CORS issues|Low|Configure in API project|

### 13.2 Scope Risks

|Risk|Impact|Mitigation|
|---|---|---|
|Feature creep|High|Strict Phase 1 scope, defer to Phase 2|
|Over-engineering|Medium|Start simple, refactor as needed|
|Time management|Medium|Iterative development, MVP first|

---

## 14. Implementation Phases

### Phase 1 - MVP (Current PRD Scope)

**Week 1:**

- Project structure setup (.NET 9 solution, 5 projects)
- Domain models
- Application interfaces
- Basic configuration

**Week 2:**

- Infrastructure: OpenRouter + NanoGPT providers
- Microsoft.Extensions.AI integration
- Provider factory

**Week 3:**

- API layer: Controllers, DTOs
- Streaming implementation
- Basic error handling

**Week 4:**

- Frontend: HTML/JS/Tailwind UI
- Provider/model selection
- Chat interface with streaming
- Testing and refinement

### Phase 2 - Enhancement (Future)

- Semantic Kernel integration
- Function calling (web search, file system)
- Conversation persistence
- Advanced error handling/logging
- Unit/integration tests

---

## 15. Appendix

### 15.1 Package Dependencies

**Domain Project:**

- None (pure .NET)

**Application Project:**

- None (interfaces only)

**Infrastructure Project:**

- `Microsoft.Extensions.AI` (preview)
- `Microsoft.Extensions.AI.OpenAI` (preview)
- `OpenAI` (latest)
- `Microsoft.Extensions.Configuration`

**API Project:**

- `Microsoft.AspNetCore.OpenApi`
- `Swashbuckle.AspNetCore` (optional)

**AppHost Project:**

- `Aspire.Hosting.AppHost`

### 15.2 Folder Structure

```
SemanticKernelFunctionCaller/
├── src/
│   ├── SemanticKernelFunctionCaller.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── ValueObjects/
│   ├── SemanticKernelFunctionCaller.Application/
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   └── UseCases/
│   ├── SemanticKernelFunctionCaller.Infrastructure/
│   │   ├── Providers/
│   │   │   ├── OpenRouterChatProvider.cs
│   │   │   └── NanoGptChatProvider.cs
│   │   ├── Factories/
│   │   └── Configuration/
│   ├── SemanticKernelFunctionCaller.API/
│   │   ├── Controllers/
│   │   ├── wwwroot/
│   │   │   ├── index.html
│   │   │   ├── js/
│   │   │   └── css/
│   │   └── Program.cs
│   └── SemanticKernelFunctionCaller.AppHost/
│       └── Program.cs
└── README.md
```

---

## 16. Definition of Done

Phase 1 is complete when:

✅ All 5 projects created with correct dependencies  
✅ Provider switching works (OpenRouter ↔ NanoGPT)  
✅ Model selection cascades correctly  
✅ Chat streaming works smoothly (SSE)  
✅ UI is clean and functional  
✅ Configuration is externalized  
✅ Code adheres to Clean Architecture  
✅ SOLID/DRY principles evident  
✅ README with architecture diagram  
✅ Can demo to interviewers confidently

