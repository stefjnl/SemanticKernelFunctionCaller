## Phase 2 Step 1: Semantic Kernel Integration - Implementation Plan

### Overview
Add Semantic Kernel as an orchestration layer that wraps existing Microsoft.Extensions.AI providers, enabling multi-step workflows, prompt templates, and plugin-based extensibility.

---

## 1. Architecture Decision Points

### **Where Semantic Kernel Lives**
- **Application Layer**: `IAIOrchestrationService` interface
- **Infrastructure Layer**: Semantic Kernel implementation, plugins, prompt templates

### **Integration Pattern**
- Semantic Kernel uses existing `ISemanticKernelFunctionCaller` providers as backends
- No changes to Domain or existing provider implementations
- New orchestration endpoints coexist with existing streaming chat

---

## 2. NuGet Package Additions

### **Application Project** (none needed - stays interface-only)

### **Infrastructure Project**
- `Microsoft.SemanticKernel` (latest stable)
- `Microsoft.SemanticKernel.Connectors.OpenAI` (if needed for SK-specific features)
- `Microsoft.SemanticKernel.Plugins.Core` (optional: built-in plugins)

---

## 3. Application Layer Changes

### **New Interface: `IAIOrchestrationService`**
**Location**: `SemanticKernelFunctionCaller.Application/Interfaces/IAIOrchestrationService.cs`

**Methods**:
- `Task<string> ExecutePromptAsync(string promptTemplate, Dictionary<string, string> variables)`
- `Task<string> ExecuteWorkflowAsync(string workflowName, Dictionary<string, string> context)`
- `IAsyncEnumerable<string> ExecutePromptStreamingAsync(...)` (for streaming support)

### **New DTOs**
**Location**: `SemanticKernelFunctionCaller.Application/DTOs/`

- `PromptRequestDto`: Template + variables
- `WorkflowRequestDto`: Workflow name + context
- `OrchestrationResponseDto`: Result + metadata (tokens used, execution steps)

### **New Use Case**
**Location**: `SemanticKernelFunctionCaller.Application/UseCases/ExecuteAIWorkflowUseCase.cs`

**Responsibility**: Orchestrate calls to `IAIOrchestrationService`

---

## 4. Infrastructure Layer Implementation

### **Semantic Kernel Service**
**Location**: `SemanticKernelFunctionCaller.Infrastructure/Orchestration/SemanticKernelOrchestrationService.cs`

**Constructor Dependencies**:
- `IProviderFactory` (to get underlying chat providers)
- `IConfiguration` (for prompt templates, kernel settings)
- `ILogger<SemanticKernelOrchestrationService>`

**Internal Setup**:
- Initialize `Kernel` instance
- Register existing chat providers as SK services via `ISemanticKernelFunctionCaller`
- Load prompt templates from configuration or embedded resources
- Register built-in plugins (if any)

**Key Methods**:
- `ExecutePromptAsync`: Uses `Kernel.InvokePromptAsync()` with template rendering
- `ExecuteWorkflowAsync`: Multi-step orchestration using planners or manual steps

### **Prompt Template Storage**
**Location**: `SemanticKernelFunctionCaller.Infrastructure/Orchestration/Templates/`

**Options**:
1. **Embedded YAML files**: Store templates as embedded resources
2. **Configuration-based**: Store in `appsettings.json` under `"PromptTemplates"` section
3. **Hybrid**: Simple templates in config, complex ones in files

**Example Template**:
```yaml
name: "SummarizeConversation"
template: |
  Summarize the following conversation in {{$style}} style:
  {{$conversation}}
parameters:
  - style: "professional"
  - conversation: ""
```

### **Plugin Architecture** (Optional for Step 1)
**Location**: `SemanticKernelFunctionCaller.Infrastructure/Orchestration/Plugins/`

**Start Simple**: No plugins in initial integration
**Future Plugins**: Web search, file system, calculator (Step 2 of Phase 2)

---

## 5. API Layer Changes

### **New Controller: `OrchestrationController`**
**Location**: `SemanticKernelFunctionCaller.API/Controllers/OrchestrationController.cs`

**Endpoints**:
- `POST /api/orchestration/prompt`: Execute prompt template
- `POST /api/orchestration/workflow`: Execute multi-step workflow
- `POST /api/orchestration/prompt/stream`: Streaming version (optional)

**Keep Separate from `ChatController`**: Maintains clear separation between basic chat and advanced orchestration

### **Dependency Registration**
**Location**: `SemanticKernelFunctionCaller.API/Program.cs`

- Register `IAIOrchestrationService` → `SemanticKernelOrchestrationService`
- Register new use cases

---

## 6. Configuration Updates

### **appsettings.json Structure**
```json
{
  "Providers": { ... }, // Existing
  "SemanticKernel": {
    "DefaultProvider": "OpenRouter",
    "DefaultModel": "google/gemini-2.5-flash-lite-preview-09-2025",
    "PromptTemplates": {
      "Summarize": "Summarize: {{$input}}",
      "Translate": "Translate to {{$language}}: {{$text}}"
    }
  }
}
```

---

## 7. Testing Strategy (Manual for Phase 2)

### **Verification Steps**:
1. Call `/api/orchestration/prompt` with simple template
2. Verify response contains expected rendered output
3. Test variable substitution in templates
4. Confirm underlying provider (OpenRouter/NanoGPT) is being called
5. Check logs for SK telemetry/tracing

### **Test Scenarios**:
- Simple prompt with 1 variable
- Prompt with multiple variables
- Missing variable (error handling)
- Invalid template syntax

---

## 8. Implementation Order

### **Week 1: Foundation**
1. Add NuGet packages to Infrastructure
2. Create `IAIOrchestrationService` interface
3. Create basic DTOs
4. Stub `SemanticKernelOrchestrationService` class

### **Week 2: Core Integration**
5. Initialize Kernel with existing providers
6. Implement `ExecutePromptAsync` with simple templates
7. Add prompt template storage (config-based initially)
8. Create `OrchestrationController` with single endpoint

### **Week 3: Testing & Polish**
9. Manual testing via Swagger/Postman
10. Add error handling and logging
11. Document new endpoints in README
12. Verify Phase 1 functionality still works

---

## 9. Success Criteria

✅ Semantic Kernel initialized with existing chat providers  
✅ Can execute simple prompt template via API  
✅ Variables substituted correctly in templates  
✅ Existing chat endpoints (`/api/chat/stream`) still work  
✅ No breaking changes to Phase 1 functionality  
✅ Clear separation: orchestration vs. basic chat  
✅ Logs show SK execution trace

---

## 10. Key Design Principles

**Non-Breaking**: Phase 1 chat continues to work as-is  
**Layered**: SK sits **above** Microsoft.Extensions.AI, not replacing it  
**Extensible**: Plugin architecture ready but not required for Step 1  
**Testable**: Orchestration service mockable via interface  
**Simple Start**: Focus on prompt templates first, advanced features later

---

## Next Steps After Step 1

Once basic SK integration works:
- **Step 2**: Add function calling plugins (web search, file system)
- **Step 3**: Multi-step workflows with planners
- **Step 4**: RAG patterns with vector embeddings

This plan keeps Step 1 focused and achievable while setting foundation for advanced features.
