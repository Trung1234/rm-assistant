# DEEP AGENT SYSTEM - Architecture Redesign v2

**Document ID**: Over-v2-deep-agent-architecture-redesign
**Created**: 2026-01-12
**Version**: v2
**Status**: Draft - Design Phase
**Previous Version**: [Over-v1-architecture-analysis.md](Over-v1-architecture-analysis.md)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [v1 Problems Analysis (Basic RCA)](#2-v1-problems-analysis-basic-rca)
3. [v2 Architecture Overview](#3-v2-architecture-overview)
4. [Leader Agent Design](#4-leader-agent-design)
5. [Context Sharing Architecture](#5-context-sharing-architecture)
6. [MCP Integration Design](#6-mcp-integration-design)
7. [Workflow Diagrams](#7-workflow-diagrams)
8. [Technical Thinking Framework Analysis](#8-technical-thinking-framework-analysis)
9. [Deep RCA Verification](#9-deep-rca-verification)
10. [Implementation Roadmap](#10-implementation-roadmap)
11. [References](#11-references)

---

## 1. Executive Summary

### 1.1 Why Redesign?

v1 architecture uses **direct function calling** from Azure OpenAI Realtime API to execute tools. This approach has fundamental limitations:

| v1 Limitation | Impact | v2 Solution |
|---------------|--------|-------------|
| Context loss between turns | Users repeat information | Persistent memory layer |
| No conversation history | Each session starts fresh | Chat history store |
| Tightly coupled tools | Hard to extend/modify | MCP-based tool abstraction |
| Single-point processing | Realtime API handles all logic | Leader Agent orchestration |
| No cross-session learning | Can't improve over time | Long-term memory |

### 1.2 v2 Design Principles

```
+══════════════════════════════════════════════════════════════════════════════+
║                         v2 DESIGN PRINCIPLES                                  ║
+══════════════════════════════════════════════════════════════════════════════+
║                                                                              ║
║  1. SEPARATION OF CONCERNS                                                   ║
║     ─────────────────────                                                    ║
║     Voice Interface ≠ Intelligence ≠ Data Access ≠ Memory                   ║
║                                                                              ║
║  2. CONTEXT PERSISTENCE                                                      ║
║     ────────────────────                                                     ║
║     Every conversation saved → Retrievable across sessions                   ║
║                                                                              ║
║  3. LEADER AGENT PATTERN                                                     ║
║     ────────────────────────                                                 ║
║     Orchestrator behind Realtime → Makes decisions, delegates work           ║
║                                                                              ║
║  4. MCP FOR DATA ACCESS                                                      ║
║     ─────────────────────                                                    ║
║     Tools exposed via MCP servers → Standardized, secure, extensible         ║
║                                                                              ║
║  5. MEMORY TIERS                                                             ║
║     ─────────────────                                                        ║
║     Working Memory → Session Memory → Long-term Memory                       ║
║                                                                              ║
+══════════════════════════════════════════════════════════════════════════════+
```

---

## 2. v1 Problems Analysis (Basic RCA)

### 2.1 Problem Statement

Users experience **context loss** during voice conversations. The AI assistant:
- Forgets information from earlier in the conversation
- Cannot recall previous sessions
- Asks repetitive clarifying questions
- Loses track of which student is being discussed

### 2.2 Evidence Collection

| Symptom | Observed Behavior | Frequency |
|---------|-------------------|-----------|
| Repeated questions | "Which student are you asking about?" | Every 3-4 turns |
| Lost context | Forgets preferences mentioned earlier | After 5+ turns |
| Session amnesia | No memory of previous day's conversation | 100% |
| Tool confusion | Calls get_education_data repeatedly | When context unclear |

### 2.3 5 Whys Analysis

```
Why 1: AI forgets information from earlier conversation
        └─► Azure OpenAI Realtime has limited context window (32K tokens)

Why 2: Why is 32K insufficient?
        └─► Full tool definitions + audio transcripts consume most tokens

Why 3: Why are tool definitions included every message?
        └─► v1 uses session.update to send all 11 tools with every turn

Why 4: Why send all tools every time?
        └─► v1 has no orchestration layer to manage context intelligently

Why 5: Why no orchestration layer?
        └─► v1 designed for rapid prototyping, not production context management
            ← ROOT CAUSE: Architecture gap
```

### 2.4 Contributing Factors

| Factor | Description | Impact Level |
|--------|-------------|--------------|
| No chat history storage | Conversations not persisted | HIGH |
| Stateless tool calls | Each function call independent | HIGH |
| No memory summarization | Old context just truncated | MEDIUM |
| Single model does all work | Realtime API overloaded | MEDIUM |
| No user preference storage | Can't learn from interactions | LOW (v1) |

### 2.5 Root Cause Summary

**[HIGH Confidence]** v1 architecture lacks a **persistent memory layer** and **intelligent orchestration**. The Realtime API is used directly for both voice I/O AND business logic, causing:

1. **Token budget exhaustion** - Tools + context compete for 32K window
2. **No context prioritization** - Old messages dropped arbitrarily
3. **No cross-session memory** - Each conversation starts from zero

---

## 3. v2 Architecture Overview

### 3.1 High-Level Stack Architecture

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    DEEP AGENT SYSTEM - v2 STACK ARCHITECTURE                      ║
+══════════════════════════════════════════════════════════════════════════════════+
║                                                                                   ║
║  USER LAYER (Browser)                                                             ║
║  ─────────────────────                                                            ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Browser (Chrome/Edge/Firefox)                                               │ ║
║  │  • voiceRealtime.js (Microphone → PCM16 → WebSocket)                        │ ║
║  │  • Blazor Server SignalR (UI state sync)                                    │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │ WebSocket + SignalR                        ║
║                                     ▼                                             ║
║  VOICE INTERFACE LAYER                                                            ║
║  ─────────────────────                                                            ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Azure OpenAI Realtime API                                                   │ ║
║  │  ┌───────────────────────────────────────────────────────────────────────┐  │ ║
║  │  │  ROLE: Voice I/O ONLY (Simplified)                                    │  │ ║
║  │  │  • Speech-to-Text (user audio → text)                                 │  │ ║
║  │  │  • Text-to-Speech (response text → audio)                             │  │ ║
║  │  │  • VAD (Voice Activity Detection)                                     │  │ ║
║  │  │  • Single Tool: delegate_to_leader(user_message, context_summary)     │  │ ║
║  │  └───────────────────────────────────────────────────────────────────────┘  │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │ Function Call: delegate_to_leader          ║
║                                     ▼                                             ║
║  LEADER AGENT LAYER (NEW)                                                         ║
║  ─────────────────────────                                                        ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Leader Agent (GPT-4o / Claude)                                             │ ║
║  │  ┌───────────────────────────────────────────────────────────────────────┐  │ ║
║  │  │  ROLE: Orchestration & Decision Making                                │  │ ║
║  │  │  • Analyze user intent                                                │  │ ║
║  │  │  • Query memory for relevant context                                  │  │ ║
║  │  │  • Decide which MCP tools to invoke                                   │  │ ║
║  │  │  • Synthesize responses                                               │  │ ║
║  │  │  • Update memory with new information                                 │  │ ║
║  │  └───────────────────────────────────────────────────────────────────────┘  │ ║
║  │                                                                              │ ║
║  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │ ║
║  │  │ Intent Analyzer │  │ Memory Manager  │  │ Response Synthesizer       │  │ ║
║  │  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │ ║
║  └───────────┼────────────────────┼──────────────────────────┼──────────────────┘ ║
║              │                    │                          │                    ║
║              ▼                    ▼                          ▼                    ║
║  MEMORY LAYER (NEW)                                                               ║
║  ──────────────────                                                               ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │                         THREE-TIER MEMORY SYSTEM                             │ ║
║  │                                                                              │ ║
║  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │ ║
║  │  │  WORKING MEMORY │  │ SESSION MEMORY  │  │    LONG-TERM MEMORY        │  │ ║
║  │  │  (In-Context)   │  │  (Redis/Cache)  │  │    (Vector DB + Graph)     │  │ ║
║  │  ├─────────────────┤  ├─────────────────┤  ├─────────────────────────────┤  │ ║
║  │  │ • Current turn  │  │ • Chat history  │  │ • User preferences         │  │ ║
║  │  │ • Active student│  │ • Recent N turns│  │ • Conversation summaries   │  │ ║
║  │  │ • Pending query │  │ • Session state │  │ • Student profiles (cached)│  │ ║
║  │  │ • Tool results  │  │ • Temp context  │  │ • Interaction patterns     │  │ ║
║  │  │                 │  │                 │  │ • Important facts          │  │ ║
║  │  │ TTL: Turn-based │  │ TTL: 24 hours   │  │ TTL: Permanent             │  │ ║
║  │  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │                                             ║
║                                     ▼                                             ║
║  MCP LAYER (NEW - Replaces Function Calling)                                      ║
║  ────────────────────────────────────────────                                     ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │                         MCP SERVER ARCHITECTURE                              │ ║
║  │                                                                              │ ║
║  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │ ║
║  │  │ education-mcp   │  │  memory-mcp     │  │     advice-mcp             │  │ ║
║  │  │    server       │  │    server       │  │       server               │  │ ║
║  │  ├─────────────────┤  ├─────────────────┤  ├─────────────────────────────┤  │ ║
║  │  │ Tools:          │  │ Tools:          │  │ Tools:                      │  │ ║
║  │  │ • get_classes   │  │ • recall        │  │ • generate_advice           │  │ ║
║  │  │ • get_assignments│  │ • store         │  │ • analyze_performance      │  │ ║
║  │  │ • get_grades    │  │ • search        │  │ • suggest_improvements     │  │ ║
║  │  │ • get_teachers  │  │ • summarize     │  │                             │  │ ║
║  │  │ • get_schedule  │  │ • forget        │  │ Resources:                  │  │ ║
║  │  │                 │  │                 │  │ • advice_templates          │  │ ║
║  │  │ Resources:      │  │ Resources:      │  │ • learning_strategies      │  │ ║
║  │  │ • student_roster│  │ • history       │  │                             │  │ ║
║  │  │ • class_catalog │  │ • user_profile  │  │                             │  │ ║
║  │  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │ ║
║  └───────────┼────────────────────┼──────────────────────────┼──────────────────┘ ║
║              │                    │                          │                    ║
║              ▼                    ▼                          ▼                    ║
║  DATA LAYER                                                                       ║
║  ───────────                                                                      ║
║  ┌─────────────────┐  ┌─────────────────────────┐  ┌─────────────────────────────┐║
║  │ Microsoft Graph │  │   Redis + Vector DB     │  │    Azure OpenAI            ║║
║  │   Education API │  │   (Qdrant/Pinecone)     │  │    (GPT-5-mini)            ║║
║  │   /education/*  │  │   + Neo4j (optional)    │  │    Advice Generation       ║║
║  └─────────────────┘  └─────────────────────────┘  └─────────────────────────────┘║
║                                                                                   ║
+══════════════════════════════════════════════════════════════════════════════════+

DATA FLOW SUMMARY:
  ┌────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
  │ Voice  │───▶│ Realtime │───▶│  Leader  │───▶│   MCP    │───▶│   Data   │
  │ Input  │    │ (STT)    │    │  Agent   │    │ Servers  │    │ Sources  │
  └────────┘    └──────────┘    └────┬─────┘    └──────────┘    └──────────┘
                                     │
                                     ▼
                               ┌──────────┐
                               │  Memory  │
                               │  Layer   │
                               └──────────┘

KEY METRICS (v2):
  MCP Servers: 3          Memory Tiers: 3         Leader Agents: 1
  Total Tools: 15+        Context Retention: 24h+ Voice Latency: <1s target
```

### 3.2 Architecture Comparison: v1 vs v2

```
+══════════════════════════════════════════════════════════════════════════════════+
║                        ARCHITECTURE COMPARISON: v1 vs v2                          ║
+══════════════════════════════════════════════════════════════════════════════════+

  v1 ARCHITECTURE (Current)              v2 ARCHITECTURE (Proposed)
  ─────────────────────────              ──────────────────────────────

  ┌─────────────────────────┐            ┌─────────────────────────┐
  │  Azure Realtime API     │            │  Azure Realtime API     │
  │  ═══════════════════    │            │  ═══════════════════    │
  │  • Voice I/O            │            │  • Voice I/O ONLY       │
  │  • ALL Tool Execution   │◀── VS ───▶ │  • Single delegate tool │
  │  • Business Logic       │            │  • Lightweight          │
  │  • Context Management   │            │                         │
  └───────────┬─────────────┘            └───────────┬─────────────┘
              │                                      │
              │ (everything)                         │ (text only)
              ▼                                      ▼
  ┌─────────────────────────┐            ┌─────────────────────────┐
  │  StudentService         │            │  LEADER AGENT           │
  │  (Direct Graph Calls)   │            │  ═══════════════        │
  │                         │            │  • Intent Analysis      │
  │  No Memory              │            │  • Memory Query         │
  │  No Context Sharing     │            │  • Tool Selection       │
  │  No History             │            │  • Response Synthesis   │
  │                         │            │                         │
  └─────────────────────────┘            └───────────┬─────────────┘
                                                     │
                                                     ▼
                                         ┌─────────────────────────┐
                                         │  MEMORY LAYER           │
                                         │  ═════════════          │
                                         │  Working │Session│Long  │
                                         └───────────┬─────────────┘
                                                     │
                                                     ▼
                                         ┌─────────────────────────┐
                                         │  MCP SERVERS            │
                                         │  ═══════════            │
                                         │  education│memory│advice│
                                         └─────────────────────────┘

COMPARISON MATRIX:
┌──────────────────────────┬────────────────────┬────────────────────┐
│ Aspect                   │ v1                 │ v2                 │
├──────────────────────────┼────────────────────┼────────────────────┤
│ Context Window Usage     │ 32K (saturated)    │ 8K (optimized)     │
│ Cross-Session Memory     │ None               │ Yes (Vector DB)    │
│ Tool Management          │ 11 inline tools    │ 15+ via MCP        │
│ Intelligence Location    │ Realtime API       │ Leader Agent       │
│ Extensibility            │ Code changes       │ Add MCP server     │
│ Security                 │ Function-level     │ OAuth 2.1 per MCP  │
│ Context Summarization    │ None (truncation)  │ Active compression │
│ Multi-Turn Coherence     │ Limited (~5 turns) │ Unlimited          │
└──────────────────────────┴────────────────────┴────────────────────┘
```

---

## 4. Leader Agent Design

### 4.1 Leader Agent Responsibilities

```
+══════════════════════════════════════════════════════════════════════════════════+
║                         LEADER AGENT RESPONSIBILITIES                             ║
+══════════════════════════════════════════════════════════════════════════════════+

                              ┌─────────────────────────────┐
                              │       LEADER AGENT          │
                              │   (Orchestrator Pattern)    │
                              └──────────────┬──────────────┘
                                             │
           ┌─────────────────┬───────────────┼───────────────┬─────────────────┐
           │                 │               │               │                 │
           ▼                 ▼               ▼               ▼                 ▼
  ┌─────────────────┐┌─────────────────┐┌─────────────────┐┌─────────────────┐┌─────────────┐
  │ INTENT ANALYSIS ││ MEMORY QUERY    ││ TOOL SELECTION  ││ RESPONSE BUILD  ││ MEMORY WRITE│
  ├─────────────────┤├─────────────────┤├─────────────────┤├─────────────────┤├─────────────┤
  │                 ││                 ││                 ││                 ││             │
  │ • Parse user    ││ • Check working ││ • Map intent    ││ • Combine tool  ││ • Store new │
  │   intent        ││   memory first  ││   to MCP tools  ││   results       ││   facts     │
  │                 ││                 ││                 ││                 ││             │
  │ • Extract       ││ • Search session││ • Prioritize    ││ • Format for    ││ • Update    │
  │   entities      ││   history       ││   tool order    ││   voice output  ││   summaries │
  │                 ││                 ││                 ││                 ││             │
  │ • Detect        ││ • Query long-   ││ • Handle tool   ││ • Add context   ││ • Log       │
  │   ambiguity     ││   term memory   ││   failures      ││   from memory   ││   interaction│
  │                 ││                 ││                 ││                 ││             │
  │ • Clarify if    ││ • Merge context ││ • Parallelize   ││ • Ensure        ││ • Mark      │
  │   needed        ││   sources       ││   where safe    ││   coherence     ││   salience  │
  │                 ││                 ││                 ││                 ││             │
  └─────────────────┘└─────────────────┘└─────────────────┘└─────────────────┘└─────────────┘

DECISION FLOW:
  User Message → Intent Analysis → Memory Query → Tool Selection → Execute → Respond → Update Memory
```

### 4.2 Leader Agent System Prompt Structure

```
+══════════════════════════════════════════════════════════════════════════════════+
║                     LEADER AGENT SYSTEM PROMPT STRUCTURE                          ║
+══════════════════════════════════════════════════════════════════════════════════+

┌─────────────────────────────────────────────────────────────────────────────────┐
│ SYSTEM PROMPT SECTIONS                                                          │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│ 1. ROLE DEFINITION                                                              │
│    "You are an educational assistant helping parents access student data..."   │
│                                                                                 │
│ 2. AVAILABLE MCP TOOLS (Injected dynamically)                                   │
│    - education-mcp: get_classes, get_assignments, get_grades...                │
│    - memory-mcp: recall, store, search, summarize...                           │
│    - advice-mcp: generate_advice, analyze_performance...                       │
│                                                                                 │
│ 3. MEMORY CONTEXT (Injected per request)                                        │
│    ┌─────────────────────────────────────────────────────────────────────────┐ │
│    │ ## Current Context                                                      │ │
│    │ - Parent: {parent_name}                                                 │ │
│    │ - Active Student: {student_name} [ID: {student_id}]                     │ │
│    │ - Students Available: A) Lucy, B) Tom                                   │ │
│    │                                                                          │ │
│    │ ## Recent Conversation Summary                                          │ │
│    │ - User asked about Math grades (2 turns ago)                            │ │
│    │ - Lucy has 3 overdue assignments                                        │ │
│    │                                                                          │ │
│    │ ## Relevant Long-term Memory                                            │ │
│    │ - Parent prefers Vietnamese language                                    │ │
│    │ - Lucy struggles with Math, excels in Science                           │ │
│    │ - Last session discussed homework strategies                            │ │
│    └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                                 │
│ 4. BEHAVIORAL GUIDELINES                                                        │
│    - Always check memory before calling education tools                        │
│    - Confirm student context before fetching data                              │
│    - Summarize long responses for voice output                                 │
│    - Store important new information in memory                                 │
│                                                                                 │
│ 5. OUTPUT FORMAT                                                                │
│    - Keep voice responses under 3 sentences                                    │
│    - Offer to provide details if user wants more                               │
│    - Use natural conversational tone                                           │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 4.3 Leader Agent Processing Pipeline

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    LEADER AGENT PROCESSING PIPELINE                               ║
+══════════════════════════════════════════════════════════════════════════════════+

  ┌──────────────────┐
  │ User Message     │
  │ "What are Lucy's │
  │  Math grades?"   │
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │  PHASE 1: INTENT ANALYSIS                                                     │
  │  ─────────────────────────                                                    │
  │  Input:  "What are Lucy's Math grades?"                                       │
  │  Output: {                                                                    │
  │    intent: "query_grades",                                                    │
  │    entities: { student: "Lucy", subject: "Math" },                            │
  │    requires_data: true,                                                       │
  │    requires_memory: true                                                      │
  │  }                                                                            │
  └────────┬─────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │  PHASE 2: MEMORY QUERY                                                        │
  │  ─────────────────────                                                        │
  │  1. Check working memory: Is "Lucy" the active student?                       │
  │  2. Check session memory: Have we discussed Math grades this session?         │
  │  3. Check long-term memory: Any notes about Lucy's Math performance?          │
  │                                                                               │
  │  Memory Result: {                                                             │
  │    active_student: "Lucy",                                                    │
  │    cached_grades: null,  // Need fresh data                                   │
  │    context: "Lucy has struggled with Math homework"                           │
  │  }                                                                            │
  └────────┬─────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │  PHASE 3: TOOL SELECTION & EXECUTION                                          │
  │  ───────────────────────────────────                                          │
  │  Selected Tools (via MCP):                                                    │
  │    1. education-mcp.get_classes(student_id="lucy-123")                        │
  │    2. education-mcp.get_grades(student_id="lucy-123", subject="Math")         │
  │                                                                               │
  │  Execution: Parallel (no dependency between calls)                            │
  │                                                                               │
  │  Results: {                                                                   │
  │    classes: ["Math 101", "Science 201", ...],                                 │
  │    grades: [                                                                  │
  │      { assignment: "Algebra Quiz", score: 85, max: 100 },                     │
  │      { assignment: "Geometry Test", score: 72, max: 100 },                    │
  │    ]                                                                          │
  │  }                                                                            │
  └────────┬─────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │  PHASE 4: RESPONSE SYNTHESIS                                                  │
  │  ─────────────────────────────                                                │
  │  Combine:                                                                     │
  │    - Tool results (grades data)                                               │
  │    - Memory context (Lucy struggles with Math)                                │
  │    - User preferences (concise voice responses)                               │
  │                                                                               │
  │  Generated Response:                                                          │
  │    "Lucy has two graded Math assignments. She scored 85% on the Algebra      │
  │     Quiz and 72% on the Geometry Test. Would you like advice on improving    │
  │     her Geometry scores?"                                                     │
  └────────┬─────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │  PHASE 5: MEMORY UPDATE                                                       │
  │  ──────────────────────                                                       │
  │  Store in session memory:                                                     │
  │    - Discussed Lucy's Math grades                                             │
  │    - Scores: Algebra 85%, Geometry 72%                                        │
  │                                                                               │
  │  Update long-term memory (if significant):                                    │
  │    - Lucy's Geometry performance is below average                             │
  └────────┬─────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────┐
  │ Voice Response   │
  │ (via Realtime)   │
  └──────────────────┘
```

---

## 5. Context Sharing Architecture

### 5.1 Three-Tier Memory System

```
+══════════════════════════════════════════════════════════════════════════════════+
║                       THREE-TIER MEMORY ARCHITECTURE                              ║
+══════════════════════════════════════════════════════════════════════════════════+

                    ┌─────────────────────────────────────────────────────────────┐
                    │                    MEMORY MANAGER                            │
                    │              (Coordinates all memory operations)             │
                    └─────────────────────────────┬───────────────────────────────┘
                                                  │
           ┌──────────────────────────────────────┼──────────────────────────────┐
           │                                      │                              │
           ▼                                      ▼                              ▼
┌─────────────────────────┐       ┌─────────────────────────┐       ┌─────────────────────────┐
│     TIER 1: WORKING     │       │    TIER 2: SESSION      │       │    TIER 3: LONG-TERM    │
│        MEMORY           │       │       MEMORY            │       │        MEMORY           │
├─────────────────────────┤       ├─────────────────────────┤       ├─────────────────────────┤
│                         │       │                         │       │                         │
│ STORAGE: In-Context     │       │ STORAGE: Redis Cache    │       │ STORAGE: Vector DB      │
│          (LLM prompt)   │       │                         │       │          + Graph DB     │
│                         │       │                         │       │                         │
│ TTL: Current turn       │       │ TTL: 24 hours           │       │ TTL: Permanent          │
│                         │       │                         │       │                         │
│ SIZE: ~2K tokens        │       │ SIZE: ~50 messages      │       │ SIZE: Unlimited         │
│                         │       │                         │       │                         │
│ CONTENTS:               │       │ CONTENTS:               │       │ CONTENTS:               │
│ • Current user message  │       │ • Full chat history     │       │ • User preferences      │
│ • Active student ID     │       │ • Recent summaries      │       │ • Student profiles      │
│ • Pending tool results  │       │ • Session state vars    │       │ • Interaction patterns  │
│ • Immediate context     │       │ • Temporary flags       │       │ • Important facts       │
│                         │       │                         │       │ • Conversation summaries│
│ ACCESS: Synchronous     │       │ ACCESS: <10ms           │       │ ACCESS: <100ms          │
│         (always in ctx) │       │                         │       │         (semantic search)│
│                         │       │                         │       │                         │
└─────────────────────────┘       └─────────────────────────┘       └─────────────────────────┘
           │                                 │                                 │
           │                                 │                                 │
           ▼                                 ▼                                 ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                              MEMORY OPERATIONS                                   │
  ├─────────────────────────────────────────────────────────────────────────────────┤
  │                                                                                 │
  │  READ PRIORITY:  Working Memory → Session Memory → Long-term Memory             │
  │                  (Fast to slow, specific to general)                            │
  │                                                                                 │
  │  WRITE TRIGGERS:                                                                │
  │    • End of turn → Update session memory                                        │
  │    • Important fact detected → Write to long-term                               │
  │    • Session end → Summarize and archive                                        │
  │                                                                                 │
  │  SUMMARIZATION:                                                                 │
  │    • Every N turns → Compress oldest messages into summary                      │
  │    • Session end → Create session summary for long-term                         │
  │    • Weekly → Consolidate long-term memories                                    │
  │                                                                                 │
  └─────────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Context Sharing Data Flow

```
+══════════════════════════════════════════════════════════════════════════════════+
║                         CONTEXT SHARING DATA FLOW                                 ║
+══════════════════════════════════════════════════════════════════════════════════+

  NEW SESSION START
  ─────────────────

  ┌─────────────┐    ┌─────────────────────────────────────────────────────────┐
  │  User Login │───▶│  MEMORY INITIALIZATION                                  │
  └─────────────┘    │                                                         │
                     │  1. Load user profile from long-term memory             │
                     │  2. Retrieve last session summary                       │
                     │  3. Search for relevant context (students, preferences) │
                     │  4. Initialize session memory (empty chat history)      │
                     │  5. Populate working memory with context summary        │
                     └─────────────────────────────────────────────────────────┘

  DURING CONVERSATION
  ───────────────────

  Turn 1: "Show me Lucy's grades"
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │                                                                              │
  │  Working Memory (injected into Leader Agent prompt):                         │
  │  ┌────────────────────────────────────────────────────────────────────────┐  │
  │  │ ## Context                                                             │  │
  │  │ Parent: John Smith                                                     │  │
  │  │ Students: A) Lucy Smith, B) Tom Smith                                  │  │
  │  │ Active: Lucy (default, first child)                                    │  │
  │  │                                                                        │  │
  │  │ ## From Last Session (2 days ago)                                      │  │
  │  │ - Discussed Lucy's Science project (completed)                         │  │
  │  │ - Tom had Math homework due Monday                                     │  │
  │  │                                                                        │  │
  │  │ ## User Preferences                                                    │  │
  │  │ - Prefers brief responses                                              │  │
  │  │ - Interested in homework completion rates                              │  │
  │  └────────────────────────────────────────────────────────────────────────┘  │
  │                                                                              │
  └──────────────────────────────────────────────────────────────────────────────┘

  Turn 2: "How about Tom?"
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │                                                                              │
  │  Working Memory (updated with Turn 1 context):                               │
  │  ┌────────────────────────────────────────────────────────────────────────┐  │
  │  │ ## Context                                                             │  │
  │  │ Parent: John Smith                                                     │  │
  │  │ Active: Tom Smith (just switched from Lucy)                            │  │
  │  │                                                                        │  │
  │  │ ## Recent Conversation                                                 │  │
  │  │ - Turn 1: Showed Lucy's grades (Math: 85%, Science: 92%)               │  │
  │  │ - Turn 2: User asking about Tom (implied: grades)                      │  │
  │  │                                                                        │  │
  │  │ ## Intent Inference                                                    │  │
  │  │ - "How about Tom?" → Show Tom's grades (same query type as Turn 1)     │  │
  │  └────────────────────────────────────────────────────────────────────────┘  │
  │                                                                              │
  │  Note: Leader Agent infers "grades" from conversation history without        │
  │        user needing to repeat the full question.                             │
  │                                                                              │
  └──────────────────────────────────────────────────────────────────────────────┘

  SESSION END
  ───────────

  ┌─────────────┐    ┌─────────────────────────────────────────────────────────┐
  │  User Exits │───▶│  MEMORY PERSISTENCE                                     │
  └─────────────┘    │                                                         │
                     │  1. Generate session summary:                           │
                     │     "Parent checked grades for Lucy (Math 85%, Science  │
                     │      92%) and Tom (Math 78%, Science 88%). Discussed    │
                     │      Tom's Math improvement opportunities."             │
                     │                                                         │
                     │  2. Store summary in long-term memory (Vector DB)       │
                     │                                                         │
                     │  3. Update user preferences if new patterns detected    │
                     │                                                         │
                     │  4. Clear session memory (Redis TTL handles this)       │
                     └─────────────────────────────────────────────────────────┘
```

### 5.3 Context Window Optimization Strategy

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    CONTEXT WINDOW OPTIMIZATION STRATEGY                           ║
+══════════════════════════════════════════════════════════════════════════════════+

  v1 PROBLEM: Context window saturated with tool definitions + raw history

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  v1 TOKEN BUDGET (32K total)                                                │
  │                                                                              │
  │  ████████████████████████████████████████████████████████████████████████   │
  │  │← Tool Definitions (11 tools) →│← Raw Chat History →│← Response →│        │
  │  │       ~8K tokens              │    ~20K tokens     │  ~4K      │        │
  │                                                                              │
  │  PROBLEM: After 5-7 turns, history truncated from beginning                  │
  │           → User context lost → AI asks clarifying questions                 │
  └─────────────────────────────────────────────────────────────────────────────┘

  v2 SOLUTION: Tiered memory with active summarization

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  v2 TOKEN BUDGET (8K context for Leader Agent)                              │
  │                                                                              │
  │  ████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   │
  │  │← System →│←Memory Summary→│← Recent 3 turns →│← Response →│              │
  │  │  ~1K     │    ~2K         │      ~3K         │    ~2K     │              │
  │                                                                              │
  │  + MCP tools loaded on-demand (not in context)                               │
  │  + Long-term memory retrieved via semantic search                            │
  │  + Session history available in Redis (not in LLM context)                   │
  └─────────────────────────────────────────────────────────────────────────────┘

  SUMMARIZATION PIPELINE:

  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
  │   Turn 1    │    │   Turn 2    │    │   Turn 3    │    │   Turn 4    │
  │  (Full msg) │    │  (Full msg) │    │  (Full msg) │    │  (Full msg) │
  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └─────────────┘
         │                  │                  │                    │
         │                  │                  │                    │
         └──────────────────┴──────────────────┘                    │
                            │                                       │
                            ▼                                       │
                   ┌─────────────────┐                              │
                   │ SUMMARIZE       │                              │
                   │ Turns 1-3:      │                              │
                   │ "Discussed Lucy │                              │
                   │  Math grades,   │◀─────────── In context ──────┘
                   │  85% on quiz"   │              (Turn 4 = recent)
                   └─────────────────┘
                            │
                            │ Injected as "Recent Context Summary"
                            ▼
                   ┌─────────────────┐
                   │ Leader Agent    │
                   │ Context Window  │
                   └─────────────────┘
```

### 5.4 Best Practices Applied

| Best Practice | Source | Implementation |
|---------------|--------|----------------|
| **Tiered Memory** | [Letta/MemGPT](https://www.letta.com/blog/agent-memory) | Working → Session → Long-term |
| **Active Summarization** | [OpenAI Cookbook](https://cookbook.openai.com/examples/context_summarization_with_realtime_api) | Compress old turns into summaries |
| **Retention Ratio** | [OpenAI Realtime Guide](https://platform.openai.com/docs/guides/realtime-conversations) | 0.8 ratio for cache optimization |
| **Semantic Search** | [Pinecone Langchain](https://www.pinecone.io/learn/series/langchain/langchain-conversational-memory/) | Vector DB for long-term memory |
| **Session Persistence** | [AWS Mem0 Guide](https://aws.amazon.com/blogs/database/build-persistent-memory-for-agentic-ai-applications-with-mem0-open-source-amazon-elasticache-for-valkey-and-amazon-neptune-analytics/) | Redis + Vector DB for durability |
| **Salience Detection** | [LangChain LangMem](https://langchain-ai.github.io/langmem/concepts/conceptual_guide/) | Store only important facts |

---

## 6. MCP Integration Design

### 6.1 Why MCP over Function Calling?

```
+══════════════════════════════════════════════════════════════════════════════════+
║                      MCP vs FUNCTION CALLING COMPARISON                           ║
+══════════════════════════════════════════════════════════════════════════════════+

┌───────────────────────┬────────────────────────────┬────────────────────────────┐
│ Aspect                │ Function Calling (v1)      │ MCP (v2)                   │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Tool Definition       │ Inline in every request    │ Server-side, loaded once   │
│ Location              │ (consumes tokens)          │ (no token cost)            │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Authentication        │ Per-function, custom       │ OAuth 2.1 standard         │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Extensibility         │ Code changes required      │ Add new MCP server         │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Security Scope        │ All-or-nothing             │ Per-server, per-tool       │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Resources (Read-only) │ Not supported              │ First-class support        │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Prompts (Templates)   │ Not supported              │ Server-provided prompts    │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Discovery             │ Manual documentation       │ Automatic via MCP protocol │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Versioning            │ Breaking changes           │ Server versioning          │
├───────────────────────┼────────────────────────────┼────────────────────────────┤
│ Multi-tenant          │ Complex                    │ Built-in per MCP spec      │
└───────────────────────┴────────────────────────────┴────────────────────────────┘
```

### 6.2 MCP Server Architecture

```
+══════════════════════════════════════════════════════════════════════════════════+
║                         MCP SERVER ARCHITECTURE                                   ║
+══════════════════════════════════════════════════════════════════════════════════+

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                           LEADER AGENT (MCP CLIENT)                          │
  └─────────────────────────────────────┬───────────────────────────────────────┘
                                        │ MCP Protocol (JSON-RPC over HTTP/WebSocket)
                                        │
        ┌───────────────────────────────┼───────────────────────────────┐
        │                               │                               │
        ▼                               ▼                               ▼
┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
│ education-mcp-server  │   │   memory-mcp-server   │   │   advice-mcp-server   │
├───────────────────────┤   ├───────────────────────┤   ├───────────────────────┤
│                       │   │                       │   │                       │
│ TOOLS:                │   │ TOOLS:                │   │ TOOLS:                │
│ ───────               │   │ ───────               │   │ ───────               │
│ • get_student_profile │   │ • recall              │   │ • generate_advice     │
│   - student_id        │   │   - query (semantic)  │   │   - student_data      │
│   - fields[]          │   │   - time_range        │   │   - advice_for        │
│                       │   │   - limit             │   │   - specific_question │
│ • get_classes         │   │                       │   │                       │
│   - student_id        │   │ • store               │   │ • analyze_performance │
│   - include_teachers  │   │   - content           │   │   - student_id        │
│                       │   │   - salience          │   │   - subject           │
│ • get_assignments     │   │   - category          │   │   - time_range        │
│   - class_id          │   │                       │   │                       │
│   - status_filter     │   │ • search              │   │ • suggest_resources   │
│   - due_filter        │   │   - query             │   │   - topic             │
│                       │   │   - filters{}         │   │   - student_level     │
│ • get_grades          │   │                       │   │                       │
│   - assignment_id     │   │ • summarize           │   │                       │
│   - student_id        │   │   - message_ids[]     │   │                       │
│                       │   │   - max_length        │   │                       │
│ • get_teachers        │   │                       │   │                       │
│   - class_id          │   │ • forget              │   │                       │
│                       │   │   - memory_id         │   │                       │
│ • get_schedule        │   │   - reason            │   │                       │
│   - student_id        │   │                       │   │                       │
│   - date_range        │   │                       │   │                       │
│                       │   │                       │   │                       │
│ RESOURCES:            │   │ RESOURCES:            │   │ RESOURCES:            │
│ ──────────            │   │ ──────────            │   │ ──────────            │
│ • student_roster      │   │ • chat_history        │   │ • advice_templates    │
│ • class_catalog       │   │ • user_preferences    │   │ • learning_strategies │
│ • assignment_list     │   │ • session_summaries   │   │ • subject_guides      │
│                       │   │                       │   │                       │
│ PROMPTS:              │   │ PROMPTS:              │   │ PROMPTS:              │
│ ────────              │   │ ────────              │   │ ────────              │
│ • format_grades       │   │ • build_context       │   │ • advice_for_parent   │
│ • summarize_schedule  │   │ • summarize_session   │   │ • advice_for_student  │
│                       │   │                       │   │                       │
├───────────────────────┤   ├───────────────────────┤   ├───────────────────────┤
│ AUTH: OAuth 2.1       │   │ AUTH: OAuth 2.1       │   │ AUTH: OAuth 2.1       │
│ SCOPES:               │   │ SCOPES:               │   │ SCOPES:               │
│ • education.read      │   │ • memory.read         │   │ • advice.generate     │
│ • education.roster    │   │ • memory.write        │   │                       │
├───────────────────────┤   ├───────────────────────┤   ├───────────────────────┤
│ BACKEND:              │   │ BACKEND:              │   │ BACKEND:              │
│ Microsoft Graph API   │   │ Redis + Qdrant        │   │ Azure OpenAI GPT-5    │
└───────────────────────┘   └───────────────────────┘   └───────────────────────┘
```

### 6.3 MCP Tool Migration from v1

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    v1 → v2 TOOL MIGRATION MAPPING                                 ║
+══════════════════════════════════════════════════════════════════════════════════+

┌────────────────────────────┬─────────────────────────────────────────────────────┐
│ v1 Function (Inline)       │ v2 MCP Tool (Server-based)                          │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│                            │                                                     │
│ get_education_data         │ SPLIT INTO:                                         │
│ (monolithic)               │ • education-mcp.get_classes                         │
│                            │ • education-mcp.get_assignments                     │
│                            │ • education-mcp.get_grades                          │
│                            │                                                     │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│                            │                                                     │
│ switch_student             │ • (Handled by Leader Agent internally)              │
│                            │ • Updates working memory                            │
│                            │ • No external tool needed                           │
│                            │                                                     │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│                            │                                                     │
│ get_advice                 │ • advice-mcp.generate_advice                        │
│                            │ • advice-mcp.analyze_performance                    │
│                            │                                                     │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│                            │                                                     │
│ (Not in v1)                │ NEW IN v2:                                          │
│                            │ • memory-mcp.recall                                 │
│                            │ • memory-mcp.store                                  │
│                            │ • memory-mcp.search                                 │
│                            │ • memory-mcp.summarize                              │
│                            │ • education-mcp.get_schedule                        │
│                            │ • advice-mcp.suggest_resources                      │
│                            │                                                     │
└────────────────────────────┴─────────────────────────────────────────────────────┘
```

### 6.4 MCP Security Model

```
+══════════════════════════════════════════════════════════════════════════════════+
║                           MCP SECURITY MODEL                                      ║
+══════════════════════════════════════════════════════════════════════════════════+

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                              OAUTH 2.1 FLOW                                  │
  └────────────────────────────────────┬────────────────────────────────────────┘
                                       │
  ┌────────────────────────────────────┼────────────────────────────────────────┐
  │                                    │                                        │
  │  1. Leader Agent requests MCP tool │                                        │
  │                                    ▼                                        │
  │         ┌─────────────────────────────────────────────────────────┐         │
  │         │  MCP Server responds: 401 Unauthorized                  │         │
  │         │  WWW-Authenticate: Bearer realm="education-mcp"         │         │
  │         └─────────────────────────────────────────────────────────┘         │
  │                                    │                                        │
  │  2. Leader Agent initiates OAuth   │                                        │
  │                                    ▼                                        │
  │         ┌─────────────────────────────────────────────────────────┐         │
  │         │  Authorization Server (Entra ID)                        │         │
  │         │  • Validate client credentials                          │         │
  │         │  • Check requested scopes                               │         │
  │         │  • Issue access token                                   │         │
  │         └─────────────────────────────────────────────────────────┘         │
  │                                    │                                        │
  │  3. Leader Agent retries with token│                                        │
  │                                    ▼                                        │
  │         ┌─────────────────────────────────────────────────────────┐         │
  │         │  MCP Server validates token:                            │         │
  │         │  • Check token signature                                │         │
  │         │  • Verify scopes match tool requirements                │         │
  │         │  • Apply rate limits                                    │         │
  │         │  • Execute tool with user context                       │         │
  │         └─────────────────────────────────────────────────────────┘         │
  │                                                                              │
  └──────────────────────────────────────────────────────────────────────────────┘

  SCOPE MATRIX:
  ┌───────────────────────┬─────────────────────────────────────────────────────┐
  │ Scope                 │ Grants Access To                                    │
  ├───────────────────────┼─────────────────────────────────────────────────────┤
  │ education.read        │ get_classes, get_assignments, get_grades (read)    │
  │ education.roster      │ get_student_profile, get_teachers (read)           │
  │ memory.read           │ recall, search (read-only)                         │
  │ memory.write          │ store, summarize, forget (write)                   │
  │ advice.generate       │ generate_advice, analyze_performance               │
  └───────────────────────┴─────────────────────────────────────────────────────┘

  SECURITY BEST PRACTICES (per MCP Spec):
  ─────────────────────────────────────────
  • Least privilege: Default read-only, explicit write scopes
  • Per-tool authorization: Each tool checks required scopes
  • Input validation: All parameters validated before execution
  • Audit logging: All tool invocations logged
  • Rate limiting: Per-user, per-tool limits
```

---

## 7. Workflow Diagrams

### 7.1 Voice Conversation Flow (v2)

```
+══════════════════════════════════════════════════════════════════════════════════+
║              WORKFLOW: VOICE CONVERSATION FLOW (v2 - WITH LEADER AGENT)           ║
+══════════════════════════════════════════════════════════════════════════════════+

  Parent        Browser JS      Realtime API     Leader Agent    MCP Servers    Memory
    │               │                │                │               │            │
    │ 1. Speak      │                │                │               │            │
    │   "Show me    │                │                │               │            │
    │    Lucy's     │                │                │               │            │
    │    grades"    │                │                │               │            │
    │ ──────────────>                │                │               │            │
    │               │ 2. Audio       │                │               │            │
    │               │    Capture     │                │               │            │
    │               │ ───────────────>                │               │            │
    │               │                │ 3. STT         │               │            │
    │               │                │   "Show me     │               │            │
    │               │                │    Lucy's      │               │            │
    │               │                │    grades"     │               │            │
    │               │                │                │               │            │
    │               │                │ 4. Function    │               │            │
    │               │                │    Call:       │               │            │
    │               │                │    delegate_to │               │            │
    │               │                │    _leader     │               │            │
    │               │                │ ───────────────>               │            │
    │               │                │                │               │            │
    │               │                │                │ 5. Query      │            │
    │               │                │                │    Memory     │            │
    │               │                │                │ ──────────────────────────>│
    │               │                │                │               │            │
    │               │                │                │ 6. Context:   │            │
    │               │                │                │    Lucy is    │            │
    │               │                │                │    active     │            │
    │               │                │                │ <──────────────────────────│
    │               │                │                │               │            │
    │               │                │                │ 7. MCP Call:  │            │
    │               │                │                │    education- │            │
    │               │                │                │    mcp.get_   │            │
    │               │                │                │    grades     │            │
    │               │                │                │ ──────────────>            │
    │               │                │                │               │            │
    │               │                │                │ 8. Grades     │            │
    │               │                │                │    Data       │            │
    │               │                │                │ <──────────────            │
    │               │                │                │               │            │
    │               │                │                │ 9. Update     │            │
    │               │                │                │    Memory     │            │
    │               │                │                │ ──────────────────────────>│
    │               │                │                │               │            │
    │               │                │ 10. Response   │               │            │
    │               │                │     Text       │               │            │
    │               │                │ <───────────────               │            │
    │               │                │                │               │            │
    │               │                │ 11. TTS +      │               │            │
    │               │                │     Audio      │               │            │
    │               │ <───────────────                │               │            │
    │               │                │                │               │            │
    │ 12. Hear      │ 13. Play       │                │               │            │
    │    Response   │     Audio      │                │               │            │
    │ <──────────────                │                │               │            │
    │               │                │                │               │            │

LATENCY BREAKDOWN:
  Step 2-3:   ~200ms  (Audio capture + STT)
  Step 4:     ~10ms   (Function call)
  Step 5-6:   ~50ms   (Memory query)
  Step 7-8:   ~300ms  (MCP + Graph API)
  Step 9:     ~20ms   (Memory update)
  Step 10-11: ~500ms  (Response + TTS)
  ─────────────────────────────────────
  Total:      ~1.1s   (Target: <1.5s)
```

### 7.2 Context Recovery Flow (NEW in v2)

```
+══════════════════════════════════════════════════════════════════════════════════+
║              WORKFLOW: CONTEXT RECOVERY (Returning User)                          ║
+══════════════════════════════════════════════════════════════════════════════════+

  Parent        Browser         Auth          Memory Layer     Leader Agent
    │               │              │                │                │
    │ 1. Navigate   │              │                │                │
    │    to app     │              │                │                │
    │ ──────────────>              │                │                │
    │               │ 2. Auth      │                │                │
    │               │    Check     │                │                │
    │               │ ─────────────>                │                │
    │               │              │ 3. Valid       │                │
    │               │              │    Session     │                │
    │               │ <─────────────                │                │
    │               │              │                │                │
    │               │ 4. Load      │                │                │
    │               │    User      │                │                │
    │               │    Context   │                │                │
    │               │ ─────────────────────────────>│                │
    │               │              │                │                │
    │               │              │                │ 5. Query       │
    │               │              │                │    Long-term   │
    │               │              │                │    Memory:     │
    │               │              │                │    - User prefs│
    │               │              │                │    - Last      │
    │               │              │                │      session   │
    │               │              │                │      summary   │
    │               │              │                │    - Student   │
    │               │              │                │      profiles  │
    │               │              │                │                │
    │               │ 6. Context   │                │                │
    │               │    Loaded    │                │                │
    │               │ <─────────────────────────────│                │
    │               │              │                │                │
    │ 7. UI shows   │              │                │                │
    │    "Welcome   │              │                │                │
    │     back,     │              │                │                │
    │     John!"    │              │                │                │
    │    + Last     │              │                │                │
    │    session:   │              │                │                │
    │    "Lucy's    │              │                │                │
    │     Math"     │              │                │                │
    │ <──────────────              │                │                │
    │               │              │                │                │
    │ 8. User says  │              │                │                │
    │   "Any        │              │                │                │
    │    updates?"  │              │                │                │
    │ ──────────────>              │                │                │
    │               │              │                │                │
    │               │              │                │ 9. Leader      │
    │               │              │                │    Agent       │
    │               │              │                │    understands:│
    │               │              │                │    "updates" = │
    │               │              │                │    Lucy's Math │
    │               │              │                │    (from       │
    │               │              │                │    context)    │
    │               │              │                │                │
    │ 10. Response: │              │                │                │
    │    "Since we  │              │                │                │
    │     last      │              │                │                │
    │     talked,   │              │                │                │
    │     Lucy got  │              │                │                │
    │     92% on    │              │                │                │
    │     her new   │              │                │                │
    │     Math      │              │                │                │
    │     quiz!"    │              │                │                │
    │ <──────────────              │                │                │
    │               │              │                │                │

KEY BENEFIT:
  User doesn't need to repeat context from previous sessions.
  Leader Agent retrieves relevant history and understands implied references.
```

### 7.3 Multi-Student Context Switching (v2)

```
+══════════════════════════════════════════════════════════════════════════════════+
║              WORKFLOW: MULTI-STUDENT CONTEXT SWITCHING (v2)                       ║
+══════════════════════════════════════════════════════════════════════════════════+

  Parent          Leader Agent         Working Memory       Session Memory
    │                   │                    │                    │
    │ "What about       │                    │                    │
    │  Tom's grades?"   │                    │                    │
    │ ─────────────────>│                    │                    │
    │                   │                    │                    │
    │                   │ 1. Intent:         │                    │
    │                   │    Query grades    │                    │
    │                   │    for "Tom"       │                    │
    │                   │                    │                    │
    │                   │ 2. Check: Is Tom   │                    │
    │                   │    active student? │                    │
    │                   │ ──────────────────>│                    │
    │                   │                    │ 3. Current:        │
    │                   │                    │    Lucy (not Tom)  │
    │                   │ <──────────────────│                    │
    │                   │                    │                    │
    │                   │ 4. Switch student  │                    │
    │                   │    context to Tom  │                    │
    │                   │ ──────────────────>│                    │
    │                   │                    │ 5. Update:         │
    │                   │                    │    active = Tom    │
    │                   │                    │                    │
    │                   │ 6. Log switch      │                    │
    │                   │    in session      │                    │
    │                   │ ─────────────────────────────────────-->│
    │                   │                    │                    │ 7. Store:
    │                   │                    │                    │    "Switched
    │                   │                    │                    │     from Lucy
    │                   │                    │                    │     to Tom"
    │                   │                    │                    │
    │                   │ 8. Now fetch       │                    │
    │                   │    Tom's grades    │                    │
    │                   │    via MCP         │                    │
    │                   │                    │                    │
    │ 9. "Tom has       │                    │                    │
    │     Math: 78%,    │                    │                    │
    │     Science: 88%" │                    │                    │
    │ <─────────────────│                    │                    │
    │                   │                    │                    │
    │ "Compare them"    │                    │                    │
    │ ─────────────────>│                    │                    │
    │                   │                    │                    │
    │                   │ 10. Intent:        │                    │
    │                   │     Compare = Tom  │                    │
    │                   │     vs Lucy grades │                    │
    │                   │                    │                    │
    │                   │ 11. Recall Lucy's  │                    │
    │                   │     grades from    │                    │
    │                   │     session memory │                    │
    │                   │ ─────────────────────────────────────-->│
    │                   │                    │                    │ 12. Return:
    │                   │                    │                    │     Lucy Math 85%
    │                   │                    │                    │     Lucy Sci 92%
    │                   │ <─────────────────────────────────────── │
    │                   │                    │                    │
    │ 13. "Lucy: 85%    │                    │                    │
    │      Math, 92%    │                    │                    │
    │      Science.     │                    │                    │
    │      Tom: 78%     │                    │                    │
    │      Math, 88%    │                    │                    │
    │      Science.     │                    │                    │
    │      Lucy leads   │                    │                    │
    │      in both."    │                    │                    │
    │ <─────────────────│                    │                    │

KEY BENEFIT (vs v1):
  - v1: User must say "Show me Lucy's grades" again to compare
  - v2: Leader Agent recalls Lucy's grades from session memory automatically
```

---

## 8. Technical Thinking Framework Analysis

### 8.1 First Principles Breakdown

```
+══════════════════════════════════════════════════════════════════════════════════+
║              FIRST PRINCIPLES: WHY THIS ARCHITECTURE?                             ║
+══════════════════════════════════════════════════════════════════════════════════+

  FUNDAMENTAL QUESTION: What does a conversational AI assistant NEED?
  ─────────────────────────────────────────────────────────────────────

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  NEED 1: Remember what was said                                             │
  │  ───────────────────────────────                                            │
  │                                                                             │
  │  Basic Truth: Conversations build on previous statements.                   │
  │                                                                             │
  │  Assumption Challenged: "LLM context window is enough"                      │
  │    → FALSE: 32K tokens fills after 5-7 detailed turns                       │
  │    → FALSE: Truncation loses critical early context                         │
  │                                                                             │
  │  First Principle Solution: External memory system                           │
  │    → Session memory for current conversation                                │
  │    → Long-term memory for cross-session recall                              │
  │    → Summarization to compress without losing meaning                       │
  └─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  NEED 2: Access data to answer questions                                    │
  │  ───────────────────────────────────────                                    │
  │                                                                             │
  │  Basic Truth: Education data lives in Microsoft Graph, not in LLM.          │
  │                                                                             │
  │  Assumption Challenged: "Function calling in Realtime API is sufficient"    │
  │    → FALSE: Tool definitions consume ~8K tokens each call                   │
  │    → FALSE: All tools sent even when only 1 needed                          │
  │    → FALSE: No standardized auth/security model                             │
  │                                                                             │
  │  First Principle Solution: MCP server architecture                          │
  │    → Tools defined once at server, not in every request                     │
  │    → OAuth 2.1 for standard security                                        │
  │    → Load only needed tools via MCP discovery                               │
  └─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  NEED 3: Make smart decisions about what to do                              │
  │  ─────────────────────────────────────────────                              │
  │                                                                             │
  │  Basic Truth: Voice input is unstructured; requires interpretation.         │
  │                                                                             │
  │  Assumption Challenged: "Realtime API can handle all logic"                 │
  │    → FALSE: Realtime optimized for voice I/O, not complex reasoning         │
  │    → FALSE: Limited orchestration capabilities                              │
  │    → FALSE: No memory management built-in                                   │
  │                                                                             │
  │  First Principle Solution: Leader Agent pattern                             │
  │    → Dedicated LLM (GPT-4o/Claude) for orchestration                        │
  │    → Realtime API becomes thin voice interface                              │
  │    → Leader makes memory + tool decisions                                   │
  └─────────────────────────────────────────────────────────────────────────────┘
```

### 8.2 MECE Analysis of Architecture Components

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    MECE ANALYSIS: ARCHITECTURE COMPONENTS                         ║
+══════════════════════════════════════════════════════════════════════════════════+

  QUESTION: What components are needed for a conversational education assistant?
  ─────────────────────────────────────────────────────────────────────────────────

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                                                                             │
  │  MUTUALLY EXCLUSIVE categories (no overlap):                                │
  │                                                                             │
  │  1. INPUT/OUTPUT          2. INTELLIGENCE         3. DATA ACCESS            │
  │     ─────────────            ────────────            ───────────            │
  │     • Voice capture          • Intent analysis       • Education API        │
  │     • Speech-to-text         • Decision making       • Memory storage       │
  │     • Text-to-speech         • Response synthesis    • Memory retrieval     │
  │     • Audio playback         • Context management                           │
  │                                                                             │
  │  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐          │
  │  │                 │    │                 │    │                 │          │
  │  │  Voice Layer    │───▶│  Leader Agent   │───▶│   MCP Layer     │          │
  │  │  (Realtime API) │    │  (GPT-4o)       │    │   (Servers)     │          │
  │  │                 │    │                 │    │                 │          │
  │  └─────────────────┘    └────────┬────────┘    └─────────────────┘          │
  │                                  │                                          │
  │                                  ▼                                          │
  │                         ┌─────────────────┐                                 │
  │                         │  Memory Layer   │                                 │
  │                         │  (Persistence)  │                                 │
  │                         └─────────────────┘                                 │
  │                                                                             │
  │  COLLECTIVELY EXHAUSTIVE (covers all needs):                                │
  │                                                                             │
  │  ✓ User can speak and hear responses (Voice Layer)                         │
  │  ✓ System understands and reasons (Leader Agent)                           │
  │  ✓ System accesses required data (MCP Layer)                               │
  │  ✓ System remembers context (Memory Layer)                                 │
  │                                                                             │
  └─────────────────────────────────────────────────────────────────────────────┘
```

### 8.3 Decision Matrix: Key Design Choices

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    DECISION MATRIX: KEY DESIGN CHOICES                            ║
+══════════════════════════════════════════════════════════════════════════════════+

  DECISION 1: Where should orchestration logic live?
  ─────────────────────────────────────────────────────

  ┌─────────────────────┬─────────────┬─────────────┬─────────────┬─────────────┐
  │ Option              │ Flexibility │ Latency     │ Complexity  │ Score       │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ A. Realtime API     │ LOW         │ LOW         │ LOW         │ ★★☆☆☆       │
  │    (v1 approach)    │ (limited)   │ (fast)      │ (simple)    │             │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ B. Separate Leader  │ HIGH        │ MEDIUM      │ MEDIUM      │ ★★★★★       │
  │    Agent (v2)       │ (full ctrl) │ (+200ms)    │ (manageable)│ CHOSEN      │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ C. Backend Service  │ MEDIUM      │ MEDIUM      │ HIGH        │ ★★★☆☆       │
  │    (no LLM)         │ (coded)     │ (varies)    │ (manual)    │             │
  └─────────────────────┴─────────────┴─────────────┴─────────────┴─────────────┘

  DECISION 2: How to persist memory?
  ───────────────────────────────────

  ┌─────────────────────┬─────────────┬─────────────┬─────────────┬─────────────┐
  │ Option              │ Speed       │ Semantics   │ Cost        │ Score       │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ A. SQL Database     │ FAST        │ NONE        │ LOW         │ ★★☆☆☆       │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ B. Vector DB only   │ MEDIUM      │ HIGH        │ MEDIUM      │ ★★★☆☆       │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ C. Redis + Vector   │ FAST        │ HIGH        │ MEDIUM      │ ★★★★★       │
  │    (Tiered)         │ (cached)    │ (semantic)  │             │ CHOSEN      │
  └─────────────────────┴─────────────┴─────────────┴─────────────┴─────────────┘

  DECISION 3: How to expose tools?
  ─────────────────────────────────

  ┌─────────────────────┬─────────────┬─────────────┬─────────────┬─────────────┐
  │ Option              │ Standard    │ Security    │ Extensible  │ Score       │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ A. Function Calling │ NONE        │ CUSTOM      │ LOW         │ ★★☆☆☆       │
  │    (v1)             │ (proprietary)│ (manual)   │ (code)      │             │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ B. MCP Protocol     │ HIGH        │ OAUTH 2.1   │ HIGH        │ ★★★★★       │
  │    (v2)             │ (open std)  │ (standard)  │ (add server)│ CHOSEN      │
  ├─────────────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
  │ C. REST APIs        │ MEDIUM      │ VARIES      │ MEDIUM      │ ★★★☆☆       │
  │    (custom)         │ (common)    │ (custom)    │             │             │
  └─────────────────────┴─────────────┴─────────────┴─────────────┴─────────────┘
```

### 8.4 Second-Order Thinking: Consequences

```
+══════════════════════════════════════════════════════════════════════════════════+
║              SECOND-ORDER THINKING: CONSEQUENCES OF v2 DESIGN                     ║
+══════════════════════════════════════════════════════════════════════════════════+

  DESIGN CHOICE: Add Leader Agent between Voice and Tools
  ────────────────────────────────────────────────────────

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  FIRST-ORDER CONSEQUENCES (Immediate)                                       │
  │                                                                             │
  │  ✓ Better context management                                                │
  │  ✓ Smarter tool selection                                                   │
  │  ✓ Memory integration                                                       │
  │  ✗ Added latency (~200ms)                                                   │
  │  ✗ Additional LLM cost                                                      │
  └─────────────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  SECOND-ORDER CONSEQUENCES (Then what?)                                     │
  │                                                                             │
  │  If better context management...                                            │
  │    → Users don't repeat information                                         │
  │    → Conversations feel more natural                                        │
  │    → Higher user satisfaction                                               │
  │                                                                             │
  │  If added latency...                                                        │
  │    → Users notice if >2s total response time                                │
  │    → SOLUTION: Parallel tool calls + caching mitigate                       │
  │    → NET: Latency acceptable if under 1.5s                                  │
  │                                                                             │
  │  If additional LLM cost...                                                  │
  │    → ~$0.01-0.05 per conversation turn (GPT-4o)                             │
  │    → Offset by reduced user frustration/churn                               │
  │    → Can use smaller model (GPT-4o-mini) for simple queries                 │
  └─────────────────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  THIRD-ORDER CONSEQUENCES (And then?)                                       │
  │                                                                             │
  │  If higher user satisfaction...                                             │
  │    → More voice sessions per user                                           │
  │    → Word-of-mouth growth                                                   │
  │    → Justifies infrastructure investment                                    │
  │                                                                             │
  │  If NET latency acceptable...                                               │
  │    → Can add more sophisticated processing                                  │
  │    → Multi-step reasoning becomes possible                                  │
  │    → Complex queries ("compare both kids' grades") become natural           │
  └─────────────────────────────────────────────────────────────────────────────┘
```

---

## 9. Deep RCA Verification

### 9.1 Verifying MCP Design Solves Context Loss

```
+══════════════════════════════════════════════════════════════════════════════════+
║              DEEP RCA VERIFICATION: DOES v2 SOLVE CONTEXT LOSS?                   ║
+══════════════════════════════════════════════════════════════════════════════════+

  HYPOTHESIS: v2 with Leader Agent + MCP + Memory Layer solves context loss
  ──────────────────────────────────────────────────────────────────────────

  CAUSAL CHAIN (v1 → Problem):

  ┌──────────────────┐     ┌────────────────────┐     ┌──────────────────┐
  │     TRIGGER      │     │    AMPLIFIER       │     │     SYMPTOM      │
  │   (Root Cause)   │     │  (Made it worse)   │     │  (User sees)     │
  ├──────────────────┤     ├────────────────────┤     ├──────────────────┤
  │ No persistent    │════▶│ 11 tool defs       │════▶│ AI forgets       │
  │ memory layer     │     │ consume 8K tokens  │     │ context after    │
  │                  │     │ per request        │     │ 5 turns          │
  └──────────────────┘     └────────────────────┘     └──────────────────┘

  v2 INTERVENTION POINTS:

  ┌──────────────────┐     ┌────────────────────┐     ┌──────────────────┐
  │     TRIGGER      │     │    AMPLIFIER       │     │     SYMPTOM      │
  │   SOLVED BY:     │     │   SOLVED BY:       │     │   RESULT:        │
  ├──────────────────┤     ├────────────────────┤     ├──────────────────┤
  │ ✓ Memory Layer   │     │ ✓ MCP servers      │     │ ✓ AI remembers   │
  │   (Redis +       │     │   (tools loaded    │     │   context        │
  │    Vector DB)    │     │    on-demand,      │     │   indefinitely   │
  │                  │     │    0 tokens in     │     │                  │
  │                  │     │    prompt)         │     │                  │
  └──────────────────┘     └────────────────────┘     └──────────────────┘

  VERIFICATION MATRIX:

  ┌────────────────────────────┬───────────────────────┬───────────────────────┐
  │ v1 Problem                 │ v2 Solution           │ Verified By           │
  ├────────────────────────────┼───────────────────────┼───────────────────────┤
  │ Context lost after 5 turns │ Memory summarization  │ Session memory stores │
  │                            │                       │ unlimited history     │
  ├────────────────────────────┼───────────────────────┼───────────────────────┤
  │ No cross-session recall    │ Long-term memory      │ Vector DB persists    │
  │                            │ (Vector DB)           │ across sessions       │
  ├────────────────────────────┼───────────────────────┼───────────────────────┤
  │ Tool defs consume tokens   │ MCP (external servers)│ 0 tool tokens in      │
  │                            │                       │ Leader Agent prompt   │
  ├────────────────────────────┼───────────────────────┼───────────────────────┤
  │ No intelligent context     │ Leader Agent with     │ Agent queries memory  │
  │ selection                  │ memory query phase    │ before each response  │
  ├────────────────────────────┼───────────────────────┼───────────────────────┤
  │ "What about Tom?" fails    │ Session memory tracks │ Leader infers "grades"│
  │                            │ conversation topics   │ from previous turn    │
  └────────────────────────────┴───────────────────────┴───────────────────────┘

  CONFIDENCE ASSESSMENT:

  ┌────────────────────────────┬─────────────┬─────────────────────────────────┐
  │ Claim                      │ Confidence  │ Evidence                        │
  ├────────────────────────────┼─────────────┼─────────────────────────────────┤
  │ Memory layer solves        │ [HIGH]      │ Industry pattern (Letta,        │
  │ context loss               │             │ LangChain, Mem0)                │
  ├────────────────────────────┼─────────────┼─────────────────────────────────┤
  │ MCP reduces token usage    │ [HIGH]      │ MCP spec: tools server-side     │
  ├────────────────────────────┼─────────────┼─────────────────────────────────┤
  │ Leader Agent improves      │ [MEDIUM]    │ Requires implementation         │
  │ decision quality           │             │ and testing to verify           │
  ├────────────────────────────┼─────────────┼─────────────────────────────────┤
  │ Latency stays <1.5s        │ [MEDIUM]    │ Depends on optimization         │
  │                            │             │ (caching, parallel calls)       │
  └────────────────────────────┴─────────────┴─────────────────────────────────┘
```

### 9.2 Pre-mortem: What Could Go Wrong?

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    PRE-MORTEM: POTENTIAL FAILURE MODES                            ║
+══════════════════════════════════════════════════════════════════════════════════+

  SCENARIO 1: Memory retrieval too slow
  ──────────────────────────────────────

  If This Happens:
    Memory query adds >500ms to every response
    → Total latency exceeds 2s
    → User experience degrades

  Mitigation:
    • Working memory always in-context (0ms)
    • Session memory in Redis (<10ms)
    • Long-term query only when needed (<100ms)
    • Pre-fetch likely queries on session start

  ─────────────────────────────────────────────────────────────────────────────

  SCENARIO 2: MCP server becomes bottleneck
  ──────────────────────────────────────────

  If This Happens:
    Education MCP server can't handle concurrent requests
    → Tool calls queue up
    → Response times spike

  Mitigation:
    • Horizontal scaling (multiple MCP server instances)
    • Caching frequent queries (student roster, class list)
    • Circuit breaker pattern for failures
    • Fallback to cached data if MCP unavailable

  ─────────────────────────────────────────────────────────────────────────────

  SCENARIO 3: Leader Agent makes poor decisions
  ──────────────────────────────────────────────

  If This Happens:
    Agent calls wrong tools or misinterprets intent
    → Incorrect responses
    → User frustration

  Mitigation:
    • Comprehensive system prompt with examples
    • Confidence scoring on intent analysis
    • Clarification questions for ambiguous requests
    • User feedback loop to improve prompts

  ─────────────────────────────────────────────────────────────────────────────

  SCENARIO 4: Memory becomes stale or inconsistent
  ────────────────────────────────────────────────

  If This Happens:
    Cached data doesn't reflect actual student data
    → AI gives outdated information

  Mitigation:
    • TTL on cached education data (1 hour)
    • Cache invalidation on known updates
    • Always show data freshness to user
    • Option to "refresh" data on demand
```

---

## 10. Implementation Roadmap

### 10.1 Phase Overview

```
+══════════════════════════════════════════════════════════════════════════════════+
║                         IMPLEMENTATION ROADMAP                                    ║
+══════════════════════════════════════════════════════════════════════════════════+

  PHASE 0: Foundation (Current → v1.1)
  ─────────────────────────────────────
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │ • Refactor v1 code for modularity                                           │
  │ • Extract tool definitions to separate files                                │
  │ • Add basic session storage (in-memory)                                     │
  │ • Create test harness for voice workflows                                   │
  │                                                                             │
  │ Deliverable: Clean codebase ready for v2 components                         │
  └─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
  PHASE 1: Memory Layer
  ──────────────────────
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │ • Deploy Redis for session memory                                           │
  │ • Integrate Vector DB (Qdrant/Pinecone) for long-term                       │
  │ • Implement memory-mcp server with basic tools                              │
  │ • Add summarization pipeline                                                │
  │                                                                             │
  │ Deliverable: Working memory layer with recall/store                         │
  └─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
  PHASE 2: MCP Infrastructure
  ───────────────────────────
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │ • Create education-mcp server (Python/Node)                                 │
  │ • Migrate v1 functions to MCP tools                                         │
  │ • Implement OAuth 2.1 authentication                                        │
  │ • Add MCP client to .NET infrastructure                                     │
  │                                                                             │
  │ Deliverable: Education data accessible via MCP                              │
  └─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
  PHASE 3: Leader Agent
  ─────────────────────
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │ • Implement Leader Agent service (GPT-4o)                                   │
  │ • Create intent analysis + memory query pipeline                            │
  │ • Connect Leader Agent to MCP servers                                       │
  │ • Add delegate_to_leader tool in Realtime API                               │
  │                                                                             │
  │ Deliverable: Voice → Leader → MCP flow working                              │
  └─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
  PHASE 4: Integration & Optimization
  ────────────────────────────────────
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │ • End-to-end voice conversation testing                                     │
  │ • Latency optimization (caching, parallel calls)                            │
  │ • Add advice-mcp server                                                     │
  │ • User acceptance testing                                                   │
  │                                                                             │
  │ Deliverable: Production-ready v2                                            │
  └─────────────────────────────────────────────────────────────────────────────┘
```

### 10.2 Component Dependencies

```
+══════════════════════════════════════════════════════════════════════════════════+
║                         COMPONENT DEPENDENCIES                                    ║
+══════════════════════════════════════════════════════════════════════════════════+

                              ┌─────────────────────┐
                              │   Phase 4: Full     │
                              │   Integration       │
                              └──────────┬──────────┘
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
                    ▼                    ▼                    ▼
          ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
          │ Phase 3:        │  │ Phase 2:        │  │ Phase 1:        │
          │ Leader Agent    │  │ MCP Servers     │  │ Memory Layer    │
          └────────┬────────┘  └────────┬────────┘  └────────┬────────┘
                   │                    │                    │
                   │           ┌────────┴────────┐           │
                   │           │                 │           │
                   └───────────┼────────┬────────┼───────────┘
                               │        │        │
                               ▼        ▼        ▼
                         ┌─────────────────────────────┐
                         │ Phase 0: Foundation         │
                         │ (Refactor v1)               │
                         └─────────────────────────────┘

  DEPENDENCY NOTES:
  • Phase 1 & 2 can run in parallel after Phase 0
  • Phase 3 requires Phase 1 (memory) + Phase 2 (MCP)
  • Phase 4 requires all previous phases
```

---

## 11. References

### 11.1 MCP Resources

| Resource | URL | Description |
|----------|-----|-------------|
| MCP Specification | [modelcontextprotocol.io/specification](https://modelcontextprotocol.io/specification/2025-11-25) | Official spec (Nov 2025) |
| MCP Best Practices | [modelcontextprotocol.info](https://modelcontextprotocol.info/docs/best-practices/) | Architecture guidelines |
| MCP Anniversary Blog | [blog.modelcontextprotocol.io](http://blog.modelcontextprotocol.io/posts/2025-11-25-first-mcp-anniversary/) | 2025 spec updates |
| MCP Security Guide | [mcp-best-practice.github.io](https://mcp-best-practice.github.io/mcp-best-practice/best-practice/) | Security best practices |

### 11.2 Memory & Context Resources

| Resource | URL | Description |
|----------|-----|-------------|
| OpenAI Realtime Context | [platform.openai.com](https://platform.openai.com/docs/guides/realtime-conversations) | Truncation & retention |
| OpenAI Context Summarization | [cookbook.openai.com](https://cookbook.openai.com/examples/context_summarization_with_realtime_api) | Summarization patterns |
| Letta Agent Memory | [letta.com](https://www.letta.com/blog/agent-memory) | Tiered memory design |
| LangChain Memory | [langchain-ai.github.io](https://langchain-ai.github.io/langmem/concepts/conceptual_guide/) | Long-term memory concepts |
| Pinecone LangChain | [pinecone.io](https://www.pinecone.io/learn/series/langchain/langchain-conversational-memory/) | Vector DB for memory |
| Mem0 Architecture | [arxiv.org](https://arxiv.org/pdf/2504.19413) | Production memory system |
| AWS Mem0 Guide | [aws.amazon.com](https://aws.amazon.com/blogs/database/build-persistent-memory-for-agentic-ai-applications-with-mem0-open-source-amazon-elasticache-for-valkey-and-amazon-neptune-analytics/) | Redis + Vector + Graph |

### 11.3 Multi-Agent Patterns

| Resource | URL | Description |
|----------|-----|-------------|
| OpenAI Conversations API | [platform.openai.com](https://platform.openai.com/docs/api-reference/conversations) | Stateful conversations |
| OpenAI Session Memory | [cookbook.openai.com](https://cookbook.openai.com/examples/agents_sdk/session_memory) | Context engineering |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v2-draft | 2026-01-12 | Claude Code | Initial architecture redesign |

---

*Generated with assistance from Claude Code - Deep Agent System Lab*
