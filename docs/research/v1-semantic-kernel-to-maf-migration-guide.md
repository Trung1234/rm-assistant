# Semantic Kernel to Microsoft Agent Framework
## Complete Migration Guide (v1)

**Version**: 1.0
**Created**: 2025-12-30 23:00 (UTC+7)
**Status**: ✅ Complete Migration Reference
**Target Audience**: Developers migrating from Semantic Kernel to Microsoft Agent Framework
**Primary Reference**: [Microsoft Agent Framework Migration Guide](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel/?pivots=programming-language-csharp)

---

## Table of Contents

### PART I: UNDERSTANDING THE ECOSYSTEM
1. [High-Level Overview](#1-high-level-overview)
2. [Core Concepts](#2-core-concepts)
3. [Conceptual Relationships](#3-conceptual-relationships)
4. [Architecture Evolution](#4-architecture-evolution)

### PART II: MIGRATION PATTERNS
5. [Migration Patterns Overview](#5-migration-patterns-overview)
6. [Pattern-by-Pattern Migration](#6-pattern-by-pattern-migration)
7. [Low-Level Implementation Details](#7-low-level-implementation-details)

### PART III: PRACTICAL GUIDANCE
8. [Common Pitfalls & Best Practices](#8-common-pitfalls--best-practices)
9. [Real-World Migration Scenarios](#9-real-world-migration-scenarios)
10. [Migration Checklist & Next Steps](#10-migration-checklist--next-steps)

---

# PART I: UNDERSTANDING THE ECOSYSTEM

## 1. High-Level Overview

### 1.1 The Microsoft Agent Framework (MAF) Ecosystem

Based on the MAF architecture diagram, the ecosystem consists of two primary components working together:

```
+===============================================================================+
|                  MICROSOFT AGENT FRAMEWORK (MAF) ECOSYSTEM                    |
+===============================================================================+
|                                                                               |
|   ┌───────────────────────────────────┐   ┌──────────────────────────────┐  |
|   │   SEMANTIC KERNEL (SK)            │   │     AUTOGEN (AG)             │  |
|   │   The Skill Builder & Connector   │   │  The Collaborator & Orchestrator |
|   │                                   │   │                              │  |
|   │  ┌──────┐                         │   │   ┌───────────────────┐     │  |
|   │  │ LLM  │                         │   │   │  Multi-Agent      │     │  |
|   │  └──┬───┘                         │   │   │  Conversation &   │     │  |
|   │     │                             │   │   │  Workflow         │     │  |
|   │  ┌──▼───────┐    ┌──────────┐    │   │   │                   │     │  |
|   │  │Plugins/  │◄──►│Memories  │    │───┼──►│  Agent A (Coder)  │     │  |
|   │  │Functions │    │(Vector   │    │   │   │  Agent B (Review) │     │  |
|   │  │(Code)    │    │   DB)    │    │   │   │  User Proxy Agent │     │  |
|   │  └──────────┘    └──────────┘    │   │   └───────────────────┘     │  |
|   │       │                           │   │            │                │  |
|   │  ┌────▼─────────┐                 │   │            │                │  |
|   │  │  Single      │                 │   │            │                │  |
|   │  │  Capable     │                 │   │            │                │  |
|   │  │  Agent       │                 │   │            │                │  |
|   │  └──────────────┘                 │   │            │                │  |
|   └───────────────────────────────────┘   └──────────────────────────────┘  |
|                                                                               |
|   FORMULA:  Semantic Kernel + AutoGen = Microsoft Agent Framework            |
|                                                                               |
+===============================================================================+
```

**Key Insight**: MAF unifies two previously separate frameworks:
- **Semantic Kernel** provides AI skills and tools (function calling, memory, plugins)
- **AutoGen** provides multi-agent orchestration and workflow management

---

### 1.2 Why Migrate? The Value Proposition

**Before (Semantic Kernel alone)**:
- Kernel-centric design with tight coupling
- Manual thread management
- Service-specific agent classes
- Complex plugin registration
- Boilerplate-heavy code

**After (Microsoft Agent Framework)**:
- Provider-agnostic design (`IChatClient` abstraction)
- Agent-managed thread lifecycle
- Unified `ChatClientAgent` + specialized agents
- Direct function registration
- **15-20% performance improvement** (reduced allocations)

---

### 1.3 Architecture Shift (First Principles)

**Fundamental Question**: Why does a Kernel exist in Semantic Kernel?

**Answer (First Principles Analysis)**:
- **Assumption**: Every AI operation needs central orchestration
- **Reality**: Most agents only need: chat client + instructions + tools + thread
- **Conclusion**: The Kernel is a **premature abstraction** for 80% of use cases

**Architectural Evolution**:
```
Semantic Kernel:   Application → Kernel → Plugins → Agent → Provider
                                   ↑ (Unnecessary middleman)

Agent Framework:   Application → IChatClient → ChatClientAgent → Provider
                                                 ↑ (Direct, minimal overhead)
```

**Design Decision (Type 1 - Hard to Change)**:
- Eliminating Kernel is irreversible → chose simplicity over flexibility
- Trade-off: Lose Kernel-level orchestration, gain simpler API surface

---

## 2. Core Concepts

### 2.1 Agents (Evolution & Unification)

#### Semantic Kernel Agent Model
```
Service-Specific Agent Classes
├─ ChatCompletionAgent (OpenAI chat models)
├─ OpenAIAssistantAgent (OpenAI Assistants API)
└─ AzureAIAgent (Azure AI Foundry)

Problem: Each requires different initialization, different Kernel config
```

#### Agent Framework Model
```
Unified Agent Interface: AIAgent
├─ ChatClientAgent (works with ANY IChatClient)
│  ├─ OpenAIChatClient
│  ├─ AzureOpenAIChatClient
│  ├─ OpenAIAssistantClient
│  └─ AzureAIAgentClient
├─ CopilotStudioAgent (specialized)
└─ A2AAgent (agent-to-agent communication)

Benefit: Single agent type works across all providers
```

**Conceptual Relationship**:
```
        AIAgent (abstract base)
           ▲
           │ implements
           │
     ChatClientAgent
           │
           │ depends on
           ▼
       IChatClient (interface)
           ▲
           │ implemented by providers
           │
    ┌──────┴──────┬───────┬───────┐
    │             │       │       │
OpenAIChatClient Azure... Copilot Other
```

**Key Insight (Feynman Technique)**:
> "Imagine you have different brands of phones (OpenAI, Azure). Semantic Kernel made you carry a different charger (agent class) for each. Agent Framework is like USB-C—one charger (ChatClientAgent) works for all phones (providers)."

---

### 2.2 Tools & Functions (Simplification)

#### Semantic Kernel Approach (Complex)
```csharp
// Step 1: Mark function
[KernelFunction]
public string GetWeather(string location) => ...;

// Step 2: Create KernelFunction wrapper
var function = KernelFunctionFactory.CreateFromMethod(GetWeather);

// Step 3: Wrap in KernelPlugin
var plugin = KernelPluginFactory.CreateFromFunctions("Weather", [function]);

// Step 4: Register with Kernel
kernel.Plugins.Add(plugin);

// Step 5: Attach Kernel to Agent
var agent = new ChatCompletionAgent { Kernel = kernel };
```

**Complexity: 5 steps**, 4 wrapper objects created

#### Agent Framework Approach (Simple)
```csharp
// One step: Direct registration
var agent = chatClient.CreateAIAgent(
    tools: [AIFunctionFactory.Create(GetWeather)]
);

// Optional: Add description for better LLM understanding
[Description("Get current weather for a city")]
public static string GetWeather(
    [Description("City name")] string location) => ...;
```

**Complexity: 1 step**, 1 factory call

**Conceptual Relationship**:
```
Function (C# method)
    ↓
AIFunctionFactory.Create()
    ↓
AIFunction (internal representation)
    ↓
Passed to Agent via tools parameter
    ↓
Agent registers with IChatClient
    ↓
LLM can invoke via function calling
```

---

### 2.3 Threads & Conversation State

#### Thread Lifecycle (Semantic Kernel)
```
1. Developer creates thread manually
   ├─ new OpenAIAssistantAgentThread(client)
   ├─ new AzureAIAgentThread(client)
   └─ new OpenAIResponseAgentThread(client)

2. Developer must know which thread type for which provider

3. Thread deletion (if supported):
   await thread.DeleteAsync()
```

#### Thread Lifecycle (Agent Framework)
```
1. Agent manages thread creation
   thread = agent.GetNewThread()

2. Works for ALL providers automatically

3. Thread types (internal):
   ├─ Service-hosted (OpenAI Assistants, Azure AI Foundry)
   └─ In-memory (default, stateless)

4. Thread deletion (manual, provider-specific):
   await assistantClient.DeleteThreadAsync(thread.ConversationId)
```

**Conceptual Relationship**:
```
AgentThread (unified interface)
    ▲
    │ abstracts
    │
    ├─ Service-hosted thread (persistent, server-side state)
    │  └─ threadId tracked by provider (OpenAI, Azure)
    │
    └─ In-memory thread (ephemeral, client-side state)
       └─ messages stored locally, no threadId
```

**Trade-off Analysis**:
| Aspect | Service-Hosted | In-Memory |
|--------|---------------|-----------|
| **State** | Server-side (persistent) | Client-side (ephemeral) |
| **Performance** | Network latency | Instant |
| **Use Case** | Long-running conversations | Single-turn queries |
| **Cleanup** | Manual (provider SDK) | Automatic (GC) |

---

### 2.4 Memory & Context Management

**Semantic Kernel Approach**:
```
Kernel (central memory)
├─ Short-term memory (conversation history)
├─ Long-term memory (Vector DB integration)
└─ Semantic memory (embeddings)

Accessed via: kernel.Memory
```

**Agent Framework Approach**:
```
Distributed Memory Model
├─ Thread-level memory (conversation history)
├─ Agent-level memory (system instructions)
└─ External memory (developer-managed, via tools)

Accessed via:
├─ thread.Messages (conversation history)
├─ agent.Instructions (system prompt)
└─ Custom tool functions (database, vector store, etc.)
```

**Migration Strategy**:
```
SK Memory Feature → MAF Equivalent
─────────────────────────────────
Conversation history → thread.Messages (automatic)
System instructions → agent.Instructions
Vector DB search → Custom tool function
Semantic memory → External service + tool integration
```

---

### 2.5 Execution Flow & Orchestration

#### Execution Flow Comparison

**Semantic Kernel Flow**:
```
User Input
    ↓
agent.InvokeAsync(input, thread)
    ↓
Kernel.Plugins.GetFunctions()
    ↓
Execute matched functions
    ↓
Kernel aggregates results
    ↓
Return IAsyncEnumerable<AgentResponseItem>
    ↓
Developer iterates over streaming items
```

**Agent Framework Flow**:
```
User Input
    ↓
agent.RunAsync(input, thread)
    ↓
IChatClient.GetChatMessageContentsAsync()
    ↓
Function calling (if tools registered)
    ↓
AgentRunResponse (single object)
    ↓
Developer accesses response.Text
```

**Key Difference**:
- SK uses **streaming iterator** even for non-streaming calls
- MAF uses **direct response object** for non-streaming, **streaming iterator** only when needed

**Orchestration Patterns**:

```
┌─────────────────────────────────────────────────────────────┐
│              ORCHESTRATION PATTERN EVOLUTION                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  SEMANTIC KERNEL (Single-Agent Focus)                       │
│  ─────────────────────────────────                          │
│   User → Agent (with tools) → LLM → Response                │
│                                                              │
│  AGENT FRAMEWORK (Multi-Agent Support)                      │
│  ────────────────────────────────────                       │
│   User → Agent A → Agent B → Agent C → Aggregated Response │
│          (Coder)   (Reviewer) (Tester)                      │
│                                                              │
│  AUTOGEN INTEGRATION                                        │
│  ────────────────────                                       │
│   User Proxy Agent                                          │
│        ↓                                                    │
│   ┌────┴────┐                                              │
│   │         │                                              │
│  Agent A  Agent B  (conversation loop until task complete) │
│   ↓         ↓                                              │
│   └────┬────┘                                              │
│        ↓                                                    │
│   Final Result                                             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Conceptual Relationships

### 3.1 Component Interaction Map (MECE Analysis)

```
+===============================================================================+
|                    MAF COMPONENT RELATIONSHIPS (MECE)                         |
+===============================================================================+
|                                                                               |
|  CATEGORY 1: CORE ABSTRACTIONS                                              |
|  ├─ AIAgent (base interface)                                                |
|  ├─ IChatClient (provider abstraction)                                      |
|  ├─ AgentThread (conversation container)                                    |
|  └─ AIFunction (tool representation)                                        |
|                                                                               |
|  CATEGORY 2: PROVIDER IMPLEMENTATIONS                                       |
|  ├─ OpenAIChatClient                                                        |
|  ├─ AzureOpenAIChatClient                                                   |
|  ├─ OpenAIAssistantClient                                                   |
|  └─ AzureAIAgentClient (Foundry)                                            |
|                                                                               |
|  CATEGORY 3: SPECIALIZED AGENTS                                             |
|  ├─ ChatClientAgent (general-purpose)                                       |
|  ├─ CopilotStudioAgent (Microsoft Copilot)                                  |
|  └─ A2AAgent (agent-to-agent)                                               |
|                                                                               |
|  CATEGORY 4: EXECUTION MODELS                                               |
|  ├─ Non-Streaming (RunAsync → AgentRunResponse)                            |
|  └─ Streaming (RunStreamingAsync → AgentRunResponseUpdate)                 |
|                                                                               |
|  CATEGORY 5: CONFIGURATION & OPTIONS                                        |
|  ├─ ChatOptions (unified settings)                                          |
|  └─ ChatClientAgentRunOptions (agent-specific)                              |
|                                                                               |
+===============================================================================+
```

**Key Relationships**:

1. **AIAgent ←→ IChatClient** (Dependency)
   - Every agent requires a chat client
   - Chat client determines provider capabilities

2. **AgentThread ←→ Messages** (Composition)
   - Thread contains message history
   - Messages accumulate during conversation

3. **AIFunction ←→ Agent** (Registration)
   - Functions registered at agent creation
   - Agent passes functions to IChatClient for invocation

4. **ChatOptions ←→ Provider** (Configuration)
   - Unified options translated to provider-specific settings
   - Not all providers support all options

---

### 3.2 Data Flow Diagram

```
┌───────────────────────────────────────────────────────────────────┐
│                      MAF DATA FLOW                                 │
└───────────────────────────────────────────────────────────────────┘

   User Input (string)
        │
        ▼
   ┌────────────────┐
   │ Agent.RunAsync │
   │  (Entry Point) │
   └───────┬────────┘
           │
           ├─ 1. Retrieve thread.Messages (conversation history)
           │
           ├─ 2. Append user input as new message
           │
           ├─ 3. Pass to IChatClient.GetChatMessageContentsAsync()
           │     │
           │     ├─ 3a. LLM processes messages
           │     │
           │     ├─ 3b. If tool call detected:
           │     │     ├─ Execute AIFunction
           │     │     ├─ Append tool result
           │     │     └─ Loop back to LLM
           │     │
           │     └─ 3c. LLM generates final response
           │
           ├─ 4. Aggregate messages into AgentRunResponse
           │
           ▼
   ┌──────────────────┐
   │ AgentRunResponse │
   │ ├─ Text          │ (Final answer)
   │ ├─ Messages      │ (All messages generated)
   │ └─ Metadata      │ (Token usage, etc.)
   └──────────────────┘
           │
           ▼
   Return to caller
```

**Message Flow Detail**:
```
Initial State:
thread.Messages = []

After user input:
thread.Messages = [
    { Role: User, Content: "What's the weather?" }
]

After LLM processes (with tool call):
thread.Messages = [
    { Role: User, Content: "What's the weather?" },
    { Role: Assistant, ToolCalls: [GetWeather("Seattle")] },
    { Role: Tool, Content: "72°F, sunny" },
    { Role: Assistant, Content: "The weather is sunny, 72°F" }
]

Response:
AgentRunResponse.Text = "The weather is sunny, 72°F"
AgentRunResponse.Messages = [all 4 messages above]
```

---

## 4. Architecture Evolution

### 4.1 Before & After Comparison (Architecture)

#### Semantic Kernel Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                   SEMANTIC KERNEL ARCHITECTURE                  │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌──────────────────────────────────────────────────────┐    │
│   │              APPLICATION LAYER                        │    │
│   └───────────────────┬──────────────────────────────────┘    │
│                       │                                        │
│                       ▼                                        │
│   ┌──────────────────────────────────────────────────────┐    │
│   │                 KERNEL                                │    │
│   │  (Central Orchestrator - Required for Everything)    │    │
│   │                                                       │    │
│   │  ├─ Service Registration (AI providers)              │    │
│   │  ├─ Plugin Management (function wrappers)            │    │
│   │  ├─ Memory Management (conversation + semantic)      │    │
│   │  └─ Execution Context                                │    │
│   └───────────┬──────────────────┬──────────────┬────────┘    │
│               │                  │              │              │
│               ▼                  ▼              ▼              │
│   ┌─────────────────┐  ┌─────────────────┐  ┌────────────┐   │
│   │ KernelPlugin 1  │  │ KernelPlugin 2  │  │ Memory     │   │
│   │  ├─ Function A  │  │  ├─ Function C  │  │ (Vector DB)│   │
│   │  └─ Function B  │  │  └─ Function D  │  └────────────┘   │
│   └─────────────────┘  └─────────────────┘                    │
│               │                                                │
│               ▼                                                │
│   ┌──────────────────────────────────────────────────────┐    │
│   │         AGENT (Service-Specific Classes)             │    │
│   │  ├─ ChatCompletionAgent                              │    │
│   │  ├─ OpenAIAssistantAgent                             │    │
│   │  └─ AzureAIAgent                                     │    │
│   │       │                                              │    │
│   │       └─ Requires Kernel property                   │    │
│   └───────────┬──────────────────────────────────────────┘    │
│               │                                                │
│               ▼                                                │
│   ┌──────────────────────────────────────────────────────┐    │
│   │             AI PROVIDER                               │    │
│   │  (OpenAI, Azure, etc.)                               │    │
│   └──────────────────────────────────────────────────────┘    │
│                                                                 │
│   DRAWBACKS:                                                   │
│   ✗ Kernel is a mandatory bottleneck                          │
│   ✗ Plugin wrapping adds object allocation overhead           │
│   ✗ Service-specific agents limit flexibility                 │
│   ✗ Complex dependency chain (App→Kernel→Plugin→Agent)        │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

#### Agent Framework Architecture

```
┌────────────────────────────────────────────────────────────────┐
│              MICROSOFT AGENT FRAMEWORK ARCHITECTURE             │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌──────────────────────────────────────────────────────┐    │
│   │              APPLICATION LAYER                        │    │
│   └───────────────────┬──────────────────────────────────┘    │
│                       │                                        │
│                       ▼                                        │
│   ┌──────────────────────────────────────────────────────┐    │
│   │          IChatClient (Provider Abstraction)           │    │
│   │                                                       │    │
│   │  Implemented by:                                     │    │
│   │  ├─ OpenAIChatClient                                 │    │
│   │  ├─ AzureOpenAIChatClient                            │    │
│   │  ├─ OpenAIAssistantClient                            │    │
│   │  └─ AzureAIAgentClient (Foundry)                     │    │
│   └───────────┬──────────────────────────────────────────┘    │
│               │                                                │
│               ▼                                                │
│   ┌──────────────────────────────────────────────────────┐    │
│   │       ChatClientAgent (Unified Agent)                 │    │
│   │                                                       │    │
│   │  ├─ Instructions (system prompt)                     │    │
│   │  ├─ Tools (AIFunction[], registered directly)        │    │
│   │  ├─ Thread Management (GetNewThread())               │    │
│   │  └─ Execution Methods:                               │    │
│   │      ├─ RunAsync() → AgentRunResponse                │    │
│   │      └─ RunStreamingAsync() → Updates                │    │
│   └───────────┬──────────────────────────────────────────┘    │
│               │                                                │
│               │ (No intermediate layers)                       │
│               │                                                │
│               ▼                                                │
│   ┌──────────────────────────────────────────────────────┐    │
│   │             AI PROVIDER                               │    │
│   │  (OpenAI, Azure, Copilot, etc.)                      │    │
│   └──────────────────────────────────────────────────────┘    │
│                                                                 │
│   SPECIALIZED AGENTS (where needed):                          │
│   ┌─────────────────────────────────────────────────────┐    │
│   │  CopilotStudioAgent (Microsoft Copilot integration)  │    │
│   │  A2AAgent (Agent-to-Agent communication)             │    │
│   └─────────────────────────────────────────────────────┘    │
│                                                                 │
│   BENEFITS:                                                    │
│   ✓ No Kernel bottleneck (direct provider access)             │
│   ✓ Unified agent type (ChatClientAgent for all providers)    │
│   ✓ Direct function registration (no plugin wrapping)         │
│   ✓ Simpler dependency chain (App→IChatClient→Agent→Provider) │
│   ✓ 15-20% performance improvement (fewer allocations)        │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

---

### 4.2 Trade-off Analysis (MECE)

| Dimension | Semantic Kernel | Agent Framework | Winner |
|-----------|-----------------|-----------------|--------|
| **Simplicity** | Complex (Kernel + Plugin layers) | Simple (Direct registration) | **MAF** |
| **Performance** | Baseline (more allocations) | 15-20% faster (fewer objects) | **MAF** |
| **Flexibility** | High (Kernel extensibility) | Medium (IChatClient constrained) | **SK** |
| **Learning Curve** | Steep (many concepts) | Gentle (fewer abstractions) | **MAF** |
| **Multi-Agent** | Limited (single-agent focus) | Native (AutoGen integration) | **MAF** |
| **Memory** | Rich (built-in semantic memory) | Basic (developer-managed) | **SK** |
| **Provider Support** | Mature (extensive) | Growing (core providers covered) | **SK** |
| **Maintenance** | Higher (more moving parts) | Lower (simpler architecture) | **MAF** |

**Decision Matrix** (MoSCoW Prioritization):

**MUST Have**:
- ✅ MAF: Simple agent creation
- ✅ MAF: Performance improvements
- ✅ MAF: Unified agent interface

**SHOULD Have**:
- ✅ MAF: Multi-agent orchestration
- ⚠️ SK: Rich memory features (can be added via tools)

**COULD Have**:
- ⚠️ SK: Kernel-level extensibility (rarely needed)

**WON'T Need**:
- SK: Complex plugin architecture

**Recommendation**: Migrate to MAF unless you heavily rely on Semantic Kernel's advanced memory features.

---

# PART II: MIGRATION PATTERNS

## 5. Migration Patterns Overview

### 5.1 Migration Complexity Matrix

```
+===============================================================================+
|                     MIGRATION COMPLEXITY ASSESSMENT                           |
+===============================================================================+
|                                                                               |
|  EASY (10-30 minutes)                                                        |
|  ├─ Pattern 1: Namespace updates                                            |
|  ├─ Pattern 2: Agent creation simplification                                |
|  └─ Pattern 3: Thread management                                            |
|                                                                               |
|  MEDIUM (30-60 minutes)                                                      |
|  ├─ Pattern 4: Tool/function registration                                   |
|  ├─ Pattern 5: Non-streaming invocation                                     |
|  ├─ Pattern 6: Streaming invocation                                         |
|  └─ Pattern 7: Options configuration                                        |
|                                                                               |
|  HARD (1-2 hours)                                                            |
|  ├─ Pattern 8: Dependency injection restructuring                           |
|  ├─ Pattern 9: Memory migration (if using SK memory features)               |
|  └─ Pattern 10: Multi-agent orchestration (if complex workflows)            |
|                                                                               |
|  TOTAL ESTIMATED EFFORT: 4-8 hours for typical application                  |
|                                                                               |
+===============================================================================+
```

### 5.2 Pattern Dependency Graph

```
Pattern 1 (Namespaces)
    ↓ (required before all others)
    │
    ├─→ Pattern 2 (Agent Creation) ───→ Pattern 3 (Threads)
    │        │                              │
    │        │                              │
    │        └─→ Pattern 4 (Tools) ─────────┤
    │                   │                   │
    │                   │                   ▼
    │                   │         Pattern 5/6 (Invocation)
    │                   │                   │
    │                   │                   │
    │                   └─→ Pattern 7 (Options)
    │                              │
    │                              │
    └─────────────────────────────→ Pattern 8 (DI)
                                          │
                                          ▼
                                    Migration Complete

Recommended Order: 1 → 2 → 3 → 4 → 5/6 → 7 → 8
```

---

## 6. Pattern-by-Pattern Migration

### Pattern 1: Namespace Updates

**Complexity**: Easy ⭐
**Effort**: 5-10 minutes
**Risk**: Low

**Before (Semantic Kernel)**:
```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
```

**After (Agent Framework)**:
```csharp
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI.Chat;  // If using OpenAI directly
```

**Migration Steps**:
1. Find/Replace: `Microsoft.SemanticKernel` → `Microsoft.Extensions.AI`
2. Find/Replace: `Microsoft.SemanticKernel.Agents` → `Microsoft.Agents.AI`
3. Remove unused SK connector imports

**Verification**:
```bash
# Check for remaining SK references
grep -r "Microsoft.SemanticKernel" ./src
```

---

### Pattern 2: Agent Creation Simplification

**Complexity**: Easy ⭐
**Effort**: 15-30 minutes
**Risk**: Low

**Before (Semantic Kernel)**:
```csharp
// Step 1: Create Kernel
Kernel kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId: "gpt-4",
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    )
    .Build();

// Step 2: Create Agent
ChatCompletionAgent agent = new()
{
    Name = "WeatherAgent",
    Instructions = "You are a helpful weather assistant.",
    Kernel = kernel  // Required dependency
};
```

**After (Agent Framework)**:
```csharp
// Single step: Create agent with provider extension
var chatClient = new OpenAIChatClient(
    model: "gpt-4",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
);

AIAgent agent = chatClient.CreateAIAgent(
    name: "WeatherAgent",
    instructions: "You are a helpful weather assistant."
);
```

**Conceptual Change**:
```
SK:  Kernel (config) → Agent (uses kernel)
MAF: ChatClient (config) → Agent (uses client)

Key: Client is lighter-weight than Kernel
```

**Azure AI Foundry Example**:
```csharp
// Before (SK)
var client = AzureAIAgent.CreateAgentsClient(endpoint, credential);
var definition = await client.Administration.CreateAgentAsync(
    deploymentName,
    instructions: instructions
);
var agent = new AzureAIAgent(definition, client);

// After (MAF)
var persistentClient = new PersistentAgentsClient(endpoint, credential);
var agent = await persistentClient.CreateAIAgentAsync(
    instructions: instructions
);
```

**Migration Checklist**:
- [ ] Replace Kernel.CreateBuilder() with provider client instantiation
- [ ] Remove .Build() calls
- [ ] Replace agent constructor with CreateAIAgent() extension
- [ ] Remove Kernel property assignment
- [ ] Test agent creation succeeds

---

### Pattern 3: Thread Management

**Complexity**: Easy ⭐
**Effort**: 10-20 minutes
**Risk**: Medium (thread cleanup)

**Before (Semantic Kernel)**:
```csharp
// Caller must know which thread type to use
AgentThread thread = new OpenAIAssistantAgentThread(assistantClient);
// OR
AgentThread thread = new AzureAIAgentThread(azureClient);
// OR
AgentThread thread = new OpenAIResponseAgentThread(responseClient);

// Thread deletion (if supported)
await thread.DeleteAsync();
```

**After (Agent Framework)**:
```csharp
// Agent manages thread creation automatically
AgentThread thread = agent.GetNewThread();

// Thread deletion (manual, provider-specific)
// NOT available on AgentThread interface
// Use provider SDK directly:
if (thread.ConversationId != null)
{
    await assistantClient.DeleteThreadAsync(thread.ConversationId);
}
```

**Thread Types (Under the Hood)**:

```
agent.GetNewThread() →
    │
    ├─ If agent has service_thread_id set:
    │      Create service-hosted thread (Azure/OpenAI Assistants)
    │      (Persistent, requires cleanup)
    │
    ├─ If agent has chat_message_store_factory set:
    │      Create in-memory thread
    │      (Ephemeral, auto-cleaned by GC)
    │
    └─ Otherwise:
           Create uninitialized thread
           (Becomes in-memory or service thread on first use)
```

**Thread Cleanup Pattern** (Important!):
```csharp
// Track threads for cleanup
public class ThreadTracker
{
    private readonly Dictionary<string, string> _threads = new();
    private readonly AssistantClient _assistantClient;

    public void TrackThread(string agentId, AgentThread thread)
    {
        if (thread.ConversationId != null)
            _threads[agentId] = thread.ConversationId;
    }

    public async Task CleanupAsync(string agentId)
    {
        if (_threads.TryGetValue(agentId, out var threadId))
        {
            await _assistantClient.DeleteThreadAsync(threadId);
            _threads.Remove(agentId);
        }
    }

    public async Task CleanupAllAsync()
    {
        foreach (var threadId in _threads.Values)
            await _assistantClient.DeleteThreadAsync(threadId);
        _threads.Clear();
    }
}

// Usage
var tracker = new ThreadTracker();
var thread = agent.GetNewThread();
tracker.TrackThread("weather-agent", thread);

// Later, cleanup
await tracker.CleanupAsync("weather-agent");
```

**Migration Checklist**:
- [ ] Replace manual thread construction with agent.GetNewThread()
- [ ] Remove thread type awareness from code
- [ ] Implement thread cleanup tracking (if using service-hosted threads)
- [ ] Test thread lifecycle (create, use, delete)

---

### Pattern 4: Tool/Function Registration

**Complexity**: Medium ⭐⭐
**Effort**: 30-60 minutes
**Risk**: Medium (signature validation)

**Before (Semantic Kernel)**:
```csharp
// Step 1: Define plugin class with attributes
public class WeatherPlugin
{
    [KernelFunction]  // Required attribute
    [Description("Get current weather")]
    public async Task<string> GetWeather(
        [Description("City name")] string city)
    {
        // Implementation
        return await _weatherService.GetCurrentAsync(city);
    }

    [KernelFunction]
    [Description("Get forecast")]
    public async Task<string> GetForecast(
        [Description("City name")] string city,
        [Description("Days ahead")] int days = 3)
    {
        return await _weatherService.GetForecastAsync(city, days);
    }
}

// Step 2: Create KernelFunction wrappers
var getWeather = KernelFunctionFactory.CreateFromMethod(
    typeof(WeatherPlugin).GetMethod(nameof(WeatherPlugin.GetWeather)));

var getForecast = KernelFunctionFactory.CreateFromMethod(
    typeof(WeatherPlugin).GetMethod(nameof(WeatherPlugin.GetForecast)));

// Step 3: Create KernelPlugin
var plugin = KernelPluginFactory.CreateFromFunctions(
    "WeatherPlugin",
    [getWeather, getForecast]
);

// Step 4: Register with Kernel
kernel.Plugins.Add(plugin);

// Step 5: Create agent with Kernel
var agent = new ChatCompletionAgent { Kernel = kernel };
```

**After (Agent Framework)**:
```csharp
// Define functions without [KernelFunction] attribute
public class WeatherTools
{
    // [Description] is optional but recommended for better LLM context
    [Description("Get current weather for a city")]
    public static async Task<string> GetWeather(
        [Description("City name")] string city)
    {
        return await _weatherService.GetCurrentAsync(city);
    }

    [Description("Get weather forecast")]
    public static async Task<string> GetForecast(
        [Description("City name")] string city,
        [Description("Days ahead")] int days = 3)
    {
        return await _weatherService.GetForecastAsync(city, days);
    }
}

// Single-step registration
var agent = chatClient.CreateAIAgent(
    instructions: "You are a weather assistant.",
    tools: [
        AIFunctionFactory.Create(WeatherTools.GetWeather),
        AIFunctionFactory.Create(WeatherTools.GetForecast)
    ]
);
```

**Alternative: Direct Method References**:
```csharp
// If methods are static
var agent = chatClient.CreateAIAgent(
    tools: [
        AIFunctionFactory.Create(GetWeather),  // Direct method reference
        AIFunctionFactory.Create(GetForecast)
    ]
);
```

**Tool Registration Patterns**:

```
Pattern A: Static Methods (Recommended for stateless tools)
─────────────────────────────────────────────────────────
public static class Tools
{
    [Description("...")]
    public static string Method(...) => ...;
}

tools: [AIFunctionFactory.Create(Tools.Method)]


Pattern B: Instance Methods (For stateful tools)
─────────────────────────────────────────────────
public class Tools
{
    private readonly IService _service;

    public Tools(IService service) => _service = service;

    [Description("...")]
    public async Task<T> Method(...) => await _service...;
}

var toolsInstance = new Tools(service);
tools: [AIFunctionFactory.Create(toolsInstance.Method)]


Pattern C: Lambda/Anonymous Functions
───────────────────────────────────────
tools: [
    AIFunctionFactory.Create(
        (string city) => $"Weather in {city}: Sunny",
        name: "GetWeather",
        description: "Get current weather"
    )
]
```

**Function Signature Best Practices**:
```csharp
// ✅ GOOD: Clear parameter descriptions
[Description("Calculate shipping cost")]
public static decimal CalculateShipping(
    [Description("Package weight in kg")] decimal weight,
    [Description("Destination country code")] string country,
    [Description("Express shipping")] bool express = false)
{
    // LLM understands purpose of each parameter
}

// ❌ BAD: No descriptions
public static decimal CalculateShipping(decimal w, string c, bool e = false)
{
    // LLM must guess parameter meanings
}

// ✅ GOOD: Return meaningful objects or JSON-serializable types
[Description("Get user profile")]
public static async Task<UserProfile> GetProfile(string userId)
{
    return new UserProfile { Name = "...", Email = "..." };
}

// ⚠️ OK: Return strings (but less structured)
public static string GetProfile(string userId)
{
    return "Name: ..., Email: ...";
}
```

**Migration Checklist**:
- [ ] Remove [KernelFunction] attributes (keep [Description] if desired)
- [ ] Convert plugin classes to simple tool classes or static methods
- [ ] Replace KernelFunctionFactory.CreateFromMethod() with AIFunctionFactory.Create()
- [ ] Remove KernelPluginFactory and kernel.Plugins.Add() calls
- [ ] Pass tools directly to CreateAIAgent()
- [ ] Test tool invocation with agent

---

### Pattern 5: Non-Streaming Invocation

**Complexity**: Medium ⭐⭐
**Effort**: 30-45 minutes
**Risk**: Medium (response type handling)

**Before (Semantic Kernel)**:
```csharp
// Streaming pattern used even for non-streaming
AgentInvokeOptions options = new()
{
    KernelArguments = new(new OpenAIPromptExecutionSettings
    {
        MaxTokens = 1000
    })
};

await foreach (AgentResponseItem<ChatMessageContent> result in
    agent.InvokeAsync(userInput, thread, agentOptions: options))
{
    // Process each response item
    Console.WriteLine($"Role: {result.Role}");
    Console.WriteLine($"Content: {result.Message.Content}");

    // If tool calls:
    if (result.Message.ToolCalls != null)
    {
        foreach (var toolCall in result.Message.ToolCalls)
        {
            Console.WriteLine($"Tool: {toolCall.FunctionName}");
        }
    }
}
```

**After (Agent Framework)**:
```csharp
// Direct response object
var options = new ChatClientAgentRunOptions(
    new ChatOptions { MaxOutputTokens = 1000 }
);

AgentRunResponse response = await agent.RunAsync(
    userInput,
    thread,
    options: options
);

// Access response
Console.WriteLine($"Final Answer: {response.Text}");  // or response.ToString()

// Access all messages (including tool calls, if any)
Console.WriteLine($"\nTotal Messages: {response.Messages.Count}");
foreach (var message in response.Messages)
{
    Console.WriteLine($"[{message.Role}]: {message.Content}");

    if (message.ToolCalls != null)
    {
        foreach (var toolCall in message.ToolCalls)
        {
            Console.WriteLine($"  Tool: {toolCall.FunctionName}");
        }
    }
}
```

**Response Object Structure**:
```
AgentRunResponse
├─ Text: string (Final answer text)
├─ Messages: IList<ChatMessage>
│  ├─ Message 1 (User input)
│  ├─ Message 2 (Assistant with tool calls)
│  ├─ Message 3 (Tool result)
│  └─ Message 4 (Assistant final answer)
└─ Metadata: Dictionary<string, object>
   ├─ TokenUsage
   └─ Other provider-specific data
```

**Comparison**:
```
Semantic Kernel: IAsyncEnumerable<AgentResponseItem>
├─ Pros: Uniform interface (streaming/non-streaming)
└─ Cons: Confusing for single responses, requires iteration

Agent Framework: AgentRunResponse
├─ Pros: Direct access, no iteration needed
└─ Cons: Different interface from streaming
```

**Migration Pattern**:
```csharp
// Before: Collect streaming items
var responses = new List<AgentResponseItem<ChatMessageContent>>();
await foreach (var item in agent.InvokeAsync(input, thread))
{
    responses.Add(item);
}
var finalAnswer = responses.Last().Message.Content;

// After: Direct access
var response = await agent.RunAsync(input, thread);
var finalAnswer = response.Text;
```

**Migration Checklist**:
- [ ] Replace `await foreach` with `await agent.RunAsync()`
- [ ] Change from `AgentResponseItem<ChatMessageContent>` to `AgentRunResponse`
- [ ] Access `.Text` instead of iterating over items
- [ ] Update logging/debugging code to use `response.Messages`
- [ ] Test with tool-calling scenarios

---

### Pattern 6: Streaming Invocation

**Complexity**: Medium ⭐⭐
**Effort**: 30-45 minutes
**Risk**: Low

**Before (Semantic Kernel)**:
```csharp
await foreach (StreamingChatMessageContent update in
    agent.InvokeStreamingAsync(userInput, thread))
{
    Console.Write(update.Content);
}
Console.WriteLine();  // Final newline
```

**After (Agent Framework)**:
```csharp
// Returns AgentRunResponseUpdate objects
await foreach (AgentRunResponseUpdate update in
    agent.RunStreamingAsync(userInput, thread))
{
    Console.Write(update.Text);  // or update.ToString()
}
Console.WriteLine();  // Final newline
```

**Update Object Comparison**:
```
StreamingChatMessageContent (SK)
├─ Content: string
├─ Role: ChatRole
└─ Metadata: Dictionary

AgentRunResponseUpdate (MAF)
├─ Text: string (incremental text)
├─ Role: ChatRole
├─ ToolCalls: IList<ToolCall> (if tool invoked)
└─ Metadata: Dictionary
```

**Reconstruct Full Response**:
```csharp
// Collect all updates into full response
var fullResponse = AgentRunResponse.FromAgentRunResponseUpdates(
    agent.RunStreamingAsync(userInput, thread)
);

Console.WriteLine($"Full Text: {fullResponse.Text}");
Console.WriteLine($"Total Messages: {fullResponse.Messages.Count}");
```

**Streaming with Tool Calls**:
```csharp
await foreach (var update in agent.RunStreamingAsync(userInput, thread))
{
    if (update.Text != null)
    {
        Console.Write(update.Text);
    }

    if (update.ToolCalls != null)
    {
        foreach (var toolCall in update.ToolCalls)
        {
            Console.WriteLine($"\n[Calling tool: {toolCall.FunctionName}]");
        }
    }
}
```

**Migration Checklist**:
- [ ] Replace InvokeStreamingAsync with RunStreamingAsync
- [ ] Change `StreamingChatMessageContent` to `AgentRunResponseUpdate`
- [ ] Update `.Content` to `.Text`
- [ ] Test streaming output renders correctly
- [ ] Test tool calls appear in stream (if applicable)

---

### Pattern 7: Options Configuration

**Complexity**: Medium ⭐⭐
**Effort**: 20-30 minutes
**Risk**: Low

**Before (Semantic Kernel)**:
```csharp
// Complex nested options
OpenAIPromptExecutionSettings settings = new()
{
    MaxTokens = 1000,
    Temperature = 0.7,
    TopP = 0.9,
    FrequencyPenalty = 0.5,
    PresencePenalty = 0.3
};

KernelArguments kernelArgs = new(settings);

AgentInvokeOptions agentOptions = new()
{
    KernelArguments = kernelArgs
};

var response = await agent.InvokeAsync(input, thread, agentOptions: agentOptions);
```

**After (Agent Framework)**:
```csharp
// Flattened options
var options = new ChatClientAgentRunOptions(
    new ChatOptions
    {
        MaxOutputTokens = 1000,
        Temperature = 0.7,
        TopP = 0.9,
        FrequencyPenalty = 0.5,
        PresencePenalty = 0.3
    }
);

var response = await agent.RunAsync(input, thread, options: options);
```

**Options Mapping**:
| SK Setting | MAF Setting |
|------------|-------------|
| `MaxTokens` | `MaxOutputTokens` |
| `Temperature` | `Temperature` |
| `TopP` | `TopP` |
| `FrequencyPenalty` | `FrequencyPenalty` |
| `PresencePenalty` | `PresencePenalty` |
| `ResponseFormat` | `ResponseFormat` |

**Inline Options** (Simplest):
```csharp
// No separate variable needed
var response = await agent.RunAsync(
    input,
    thread,
    options: new ChatClientAgentRunOptions(
        new ChatOptions { MaxOutputTokens = 500 }
    )
);
```

**Default Options** (Agent-Level):
```csharp
// Set defaults at agent creation
var agent = chatClient.CreateAIAgent(
    instructions: "...",
    // Default options applied to all runs
    options: new ChatOptions { Temperature = 0.8 }
);

// Override per-run if needed
var response = await agent.RunAsync(
    input,
    thread,
    options: new ChatClientAgentRunOptions(
        new ChatOptions { Temperature = 0.5 }  // Override
    )
);
```

**Migration Checklist**:
- [ ] Replace OpenAIPromptExecutionSettings with ChatOptions
- [ ] Replace KernelArguments with ChatClientAgentRunOptions
- [ ] Remove AgentInvokeOptions wrapper
- [ ] Update MaxTokens → MaxOutputTokens
- [ ] Test options are applied correctly

---

### Pattern 8: Dependency Injection

**Complexity**: Hard ⭐⭐⭐
**Effort**: 60-120 minutes
**Risk**: High (affects startup)

**Before (Semantic Kernel)**:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

var services = new ServiceCollection();

// Register Kernel as singleton
services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: "gpt-4",
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    );

// Register agents with Kernel dependency
services.AddKeyedSingleton<SemanticKernel.Agents.Agent>(
    "WeatherAgent",
    (sp, key) =>
    {
        var kernel = sp.GetRequiredService<Kernel>();

        // Load plugins
        var weatherPlugin = new WeatherPlugin(sp.GetRequiredService<IWeatherService>());
        kernel.Plugins.Add(
            KernelPluginFactory.CreateFromObject(weatherPlugin, "Weather")
        );

        return new ChatCompletionAgent
        {
            Name = "WeatherAgent",
            Instructions = "You are a weather assistant.",
            Kernel = kernel
        };
    }
);

services.AddKeyedSingleton<SemanticKernel.Agents.Agent>(
    "SupportAgent",
    (sp, key) =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return new ChatCompletionAgent
        {
            Name = "SupportAgent",
            Instructions = "You are a support agent.",
            Kernel = kernel
        };
    }
);

var serviceProvider = services.BuildServiceProvider();

// Use agents
var weatherAgent = serviceProvider.GetRequiredKeyedService<Agent>("WeatherAgent");
var supportAgent = serviceProvider.GetRequiredKeyedService<Agent>("SupportAgent");
```

**After (Agent Framework)**:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

var services = new ServiceCollection();

// Register IChatClient (no Kernel needed)
services.AddSingleton<IChatClient>(sp =>
    new OpenAIChatClient(
        model: "gpt-4",
        apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    )
);

// Register agents directly
services.AddKeyedSingleton<AIAgent>(
    "WeatherAgent",
    (sp, key) =>
    {
        var chatClient = sp.GetRequiredService<IChatClient>();
        var weatherService = sp.GetRequiredService<IWeatherService>();

        var weatherTools = new WeatherTools(weatherService);

        return chatClient.CreateAIAgent(
            name: "WeatherAgent",
            instructions: "You are a weather assistant.",
            tools: [
                AIFunctionFactory.Create(weatherTools.GetWeather),
                AIFunctionFactory.Create(weatherTools.GetForecast)
            ]
        );
    }
);

services.AddKeyedSingleton<AIAgent>(
    "SupportAgent",
    (sp, key) =>
    {
        var chatClient = sp.GetRequiredService<IChatClient>();
        return chatClient.CreateAIAgent(
            name: "SupportAgent",
            instructions: "You are a support agent."
        );
    }
);

var serviceProvider = services.BuildServiceProvider();

// Use agents (same interface)
var weatherAgent = serviceProvider.GetRequiredKeyedService<AIAgent>("WeatherAgent");
var supportAgent = serviceProvider.GetRequiredKeyedService<AIAgent>("SupportAgent");
```

**Advanced DI Pattern (Factory)**:
```csharp
// Create agent factory
public class AgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly IWeatherService _weatherService;
    private readonly ISupportService _supportService;

    public AgentFactory(
        IChatClient chatClient,
        IWeatherService weatherService,
        ISupportService supportService)
    {
        _chatClient = chatClient;
        _weatherService = weatherService;
        _supportService = supportService;
    }

    public AIAgent CreateWeatherAgent()
    {
        var tools = new WeatherTools(_weatherService);
        return _chatClient.CreateAIAgent(
            name: "WeatherAgent",
            instructions: "You are a weather assistant.",
            tools: [
                AIFunctionFactory.Create(tools.GetWeather),
                AIFunctionFactory.Create(tools.GetForecast)
            ]
        );
    }

    public AIAgent CreateSupportAgent()
    {
        var tools = new SupportTools(_supportService);
        return _chatClient.CreateAIAgent(
            name: "SupportAgent",
            instructions: "You are a support agent.",
            tools: [
                AIFunctionFactory.Create(tools.CreateTicket),
                AIFunctionFactory.Create(tools.GetStatus)
            ]
        );
    }
}

// Register factory
services.AddSingleton<AgentFactory>();

// Register agents via factory
services.AddKeyedSingleton<AIAgent>(
    "WeatherAgent",
    (sp, key) => sp.GetRequiredService<AgentFactory>().CreateWeatherAgent()
);

services.AddKeyedSingleton<AIAgent>(
    "SupportAgent",
    (sp, key) => sp.GetRequiredService<AgentFactory>().CreateSupportAgent()
);
```

**Migration Checklist**:
- [ ] Remove `services.AddKernel()` registration
- [ ] Register `IChatClient` instead
- [ ] Update agent factories to use `ChatClient.CreateAIAgent()`
- [ ] Remove Kernel dependency from agent constructors
- [ ] Update all `GetRequiredService<Kernel>()` calls
- [ ] Test DI container builds successfully
- [ ] Test agents resolve correctly

---

## 7. Low-Level Implementation Details

### 7.1 IChatClient Interface (Foundation)

The entire MAF architecture is built on `IChatClient`:

```csharp
namespace Microsoft.Extensions.AI;

public interface IChatClient
{
    // Non-streaming invocation
    Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // Streaming invocation
    IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    // Metadata
    ChatClientMetadata Metadata { get; }
}
```

**How Agents Use IChatClient**:
```
AgentRunAsync(input, thread)
    ↓
1. Build chatMessages list from thread.Messages + new user input
    ↓
2. Call IChatClient.CompleteAsync(chatMessages, options)
    ↓
3. LLM processes messages, may invoke tools
    ↓
4. If tool calls detected:
   ├─ Execute tool functions
   ├─ Append tool results to chatMessages
   └─ Loop back to step 2
    ↓
5. LLM generates final response
    ↓
6. Construct AgentRunResponse from all messages
    ↓
7. Return to caller
```

**Provider Implementations**:

```csharp
// OpenAI
public class OpenAIChatClient : IChatClient
{
    public Task<ChatCompletion> CompleteAsync(...) =>
        // Call OpenAI API
}

// Azure OpenAI
public class AzureOpenAIChatClient : IChatClient
{
    public Task<ChatCompletion> CompleteAsync(...) =>
        // Call Azure OpenAI API
}

// OpenAI Assistants
public class OpenAIAssistantClient : IChatClient
{
    public Task<ChatCompletion> CompleteAsync(...) =>
        // Call Assistants API (with runs, threads, etc.)
}
```

---

### 7.2 Message Flow Internals

**Message Structure**:
```csharp
public class ChatMessage
{
    public ChatRole Role { get; set; }  // User, Assistant, System, Tool
    public string? Content { get; set; }  // Text content
    public IList<ChatToolCall>? ToolCalls { get; set; }  // Function calls
    public string? ToolCallId { get; set; }  // For tool results
}
```

**Example Flow with Tool Calls**:

**Input**:
```
User: "What's the weather in Seattle and create a reminder for tomorrow?"
```

**Message History Evolution**:

```
State 1 (Initial):
[
    { Role: User, Content: "What's the weather in Seattle and create a reminder for tomorrow?" }
]

State 2 (After LLM processes, detects tool calls):
[
    { Role: User, Content: "..." },
    { Role: Assistant, ToolCalls: [
        { FunctionName: "GetWeather", Arguments: { city: "Seattle" } },
        { FunctionName: "CreateReminder", Arguments: { text: "...", date: "2025-12-31" } }
    ]}
]

State 3 (After tool execution):
[
    { Role: User, Content: "..." },
    { Role: Assistant, ToolCalls: [...] },
    { Role: Tool, ToolCallId: "call_1", Content: "Seattle: 72°F, sunny" },
    { Role: Tool, ToolCallId: "call_2", Content: "Reminder created successfully" }
]

State 4 (After LLM generates final response):
[
    { Role: User, Content: "..." },
    { Role: Assistant, ToolCalls: [...] },
    { Role: Tool, ToolCallId: "call_1", Content: "..." },
    { Role: Tool, ToolCallId: "call_2", Content: "..." },
    { Role: Assistant, Content: "The weather in Seattle is sunny, 72°F. I've created a reminder for tomorrow." }
]
```

**AgentRunResponse**:
```
response.Text = "The weather in Seattle is sunny, 72°F. I've created a reminder for tomorrow."
response.Messages = [all 5 messages above]
response.Messages.Count = 5
```

---

### 7.3 Tool Execution Mechanism

**Tool Invocation Flow**:

```
1. LLM returns ChatToolCall objects
    {
        Id: "call_abc123",
        FunctionName: "GetWeather",
        Arguments: { "city": "Seattle" }
    }

2. Agent looks up AIFunction by name
    registered_tools["GetWeather"] → AIFunction instance

3. AIFunction deserializes arguments
    Arguments JSON → C# method parameters

4. AIFunction invokes C# method
    GetWeather("Seattle") → "72°F, sunny"

5. Result wrapped in ChatMessage
    {
        Role: Tool,
        ToolCallId: "call_abc123",
        Content: "72°F, sunny"
    }

6. Append to messages and loop back to LLM
```

**Error Handling in Tools**:

```csharp
[Description("Get user by ID")]
public static async Task<User> GetUser(string userId)
{
    try
    {
        var user = await _userService.GetAsync(userId);
        if (user == null)
        {
            // Return structured error
            return new User { Error = "User not found" };
        }
        return user;
    }
    catch (Exception ex)
    {
        // Exception captured and returned as tool result
        // LLM sees: "Tool execution failed: {ex.Message}"
        throw;  // Or handle gracefully
    }
}
```

---

### 7.4 ChatOptions → Provider Settings Translation

**ChatOptions (Unified)**:
```csharp
public class ChatOptions
{
    public int? MaxOutputTokens { get; set; }
    public float? Temperature { get; set; }
    public float? TopP { get; set; }
    public float? FrequencyPenalty { get; set; }
    public float? PresencePenalty { get; set; }
    public string? ResponseFormat { get; set; }
    public IList<string>? StopSequences { get; set; }
}
```

**Provider-Specific Translation**:

```
ChatOptions (MAF) → OpenAI ChatCompletionOptions
─────────────────────────────────────────────────
MaxOutputTokens → max_tokens
Temperature → temperature
TopP → top_p
FrequencyPenalty → frequency_penalty
PresencePenalty → presence_penalty
ResponseFormat → response_format (e.g., "json_object")
StopSequences → stop


ChatOptions (MAF) → Azure OpenAI ChatCompletionOptions
───────────────────────────────────────────────────────
(Same as OpenAI, plus Azure-specific extensions)


Not all providers support all options:
- Some providers ignore unsupported options
- Check provider documentation for compatibility
```

---

## 8. Common Pitfalls & Best Practices

### 8.1 Pitfall: Kernel-Based Thinking

**Problem**: Trying to replicate SK's Kernel-centric model in MAF

❌ **Wrong Approach**:
```csharp
// Don't try to create a "Kernel replacement"
public class MyKernel
{
    private IChatClient _client;
    private Dictionary<string, Plugin> _plugins = new();

    public void AddPlugin(Plugin plugin) { ... }
    public Agent CreateAgent() { ... }
}
```

✅ **Correct Approach**:
```csharp
// Embrace provider-centric model
var client = new OpenAIChatClient(...);
var agent = client.CreateAIAgent(
    tools: [tool1, tool2, tool3]  // Direct registration
);
```

**Reason**: The Kernel abstraction adds unnecessary complexity. MAF intentionally simplifies.

---

### 8.2 Pitfall: Thread Lifecycle Mismanagement

**Problem**: Orphaned service-hosted threads

❌ **Wrong Approach**:
```csharp
// Create thread, use it, forget it
var thread = agent.GetNewThread();
var response = await agent.RunAsync(input, thread);
// Thread ID lost → orphaned on provider
```

✅ **Correct Approach**:
```csharp
// Track and cleanup
var threadTracker = new ThreadTracker();
var thread = agent.GetNewThread();
threadTracker.Track("agent-1", thread);

var response = await agent.RunAsync(input, thread);

// Later
await threadTracker.CleanupAsync("agent-1");
```

**Reason**: Service-hosted threads (OpenAI Assistants, Azure AI Foundry) persist server-side and can accumulate.

---

### 8.3 Pitfall: Tool Registration Complexity

**Problem**: Over-engineering tool registration

❌ **Wrong Approach**:
```csharp
// Don't manually create wrappers
var tools = new List<AIFunction>();
foreach (var method in typeof(ToolsClass).GetMethods())
{
    var function = AIFunctionFactory.Create(method);
    tools.Add(function);
}
var agent = client.CreateAIAgent(tools: tools);
```

✅ **Correct Approach**:
```csharp
// Simple, declarative registration
var agent = client.CreateAIAgent(
    tools: [
        AIFunctionFactory.Create(Tool1),
        AIFunctionFactory.Create(Tool2),
        AIFunctionFactory.Create(Tool3)
    ]
);
```

**Reason**: Explicit is better than dynamic. Clear what tools are registered.

---

### 8.4 Pitfall: Response Type Confusion

**Problem**: Treating non-streaming like streaming

❌ **Wrong Approach**:
```csharp
var response = agent.RunAsync(input, thread);  // Returns Task<AgentRunResponse>
await foreach (var item in response)  // Compile error! Not IAsyncEnumerable
{
    Console.WriteLine(item);
}
```

✅ **Correct Approach**:
```csharp
// Non-streaming
var response = await agent.RunAsync(input, thread);
Console.WriteLine(response.Text);

// Streaming
await foreach (var update in agent.RunStreamingAsync(input, thread))
{
    Console.Write(update.Text);
}
```

**Reason**: Different return types for different use cases. RunAsync is NOT IAsyncEnumerable.

---

### 8.5 Best Practice: Use Provider Extensions

**Benefit**: Leverage provider-specific convenience methods

✅ **Recommended**:
```csharp
// OpenAI
var agent = chatClient.CreateAIAgent(instructions: "...");

// Azure AI Foundry
var agent = await persistentClient.CreateAIAgentAsync(instructions: "...");

// OpenAI Assistants
var agent = await assistantClient.CreateAIAgentAsync(instructions: "...");
```

**Instead of**:
```csharp
// Manual construction (more boilerplate)
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "...",
    name: "Agent1"
);
```

**Reason**: Extensions handle provider-specific setup details automatically.

---

### 8.6 Best Practice: Graceful Options Handling

**Pattern**:
```csharp
public async Task<AgentRunResponse> RunWithOptionsAsync(
    AIAgent agent,
    string input,
    AgentThread thread)
{
    ChatClientAgentRunOptions? options = null;

    if (agent is ChatClientAgent)
    {
        // ChatClientAgent supports ChatOptions
        options = new ChatClientAgentRunOptions(
            new ChatOptions { MaxOutputTokens = 1000 }
        );
    }

    return await agent.RunAsync(input, thread, options: options);
}
```

**Reason**: Not all agent types support all options. Check type before applying.

---

### 8.7 Best Practice: Tool Error Handling

**Pattern**:
```csharp
[Description("Get customer order")]
public static async Task<Order> GetOrder(
    [Description("Order ID")] string orderId)
{
    try
    {
        var order = await _orderService.GetAsync(orderId);

        if (order == null)
        {
            // Return structured "not found" rather than throwing
            return new Order
            {
                Error = $"Order {orderId} not found"
            };
        }

        return order;
    }
    catch (Exception ex)
    {
        // Log error
        _logger.LogError(ex, "Failed to get order {OrderId}", orderId);

        // Return error to LLM (don't throw)
        return new Order
        {
            Error = $"Failed to retrieve order: {ex.Message}"
        };
    }
}
```

**Reason**: Exceptions break tool execution loop. Structured errors let LLM handle gracefully.

---

### 8.8 Best Practice: Thread State Management

**For Stateless Scenarios**:
```csharp
// Create thread per request
public async Task<string> HandleRequestAsync(string input)
{
    var thread = agent.GetNewThread();
    var response = await agent.RunAsync(input, thread);
    return response.Text;
    // Thread auto-cleaned by GC
}
```

**For Stateful Scenarios**:
```csharp
// Persist thread ID
public class ConversationManager
{
    private readonly Dictionary<string, string> _userThreads = new();

    public async Task<string> ContinueConversationAsync(
        string userId,
        string input)
    {
        // Get or create thread
        if (!_userThreads.TryGetValue(userId, out var threadId))
        {
            var newThread = agent.GetNewThread();
            threadId = newThread.ConversationId;
            _userThreads[userId] = threadId;
        }

        // Reconstruct thread from ID
        var thread = agent.GetNewThread();  // Reuses existing threadId
        var response = await agent.RunAsync(input, thread);
        return response.Text;
    }

    public async Task EndConversationAsync(string userId)
    {
        if (_userThreads.TryGetValue(userId, out var threadId))
        {
            await assistantClient.DeleteThreadAsync(threadId);
            _userThreads.Remove(userId);
        }
    }
}
```

---

## 9. Real-World Migration Scenarios

### 9.1 Scenario: Customer Support Agent

**Before (Semantic Kernel)**:
```csharp
// Setup
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4", apiKey)
    .Build();

// Add support plugin
public class SupportPlugin
{
    private readonly ITicketService _ticketService;

    public SupportPlugin(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [KernelFunction]
    [Description("Get customer info")]
    public async Task<Customer> GetCustomer(string customerId) =>
        await _ticketService.GetCustomerAsync(customerId);

    [KernelFunction]
    [Description("Create support ticket")]
    public async Task<Ticket> CreateTicket(
        string customerId,
        string issue,
        string priority = "Medium") =>
        await _ticketService.CreateTicketAsync(customerId, issue, priority);
}

var supportPlugin = new SupportPlugin(ticketService);
kernel.Plugins.Add(
    KernelPluginFactory.CreateFromObject(supportPlugin, "Support")
);

// Create agent
var agent = new ChatCompletionAgent
{
    Name = "SupportAgent",
    Instructions = @"
You are a helpful customer support agent.
Use available tools to:
- Look up customer information
- Create support tickets
- Provide helpful assistance
",
    Kernel = kernel
};

// Use agent
var thread = new OpenAIResponseAgentThread(client);

await foreach (var response in agent.InvokeAsync(customerQuery, thread))
{
    Console.WriteLine(response.Message.Content);
}
```

**After (Agent Framework)**:
```csharp
// Setup - simpler
var chatClient = new OpenAIChatClient("gpt-4", apiKey);

// Define tools (no plugin class needed)
public class SupportTools
{
    private readonly ITicketService _ticketService;

    public SupportTools(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [Description("Get customer information by ID")]
    public async Task<Customer> GetCustomer(
        [Description("Customer ID")] string customerId) =>
        await _ticketService.GetCustomerAsync(customerId);

    [Description("Create a new support ticket")]
    public async Task<Ticket> CreateTicket(
        [Description("Customer ID")] string customerId,
        [Description("Issue description")] string issue,
        [Description("Priority level")] string priority = "Medium") =>
        await _ticketService.CreateTicketAsync(customerId, issue, priority);
}

// Create agent with tools inline
var supportTools = new SupportTools(ticketService);

var agent = chatClient.CreateAIAgent(
    name: "SupportAgent",
    instructions: @"
You are a helpful customer support agent.
Use available tools to:
- Look up customer information
- Create support tickets
- Provide helpful assistance
",
    tools: [
        AIFunctionFactory.Create(supportTools.GetCustomer),
        AIFunctionFactory.Create(supportTools.CreateTicket)
    ]
);

// Use agent - cleaner
var thread = agent.GetNewThread();
var response = await agent.RunAsync(customerQuery, thread);
Console.WriteLine(response.Text);
```

**Benefits**:
- **40% less code** (removed Kernel setup, plugin wrapping)
- **Faster execution** (direct tool registration)
- **Clearer intent** (tools passed directly to agent)

---

### 9.2 Scenario: Multi-Agent Workflow

**Before (Semantic Kernel)**:
```csharp
// Create kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4", apiKey)
    .Build();

// Analyzer agent
var analyzerAgent = new ChatCompletionAgent
{
    Name = "Analyzer",
    Instructions = "Analyze user requests and extract key information.",
    Kernel = kernel
};

// Planner agent
var plannerAgent = new ChatCompletionAgent
{
    Name = "Planner",
    Instructions = "Create detailed plans based on analysis.",
    Kernel = kernel
};

// Executor agent
var executorAgent = new ChatCompletionAgent
{
    Name = "Executor",
    Instructions = "Execute plans and report results.",
    Kernel = kernel
};

// Orchestrate
var thread1 = new OpenAIResponseAgentThread(client);
await foreach (var analysis in analyzerAgent.InvokeAsync(userRequest, thread1))
{
    analysisResult = analysis.Message.Content;
}

var thread2 = new OpenAIResponseAgentThread(client);
await foreach (var plan in plannerAgent.InvokeAsync($"Based on: {analysisResult}", thread2))
{
    planResult = plan.Message.Content;
}

var thread3 = new OpenAIResponseAgentThread(client);
await foreach (var result in executorAgent.InvokeAsync($"Execute: {planResult}", thread3))
{
    finalResult = result.Message.Content;
}
```

**After (Agent Framework)**:
```csharp
// Create chat client
var chatClient = new OpenAIChatClient("gpt-4", apiKey);

// Create agents
var analyzerAgent = chatClient.CreateAIAgent(
    name: "Analyzer",
    instructions: "Analyze user requests and extract key information."
);

var plannerAgent = chatClient.CreateAIAgent(
    name: "Planner",
    instructions: "Create detailed plans based on analysis."
);

var executorAgent = chatClient.CreateAIAgent(
    name: "Executor",
    instructions: "Execute plans and report results."
);

// Orchestrate - cleaner
var thread1 = analyzerAgent.GetNewThread();
var analysisResult = await analyzerAgent.RunAsync(userRequest, thread1);

var thread2 = plannerAgent.GetNewThread();
var planResult = await plannerAgent.RunAsync(
    $"Based on: {analysisResult.Text}",
    thread2
);

var thread3 = executorAgent.GetNewThread();
var finalResult = await executorAgent.RunAsync(
    $"Execute: {planResult.Text}",
    thread3
);

Console.WriteLine($"Final Result: {finalResult.Text}");
```

**Benefits**:
- **50% less code** (removed Kernel, simplified invocations)
- **Clearer flow** (linear execution, no async enumerables)
- **Better debugging** (direct response access)

---

### 9.3 Scenario: Azure AI Foundry Integration

**Before (Semantic Kernel)**:
```csharp
// Connect to Azure AI Foundry
var client = AzureAIAgent.CreateAgentsClient(
    new Uri("https://my-project.azureai.net"),
    new AzureCliCredential()
);

// Create agent definition
var definition = await client.Administration.CreateAgentAsync(
    deploymentName: "gpt-4",
    instructions: "You are a helpful assistant.",
    name: "AssistantAgent"
);

// Wrap in SK agent
var agent = new AzureAIAgent(definition, client);

// Kernel still needed for tools
var kernel = Kernel.CreateBuilder().Build();
kernel.Plugins.Add(...);
agent.Kernel = kernel;

// Use
var thread = new AzureAIAgentThread(client);
await foreach (var response in agent.InvokeAsync(input, thread))
{
    Console.WriteLine(response.Message.Content);
}
```

**After (Agent Framework)**:
```csharp
// Connect to Azure AI Foundry
var persistentClient = new PersistentAgentsClient(
    new Uri("https://my-project.azureai.net"),
    new AzureCliCredential()
);

// Create agent with tools in one call
var agent = await persistentClient.CreateAIAgentAsync(
    instructions: "You are a helpful assistant.",
    name: "AssistantAgent",
    tools: [
        AIFunctionFactory.Create(GetWeather),
        AIFunctionFactory.Create(CreateReminder)
    ]
);

// Use - simpler
var thread = agent.GetNewThread();
var response = await agent.RunAsync(input, thread);
Console.WriteLine(response.Text);

// Retrieve existing agent
var existingAgent = await persistentClient.GetAIAgentAsync(agentId);
```

**Benefits**:
- **No Kernel dependency**
- **Tools registered at creation**
- **Unified API across all providers**

---

## 10. Migration Checklist & Next Steps

### 10.1 Pre-Migration Checklist

**Phase 1: Assessment**
- [ ] Audit Semantic Kernel usage across codebase
  ```bash
  grep -r "using Microsoft.SemanticKernel" ./src
  ```
- [ ] Count agent instances (estimate migration effort)
- [ ] Identify tool/plugin patterns used
- [ ] Map provider dependencies (OpenAI, Azure, etc.)
- [ ] Document advanced SK features used (memory, planning, etc.)

**Phase 2: Planning**
- [ ] Estimate migration effort per component
  - Easy: Namespace + Agent creation (~30 min each)
  - Medium: Tools + Invocation (~60 min each)
  - Hard: DI + Complex workflows (~120 min each)
- [ ] Prioritize components (critical path first)
- [ ] Design thread cleanup strategy (if using service-hosted threads)
- [ ] Plan testing approach (unit + integration tests)
- [ ] Create backup/rollback plan

---

### 10.2 Migration Execution Checklist

**Week 1: Foundation**
- [ ] **Day 1-2: Namespace & Agent Creation**
  - [ ] Update all `using` statements
  - [ ] Replace Kernel creation with IChatClient instantiation
  - [ ] Convert agent constructors to `CreateAIAgent()` calls
  - [ ] Remove Kernel property assignments
  - [ ] Run initial compilation test

- [ ] **Day 3-4: Thread & Tool Management**
  - [ ] Replace manual thread creation with `agent.GetNewThread()`
  - [ ] Implement ThreadTracker (if needed)
  - [ ] Convert Plugin classes to tool classes
  - [ ] Remove [KernelFunction] attributes
  - [ ] Update tool registration to use `AIFunctionFactory`

- [ ] **Day 5: Validation**
  - [ ] Unit tests for agent creation
  - [ ] Integration tests for tool invocation
  - [ ] Thread lifecycle tests

**Week 2: Invocation & Configuration**
- [ ] **Day 1-2: Invocation Methods**
  - [ ] Replace `InvokeAsync` with `RunAsync`
  - [ ] Replace `InvokeStreamingAsync` with `RunStreamingAsync`
  - [ ] Update response handling (remove `await foreach` for non-streaming)
  - [ ] Update logging/debugging code

- [ ] **Day 3-4: Options Configuration**
  - [ ] Replace `OpenAIPromptExecutionSettings` with `ChatOptions`
  - [ ] Replace `KernelArguments` with `ChatClientAgentRunOptions`
  - [ ] Update `MaxTokens` → `MaxOutputTokens`
  - [ ] Test options applied correctly

- [ ] **Day 5: Integration Testing**
  - [ ] End-to-end tests with all patterns
  - [ ] Performance benchmarking (compare SK vs MAF)

**Week 3: Advanced Features**
- [ ] **Day 1-2: Dependency Injection**
  - [ ] Remove `services.AddKernel()`
  - [ ] Register `IChatClient` instead
  - [ ] Update agent factory methods
  - [ ] Test DI container resolution

- [ ] **Day 3-4: Memory Migration** (if applicable)
  - [ ] Map SK memory features to MAF equivalents
  - [ ] Implement custom tools for external memory (vector DB, etc.)
  - [ ] Test memory persistence

- [ ] **Day 5: Multi-Agent Workflows** (if applicable)
  - [ ] Refactor orchestration logic
  - [ ] Test agent-to-agent communication

**Week 4: Production Readiness**
- [ ] **Day 1-2: Parallel Deployment** (Feature Flag)
  - [ ] Implement adapter pattern (if needed)
  - [ ] Deploy with feature flag (10% traffic)
  - [ ] Monitor metrics (latency, errors, token usage)

- [ ] **Day 3: Gradual Rollout**
  - [ ] Increase to 50% traffic
  - [ ] Monitor and validate
  - [ ] Increase to 100% traffic

- [ ] **Day 4: Cleanup & Optimization**
  - [ ] Remove SK dependencies from project
  - [ ] Optimize performance based on metrics
  - [ ] Update documentation

- [ ] **Day 5: Retrospective**
  - [ ] Document lessons learned
  - [ ] Plan Phase 2 (advanced features)

---

### 10.3 Post-Migration Validation

**Functional Testing**:
- [ ] All agents create successfully
- [ ] Tool invocation works correctly
- [ ] Streaming responses render properly
- [ ] Thread lifecycle managed correctly
- [ ] Options applied as expected
- [ ] Multi-agent workflows function correctly

**Performance Testing**:
- [ ] Measure response latency (compare SK vs MAF)
- [ ] Check memory usage (expect 15-20% reduction)
- [ ] Validate token usage (should be similar)
- [ ] Test concurrency (multiple simultaneous agents)

**Integration Testing**:
- [ ] Test with all providers (OpenAI, Azure, etc.)
- [ ] Verify error handling
- [ ] Test edge cases (empty input, malformed requests, etc.)
- [ ] Validate logging and monitoring

---

### 10.4 Next Steps

**Immediate (Post-Migration)**:
1. **Monitor production** for 1-2 weeks
2. **Gather feedback** from team
3. **Optimize** based on telemetry
4. **Document** any issues encountered

**Short-Term (1-3 months)**:
1. Explore **AutoGen integration** for advanced multi-agent scenarios
2. Implement **Copilot Studio agents** (if applicable)
3. Add **advanced tooling** (vector stores, RAG, etc.)
4. Optimize **token usage** and costs

**Long-Term (3-6 months)**:
1. Contribute to **MAF open source** (if bugs found)
2. Stay updated with **MAF releases** (currently in preview)
3. Explore **agent-to-agent (A2A)** communication patterns
4. Plan migration to **MAF v2.0** (when GA)

---

### 10.5 Resources & References

**Official Documentation**:
- [Microsoft Agent Framework Migration Guide](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel/?pivots=programming-language-csharp)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/)
- [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/)

**Community Resources**:
- [MAF GitHub Repository](https://github.com/microsoft/semantic-kernel) (includes MAF)
- [MAF Samples](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples)
- [Discord Community](https://aka.ms/sk-discord)

**Related Technologies**:
- [AutoGen Documentation](https://microsoft.github.io/autogen/)
- [OpenAI Assistants API](https://platform.openai.com/docs/assistants/overview)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)

---

## Summary

### Key Takeaways

**Architectural Shift** (First Principles):
- Semantic Kernel's Kernel abstraction is **eliminated**
- MAF uses **provider-agnostic IChatClient** interface
- Result: **15-20% performance improvement**, **40-50% less code**

**Migration Complexity** (MoSCoW):
- **MUST**: Namespace + Agent creation + Thread management (Easy)
- **SHOULD**: Tools + Invocation methods (Medium)
- **COULD**: DI restructuring (Hard)
- **TOTAL EFFORT**: 4-8 hours for typical application

**Risk Assessment**:
- **Low Risk**: Namespace, agent creation, tools, invocation
- **Medium Risk**: Thread cleanup, options configuration
- **High Risk**: DI changes (affects startup)

**Decision Recommendation** (Type 1/Type 2):
- Migration is **mostly reversible** (Type 2 decision)
- **Adopt MAF** for all new development
- **Migrate existing** using 4-week roadmap with OODA feedback loops

---

**Status**: ✅ Complete Migration Reference
**Next Action**: Begin Phase 1 (Assessment) from checklist
**Support**: Refer to this guide during migration + official MS docs

**End of Migration Guide v1**
