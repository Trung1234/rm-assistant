# DEEP AGENT SYSTEM - Architecture v3 (Improved)

**Document ID**: Over-v3-deep-agent-architecture-improved
**Created**: 2026-01-12
**Version**: v3
**Status**: Design Complete - Verified Against Research
**Previous Version**: [Over-v2-deep-agent-architecture-redesign.md](Over-v2-deep-agent-architecture-redesign.md)
**Research References**:
- [v1-explain-magentic-leader-plan.md](../research/v1-explain-magentic-leader-plan.md)
- [v1-context-sharing-memory-architecture.md](../research/v1-context-sharing-memory-architecture.md)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [v3 Improvements Over v2](#2-v3-improvements-over-v2)
3. [Dual Loop Architecture (Magentic-One Pattern)](#3-dual-loop-architecture-magentic-one-pattern)
4. [Two-Tier Memory Architecture](#4-two-tier-memory-architecture)
5. [Leader Agent with ReAct Pattern](#5-leader-agent-with-react-pattern)
6. [Memory Scope Hierarchy](#6-memory-scope-hierarchy)
7. [Blackboard Synchronization Pattern](#7-blackboard-synchronization-pattern)
8. [Complete System Architecture](#8-complete-system-architecture)
9. [Workflow Diagrams (Improved)](#9-workflow-diagrams-improved)
10. [Implementation Decision Framework](#10-implementation-decision-framework)
11. [Verification Against Research](#11-verification-against-research)
12. [References](#12-references)

---

## 1. Executive Summary

### 1.1 What's New in v3?

v3 incorporates key architectural patterns from research documents that were missing in v2:

| Concept | v2 Status | v3 Improvement | Research Source |
|---------|-----------|----------------|-----------------|
| **Dual Loop System** | Missing | Outer Loop (Strategic) + Inner Loop (Tactical) | Magentic-One |
| **Task Ledger** | Missing | FACTS, GUESSES, PLAN tracking | Magentic-One |
| **Progress Ledger** | Missing | Task assignment, completion status | Magentic-One |
| **ReAct Pattern** | Implicit | Explicit Reason + Act interleaving | CoT Research |
| **Two-Tier Memory** | Three-tier | Private + Shared (clearer separation) | Memory Research |
| **Memory Scope Hierarchy** | Flat | Global → User → Session → Agent | Memory Research |
| **Blackboard Pattern** | Missing | Centralized shared state coordination | Memory Research |
| **Self-Correction** | Missing | Validate guesses, track progress, handle stalls | Magentic-One |

### 1.2 v3 Design Philosophy

```
+==============================================================================+
|                         v3 DESIGN PHILOSOPHY                                  |
+==============================================================================+
|                                                                              |
|  1. DUAL LOOP CONTROL (from Magentic-One)                                    |
|     ─────────────────────────────────────                                    |
|     OUTER LOOP: Strategic planning (Task Ledger - FACTS/GUESSES/PLAN)        |
|     INNER LOOP: Tactical execution (Progress Ledger - assignment/status)     |
|                                                                              |
|  2. REASON BEFORE ACTION (ReAct Pattern)                                     |
|     ──────────────────────────────────────                                   |
|     Every action must be preceded by explicit reasoning                      |
|     Thought → Action → Observation → Thought → ...                           |
|                                                                              |
|  3. TWO-TIER MEMORY (Private + Shared)                                       |
|     ─────────────────────────────────────                                    |
|     PRIVATE: Agent-specific knowledge, role context                          |
|     SHARED: Team state, current task, decisions (Blackboard)                 |
|                                                                              |
|  4. MEMORY SCOPE HIERARCHY                                                   |
|     ──────────────────────────                                               |
|     Global → User → Session → Agent                                          |
|     (Broadest to most specific, each level inherits from parent)             |
|                                                                              |
|  5. SELF-CORRECTION MECHANISM                                                |
|     ────────────────────────────                                             |
|     • Track progress at every step                                           |
|     • Validate assumptions before proceeding                                 |
|     • Detect stalls and trigger re-planning                                  |
|                                                                              |
+==============================================================================+
```

---

## 2. v3 Improvements Over v2

### 2.1 Architecture Comparison Matrix

```
+==============================================================================+
|                    ARCHITECTURE EVOLUTION: v1 → v2 → v3                       |
+==============================================================================+

┌────────────────────────┬────────────────────┬────────────────────┬────────────────────┐
│ Aspect                 │ v1                 │ v2                 │ v3 (Improved)      │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Planning Approach      │ None               │ Implicit           │ Dual Loop          │
│                        │                    │                    │ (Task + Progress)  │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Reasoning Pattern      │ Direct response    │ Intent analysis    │ ReAct              │
│                        │                    │                    │ (Thought→Act→Obs)  │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Memory Architecture    │ None               │ Three-tier         │ Two-tier           │
│                        │                    │ (Working/Session/  │ (Private+Shared)   │
│                        │                    │ Long-term)         │ + Scope Hierarchy  │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Context Coordination   │ None               │ Memory Manager     │ Blackboard Pattern │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Self-Correction        │ None               │ None               │ Progress tracking  │
│                        │                    │                    │ + Stall detection  │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Task Tracking          │ None               │ None               │ Task Ledger        │
│                        │                    │                    │ (FACTS/GUESSES/    │
│                        │                    │                    │ PLAN)              │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Token Efficiency       │ Poor (32K sat.)    │ Good (~8K)         │ Excellent (~6K)    │
│                        │                    │                    │ (memory formation) │
├────────────────────────┼────────────────────┼────────────────────┼────────────────────┤
│ Cross-Session Memory   │ None               │ Vector DB          │ Hybrid: Mem0-style │
│                        │                    │                    │ + Graph DB         │
└────────────────────────┴────────────────────┴────────────────────┴────────────────────┘
```

### 2.2 Key Gap Analysis: v2 vs Research

| v2 Gap | Research Best Practice | v3 Solution |
|--------|------------------------|-------------|
| No explicit planning phase | Magentic-One Outer Loop creates plan before execution | Add Task Ledger with FACTS/GUESSES/PLAN |
| No progress tracking | Magentic-One Inner Loop tracks task completion | Add Progress Ledger with status updates |
| Implicit reasoning | ReAct pattern requires explicit reasoning steps | Enforce Thought→Action→Observation cycle |
| Three-tier memory unclear | Two-tier (Private+Shared) is clearer for multi-agent | Restructure to Private+Shared with scopes |
| No central coordination | Blackboard pattern for shared state | Add Blackboard as coordination hub |
| No self-correction | Magentic-One validates guesses, detects stalls | Add validation and re-planning triggers |

---

## 3. Dual Loop Architecture (Magentic-One Pattern)

### 3.1 Overview: Outer Loop + Inner Loop

```
+==============================================================================+
|                    DUAL LOOP ARCHITECTURE (from Magentic-One)                 |
+==============================================================================+

                              ┌─────────────────────────────────────────────┐
                              │              USER REQUEST                    │
                              │  "Show me Lucy's grades and give advice"    │
                              └──────────────────────┬──────────────────────┘
                                                     │
                                                     ▼
+══════════════════════════════════════════════════════════════════════════════+
║                          OUTER LOOP (Strategic)                               ║
║                          ══════════════════════                               ║
║                                                                               ║
║   PURPOSE: Create and maintain the overall plan                               ║
║   FREQUENCY: Once per user request + when re-planning needed                  ║
║                                                                               ║
║   ┌─────────────────────────────────────────────────────────────────────────┐ ║
║   │                         TASK LEDGER                                      │ ║
║   ├─────────────────────────────────────────────────────────────────────────┤ ║
║   │                                                                          │ ║
║   │  FACTS (verified information):                                           │ ║
║   │  ─────────────────────────────                                           │ ║
║   │  • Parent: John Smith (authenticated)                                    │ ║
║   │  • Children: Lucy (age 12), Tom (age 10)                                 │ ║
║   │  • Active student: Lucy                                                  │ ║
║   │  • Request type: Query grades + generate advice                          │ ║
║   │                                                                          │ ║
║   │  GUESSES (assumptions to validate):                                      │ ║
║   │  ──────────────────────────────────                                      │ ║
║   │  • Lucy has current grades available in Graph API                        │ ║
║   │  • User wants comprehensive grade report (not single subject)            │ ║
║   │  • "Advice" means educational recommendations for parent                 │ ║
║   │                                                                          │ ║
║   │  PLAN (ordered steps):                                                   │ ║
║   │  ────────────────────                                                    │ ║
║   │  1. Fetch Lucy's class list from education-mcp                          │ ║
║   │  2. Fetch grades for each class                                         │ ║
║   │  3. Retrieve Lucy's historical performance from memory                  │ ║
║   │  4. Generate personalized advice using advice-mcp                       │ ║
║   │  5. Synthesize response and update memory                               │ ║
║   │                                                                          │ ║
║   └─────────────────────────────────────────────────────────────────────────┘ ║
║                                                                               ║
║   TRIGGERS FOR RE-PLANNING:                                                   ║
║   • GUESS proven wrong → Update facts, revise plan                           ║
║   • Step fails repeatedly → Find alternative approach                        ║
║   • User provides new information → Incorporate into facts                   ║
║                                                                               ║
+══════════════════════════════════════════════════════════════════════════════+
                                        │
                                        │ Plan Steps
                                        ▼
+══════════════════════════════════════════════════════════════════════════════+
║                          INNER LOOP (Tactical)                                ║
║                          ═════════════════════                                ║
║                                                                               ║
║   PURPOSE: Execute plan steps and track progress                              ║
║   FREQUENCY: Every action until plan complete                                 ║
║                                                                               ║
║   ┌─────────────────────────────────────────────────────────────────────────┐ ║
║   │                       PROGRESS LEDGER                                    │ ║
║   ├─────────────────────────────────────────────────────────────────────────┤ ║
║   │                                                                          │ ║
║   │  CURRENT_PROGRESS:                                                       │ ║
║   │  ─────────────────                                                       │ ║
║   │  ✅ Step 1: Fetched classes (Math 101, Science 201, History 301)        │ ║
║   │  ✅ Step 2: Fetched grades (Math: 85%, Science: 92%, History: 78%)      │ ║
║   │  🔄 Step 3: IN PROGRESS - Querying memory for historical data           │ ║
║   │  ⬜ Step 4: Pending                                                      │ ║
║   │  ⬜ Step 5: Pending                                                      │ ║
║   │                                                                          │ ║
║   │  TASK_ASSIGNMENT:                                                        │ ║
║   │  ─────────────────                                                       │ ║
║   │  • Current: memory-mcp.recall(query="Lucy performance history")         │ ║
║   │  • Next: advice-mcp.generate_advice(student_data, grades)               │ ║
║   │                                                                          │ ║
║   │  COMPLETION_STATUS:                                                      │ ║
║   │  ──────────────────                                                      │ ║
║   │  • Progress: 2/5 steps (40%)                                            │ ║
║   │  • Stall detected: NO                                                   │ ║
║   │  • Retry count: 0                                                       │ ║
║   │                                                                          │ ║
║   └─────────────────────────────────────────────────────────────────────────┘ ║
║                                                                               ║
║   STALL DETECTION:                                                            ║
║   • Same step fails 3+ times → Escalate to Outer Loop                        ║
║   • No progress for N seconds → Timeout, report partial results              ║
║   • Error pattern detected → Trigger re-planning                             ║
║                                                                               ║
+══════════════════════════════════════════════════════════════════════════════+
                                        │
                                        │ Completed Steps
                                        ▼
                              ┌─────────────────────────────────────────────┐
                              │              RESPONSE TO USER                │
                              │  + Update Memory with new facts              │
                              └─────────────────────────────────────────────┘
```

### 3.2 Task Ledger Structure

```
+==============================================================================+
|                         TASK LEDGER SCHEMA (v3)                               |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ interface TaskLedger {                                                       │
│                                                                              │
│   // Unique identifier for this task instance                                │
│   taskId: string;                                                            │
│                                                                              │
│   // Original user request                                                   │
│   userRequest: string;                                                       │
│                                                                              │
│   // FACTS: Verified information (won't change during execution)             │
│   facts: {                                                                   │
│     source: 'memory' | 'auth' | 'tool_result' | 'user_input';               │
│     content: string;                                                         │
│     confidence: number;  // 1.0 = verified fact                              │
│   }[];                                                                       │
│                                                                              │
│   // GUESSES: Assumptions to validate                                        │
│   guesses: {                                                                 │
│     assumption: string;                                                      │
│     validatedBy?: string;  // Which step validates this                      │
│     status: 'pending' | 'confirmed' | 'refuted';                            │
│   }[];                                                                       │
│                                                                              │
│   // PLAN: Ordered steps to complete the task                                │
│   plan: {                                                                    │
│     stepNumber: number;                                                      │
│     action: string;  // Human-readable description                           │
│     tool?: string;   // MCP tool to call                                     │
│     dependsOn?: number[];  // Step dependencies                              │
│     estimatedTokens?: number;                                                │
│   }[];                                                                       │
│                                                                              │
│   // Metadata                                                                │
│   createdAt: Date;                                                           │
│   lastModified: Date;                                                        │
│   status: 'planning' | 'executing' | 'completed' | 'failed' | 'replanning'; │
│ }                                                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Progress Ledger Structure

```
+==============================================================================+
|                       PROGRESS LEDGER SCHEMA (v3)                             |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ interface ProgressLedger {                                                   │
│                                                                              │
│   // Reference to parent Task Ledger                                         │
│   taskId: string;                                                            │
│                                                                              │
│   // Current execution state                                                 │
│   currentProgress: {                                                         │
│     stepNumber: number;                                                      │
│     status: 'pending' | 'in_progress' | 'completed' | 'failed' | 'skipped'; │
│     startedAt?: Date;                                                        │
│     completedAt?: Date;                                                      │
│     result?: any;  // Tool output                                            │
│     error?: string;                                                          │
│     retryCount: number;                                                      │
│   }[];                                                                       │
│                                                                              │
│   // Current task assignment                                                 │
│   taskAssignment: {                                                          │
│     currentStep: number;                                                     │
│     nextStep: number | null;                                                 │
│     blockedReason?: string;                                                  │
│   };                                                                         │
│                                                                              │
│   // Completion tracking                                                     │
│   completionStatus: {                                                        │
│     totalSteps: number;                                                      │
│     completedSteps: number;                                                  │
│     progressPercent: number;                                                 │
│     stallDetected: boolean;                                                  │
│     lastProgressAt: Date;                                                    │
│   };                                                                         │
│                                                                              │
│   // Self-correction triggers                                                │
│   selfCorrection: {                                                          │
│     maxRetries: number;  // Default: 3                                       │
│     stallTimeoutMs: number;  // Default: 30000                               │
│     escalateToOuterLoop: boolean;                                            │
│     replanReason?: string;                                                   │
│   };                                                                         │
│ }                                                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Two-Tier Memory Architecture

### 4.1 Private vs Shared Memory

```
+==============================================================================+
|                    TWO-TIER MEMORY ARCHITECTURE (v3)                          |
+==============================================================================+
|                                                                              |
|   ┌─────────────────────────────────────────────────────────────────────┐   |
|   │                                                                      │   |
|   │   TIER 1: PRIVATE MEMORY (Per-agent)                                │   |
|   │   ════════════════════════════════════                              │   |
|   │                                                                      │   |
|   │   PURPOSE: Agent-specific knowledge that shouldn't be shared        │   |
|   │                                                                      │   |
|   │   ┌───────────────────┐   ┌───────────────────┐                    │   |
|   │   │   LEADER AGENT    │   │  EDUCATION AGENT  │   (future)         │   |
|   │   │   PRIVATE MEMORY  │   │  PRIVATE MEMORY   │                    │   |
|   │   ├───────────────────┤   ├───────────────────┤                    │   |
|   │   │                   │   │                   │                    │   |
|   │   │ • Reasoning       │   │ • Subject-specific│                    │   |
|   │   │   strategies      │   │   knowledge       │                    │   |
|   │   │                   │   │                   │                    │   |
|   │   │ • Tool selection  │   │ • Grade scale     │                    │   |
|   │   │   patterns        │   │   interpretations │                    │   |
|   │   │                   │   │                   │                    │   |
|   │   │ • Error handling  │   │ • Curriculum      │                    │   |
|   │   │   history         │   │   mapping         │                    │   |
|   │   │                   │   │                   │                    │   |
|   │   │ • Role-specific   │   │ • School-specific │                    │   |
|   │   │   instructions    │   │   conventions     │                    │   |
|   │   │                   │   │                   │                    │   |
|   │   └───────────────────┘   └───────────────────┘                    │   |
|   │                                                                      │   |
|   │   CHARACTERISTICS:                                                   │   |
|   │   • Storage: In-memory or fast cache (Redis per agent)              │   |
|   │   • Scope: Only accessible by owning agent                          │   |
|   │   • Persistence: Session-lifetime or longer for learned patterns    │   |
|   │                                                                      │   |
|   └─────────────────────────────────────────────────────────────────────┘   |
|                                    │                                         |
|                                    │ Read/Write via Memory Gateway           |
|                                    ▼                                         |
|   ┌─────────────────────────────────────────────────────────────────────┐   |
|   │                                                                      │   |
|   │   TIER 2: SHARED MEMORY (Blackboard)                                │   |
|   │   ══════════════════════════════════                                │   |
|   │                                                                      │   |
|   │   PURPOSE: Coordination state accessible by all agents              │   |
|   │                                                                      │   |
|   │   ┌─────────────────────────────────────────────────────────────┐   │   |
|   │   │                      BLACKBOARD                              │   │   |
|   │   ├─────────────────────────────────────────────────────────────┤   │   |
|   │   │                                                              │   │   |
|   │   │  CURRENT TASK STATE (from Task Ledger):                      │   │   |
|   │   │  ─────────────────────────────────────                       │   │   |
|   │   │  • taskId: "task-12345"                                     │   │   |
|   │   │  • userRequest: "Show Lucy's grades and give advice"        │   │   |
|   │   │  • currentStep: 3                                           │   │   |
|   │   │  • status: "executing"                                      │   │   |
|   │   │                                                              │   │   |
|   │   │  SHARED FACTS:                                               │   │   |
|   │   │  ─────────────                                               │   │   |
|   │   │  • activeStudent: { id: "lucy-123", name: "Lucy Smith" }    │   │   |
|   │   │  • parentId: "john-456"                                      │   │   |
|   │   │  • latestGrades: { Math: 85, Science: 92, History: 78 }     │   │   |
|   │   │                                                              │   │   |
|   │   │  TEAM DECISIONS:                                             │   │   |
|   │   │  ───────────────                                             │   │   |
|   │   │  • responseFormat: "voice-optimized" (short sentences)       │   │   |
|   │   │  • adviceStyle: "encouraging"                                │   │   |
|   │   │                                                              │   │   |
|   │   │  SHARED ARTIFACTS:                                           │   │   |
|   │   │  ─────────────────                                           │   │   |
|   │   │  • fetchedClasses: ["Math 101", "Science 201", "History 301"]│   │   |
|   │   │  • generatedAdvice: null  (not yet generated)               │   │   |
|   │   │                                                              │   │   |
|   │   └─────────────────────────────────────────────────────────────┘   │   |
|   │                                                                      │   |
|   │   CHARACTERISTICS:                                                   │   |
|   │   • Storage: Redis + Vector DB (Qdrant) for semantic search         │   |
|   │   • Scope: All agents can read; controlled write access             │   |
|   │   • Persistence: Task-lifetime → Session-lifetime → Long-term       │   |
|   │                                                                      │   |
|   └─────────────────────────────────────────────────────────────────────┘   |
|                                                                              |
+==============================================================================+
```

### 4.2 v3 Memory vs v2 Memory Comparison

```
+==============================================================================+
|                    MEMORY ARCHITECTURE COMPARISON: v2 vs v3                   |
+==============================================================================+

v2: THREE-TIER (Less Clear)              v3: TWO-TIER (Clearer)
──────────────────────────               ─────────────────────────

┌─────────────────────┐                  ┌─────────────────────────────────┐
│ WORKING MEMORY      │                  │ PRIVATE MEMORY (per agent)      │
│ • Current turn      │                  │ • Agent-specific knowledge      │
│ • Active context    │     ═══════►     │ • Role instructions             │
│ • Tool results      │      MAPS TO     │ • Learned patterns              │
└─────────────────────┘                  │ • Current reasoning state       │
                                         └─────────────────────────────────┘
┌─────────────────────┐
│ SESSION MEMORY      │
│ • Chat history      │                  ┌─────────────────────────────────┐
│ • Recent summaries  │     ═══════►     │ SHARED MEMORY (Blackboard)      │
│ • Session state     │      MAPS TO     │                                 │
└─────────────────────┘                  │ TASK SCOPE:                     │
                                         │ • Current task state            │
┌─────────────────────┐                  │ • Facts, Guesses, Plan          │
│ LONG-TERM MEMORY    │                  │ • Progress tracking             │
│ • User preferences  │     ═══════►     │                                 │
│ • Conversation sums │      MAPS TO     │ SESSION SCOPE:                  │
│ • Student profiles  │                  │ • Chat history                  │
└─────────────────────┘                  │ • Session context               │
                                         │                                 │
                                         │ USER SCOPE:                     │
                                         │ • User preferences              │
                                         │ • Student profiles              │
                                         │                                 │
                                         │ GLOBAL SCOPE:                   │
                                         │ • System knowledge              │
                                         │ • Shared templates              │
                                         └─────────────────────────────────┘

KEY IMPROVEMENT:
  v2 conflated "where data lives" (tier) with "who can access it" (scope)
  v3 separates "private vs shared" from "lifetime/scope" (Global→User→Session→Agent)
```

---

## 5. Leader Agent with ReAct Pattern

### 5.1 ReAct Pattern Implementation

```
+==============================================================================+
|                    ReAct PATTERN FOR LEADER AGENT (v3)                        |
+==============================================================================+

  ReAct = REASON + ACT (interleaved)

  Traditional (v2):           ReAct (v3):
  ─────────────────           ───────────────────────────────────────────────
  Intent → Action → Result    Thought → Action → Observation → Thought → ...

+------------------------------------------------------------------------------+
|                                                                              |
|  USER: "Show me Lucy's grades and give advice"                               |
|                                                                              |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ THOUGHT 1:                                                              │  |
|  │ The user wants two things: (1) Lucy's grades, (2) educational advice.  │  |
|  │ I need to first verify Lucy is the active student, then fetch grades,  │  |
|  │ then generate advice based on the grades.                               │  |
|  │                                                                         │  |
|  │ I will start by checking memory for the active student context.        │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ ACTION 1:                                                               │  |
|  │ memory-mcp.recall(query="active student", scope="session")              │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ OBSERVATION 1:                                                          │  |
|  │ { "activeStudent": { "id": "lucy-123", "name": "Lucy Smith" } }         │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ THOUGHT 2:                                                              │  |
|  │ Good, Lucy is confirmed as the active student. Now I need to fetch     │  |
|  │ her grades. I should get all classes first, then grades for each.      │  |
|  │ Actually, I can call get_grades directly with student_id.              │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ ACTION 2:                                                               │  |
|  │ education-mcp.get_grades(student_id="lucy-123", include_all=true)       │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ OBSERVATION 2:                                                          │  |
|  │ { "grades": [                                                           │  |
|  │   { "subject": "Math", "score": 85, "trend": "improving" },            │  |
|  │   { "subject": "Science", "score": 92, "trend": "stable" },            │  |
|  │   { "subject": "History", "score": 78, "trend": "declining" }          │  |
|  │ ]}                                                                      │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ THOUGHT 3:                                                              │  |
|  │ I have Lucy's grades. I notice History is declining at 78%.            │  |
|  │ The advice should focus on the declining subject while acknowledging   │  |
|  │ her strengths. I'll call advice-mcp with this context.                 │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ ACTION 3:                                                               │  |
|  │ advice-mcp.generate_advice(                                             │  |
|  │   student_data={ name: "Lucy", grades: [...] },                         │  |
|  │   focus_area="declining_subject",                                       │  |
|  │   style="encouraging"                                                   │  |
|  │ )                                                                       │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ OBSERVATION 3:                                                          │  |
|  │ { "advice": "Lucy is doing great in Science (92%)! Her Math is         │  |
|  │    improving too. For History, consider reviewing notes together        │  |
|  │    and discussing historical events as stories - this often helps       │  |
|  │    students who are visual learners." }                                 │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ THOUGHT 4:                                                              │  |
|  │ I have grades and advice. Now I should:                                │  |
|  │ 1. Format a voice-friendly response                                    │  |
|  │ 2. Store the fact that we discussed Lucy's grades in memory            │  |
|  │ 3. Note the declining History trend for future reference               │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                   │                                          |
|                                   ▼                                          |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ FINAL RESPONSE:                                                         │  |
|  │ "Lucy has Math at 85% and improving, Science at 92%, and History       │  |
|  │  at 78% which is declining. She's doing great in Science! For History, │  |
|  │  try reviewing notes together and discussing events as stories."       │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                                                              |
+------------------------------------------------------------------------------+

KEY BENEFITS OF ReAct:
  1. Explicit reasoning makes decisions transparent and debuggable
  2. Observations validate assumptions before proceeding
  3. Each thought can trigger re-planning if needed
  4. Audit trail of reasoning for quality improvement
```

### 5.2 Leader Agent System Prompt (v3 Enhanced)

```
+==============================================================================+
|                    LEADER AGENT SYSTEM PROMPT (v3)                            |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ ## ROLE                                                                      │
│                                                                              │
│ You are the Leader Agent for an educational assistant. Your job is to:      │
│ 1. Understand parent requests about their children's education              │
│ 2. Plan the steps needed to fulfill requests                                │
│ 3. Execute steps using MCP tools                                            │
│ 4. Synthesize information into helpful responses                            │
│                                                                              │
│ ## REASONING PATTERN (ReAct)                                                 │
│                                                                              │
│ For EVERY action, you MUST follow this pattern:                             │
│                                                                              │
│ THOUGHT: Explain what you're trying to do and why                           │
│ ACTION: Call a tool or make a decision                                      │
│ OBSERVATION: Analyze the result                                             │
│ THOUGHT: Decide next step based on observation                              │
│ ... (repeat until complete)                                                 │
│                                                                              │
│ ## DUAL LOOP BEHAVIOR                                                        │
│                                                                              │
│ OUTER LOOP (Planning):                                                       │
│ - Create Task Ledger with FACTS, GUESSES, PLAN                              │
│ - Re-plan when guesses are refuted or steps fail                            │
│                                                                              │
│ INNER LOOP (Execution):                                                      │
│ - Track progress in Progress Ledger                                          │
│ - Retry failed steps up to 3 times                                          │
│ - Escalate to re-planning if stuck                                          │
│                                                                              │
│ ## MEMORY USAGE                                                              │
│                                                                              │
│ Before calling education tools, ALWAYS:                                      │
│ 1. Check shared memory (Blackboard) for cached data                         │
│ 2. Verify active student context                                            │
│ 3. Look for relevant past interactions                                      │
│                                                                              │
│ After completing a request, ALWAYS:                                          │
│ 1. Store important new facts in memory                                       │
│ 2. Update Blackboard with task results                                       │
│ 3. Note any patterns worth remembering                                      │
│                                                                              │
│ ## AVAILABLE MCP TOOLS                                                       │
│ (Dynamically injected from MCP discovery)                                   │
│                                                                              │
│ ## CURRENT CONTEXT                                                           │
│ (Injected from Blackboard per request)                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Memory Scope Hierarchy

### 6.1 Four-Level Scope Structure

```
+==============================================================================+
|                    MEMORY SCOPE HIERARCHY (v3)                                |
+==============================================================================+

                    ┌───────────────────────────────────────────────────────┐
                    │                    GLOBAL SCOPE                        │
                    │  ════════════════════════════════                      │
                    │  • System knowledge (all users)                        │
                    │  • MCP tool definitions                                │
                    │  • Prompt templates                                    │
                    │  • School calendars, grade scales                      │
                    │                                                        │
                    │  TTL: Permanent                                        │
                    │  Access: Read-only for agents                          │
                    └───────────────────────────┬───────────────────────────┘
                                                │
                                                │ Inherits
                                                ▼
                    ┌───────────────────────────────────────────────────────┐
                    │                     USER SCOPE                         │
                    │  ═══════════════════════════════                       │
                    │  • User preferences (language, response style)        │
                    │  • Children profiles                                   │
                    │  • Cross-session conversation summaries               │
                    │  • Important facts (e.g., "Lucy struggles with Math") │
                    │                                                        │
                    │  TTL: Permanent (with periodic consolidation)          │
                    │  Access: Read/Write, user-isolated                     │
                    └───────────────────────────┬───────────────────────────┘
                                                │
                                                │ Inherits
                                                ▼
                    ┌───────────────────────────────────────────────────────┐
                    │                   SESSION SCOPE                        │
                    │  ════════════════════════════════                      │
                    │  • Current conversation history                        │
                    │  • Active student context                              │
                    │  • Recent tool results (cached)                        │
                    │  • Session-level decisions                             │
                    │                                                        │
                    │  TTL: 24 hours (or until explicit logout)              │
                    │  Access: Read/Write, session-isolated                  │
                    └───────────────────────────┬───────────────────────────┘
                                                │
                                                │ Inherits
                                                ▼
                    ┌───────────────────────────────────────────────────────┐
                    │                    AGENT SCOPE                         │
                    │  ═══════════════════════════════                       │
                    │  • Current task state (Task + Progress Ledger)        │
                    │  • Reasoning trace (Thoughts, Actions, Observations)  │
                    │  • Working context (in LLM prompt)                     │
                    │  • Intermediate tool results                           │
                    │                                                        │
                    │  TTL: Task lifetime                                     │
                    │  Access: Private to executing agent                     │
                    └───────────────────────────────────────────────────────┘

INHERITANCE RULE:
  Agent Scope can READ from Session → User → Global
  Agent Scope can WRITE to Session, User (with salience check)

EXAMPLE QUERY RESOLUTION:
  Query: "What is Lucy's preferred learning style?"

  1. Check AGENT scope → Not found (task-specific only)
  2. Check SESSION scope → Not found (not discussed this session)
  3. Check USER scope → Found! "Lucy is a visual learner"
  4. Return result

EXAMPLE WRITE OPERATION:
  New fact: "Tom got 95% on his Science test today"

  1. Salience check: Is this important long-term?
     - Yes: Write to USER scope (cross-session)
     - No: Write to SESSION scope only
  2. Update Blackboard (shared memory) for current task
```

### 6.2 Memory Scope Implementation

```
+==============================================================================+
|                    MEMORY SCOPE IMPLEMENTATION (v3)                           |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ interface MemoryScope {                                                      │
│   scope: 'global' | 'user' | 'session' | 'agent';                           │
│   parentScope?: MemoryScope;  // For inheritance                             │
│ }                                                                            │
│                                                                              │
│ interface MemoryItem {                                                       │
│   id: string;                                                                │
│   scope: MemoryScope;                                                        │
│   content: string;                                                           │
│   contentType: 'fact' | 'preference' | 'context' | 'artifact';              │
│   createdAt: Date;                                                           │
│   expiresAt?: Date;                                                          │
│   salience: number;  // 0.0 - 1.0, for prioritization                       │
│   tags: string[];    // For filtering                                        │
│   embedding?: number[];  // For semantic search                              │
│ }                                                                            │
│                                                                              │
│ class MemoryManager {                                                        │
│                                                                              │
│   async recall(query: string, options: RecallOptions): Promise<MemoryItem[]>{│
│     // Search in order: agent → session → user → global                     │
│     // Return merged, deduplicated results                                   │
│   }                                                                          │
│                                                                              │
│   async store(item: MemoryItem): Promise<void> {                            │
│     // Check salience to determine scope                                     │
│     // High salience (>0.8) → User scope (persistent)                       │
│     // Medium salience → Session scope                                       │
│     // Low salience → Agent scope only                                       │
│   }                                                                          │
│                                                                              │
│   async summarize(scope: MemoryScope): Promise<string> {                    │
│     // Generate compressed summary of scope contents                        │
│     // Used for context injection into LLM prompt                           │
│   }                                                                          │
│ }                                                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Blackboard Synchronization Pattern

### 7.1 Blackboard Architecture

```
+==============================================================================+
|                    BLACKBOARD SYNCHRONIZATION PATTERN (v3)                    |
+==============================================================================+

  PURPOSE: Central coordination hub for shared state in multi-agent context

  ┌─────────────────────────────────────────────────────────────────────────┐
  │                           BLACKBOARD                                     │
  │                        (Shared Memory Hub)                               │
  ├─────────────────────────────────────────────────────────────────────────┤
  │                                                                          │
  │  ┌───────────────────────────────────────────────────────────────────┐  │
  │  │  TASK STATE (from Task Ledger)                                    │  │
  │  │  ─────────────────────────────                                    │  │
  │  │  • taskId: "task-2026-01-12-001"                                  │  │
  │  │  • userRequest: "Show Lucy's grades and give advice"              │  │
  │  │  • status: "executing"                                            │  │
  │  │  • currentStep: 3 of 5                                            │  │
  │  └───────────────────────────────────────────────────────────────────┘  │
  │                                                                          │
  │  ┌───────────────────────────────────────────────────────────────────┐  │
  │  │  SHARED FACTS                                                      │  │
  │  │  ────────────                                                      │  │
  │  │  • activeStudent: { id: "lucy-123", name: "Lucy" }                │  │
  │  │  • fetchedGrades: { Math: 85, Science: 92, History: 78 }          │  │
  │  │  • parentPreferences: { language: "en", style: "encouraging" }    │  │
  │  └───────────────────────────────────────────────────────────────────┘  │
  │                                                                          │
  │  ┌───────────────────────────────────────────────────────────────────┐  │
  │  │  PENDING UPDATES (Event Queue)                                     │  │
  │  │  ─────────────────────────────                                     │  │
  │  │  [1] { type: "FACT_ADDED", data: { ... }, timestamp: ... }        │  │
  │  │  [2] { type: "STEP_COMPLETED", step: 3, result: { ... } }         │  │
  │  └───────────────────────────────────────────────────────────────────┘  │
  │                                                                          │
  └─────────────────────────────────────────────────────────────────────────┘
           ▲              ▲              ▲              ▲
           │ read         │ read/write   │ read         │ subscribe
           │              │              │              │
  ┌────────┴────┐  ┌──────┴──────┐  ┌────┴────────┐  ┌──┴──────────────┐
  │ Voice API   │  │ Leader      │  │ MCP         │  │ Monitoring      │
  │ (read task  │  │ Agent       │  │ Servers     │  │ (observes       │
  │  context)   │  │ (main       │  │ (read for   │  │  state changes) │
  │             │  │  controller)│  │  context)   │  │                 │
  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────────┘

SYNCHRONIZATION PATTERNS:

1. PUBLISH-SUBSCRIBE:
   ────────────────────
   Leader Agent publishes updates → Subscribers notified

   Example:
   - Leader publishes: { event: "STUDENT_SWITCHED", data: { to: "Tom" } }
   - Voice UI subscribes: Updates display to show Tom context
   - Memory MCP subscribes: Caches Tom's data preemptively

2. READ-MODIFY-WRITE (Atomic):
   ────────────────────────────
   For concurrent updates, use optimistic locking

   Example:
   - Leader reads: fetchedGrades = null
   - Leader fetches: grades from education-mcp
   - Leader writes: fetchedGrades = { ... } (with version check)

3. EVENT SOURCING:
   ─────────────────
   All changes logged as events for audit trail

   Example log:
   [2026-01-12T10:00:00Z] TASK_STARTED { taskId: "...", request: "..." }
   [2026-01-12T10:00:01Z] MEMORY_QUERIED { scope: "session", found: true }
   [2026-01-12T10:00:02Z] MCP_CALLED { tool: "get_grades", result: {...} }
   [2026-01-12T10:00:03Z] FACT_STORED { fact: "Lucy Math=85%", scope: "user" }
```

### 7.2 Blackboard Schema

```
+==============================================================================+
|                    BLACKBOARD DATA SCHEMA (v3)                                |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ interface Blackboard {                                                       │
│                                                                              │
│   // Core identification                                                     │
│   sessionId: string;                                                         │
│   userId: string;                                                            │
│                                                                              │
│   // Current task state                                                      │
│   taskState: {                                                               │
│     taskLedger: TaskLedger;                                                  │
│     progressLedger: ProgressLedger;                                          │
│     status: 'idle' | 'planning' | 'executing' | 'completed' | 'failed';     │
│   };                                                                         │
│                                                                              │
│   // Shared facts (accessible to all agents)                                 │
│   sharedFacts: Map<string, {                                                │
│     value: any;                                                              │
│     source: string;  // Which agent/tool set this                            │
│     timestamp: Date;                                                         │
│     confidence: number;                                                      │
│   }>;                                                                        │
│                                                                              │
│   // Shared artifacts (computed results)                                     │
│   artifacts: Map<string, {                                                  │
│     type: 'grades' | 'advice' | 'schedule' | 'report';                      │
│     data: any;                                                               │
│     generatedBy: string;                                                     │
│     timestamp: Date;                                                         │
│   }>;                                                                        │
│                                                                              │
│   // Event queue for pub/sub                                                 │
│   eventQueue: {                                                              │
│     eventId: string;                                                         │
│     eventType: string;                                                       │
│     payload: any;                                                            │
│     timestamp: Date;                                                         │
│     processed: boolean;                                                      │
│   }[];                                                                       │
│                                                                              │
│   // Concurrency control                                                     │
│   version: number;  // Incremented on every write                            │
│   lastModified: Date;                                                        │
│ }                                                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Complete System Architecture

### 8.1 v3 Stack Architecture

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    DEEP AGENT SYSTEM - v3 STACK ARCHITECTURE                      ║
+══════════════════════════════════════════════════════════════════════════════════+
║                                                                                   ║
║  USER LAYER                                                                       ║
║  ───────────                                                                      ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Browser (Chrome/Edge/Firefox)                                               │ ║
║  │  • voiceRealtime.js (Audio capture/playback)                                │ ║
║  │  • Blazor Server SignalR (UI state)                                         │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │                                             ║
║                                     ▼                                             ║
║  VOICE INTERFACE LAYER                                                            ║
║  ─────────────────────                                                            ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Azure OpenAI Realtime API                                                   │ ║
║  │  • Role: Voice I/O ONLY (STT, TTS, VAD)                                     │ ║
║  │  • Single tool: delegate_to_leader(user_message, context_summary)           │ ║
║  │  • Reads Blackboard for context summary to inject                           │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │                                             ║
║                                     ▼                                             ║
║  LEADER AGENT LAYER (v3 Enhanced)                                                 ║
║  ────────────────────────────────                                                 ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │                                                                              │ ║
║  │  ┌─────────────────────────────────────────────────────────────────────┐   │ ║
║  │  │                        LEADER AGENT                                  │   │ ║
║  │  │                   (GPT-4o / Claude with ReAct)                       │   │ ║
║  │  └───────────────────────────────┬─────────────────────────────────────┘   │ ║
║  │                                  │                                          │ ║
║  │         ┌────────────────────────┼────────────────────────┐                │ ║
║  │         │                        │                        │                │ ║
║  │         ▼                        ▼                        ▼                │ ║
║  │  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐          │ ║
║  │  │   OUTER LOOP    │   │   INNER LOOP    │   │   ReAct ENGINE  │          │ ║
║  │  │   (Strategic)   │   │   (Tactical)    │   │   (Reasoning)   │          │ ║
║  │  ├─────────────────┤   ├─────────────────┤   ├─────────────────┤          │ ║
║  │  │ Task Ledger:    │   │ Progress Ledger:│   │ Cycle:          │          │ ║
║  │  │ • FACTS         │   │ • Step tracking │   │ Thought →       │          │ ║
║  │  │ • GUESSES       │   │ • Retry logic   │   │ Action →        │          │ ║
║  │  │ • PLAN          │   │ • Stall detect  │   │ Observation →   │          │ ║
║  │  │                 │   │ • Escalation    │   │ Thought → ...   │          │ ║
║  │  └────────┬────────┘   └────────┬────────┘   └────────┬────────┘          │ ║
║  │           │                     │                     │                    │ ║
║  │           └─────────────────────┼─────────────────────┘                    │ ║
║  │                                 │                                          │ ║
║  └─────────────────────────────────┼──────────────────────────────────────────┘ ║
║                                    │                                             ║
║                                    ▼                                             ║
║  MEMORY LAYER (v3 Two-Tier)                                                       ║
║  ──────────────────────────                                                       ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │                                                                              │ ║
║  │  ┌─────────────────────────────────────────────────────────────────────┐   │ ║
║  │  │                         BLACKBOARD                                   │   │ ║
║  │  │                    (Shared Memory Hub)                               │   │ ║
║  │  │                                                                      │   │ ║
║  │  │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐  │   │ ║
║  │  │   │ Task State  │  │ Shared      │  │ Artifacts   │  │ Event     │  │   │ ║
║  │  │   │ (Ledgers)   │  │ Facts       │  │ (Results)   │  │ Queue     │  │   │ ║
║  │  │   └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘  │   │ ║
║  │  │                                                                      │   │ ║
║  │  └─────────────────────────────────────────────────────────────────────┘   │ ║
║  │                                                                              │ ║
║  │  ┌─────────────────────────────┐  ┌─────────────────────────────────────┐  │ ║
║  │  │    PRIVATE MEMORY           │  │     MEMORY SCOPE STORE              │  │ ║
║  │  │    (Per-Agent)              │  │     (Global → User → Session)       │  │ ║
║  │  │                             │  │                                      │  │ ║
║  │  │  • Leader Agent state       │  │  Global: System knowledge           │  │ ║
║  │  │  • Reasoning history        │  │  User: Preferences, profiles        │  │ ║
║  │  │  • Role-specific context    │  │  Session: Chat history, cache       │  │ ║
║  │  │                             │  │                                      │  │ ║
║  │  │  Storage: In-memory/Redis   │  │  Storage: Redis + Vector DB         │  │ ║
║  │  └─────────────────────────────┘  └─────────────────────────────────────┘  │ ║
║  │                                                                              │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │                                             ║
║                                     ▼                                             ║
║  MCP LAYER                                                                        ║
║  ─────────                                                                        ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │                                                                              │ ║
║  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │ ║
║  │  │ education-mcp   │  │  memory-mcp     │  │     advice-mcp             │  │ ║
║  │  ├─────────────────┤  ├─────────────────┤  ├─────────────────────────────┤  │ ║
║  │  │ Tools:          │  │ Tools:          │  │ Tools:                      │  │ ║
║  │  │ • get_grades    │  │ • recall        │  │ • generate_advice           │  │ ║
║  │  │ • get_classes   │  │ • store         │  │ • analyze_performance      │  │ ║
║  │  │ • get_schedule  │  │ • search        │  │ • suggest_resources        │  │ ║
║  │  │ • get_teachers  │  │ • summarize     │  │                             │  │ ║
║  │  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │ ║
║  │           │                    │                          │                  │ ║
║  └───────────┼────────────────────┼──────────────────────────┼──────────────────┘ ║
║              │                    │                          │                    ║
║              ▼                    ▼                          ▼                    ║
║  DATA LAYER                                                                       ║
║  ──────────                                                                       ║
║  ┌─────────────────┐  ┌─────────────────────────┐  ┌─────────────────────────────┐║
║  │ Microsoft Graph │  │   Redis + Qdrant        │  │    Azure OpenAI            ║║
║  │   Education API │  │   (Vector DB)           │  │    (GPT-4o-mini)           ║║
║  └─────────────────┘  └─────────────────────────┘  └─────────────────────────────┘║
║                                                                                   ║
+══════════════════════════════════════════════════════════════════════════════════+

DATA FLOW SUMMARY:
  ┌────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
  │ Voice  │───▶│ Realtime │───▶│  Leader  │───▶│   MCP    │───▶│   Data   │
  │ Input  │    │ (STT)    │    │  Agent   │    │ Servers  │    │ Sources  │
  └────────┘    └──────────┘    └────┬─────┘    └──────────┘    └──────────┘
                                     │
                  ┌──────────────────┼──────────────────┐
                  ▼                  ▼                  ▼
            ┌──────────┐      ┌──────────┐      ┌──────────┐
            │ Outer    │      │  Inner   │      │  Black-  │
            │ Loop     │      │  Loop    │      │  board   │
            │(Planning)│      │(Execute) │      │ (Shared) │
            └──────────┘      └──────────┘      └──────────┘

KEY METRICS (v3):
  Outer Loop: 1 per request    Inner Loop: N per plan step    Memory Tiers: 2
  MCP Servers: 3               Blackboard: 1 per session      Context: ~6K tokens
```

---

## 9. Workflow Diagrams (Improved)

### 9.1 Complete Voice Conversation Flow (v3)

```
+══════════════════════════════════════════════════════════════════════════════════+
║        WORKFLOW: VOICE CONVERSATION FLOW (v3 - WITH DUAL LOOP + ReAct)           ║
+══════════════════════════════════════════════════════════════════════════════════+

 Parent      Realtime     Leader Agent     Blackboard    MCP Servers    Memory
   │            │              │               │              │            │
   │ 1. Speak   │              │               │              │            │
   │ "Show me   │              │               │              │            │
   │  Lucy's    │              │               │              │            │
   │  grades    │              │               │              │            │
   │  and give  │              │               │              │            │
   │  advice"   │              │               │              │            │
   │ ───────────>              │               │              │            │
   │            │ 2. STT +     │               │              │            │
   │            │ delegate_to  │               │              │            │
   │            │ _leader      │               │              │            │
   │            │ ─────────────>               │              │            │
   │            │              │               │              │            │
   │            │              │ ══════════════════════════════════════════│
   │            │              │ ║        OUTER LOOP (Planning)           ║│
   │            │              │ ══════════════════════════════════════════│
   │            │              │               │              │            │
   │            │              │ 3. Read       │              │            │
   │            │              │    context    │              │            │
   │            │              │ ──────────────>              │            │
   │            │              │               │              │            │
   │            │              │ 4. Context:   │              │            │
   │            │              │    Lucy=active│              │            │
   │            │              │    parent=John│              │            │
   │            │              │ <──────────────              │            │
   │            │              │               │              │            │
   │            │              │ 5. Create Task Ledger:       │            │
   │            │              │    FACTS: Lucy active        │            │
   │            │              │    GUESSES: grades available │            │
   │            │              │    PLAN: 1) get_grades       │            │
   │            │              │          2) recall history   │            │
   │            │              │          3) generate_advice  │            │
   │            │              │               │              │            │
   │            │              │ 6. Write Task│              │            │
   │            │              │    Ledger     │              │            │
   │            │              │ ──────────────>              │            │
   │            │              │               │              │            │
   │            │              │ ══════════════════════════════════════════│
   │            │              │ ║        INNER LOOP (Execution)          ║│
   │            │              │ ══════════════════════════════════════════│
   │            │              │               │              │            │
   │            │              │ ┌─────────────────────────────────────────│
   │            │              │ │ THOUGHT 1: Need Lucy's grades first     │
   │            │              │ └─────────────────────────────────────────│
   │            │              │               │              │            │
   │            │              │ 7. ACTION 1:  │              │            │
   │            │              │    education-mcp.get_grades  │            │
   │            │              │ ─────────────────────────────>            │
   │            │              │               │              │            │
   │            │              │ 8. OBSERVATION 1:            │            │
   │            │              │    grades=[Math:85,Sci:92,His:78]        │
   │            │              │ <─────────────────────────────            │
   │            │              │               │              │            │
   │            │              │ 9. Update     │              │            │
   │            │              │    Progress   │              │            │
   │            │              │    Ledger     │              │            │
   │            │              │ ──────────────>              │            │
   │            │              │               │              │            │
   │            │              │ ┌─────────────────────────────────────────│
   │            │              │ │ THOUGHT 2: Got grades, need history     │
   │            │              │ │ from memory to provide context          │
   │            │              │ └─────────────────────────────────────────│
   │            │              │               │              │            │
   │            │              │ 10. ACTION 2: │              │            │
   │            │              │     memory-mcp.recall        │            │
   │            │              │ ─────────────────────────────────────────>│
   │            │              │               │              │            │
   │            │              │ 11. OBSERVATION 2:           │            │
   │            │              │     "Lucy struggles with History"         │
   │            │              │ <─────────────────────────────────────────│
   │            │              │               │              │            │
   │            │              │ ┌─────────────────────────────────────────│
   │            │              │ │ THOUGHT 3: Have grades + history.       │
   │            │              │ │ History declining matches memory.       │
   │            │              │ │ Now generate advice with this context.  │
   │            │              │ └─────────────────────────────────────────│
   │            │              │               │              │            │
   │            │              │ 12. ACTION 3: │              │            │
   │            │              │     advice-mcp.generate_advice            │
   │            │              │ ─────────────────────────────>            │
   │            │              │               │              │            │
   │            │              │ 13. OBSERVATION 3:           │            │
   │            │              │     "Lucy excels in Science! For History, │
   │            │              │      try study techniques..."             │
   │            │              │ <─────────────────────────────            │
   │            │              │               │              │            │
   │            │              │ 14. Update    │              │            │
   │            │              │     Blackboard│              │            │
   │            │              │     with      │              │            │
   │            │              │     results   │              │            │
   │            │              │ ──────────────>              │            │
   │            │              │               │              │            │
   │            │              │ 15. Store new facts in memory│            │
   │            │              │     "Discussed grades 2026-01-12"         │
   │            │              │ ─────────────────────────────────────────>│
   │            │              │               │              │            │
   │            │ 16. Response │               │              │            │
   │            │     Text     │               │              │            │
   │            │ <─────────────               │              │            │
   │            │              │               │              │            │
   │ 17. TTS +  │              │               │              │            │
   │ Hear       │              │               │              │            │
   │ response   │              │               │              │            │
   │ <───────────              │               │              │            │
   │            │              │               │              │            │

LATENCY BREAKDOWN (v3):
  Step 2-4:   ~200ms  (STT + Context read)
  Step 5-6:   ~100ms  (Task Ledger creation)
  Step 7-8:   ~300ms  (MCP + Graph API)
  Step 10-11: ~50ms   (Memory recall)
  Step 12-13: ~400ms  (Advice generation)
  Step 14-16: ~100ms  (Update + Response)
  Step 17:    ~300ms  (TTS)
  ─────────────────────────────────────
  Total:      ~1.5s   (Target: <2s)

IMPROVEMENT OVER v2:
  • Explicit reasoning (THOUGHT) makes decisions traceable
  • Progress tracking enables recovery from failures
  • Task Ledger ensures plan completeness
```

### 9.2 Self-Correction Flow (NEW in v3)

```
+══════════════════════════════════════════════════════════════════════════════════+
║              WORKFLOW: SELF-CORRECTION (When Things Go Wrong)                    ║
+══════════════════════════════════════════════════════════════════════════════════+

  Scenario: MCP tool fails, retry limit reached, need re-planning

  Leader Agent        Progress Ledger      Outer Loop         User
       │                    │                  │                │
       │ 1. ACTION:         │                  │                │
       │    education-mcp   │                  │                │
       │    .get_grades()   │                  │                │
       │ ───────────────────X (FAIL: timeout)  │                │
       │                    │                  │                │
       │ 2. Update retry    │                  │                │
       │    count: 1        │                  │                │
       │ ──────────────────>│                  │                │
       │                    │                  │                │
       │ 3. THOUGHT:        │                  │                │
       │    Tool failed.    │                  │                │
       │    Will retry.     │                  │                │
       │                    │                  │                │
       │ 4. ACTION: Retry   │                  │                │
       │ ───────────────────X (FAIL: timeout)  │                │
       │                    │                  │                │
       │ 5. Update retry    │                  │                │
       │    count: 2        │                  │                │
       │ ──────────────────>│                  │                │
       │                    │                  │                │
       │ 6. ACTION: Retry   │                  │                │
       │ ───────────────────X (FAIL: timeout)  │                │
       │                    │                  │                │
       │ 7. Update retry    │                  │                │
       │    count: 3        │                  │                │
       │ ──────────────────>│                  │                │
       │                    │                  │                │
       │                    │ 8. Stall         │                │
       │                    │    Detected!     │                │
       │                    │    retries >= 3  │                │
       │                    │ ─────────────────>                │
       │                    │                  │                │
       │                    │                  │ 9. OUTER LOOP: │
       │                    │                  │    Re-plan     │
       │                    │                  │                │
       │                    │                  │ New Plan:      │
       │                    │                  │ • Skip grades  │
       │                    │                  │ • Use cached   │
       │                    │                  │   data from    │
       │                    │                  │   memory       │
       │                    │                  │ • Inform user  │
       │                    │                  │   of partial   │
       │                    │                  │   results      │
       │                    │                  │                │
       │ 10. New Task       │                  │                │
       │     Ledger         │                  │                │
       │ <──────────────────────────────────────                │
       │                    │                  │                │
       │ 11. THOUGHT:       │                  │                │
       │     Plan changed.  │                  │                │
       │     Using cached   │                  │                │
       │     grades from    │                  │                │
       │     memory.        │                  │                │
       │                    │                  │                │
       │ 12. ACTION:        │                  │                │
       │     memory-mcp     │                  │                │
       │     .recall        │                  │                │
       │     ("Lucy grades")│                  │                │
       │ ────────────────────────────────────────────────> (SUCCESS)
       │                    │                  │                │
       │ 13. Response with  │                  │                │
       │     cached data +  │                  │                │
       │     disclaimer     │                  │                │
       │ ──────────────────────────────────────────────────────>│
       │                    │                  │                │
       │                    │                  │     "I couldn't│
       │                    │                  │      get the   │
       │                    │                  │      latest    │
       │                    │                  │      grades    │
       │                    │                  │      right now,│
       │                    │                  │      but based │
       │                    │                  │      on last   │
       │                    │                  │      week's    │
       │                    │                  │      data..."  │

KEY SELF-CORRECTION MECHANISMS:
  1. Retry with backoff (3 attempts default)
  2. Stall detection triggers re-planning
  3. Alternative strategies in Outer Loop
  4. Graceful degradation with cached data
  5. Transparent communication to user
```

---

## 10. Implementation Decision Framework

### 10.1 Technology Stack Decisions

```
+==============================================================================+
|                    IMPLEMENTATION DECISIONS (v3)                              |
+==============================================================================+

┌────────────────────────┬─────────────────────────────────────────────────────┐
│ Decision               │ Recommendation                                       │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ Memory Platform        │ Mem0 (Best benchmark scores: 66.9% accuracy,        │
│                        │ 91% token savings, production-ready)                │
│                        │                                                      │
│                        │ Alternative: Custom (Redis + Qdrant) if need        │
│                        │ more control                                        │
│                        │                                                      │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ Vector Database        │ Qdrant (Open-source, self-hostable, <10ms latency) │
│                        │                                                      │
│                        │ Alternative: Pinecone (managed) if prefer SaaS      │
│                        │                                                      │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ Blackboard Storage     │ Redis (In-memory, <5ms latency, pub/sub built-in)  │
│                        │                                                      │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ Leader Agent Model     │ GPT-4o (Best balance of speed + capability)         │
│                        │                                                      │
│                        │ Alternative: Claude 3.5 for complex reasoning       │
│                        │                                                      │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ MCP Server Runtime     │ .NET 8 (Matches existing codebase)                  │
│                        │                                                      │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ Summarization Strategy │ Contextual (Recent verbatim + older summarized)    │
│                        │                                                      │
│                        │ Every 10 turns → Summarize oldest 7                 │
│                        │ Keep recent 3 verbatim                              │
│                        │                                                      │
├────────────────────────┼─────────────────────────────────────────────────────┤
│                        │                                                      │
│ Memory Formation       │ Confidence-based update strategy                    │
│                        │                                                      │
│                        │ High confidence (>0.8) → Override existing          │
│                        │ Low confidence → Keep version history               │
│                        │                                                      │
└────────────────────────┴─────────────────────────────────────────────────────┘
```

### 10.2 Implementation Priority

```
+==============================================================================+
|                    IMPLEMENTATION PRIORITY ORDER                              |
+==============================================================================+

Phase 0: Foundation (Week 1-2)
────────────────────────────────
 ✅ P0-1: Set up Redis for Blackboard
 ✅ P0-2: Basic Leader Agent with ReAct prompt
 ✅ P0-3: Task Ledger / Progress Ledger data structures

Phase 1: Memory Layer (Week 3-4)
────────────────────────────────
 ⬜ P1-1: Two-tier memory (Private + Shared)
 ⬜ P1-2: Memory scope hierarchy (Global→User→Session→Agent)
 ⬜ P1-3: Integration with Qdrant for semantic search

Phase 2: MCP Infrastructure (Week 5-6)
──────────────────────────────────────
 ⬜ P2-1: education-mcp server
 ⬜ P2-2: memory-mcp server
 ⬜ P2-3: advice-mcp server
 ⬜ P2-4: OAuth 2.1 authentication

Phase 3: Leader Agent Enhancement (Week 7-8)
────────────────────────────────────────────
 ⬜ P3-1: Dual Loop (Outer + Inner) implementation
 ⬜ P3-2: Self-correction mechanism
 ⬜ P3-3: Stall detection and re-planning

Phase 4: Integration & Testing (Week 9-10)
──────────────────────────────────────────
 ⬜ P4-1: End-to-end voice conversation flow
 ⬜ P4-2: Context recovery testing
 ⬜ P4-3: Performance benchmarking
 ⬜ P4-4: Token efficiency validation
```

---

## 11. Verification Against Research

### 11.1 Magentic-One Pattern Compliance

```
+==============================================================================+
|            VERIFICATION: v3 vs MAGENTIC-ONE RESEARCH                          |
+==============================================================================+

┌─────────────────────────┬──────────────────┬──────────────────────────────────┐
│ Magentic-One Concept    │ v3 Implementation│ Evidence                          │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Dual Loop System        │ ✅ IMPLEMENTED   │ Section 3: Outer + Inner Loop    │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Task Ledger             │ ✅ IMPLEMENTED   │ Section 3.2: FACTS/GUESSES/PLAN  │
│ (FACTS, GUESSES, PLAN)  │                  │                                  │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Progress Ledger         │ ✅ IMPLEMENTED   │ Section 3.3: Step tracking,      │
│                         │                  │ completion status                 │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Self-Correction         │ ✅ IMPLEMENTED   │ Section 9.2: Stall detection,    │
│                         │                  │ re-planning workflow             │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ ReAct Pattern           │ ✅ IMPLEMENTED   │ Section 5.1: Thought→Action→     │
│                         │                  │ Observation cycle                 │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Chain-of-Thought        │ ✅ IMPLEMENTED   │ Section 5: Explicit THOUGHT      │
│                         │                  │ steps in ReAct                   │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Plan Quality Criteria   │ ✅ ADDRESSED     │ Task Ledger includes:            │
│                         │                  │ • Dependencies (stepDependsOn)   │
│                         │                  │ • Measurable progress (%)        │
│                         │                  │ • Fallback (re-planning)         │
└─────────────────────────┴──────────────────┴──────────────────────────────────┘
```

### 11.2 Memory Architecture Research Compliance

```
+==============================================================================+
|            VERIFICATION: v3 vs MEMORY RESEARCH                                |
+==============================================================================+

┌─────────────────────────┬──────────────────┬──────────────────────────────────┐
│ Research Concept        │ v3 Implementation│ Evidence                          │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Two-Tier Memory         │ ✅ IMPLEMENTED   │ Section 4: Private + Shared      │
│ (Private + Shared)      │                  │                                  │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Blackboard Pattern      │ ✅ IMPLEMENTED   │ Section 7: Centralized shared    │
│                         │                  │ state coordination               │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Memory Scope Hierarchy  │ ✅ IMPLEMENTED   │ Section 6: Global→User→Session→  │
│                         │                  │ Agent with inheritance           │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Contextual Summarization│ ✅ IMPLEMENTED   │ Section 10.1: Recent verbatim +  │
│                         │                  │ older summarized                 │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Memory Formation        │ ✅ IMPLEMENTED   │ Section 10.1: Confidence-based   │
│ (Fact Extraction)       │                  │ update strategy                  │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Hybrid Architecture     │ ✅ IMPLEMENTED   │ Section 8: Sliding window +      │
│                         │                  │ Memory formation + RAG           │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Pub/Sub Synchronization │ ✅ IMPLEMENTED   │ Section 7.1: Event queue in      │
│                         │                  │ Blackboard                       │
├─────────────────────────┼──────────────────┼──────────────────────────────────┤
│ Token Efficiency        │ ✅ IMPROVED      │ ~6K tokens (v3) vs ~8K (v2)      │
│                         │                  │ via better summarization         │
└─────────────────────────┴──────────────────┴──────────────────────────────────┘
```

### 11.3 Gap Summary: What v3 Adds Over v2

```
+==============================================================================+
|                    v3 IMPROVEMENTS SUMMARY                                    |
+==============================================================================+

FROM MAGENTIC-ONE RESEARCH:
────────────────────────────
  ✅ Dual Loop Architecture (Strategic + Tactical)
  ✅ Task Ledger with explicit FACTS, GUESSES, PLAN
  ✅ Progress Ledger with step-by-step tracking
  ✅ Self-Correction (stall detection, re-planning)
  ✅ ReAct Pattern (explicit reasoning before action)

FROM MEMORY RESEARCH:
─────────────────────
  ✅ Two-Tier Memory (clearer than three-tier)
  ✅ Blackboard Pattern for coordination
  ✅ Memory Scope Hierarchy (4 levels)
  ✅ Contextual Summarization strategy
  ✅ Memory Formation with confidence-based updates
  ✅ Pub/Sub event queue for state changes

OVERALL IMPROVEMENTS:
─────────────────────
  • More logical architecture (aligned with research)
  • Clearer separation of concerns
  • Explicit reasoning trail (debuggable)
  • Self-healing capabilities
  • Better token efficiency (~6K vs ~8K)
  • Production-ready patterns (Mem0 recommendations)
```

---

## 12. References

### 12.1 Research Documents

| Document | Key Concepts Used |
|----------|-------------------|
| [v1-explain-magentic-leader-plan.md](../research/v1-explain-magentic-leader-plan.md) | Dual Loop, Task Ledger, Progress Ledger, ReAct, Self-Correction |
| [v1-context-sharing-memory-architecture.md](../research/v1-context-sharing-memory-architecture.md) | Two-Tier Memory, Blackboard, Memory Scope, Summarization, Hybrid Architecture |

### 12.2 Previous Architecture Documents

| Document | Relationship |
|----------|--------------|
| [Over-v1-architecture-analysis.md](Over-v1-architecture-analysis.md) | Original v1 architecture (problem statement) |
| [Over-v2-deep-agent-architecture-redesign.md](Over-v2-deep-agent-architecture-redesign.md) | Initial redesign (foundation for v3) |
| [TASK-v1-deep-agent-system-tracker.md](TASK-v1-deep-agent-system-tracker.md) | Implementation tracking |

### 12.3 External References

| Source | Topic |
|--------|-------|
| [Magentic-One Paper](https://arxiv.org/abs/2411.04468) | Multi-agent orchestration patterns |
| [Mem0 Benchmark](https://mem0.ai/blog/benchmarked-openai-memory-vs-langmem-vs-memgpt-vs-mem0) | Memory platform comparison |
| [MCP Specification](https://spec.modelcontextprotocol.io/) | Model Context Protocol |
| [ReAct Paper](https://arxiv.org/abs/2210.03629) | Reasoning + Acting pattern |

---

## Appendix: Quick Reference

```
+==============================================================================+
|                    v3 ARCHITECTURE QUICK REFERENCE                            |
+==============================================================================+

DUAL LOOP SYSTEM:
  OUTER LOOP (Strategic)     INNER LOOP (Tactical)
  ────────────────────────   ────────────────────────────
  Task Ledger:               Progress Ledger:
  • FACTS (verified)         • Step status (pending/done)
  • GUESSES (assumptions)    • Retry count
  • PLAN (ordered steps)     • Stall detection

ReAct PATTERN:
  Thought → Action → Observation → Thought → ...
  (Explicit reasoning before every action)

TWO-TIER MEMORY:
  PRIVATE                    SHARED (Blackboard)
  ────────────────────────   ────────────────────────────
  • Agent-specific           • Task state
  • Role context             • Shared facts
  • Learned patterns         • Artifacts
                             • Event queue

MEMORY SCOPE HIERARCHY:
  Global → User → Session → Agent
  (Each level inherits from parent)

SELF-CORRECTION:
  1. Retry (3 attempts)
  2. Stall detection
  3. Escalate to Outer Loop
  4. Re-plan with alternatives
  5. Graceful degradation

KEY METRICS:
  Context Window: ~6K tokens
  Memory Tiers: 2 (Private + Shared)
  MCP Servers: 3
  Target Latency: <2s
```
