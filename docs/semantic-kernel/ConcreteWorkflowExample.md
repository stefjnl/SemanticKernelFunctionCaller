# Concrete Workflow Example: Research and Report

## Overview
This document defines a concrete workflow example that demonstrates the multi-step planning and execution capabilities of the Semantic Kernel integration. The example shows how a user request is broken down into discrete steps, executed, and compiled into a final response.

## Workflow Scenario

### User Request
```
User: "Research the latest advancements in quantum computing and summarize the key findings in a report format with a conclusion."
```

### Workflow Definition
```json
{
  "goal": "Research the latest advancements in quantum computing and summarize the key findings in a report format with a conclusion",
  "context": "The user is preparing a presentation for a technology conference",
  "availableFunctions": ["WebSearch", "Summarize", "ReportWriter"],
  "maxSteps": 10
}
```

## Step-by-Step Execution

### Step 1: Web Search
**Function Called**: WebSearch.SearchAsync
**Parameters**: 
```json
{
  "query": "latest advancements in quantum computing 2025",
  "maxResults": 5
}
```

**Result**:
```json
{
  "results": [
    {
      "title": "Breakthrough in Quantum Error Correction Achieved",
      "url": "https://quantumjournal.org/breakthrough-error-correction",
      "snippet": "Scientists at MIT have developed a new method for quantum error correction that reduces error rates by 75%..."
    },
    {
      "title": "IBM Unveils 1000-Qubit Quantum Processor",
      "url": "https://ibmresearch.com/1000-qubit-processor",
      "snippet": "IBM has announced the development of a 1000-qubit quantum processor, doubling the previous record..."
    },
    {
      "title": "Quantum Supremacy Demonstrated in Chemical Simulation",
      "url": "https://nature.com/quantum-chemistry-supremacy",
      "snippet": "Google's quantum computer has solved a complex chemical simulation problem that would take classical computers centuries..."
    }
  ]
}
```

### Step 2: Summarize Findings
**Function Called**: Summarize.SummarizeTextAsync
**Parameters**:
```json
{
  "text": "Scientists at MIT have developed a new method for quantum error correction that reduces error rates by 75%. IBM has announced the development of a 1000-qubit quantum processor, doubling the previous record. Google's quantum computer has solved a complex chemical simulation problem that would take classical computers centuries.",
  "sentenceCount": 3
}
```

**Result**:
```json
{
  "summary": "Recent quantum computing advancements include a 75% reduction in error rates through new error correction methods, a doubling of qubit count to 1000 in IBM's new processor, and Google's demonstration of quantum supremacy in chemical simulations."
}
```

### Step 3: Generate Report Structure
**Function Called**: ReportWriter.CreateOutlineAsync
**Parameters**:
```json
{
  "topic": "Quantum Computing Advancements",
  "sections": ["Introduction", "Error Correction Breakthrough", "Hardware Improvements", "Practical Applications", "Conclusion"]
}
```

**Result**:
```json
{
  "outline": {
    "title": "Quantum Computing Advancements: Recent Breakthroughs and Future Prospects",
    "sections": [
      {
        "heading": "Introduction",
        "description": "Overview of the current state of quantum computing"
      },
      {
        "heading": "Error Correction Breakthrough",
        "description": "Analysis of MIT's new error correction method"
      },
      {
        "heading": "Hardware Improvements",
        "description": "Examination of IBM's 1000-qubit processor"
      },
      {
        "heading": "Practical Applications",
        "description": "Review of Google's chemical simulation achievement"
      },
      {
        "heading": "Conclusion",
        "description": "Summary and future outlook"
      }
    ]
  }
}
```

### Step 4: Populate Introduction
**Function Called**: ReportWriter.WriteSectionAsync
**Parameters**:
```json
{
  "section": "Introduction",
  "content": "Quantum computing is rapidly advancing beyond theoretical concepts into practical applications. Recent breakthroughs in error correction, hardware scalability, and real-world problem solving are bringing us closer to widespread quantum advantage.",
  "tone": "professional"
}
```

**Result**:
```json
{
  "writtenSection": "Introduction\n\nQuantum computing is rapidly advancing beyond theoretical concepts into practical applications. Recent breakthroughs in error correction, hardware scalability, and real-world problem solving are bringing us closer to widespread quantum advantage. As we stand on the brink of a new computational era, understanding these developments is crucial for technology professionals and researchers alike."
}
```

### Step 5: Populate Error Correction Section
**Function Called**: ReportWriter.WriteSectionAsync
**Parameters**:
```json
{
  "section": "Error Correction Breakthrough",
  "content": "MIT researchers have developed a novel quantum error correction method that reduces error rates by 75%. This advancement addresses one of the primary challenges in quantum computing, where quantum states are extremely fragile and susceptible to environmental interference.",
  "tone": "technical"
}
```

**Result**:
```json
{
  "writtenSection": "Error Correction Breakthrough\n\nMIT researchers have developed a novel quantum error correction method that reduces error rates by 75%. This advancement addresses one of the primary challenges in quantum computing, where quantum states are extremely fragile and susceptible to environmental interference. The new technique employs a hybrid approach combining surface code protocols with machine learning algorithms to predict and preemptively correct errors before they cascade through the quantum system."
}
```

### Step 6: Populate Hardware Section
**Function Called**: ReportWriter.WriteSectionAsync
**Parameters**:
```json
{
  "section": "Hardware Improvements",
  "content": "IBM's announcement of a 1000-qubit processor represents a significant leap in quantum hardware scalability. This doubles the previous record and brings us closer to fault-tolerant quantum computers capable of solving complex real-world problems.",
  "tone": "informative"
}
```

**Result**:
```json
{
  "writtenSection": "Hardware Improvements\n\nIBM's announcement of a 1000-qubit processor represents a significant leap in quantum hardware scalability. This doubles the previous record and brings us closer to fault-tolerant quantum computers capable of solving complex real-world problems. The new processor utilizes advanced cryogenic engineering and improved qubit coherence times, allowing for more stable and reliable quantum computations over extended periods."
}
```

### Step 7: Populate Applications Section
**Function Called**: ReportWriter.WriteSectionAsync
**Parameters**:
```json
{
  "section": "Practical Applications",
  "content": "Google's quantum computer has demonstrated practical quantum supremacy by solving a complex chemical simulation problem. This achievement showcases the potential for quantum computing to revolutionize fields such as drug discovery, materials science, and cryptography.",
  "tone": "analytical"
}
```

**Result**:
```json
{
  "writtenSection": "Practical Applications\n\nGoogle's quantum computer has demonstrated practical quantum supremacy by solving a complex chemical simulation problem that would take classical computers centuries to complete. This achievement showcases the potential for quantum computing to revolutionize fields such as drug discovery, materials science, and cryptography. The simulation accurately modeled the electronic structure of a complex molecule, providing insights that could accelerate the development of new pharmaceuticals and materials with tailored properties."
}
```

### Step 8: Generate Conclusion
**Function Called**: ReportWriter.WriteSectionAsync
**Parameters**:
```json
{
  "section": "Conclusion",
  "content": "The recent advancements in quantum computing represent significant milestones toward practical quantum advantage. With improved error correction, scalable hardware, and proven applications, the field is progressing rapidly from laboratory curiosity to industrial tool.",
  "tone": "conclusive"
}
```

**Result**:
```json
{
  "writtenSection": "Conclusion\n\nThe recent advancements in quantum computing represent significant milestones toward practical quantum advantage. With improved error correction reducing error rates by 75%, scalable hardware reaching 1000 qubits, and proven applications in complex problem solving, the field is progressing rapidly from laboratory curiosity to industrial tool. As these technologies mature, we can expect quantum computing to become an integral part of scientific research and commercial innovation, potentially transforming industries and solving problems previously thought intractable."
}
```

### Step 9: Compile Final Report
**Function Called**: ReportWriter.CompileReportAsync
**Parameters**:
```json
{
  "title": "Quantum Computing Advancements: Recent Breakthroughs and Future Prospects",
  "sections": [
    "Introduction\n\nQuantum computing is rapidly advancing beyond theoretical concepts into practical applications...",
    "Error Correction Breakthrough\n\nMIT researchers have developed a novel quantum error correction method...",
    "Hardware Improvements\n\nIBM's announcement of a 1000-qubit processor represents a significant leap...",
    "Practical Applications\n\nGoogle's quantum computer has demonstrated practical quantum supremacy...",
    "Conclusion\n\nThe recent advancements in quantum computing represent significant milestones..."
  ]
}
```

**Result**:
```json
{
  "compiledReport": "Quantum Computing Advancements: Recent Breakthroughs and Future Prospects\n\nIntroduction\n\nQuantum computing is rapidly advancing beyond theoretical concepts into practical applications...\n\nError Correction Breakthrough\n\nMIT researchers have developed a novel quantum error correction method...\n\nHardware Improvements\n\nIBM's announcement of a 1000-qubit processor represents a significant leap...\n\nPractical Applications\n\nGoogle's quantum computer has demonstrated practical quantum supremacy...\n\nConclusion\n\nThe recent advancements in quantum computing represent significant milestones..."
}
```

## Final Response to User

### API Response
```json
{
  "content": "Quantum Computing Advancements: Recent Breakthroughs and Future Prospects\n\nIntroduction\n\nQuantum computing is rapidly advancing beyond theoretical concepts into practical applications. Recent breakthroughs in error correction, hardware scalability, and real-world problem solving are bringing us closer to widespread quantum advantage. As we stand on the brink of a new computational era, understanding these developments is crucial for technology professionals and researchers alike.\n\nError Correction Breakthrough\n\nMIT researchers have developed a novel quantum error correction method that reduces error rates by 75%. This advancement addresses one of the primary challenges in quantum computing, where quantum states are extremely fragile and susceptible to environmental interference. The new technique employs a hybrid approach combining surface code protocols with machine learning algorithms to predict and preemptively correct errors before they cascade through the quantum system.\n\nHardware Improvements\n\nIBM's announcement of a 1000-qubit processor represents a significant leap in quantum hardware scalability. This doubles the previous record and brings us closer to fault-tolerant quantum computers capable of solving complex real-world problems. The new processor utilizes advanced cryogenic engineering and improved qubit coherence times, allowing for more stable and reliable quantum computations over extended periods.\n\nPractical Applications\n\nGoogle's quantum computer has demonstrated practical quantum supremacy by solving a complex chemical simulation problem that would take classical computers centuries to complete. This achievement showcases the potential for quantum computing to revolutionize fields such as drug discovery, materials science, and cryptography. The simulation accurately modeled the electronic structure of a complex molecule, providing insights that could accelerate the development of new pharmaceuticals and materials with tailored properties.\n\nConclusion\n\nThe recent advancements in quantum computing represent significant milestones toward practical quantum advantage. With improved error correction reducing error rates by 75%, scalable hardware reaching 1000 qubits, and proven applications in complex problem solving, the field is progressing rapidly from laboratory curiosity to industrial tool. As these technologies mature, we can expect quantum computing to become an integral part of scientific research and commercial innovation, potentially transforming industries and solving problems previously thought intractable.",
  "modelId": "google/gemini-2.5-flash-lite-preview-09-2025",
  "providerId": "OpenRouter",
  "metadata": {
    "functionCalls": [
      {
        "functionName": "WebSearch.SearchAsync",
        "arguments": "{\"query\":\"latest advancements in quantum computing 2025\",\"maxResults\":5}",
        "result": "[Search results]",
        "timestamp": "2025-10-06T10:15:22Z"
      },
      {
        "functionName": "Summarize.SummarizeTextAsync",
        "arguments": "{\"text\":\"Scientific breakthroughs in quantum computing...\",\"sentenceCount\":3}",
        "result": "\"Recent quantum computing advancements include...\"",
        "timestamp": "2025-10-06T10:15:25Z"
      },
      {
        "functionName": "ReportWriter.CreateOutlineAsync",
        "arguments": "{\"topic\":\"Quantum Computing Advancements\",\"sections\":[\"Introduction\",\"Error Correction Breakthrough\",\"Hardware Improvements\",\"Practical Applications\",\"Conclusion\"]}",
        "result": "[Report outline]",
        "timestamp": "2025-10-06T10:15:27Z"
      },
      {
        "functionName": "ReportWriter.WriteSectionAsync",
        "arguments": "{\"section\":\"Introduction\",\"content\":\"Quantum computing is rapidly advancing...\",\"tone\":\"professional\"}",
        "result": "[Written introduction]",
        "timestamp": "2025-10-06T10:15:30Z"
      },
      {
        "functionName": "ReportWriter.WriteSectionAsync",
        "arguments": "{\"section\":\"Error Correction Breakthrough\",\"content\":\"MIT researchers have developed...\",\"tone\":\"technical\"}",
        "result": "[Written error correction section]",
        "timestamp": "2025-10-06T10:15:35Z"
      },
      {
        "functionName": "ReportWriter.WriteSectionAsync",
        "arguments": "{\"section\":\"Hardware Improvements\",\"content\":\"IBM's announcement of a 1000-qubit...\",\"tone\":\"informative\"}",
        "result": "[Written hardware section]",
        "timestamp": "2025-10-06T10:15:40Z"
      },
      {
        "functionName": "ReportWriter.WriteSectionAsync",
        "arguments": "{\"section\":\"Practical Applications\",\"content\":\"Google's quantum computer has demonstrated...\",\"tone\":\"analytical\"}",
        "result": "[Written applications section]",
        "timestamp": "2025-10-06T10:15:45Z"
      },
      {
        "functionName": "ReportWriter.WriteSectionAsync",
        "arguments": "{\"section\":\"Conclusion\",\"content\":\"The recent advancements in quantum computing...\",\"tone\":\"conclusive\"}",
        "result": "[Written conclusion]",
        "timestamp": "2025-10-06T10:15:48Z"
      },
      {
        "functionName": "ReportWriter.CompileReportAsync",
        "arguments": "{\"title\":\"Quantum Computing Advancements...\",\"sections\":[\"Introduction...\",\"Error Correction...\",\"Hardware...\",\"Applications...\",\"Conclusion...\"]}",
        "result": "[Compiled report]",
        "timestamp": "2025-10-06T10:15:50Z"
      }
    ],
    "stepsExecuted": 9,
    "goal": "Research the latest advancements in quantum computing and summarize the key findings in a report format with a conclusion"
  }
}
```

## Streaming Behavior During Workflow Execution

### Streaming Updates
During the workflow execution, the API would send streaming updates to inform the user of progress:

```
data: {"content":"Researching latest advancements in quantum computing...","isFinal":false}

data: {"content":"Found 3 key areas: error correction, hardware improvements, and practical applications.","isFinal":false}

data: {"content":"Generating detailed report with 5 sections...","isFinal":false}

data: {"content":"Quantum Computing Advancements: Recent Breakthroughs and Future Prospects\n\nIntroduction\n\nQuantum computing is rapidly advancing...","isFinal":false}

data: {"content":"Error Correction Breakthrough\n\nMIT researchers have developed a novel quantum error correction method...","isFinal":false}

data: {"content":"Hardware Improvements\n\nIBM's announcement of a 1000-qubit processor represents a significant leap...","isFinal":false}

data: {"content":"Practical Applications\n\nGoogle's quantum computer has demonstrated practical quantum supremacy...","isFinal":false}

data: {"content":"Conclusion\n\nThe recent advancements in quantum computing represent significant milestones...","isFinal":true,"metadata":{"functionCalls":[...],"stepsExecuted":9}}
```

## Implementation Details

### Planner Selection
For this workflow, we would use the **FunctionCallingStepwisePlanner** which is appropriate for multi-step tasks that require sequential execution of functions.

### Planner Configuration
```csharp
var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions
{
    MaxIterations = 10,
    MinIterationTimeMs = 1000
});
```

### Error Handling
If any step fails, the planner would attempt to recover by either:
1. Retrying the function call
2. Asking the LLM to adjust the plan
3. Proceeding with available information

### User Experience
The user receives a comprehensive, well-structured report that addresses their original request, with full transparency about the steps taken to generate the response.

This concrete example demonstrates how the Semantic Kernel orchestration can break down complex user requests into manageable steps, execute them using available plugins, and compile the results into a coherent final response.