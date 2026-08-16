# Context Sharing & Memory Architecture for Chatbots and Multi-Agent Systems

**Version**: 1.0
**Created**: 2026-01-05
**Status**: Research Notes
**Related**:
- [v1-explain-magentic-leader-plan.md](./v1-explain-magentic-leader-plan.md)
- [v2-microsoft-agent-framework-use-case-guide.md](./v2-microsoft-agent-framework-use-case-guide.md)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [The Core Problem: Disconnected Models](#2-the-core-problem-disconnected-models)
3. [Memory Types Taxonomy (MECE)](#3-memory-types-taxonomy-mece)
4. [Context Sharing Approaches](#4-context-sharing-approaches)
5. [Multi-Agent Memory Architecture](#5-multi-agent-memory-architecture)
6. [Memory Management Techniques](#6-memory-management-techniques)
7. [Vector Database Comparison](#7-vector-database-comparison)
8. [Memory Platform Comparison](#8-memory-platform-comparison)
9. [Design Patterns by Use Case](#9-design-patterns-by-use-case)
10. [Implementation Decision Framework](#10-implementation-decision-framework)
11. [References](#11-references)

---

## 1. Executive Summary

### 1.1 Bản chất vấn đề

```
+===============================================================================+
|                    THE DISCONNECTED MODELS PROBLEM                             |
+===============================================================================+
|                                                                               |
|   "To be autonomous you have to carry context through a bunch of actions,    |
|    but the models are very disconnected and don't have continuity            |
|    the way we do."                                                           |
|                            — Sam Schillace, Microsoft (2025)                 |
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                      THE FUNDAMENTAL CHALLENGE                       │    |
|   │                                                                      │    |
|   │   LLM Context Window = Fixed Size (e.g., 32K, 128K tokens)          │    |
|   │                                                                      │    |
|   │   Problems:                                                          │    |
|   │   ├── Context window overflow → Old information lost                │    |
|   │   ├── Session isolation → No memory between sessions                │    |
|   │   ├── Cost increases linearly with context length                   │    |
|   │   └── Latency increases with longer prompts                         │    |
|   │                                                                      │    |
|   │   Human Analogy:                                                     │    |
|   │   ├── Working Memory = Context Window (limited, immediate)          │    |
|   │   ├── Long-term Memory = External Storage (unlimited, persistent)   │    |
|   │   └── Recall = Retrieval Mechanism (search & fetch relevant info)   │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 1.2 Key Insight

> **Memory Engineering là yếu tố then chốt phân biệt AI systems production-ready với prototypes.**
>
> "Companies succeeding with AI agents have figured out memory architecture, not just prompt engineering."

---

## 2. The Core Problem: Disconnected Models

### 2.1 Context Window Limitations

```
+===============================================================================+
|                    CONTEXT WINDOW VISUALIZATION                                |
+===============================================================================+
|                                                                               |
|   LLM CONTEXT WINDOW (e.g., GPT-4: 128K tokens)                              |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   ┌──────────────────────────────────────────────────────────────┐  │    |
|   │   │ System Prompt │ Chat History │ Current Query │ Tools/Context │  │    |
|   │   │    (Fixed)    │ (Growing!)   │   (Variable)  │   (Variable)  │  │    |
|   │   └──────────────────────────────────────────────────────────────┘  │    |
|   │                                                                      │    |
|   │   AS CONVERSATION GROWS:                                             │    |
|   │                                                                      │    |
|   │   Turn 1:  [System][History: 1 msg  ][Query][Tools]  ✅ Fits        │    |
|   │   Turn 10: [System][History: 10 msgs][Query][Tools]  ✅ Fits        │    |
|   │   Turn 50: [System][History: 50 msgs][Query][Tools]  ⚠️  Tight      │    |
|   │   Turn 100:[System][History: OVERFLOW!!! ][Query]    ❌ LOST DATA   │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   IMPACT METRICS:                                                             |
|   ┌───────────────┬───────────────┬───────────────┬───────────────┐          |
|   │ Context Size  │ Latency       │ Cost/Call     │ Quality       │          |
|   ├───────────────┼───────────────┼───────────────┼───────────────┤          |
|   │ 1K tokens     │ ~500ms        │ $0.01         │ High          │          |
|   │ 10K tokens    │ ~2s           │ $0.10         │ High          │          |
|   │ 50K tokens    │ ~8s           │ $0.50         │ Medium        │          |
|   │ 100K+ tokens  │ ~15s+         │ $1.00+        │ Degrading     │          |
|   └───────────────┴───────────────┴───────────────┴───────────────┘          |
|                                                                               |
+===============================================================================+
```

### 2.2 The Session Isolation Problem

```
+===============================================================================+
|                    SESSION ISOLATION PROBLEM                                   |
+===============================================================================+
|                                                                               |
|   SESSION 1 (Monday):                                                         |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   User: "Tôi thích màu xanh và phong cách minimalist"                │    |
|   │   Bot:  "Tôi đã ghi nhận sở thích của bạn..."                       │    |
|   │                                                                      │    |
|   │   [Context Window has this info]                                     │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                              │                                                |
|                              ▼ Session ends → Context ERASED                 |
|                                                                               |
|   SESSION 2 (Tuesday):                                                        |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   User: "Gợi ý màu sơn cho phòng khách"                             │    |
|   │   Bot:  "Có nhiều màu phổ biến như trắng, be, xám..."              │    |
|   │                                                                      │    |
|   │   ❌ Bot KHÔNG NHỚ user thích màu xanh!                             │    |
|   │   ❌ Bot KHÔNG NHỚ user thích minimalist!                           │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   WHAT USER EXPECTED:                                                         |
|   "Với sở thích minimalist và màu xanh của bạn, tôi gợi ý:                  |
|    - Xanh pastel nhạt cho tường chính                                        |
|    - Trắng cho các chi tiết..."                                              |
|                                                                               |
+===============================================================================+
```

---

## 3. Memory Types Taxonomy (MECE)

### 3.1 Memory Classification Framework

```
+===============================================================================+
|                    MEMORY TYPES TAXONOMY (MECE)                                |
+===============================================================================+
|                                                                               |
|   Based on Cognitive Psychology & AI Systems Design                          |
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                       MEMORY TYPES                                   │    |
|   │                                                                      │    |
|   │   BY DURATION:                                                       │    |
|   │   ┌─────────────────┬─────────────────┬─────────────────┐           │    |
|   │   │   SHORT-TERM    │   LONG-TERM     │   SEMANTIC      │           │    |
|   │   │   (Working)     │   (Persistent)  │   (Knowledge)   │           │    |
|   │   │                 │                 │                 │           │    |
|   │   │ • Current conv  │ • User prefs    │ • Vector DB     │           │    |
|   │   │ • Session state │ • Facts/History │ • Embeddings    │           │    |
|   │   │ • Temp vars     │ • Relationships │ • RAG retrieval │           │    |
|   │   │                 │                 │                 │           │    |
|   │   │ Lifecycle:      │ Lifecycle:      │ Lifecycle:      │           │    |
|   │   │ Session-based   │ Persistent      │ Indexed &       │           │    |
|   │   │ (in context)    │ (cross-session) │ Searchable      │           │    |
|   │   └─────────────────┴─────────────────┴─────────────────┘           │    |
|   │                                                                      │    |
|   │   BY FUNCTION (Cognitive Model):                                     │    |
|   │   ┌─────────────────┬─────────────────┬─────────────────┐           │    |
|   │   │   EPISODIC      │   PROCEDURAL    │   SEMANTIC      │           │    |
|   │   │   (Experiences) │   (How-to)      │   (Facts)       │           │    |
|   │   │                 │                 │                 │           │    |
|   │   │ • Past events   │ • Learned tasks │ • Concepts      │           │    |
|   │   │ • Interactions  │ • Workflows     │ • Definitions   │           │    |
|   │   │ • Context       │ • Skills        │ • Relationships │           │    |
|   │   └─────────────────┴─────────────────┴─────────────────┘           │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 3.2 Memory Type Deep Dive

| Memory Type | Bản chất | Storage | Retrieval | Use Case |
|-------------|----------|---------|-----------|----------|
| **Short-term** | Buffer cho conversation hiện tại | In-context (prompt) | Direct access | Chat continuity |
| **Long-term** | Facts và preferences persist | Key-value / Graph DB | Direct lookup | User personalization |
| **Semantic** | Knowledge được embed thành vectors | Vector DB | Similarity search | RAG, knowledge retrieval |
| **Episodic** | Past experiences và interactions | Time-series DB | Temporal query | Learning from history |
| **Procedural** | How-to knowledge, workflows | Structured storage | Pattern match | Task execution |

### 3.3 Memory Scope (Multi-Agent Context)

```
+===============================================================================+
|                    MEMORY SCOPE HIERARCHY                                      |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   GLOBAL MEMORY (System-wide)                                        │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • Shared knowledge base                                      │   │    |
|   │   │ • System configurations                                      │   │    |
|   │   │ • Cross-user analytics                                       │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                         │                                            │    |
|   │                         ▼                                            │    |
|   │   USER MEMORY (Per-user persistent)                                  │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • User preferences                                           │   │    |
|   │   │ • Historical interactions                                    │   │    |
|   │   │ • Personal knowledge                                         │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                         │                                            │    |
|   │                         ▼                                            │    |
|   │   SESSION MEMORY (Per-conversation)                                  │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • Current conversation context                               │   │    |
|   │   │ • Temporary state                                            │   │    |
|   │   │ • Active task tracking                                       │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                         │                                            │    |
|   │                         ▼                                            │    |
|   │   AGENT MEMORY (Per-agent in multi-agent)                           │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • Agent-specific knowledge                                   │   │    |
|   │   │ • Role-based context                                         │   │    |
|   │   │ • Task execution history                                     │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

---

## 4. Context Sharing Approaches

### 4.1 MECE Classification of Approaches

```
+===============================================================================+
|                    CONTEXT SHARING APPROACHES (MECE)                           |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   APPROACH 1: FULL CONTEXT (Naive)                                  │    |
|   │   ────────────────────────────────                                  │    |
|   │   Pass entire chat history in prompt                                │    |
|   │                                                                      │    |
|   │   APPROACH 2: SLIDING WINDOW                                        │    |
|   │   ────────────────────────────                                      │    |
|   │   Keep only last N messages                                         │    |
|   │                                                                      │    |
|   │   APPROACH 3: SUMMARIZATION                                         │    |
|   │   ──────────────────────────                                        │    |
|   │   Compress old context into summaries                               │    |
|   │                                                                      │    |
|   │   APPROACH 4: RAG (Retrieval Augmented Generation)                  │    |
|   │   ────────────────────────────────────────────                      │    |
|   │   Store in vector DB, retrieve relevant context                     │    |
|   │                                                                      │    |
|   │   APPROACH 5: MEMORY FORMATION                                      │    |
|   │   ─────────────────────────────                                     │    |
|   │   Extract key facts, store structured memory                        │    |
|   │                                                                      │    |
|   │   APPROACH 6: HIERARCHICAL MEMORY (MemGPT-style)                    │    |
|   │   ─────────────────────────────────────────────                     │    |
|   │   Multi-tier memory with paging mechanism                           │    |
|   │                                                                      │    |
|   │   APPROACH 7: HYBRID                                                │    |
|   │   ────────────────                                                  │    |
|   │   Combine multiple approaches                                       │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 4.2 Approach 1: Full Context (Naive)

```
+===============================================================================+
|                    APPROACH 1: FULL CONTEXT                                    |
+===============================================================================+
|                                                                               |
|   MECHANISM:                                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   User Query + ENTIRE Chat History → LLM → Response                 │    |
|   │                                                                      │    |
|   │   [Turn 1] + [Turn 2] + ... + [Turn N] + [Current Query]            │    |
|   │                    │                                                 │    |
|   │                    └── ALL included in prompt                        │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   PROS:                                  CONS:                                |
|   ┌─────────────────────────┐            ┌─────────────────────────┐         |
|   │ ✅ Complete context     │            │ ❌ Cost scales O(n)     │         |
|   │ ✅ No information loss  │            │ ❌ Latency increases    │         |
|   │ ✅ Simple implement     │            │ ❌ Context overflow     │         |
|   │ ✅ No external deps     │            │ ❌ Not scalable         │         |
|   └─────────────────────────┘            └─────────────────────────┘         |
|                                                                               |
|   BEST FOR:                                                                   |
|   • Short conversations (< 20 turns)                                         |
|   • Simple Q&A bots                                                          |
|   • Development/Testing                                                       |
|                                                                               |
|   TOKEN COST ANALYSIS:                                                        |
|   ┌────────────────┬──────────────┬──────────────┐                           |
|   │ Conversation   │ Tokens/Call  │ Cost (GPT-4) │                           |
|   ├────────────────┼──────────────┼──────────────┤                           |
|   │ 10 turns       │ ~2,000       │ $0.06        │                           |
|   │ 50 turns       │ ~10,000      │ $0.30        │                           |
|   │ 100 turns      │ ~20,000      │ $0.60        │                           |
|   │ 500 turns      │ ~100,000     │ $3.00        │                           |
|   └────────────────┴──────────────┴──────────────┘                           |
|                                                                               |
+===============================================================================+
```

### 4.3 Approach 2: Sliding Window

```
+===============================================================================+
|                    APPROACH 2: SLIDING WINDOW                                  |
+===============================================================================+
|                                                                               |
|   MECHANISM:                                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Keep only LAST N messages in context                              │    |
|   │                                                                      │    |
|   │   Full History: [T1][T2][T3][T4][T5][T6][T7][T8][T9][T10]           │    |
|   │                                                                      │    |
|   │   Window Size = 5:                                                   │    |
|   │                        ┌──────────────────────────┐                  │    |
|   │   Visible:            [T6][T7][T8][T9][T10]      │                  │    |
|   │                        └──────────────────────────┘                  │    |
|   │   Lost: [T1][T2][T3][T4][T5] ← GONE FOREVER                         │    |
|   │                                                                      │    |
|   │   AS CONVERSATION CONTINUES:                                         │    |
|   │                                                                      │    |
|   │   Turn 11:            [T7][T8][T9][T10][T11]    (T6 dropped)        │    |
|   │   Turn 12:            [T8][T9][T10][T11][T12]   (T7 dropped)        │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   PROS:                                  CONS:                                |
|   ┌─────────────────────────┐            ┌─────────────────────────┐         |
|   │ ✅ Fixed cost           │            │ ❌ Early context lost   │         |
|   │ ✅ Predictable latency  │            │ ❌ May lose important   │         |
|   │ ✅ Simple to implement  │            │    information          │         |
|   │ ✅ No external storage  │            │ ❌ Breaks multi-turn    │         |
|   └─────────────────────────┘            │    references           │         |
|                                          └─────────────────────────┘         |
|                                                                               |
|   WINDOW SIZE TRADE-OFFS:                                                     |
|   ┌────────────────┬──────────────────────┬───────────────────────┐          |
|   │ Window Size    │ Context Quality      │ Cost/Latency          │          |
|   ├────────────────┼──────────────────────┼───────────────────────┤          |
|   │ 3-5 messages   │ Poor (short memory)  │ Very Low              │          |
|   │ 10-15 messages │ Good (balanced)      │ Low                   │          |
|   │ 20-30 messages │ Excellent            │ Medium                │          |
|   │ 50+ messages   │ Overkill             │ High                  │          |
|   └────────────────┴──────────────────────┴───────────────────────┘          |
|                                                                               |
|   BEST FOR:                                                                   |
|   • Task-focused conversations                                                |
|   • Customer support (each ticket = new context)                             |
|   • When early context rarely matters                                         |
|                                                                               |
+===============================================================================+
```

### 4.4 Approach 3: Summarization

```
+===============================================================================+
|                    APPROACH 3: SUMMARIZATION                                   |
+===============================================================================+
|                                                                               |
|   MECHANISM:                                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Compress older messages into summaries                            │    |
|   │                                                                      │    |
|   │   BEFORE:                                                            │    |
|   │   [T1][T2][T3][T4][T5][T6][T7][T8][T9][T10] = 10 messages           │    |
|   │                                                                      │    |
|   │   AFTER:                                                             │    |
|   │   [SUMMARY of T1-T7] + [T8][T9][T10] = 1 summary + 3 recent         │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   SUMMARIZATION STRATEGIES:                                                   |
|                                                                               |
|   1. PERIODIC SUMMARIZATION:                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Every N messages → Generate summary → Replace old messages        │    |
|   │                                                                      │    |
|   │   [T1-T10] → "User discussed project timeline, agreed on dates"     │    |
|   │   [T11-T20] → "Reviewed technical requirements, chose React"        │    |
|   │                                                                      │    |
|   │   Final prompt: [Summary1][Summary2][Recent messages]               │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   2. RECURSIVE SUMMARIZATION:                                                 |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Level 1: Summarize each chunk                                     │    |
|   │   Level 2: Summarize the summaries                                  │    |
|   │   Level N: Continue until manageable size                           │    |
|   │                                                                      │    |
|   │   [T1-T10] → S1                                                     │    |
|   │   [T11-T20] → S2      →  [S1-S5] → Meta-Summary                     │    |
|   │   [T21-T30] → S3                                                    │    |
|   │   ...                                                                │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   3. HIERARCHICAL SUMMARIZATION:                                              |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Short-term summaries (recent, detailed)                           │    |
|   │          +                                                           │    |
|   │   Long-term summaries (older, high-level)                           │    |
|   │                                                                      │    |
|   │   Prompt = [Long-term Summary] + [Short-term Summary] + [Recent]    │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   PROS:                                  CONS:                                |
|   ┌─────────────────────────┐            ┌─────────────────────────┐         |
|   │ ✅ 60-70% token reduce  │            │ ❌ Detail loss          │         |
|   │ ✅ Preserves key info   │            │ ❌ Hallucination risk   │         |
|   │ ✅ Scalable to long     │            │ ❌ Extra LLM calls      │         |
|   │    conversations        │            │ ❌ Summarization cost   │         |
|   └─────────────────────────┘            └─────────────────────────┘         |
|                                                                               |
+===============================================================================+
```

### 4.5 Approach 4: RAG (Retrieval Augmented Generation)

```
+===============================================================================+
|                    APPROACH 4: RAG (SEMANTIC MEMORY)                           |
+===============================================================================+
|                                                                               |
|   MECHANISM:                                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Store conversations as embeddings → Retrieve relevant context     │    |
|   │                                                                      │    |
|   │   INDEXING PHASE (Offline):                                         │    |
|   │                                                                      │    |
|   │   Messages ──► Embedding ──► Vector Store                           │    |
|   │   [T1]         [0.23, -0.41, ...]    ┌─────────────┐               │    |
|   │   [T2]         [0.18, 0.55, ...]     │  Pinecone   │               │    |
|   │   [T3]         [-0.32, 0.11, ...]    │  Chroma     │               │    |
|   │                                       │  Weaviate   │               │    |
|   │                                       └─────────────┘               │    |
|   │                                                                      │    |
|   │   RETRIEVAL PHASE (Online):                                         │    |
|   │                                                                      │    |
|   │   User Query: "What color did I say I liked?"                       │    |
|   │        │                                                             │    |
|   │        ▼                                                             │    |
|   │   ┌────────────────┐                                                │    |
|   │   │ Embed Query    │ → [0.21, -0.39, 0.15, ...]                    │    |
|   │   └────────────────┘                                                │    |
|   │        │                                                             │    |
|   │        ▼                                                             │    |
|   │   ┌────────────────┐    ┌─────────────────────────────────────┐    │    |
|   │   │ Similarity     │ →  │ Top-K Results:                      │    │    |
|   │   │ Search         │    │ 1. "I like blue color" (0.92)       │    │    |
|   │   └────────────────┘    │ 2. "Blue is my favorite" (0.88)     │    │    |
|   │                         └─────────────────────────────────────┘    │    |
|   │        │                                                             │    |
|   │        ▼                                                             │    |
|   │   ┌────────────────┐                                                │    |
|   │   │ Augment Prompt │ → System + Retrieved Context + Query → LLM    │    |
|   │   └────────────────┘                                                │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   PROS:                                  CONS:                                |
|   ┌─────────────────────────┐            ┌─────────────────────────┐         |
|   │ ✅ Semantic search      │            │ ❌ Setup complexity     │         |
|   │ ✅ Scalable to millions │            │ ❌ Vector DB cost       │         |
|   │ ✅ Cross-session recall │            │ ❌ Embedding latency    │         |
|   │ ✅ Only relevant info   │            │ ❌ May miss context     │         |
|   └─────────────────────────┘            └─────────────────────────┘         |
|                                                                               |
+===============================================================================+
```

### 4.6 Approach 5: Memory Formation (Selective Facts)

```
+===============================================================================+
|                    APPROACH 5: MEMORY FORMATION                                |
+===============================================================================+
|                                                                               |
|   MECHANISM:                                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Extract KEY FACTS from conversations → Store structured memory    │    |
|   │   (Instead of storing everything)                                   │    |
|   │                                                                      │    |
|   │   CONVERSATION:                                                      │    |
|   │   User: "Tôi là developer, thích Python, làm việc ở Hà Nội"        │    |
|   │   User: "Dự án hiện tại dùng FastAPI và PostgreSQL"                 │    |
|   │   User: "Tôi thường làm việc buổi tối"                              │    |
|   │                                                                      │    |
|   │   EXTRACTED FACTS:                                                   │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ Memory Store:                                                │   │    |
|   │   │                                                              │   │    |
|   │   │ {                                                            │   │    |
|   │   │   "profession": "developer",                                 │   │    |
|   │   │   "preferred_language": "Python",                            │   │    |
|   │   │   "location": "Hanoi",                                       │   │    |
|   │   │   "current_stack": ["FastAPI", "PostgreSQL"],                │   │    |
|   │   │   "work_schedule": "evening"                                 │   │    |
|   │   │ }                                                            │   │    |
|   │   │                                                              │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                                                                      │    |
|   │   KEY INSIGHT:                                                       │    |
|   │   Memory Formation ≠ Summarization                                  │    |
|   │   • Summarization: Compress everything                              │    |
|   │   • Memory Formation: Select only important facts                   │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   PERFORMANCE (Mem0 Benchmark):                                               |
|   ┌────────────────────┬──────────────────┬──────────────────┐               |
|   │ Method             │ Token Savings    │ Quality          │               |
|   ├────────────────────┼──────────────────┼──────────────────┤               |
|   │ Full Context       │ 0%               │ Baseline         │               |
|   │ Summarization      │ 60-70%           │ -10% (detail loss)│              |
|   │ Memory Formation   │ 80-90%           │ +26% improvement │               |
|   └────────────────────┴──────────────────┴──────────────────┘               |
|                                                                               |
+===============================================================================+
```

### 4.7 Approach 6: Hierarchical Memory (MemGPT-style)

```
+===============================================================================+
|                    APPROACH 6: HIERARCHICAL MEMORY (MemGPT)                    |
+===============================================================================+
|                                                                               |
|   INSPIRATION: Operating System Memory Management                            |
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   OS ANALOGY:                                                        │    |
|   │                                                                      │    |
|   │   CPU Registers  ←→  Working Memory (Active Context)                │    |
|   │   RAM            ←→  Main Memory (Recent History)                   │    |
|   │   Hard Drive     ←→  Archive (Long-term Storage)                    │    |
|   │                                                                      │    |
|   │   PAGING: Swap data between tiers based on access patterns          │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   ARCHITECTURE:                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   TIER 1: WORKING MEMORY (In-context)                               │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • Currently active context                                   │   │    |
|   │   │ • System prompt + Recent turns                               │   │    |
|   │   │ • Size: Limited by context window                            │   │    |
|   │   │ • Access: Immediate (in prompt)                              │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                         │                                            │    |
|   │                         │ PAGE OUT (when full)                       │    |
|   │                         ▼                                            │    |
|   │   TIER 2: MAIN MEMORY (External, fast retrieval)                    │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • Recent conversation history                                │   │    |
|   │   │ • Frequently accessed facts                                  │   │    |
|   │   │ • Size: Configurable (KB to MB)                             │   │    |
|   │   │ • Access: Fast (in-memory DB)                               │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                         │                                            │    |
|   │                         │ ARCHIVE (cold data)                        │    |
|   │                         ▼                                            │    |
|   │   TIER 3: ARCHIVE (Persistent, searchable)                          │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │ • Full conversation history                                  │   │    |
|   │   │ • Document storage                                           │   │    |
|   │   │ • Size: Unlimited                                            │   │    |
|   │   │ • Access: Retrieval-based (vector search)                   │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                                                                      │    |
|   │   LLM CAN:                                                           │    |
|   │   • Request data from lower tiers (PAGE IN)                         │    |
|   │   • Push data to lower tiers (PAGE OUT)                             │    |
|   │   • Search across all tiers                                         │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   FUNCTION CALLS (LLM-initiated):                                             |
|   ┌────────────────────┬──────────────────────────────────────┐              |
|   │ Function           │ Description                          │              |
|   ├────────────────────┼──────────────────────────────────────┤              |
|   │ core_memory_read   │ Read from working memory             │              |
|   │ core_memory_write  │ Write to working memory              │              |
|   │ recall_memory      │ Search main memory                   │              |
|   │ archival_search    │ Search archive                       │              |
|   │ archival_insert    │ Store to archive                     │              |
|   └────────────────────┴──────────────────────────────────────┘              |
|                                                                               |
+===============================================================================+
```

### 4.8 Approach 7: Hybrid (Production Recommended)

```
+===============================================================================+
|                    APPROACH 7: HYBRID ARCHITECTURE                             |
+===============================================================================+
|                                                                               |
|   COMBINE BEST OF ALL APPROACHES:                                             |
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │                    USER QUERY                                        │    |
|   │                        │                                             │    |
|   │                        ▼                                             │    |
|   │   ┌────────────────────────────────────────────────────────────┐    │    |
|   │   │              MEMORY ORCHESTRATOR                            │    │    |
|   │   └────────────────────────────────────────────────────────────┘    │    |
|   │          │              │              │              │              │    |
|   │          ▼              ▼              ▼              ▼              │    |
|   │   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐        │    |
|   │   │ SLIDING  │   │ SUMMARY  │   │   RAG    │   │ MEMORY   │        │    |
|   │   │ WINDOW   │   │  BUFFER  │   │ RETRIEVAL│   │  STORE   │        │    |
|   │   │          │   │          │   │          │   │          │        │    |
|   │   │ Last 10  │   │ Older    │   │ Semantic │   │ Key      │        │    |
|   │   │ messages │   │ context  │   │ search   │   │ facts    │        │    |
|   │   └──────────┘   └──────────┘   └──────────┘   └──────────┘        │    |
|   │          │              │              │              │              │    |
|   │          └──────────────┴──────────────┴──────────────┘              │    |
|   │                              │                                       │    |
|   │                              ▼                                       │    |
|   │   ┌────────────────────────────────────────────────────────────┐    │    |
|   │   │                    CONTEXT ASSEMBLER                        │    │    |
|   │   │                                                             │    │    |
|   │   │   FINAL PROMPT =                                            │    │    |
|   │   │     [System Prompt]                                         │    │    |
|   │   │   + [User Memory: preferences, facts]                       │    │    |
|   │   │   + [Retrieved Context: relevant history]                   │    │    |
|   │   │   + [Summary: older conversation]                           │    │    |
|   │   │   + [Recent Messages: sliding window]                       │    │    |
|   │   │   + [Current Query]                                         │    │    |
|   │   │                                                             │    │    |
|   │   └────────────────────────────────────────────────────────────┘    │    |
|   │                              │                                       │    |
|   │                              ▼                                       │    |
|   │                            LLM                                       │    |
|   │                              │                                       │    |
|   │                              ▼                                       │    |
|   │                          RESPONSE                                    │    |
|   │                              │                                       │    |
|   │                              ▼                                       │    |
|   │   ┌────────────────────────────────────────────────────────────┐    │    |
|   │   │                   MEMORY UPDATER                            │    │    |
|   │   │                                                             │    │    |
|   │   │   • Extract new facts → Memory Store                        │    │    |
|   │   │   • Embed conversation → Vector DB                          │    │    |
|   │   │   • Update summaries if threshold reached                   │    │    |
|   │   │                                                             │    │    |
|   │   └────────────────────────────────────────────────────────────┘    │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 4.9 Comparison Matrix

```
+===============================================================================+
|                    CONTEXT SHARING APPROACHES COMPARISON                       |
+===============================================================================+
|                                                                               |
|   ┌─────────────┬────────┬────────┬────────┬──────────┬────────┬──────────┐  |
|   │ Approach    │ Cost   │Latency │Quality │Complexity│Scalable│Cross-Sess│  |
|   ├─────────────┼────────┼────────┼────────┼──────────┼────────┼──────────┤  |
|   │Full Context │ High   │ High   │ Best   │ Low      │ No     │ No       │  |
|   │Sliding Win  │ Low    │ Low    │ Poor   │ Low      │ Yes    │ No       │  |
|   │Summarize    │ Medium │ Medium │ Good   │ Medium   │ Yes    │ Partial  │  |
|   │RAG          │ Medium │ Medium │ Good   │ High     │ Yes    │ Yes      │  |
|   │Memory Form  │ Low    │ Low    │ Great  │ Medium   │ Yes    │ Yes      │  |
|   │Hierarchical │ Medium │ Medium │ Great  │ High     │ Yes    │ Yes      │  |
|   │Hybrid       │ Medium │ Medium │ Best   │ High     │ Yes    │ Yes      │  |
|   └─────────────┴────────┴────────┴────────┴──────────┴────────┴──────────┘  |
|                                                                               |
|   LEGEND:                                                                     |
|   • Cost: Token/API cost per interaction                                     |
|   • Latency: Response time                                                   |
|   • Quality: Context accuracy and completeness                               |
|   • Complexity: Implementation difficulty                                     |
|   • Scalable: Handles long conversations                                     |
|   • Cross-Sess: Remembers across sessions                                    |
|                                                                               |
+===============================================================================+
```

---

## 5. Multi-Agent Memory Architecture

### 5.1 Memory Hosting Patterns

```
+===============================================================================+
|                    MULTI-AGENT MEMORY HOSTING PATTERNS                         |
+===============================================================================+
|                                                                               |
|   PATTERN 1: PER-AGENT HOSTING                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Each agent maintains its own isolated memory                       │    |
|   │                                                                      │    |
|   │   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐               │    |
|   │   │  Agent A    │   │  Agent B    │   │  Agent C    │               │    |
|   │   │ ┌─────────┐ │   │ ┌─────────┐ │   │ ┌─────────┐ │               │    |
|   │   │ │ Memory  │ │   │ │ Memory  │ │   │ │ Memory  │ │               │    |
|   │   │ └─────────┘ │   │ └─────────┘ │   │ └─────────┘ │               │    |
|   │   └─────────────┘   └─────────────┘   └─────────────┘               │    |
|   │                                                                      │    |
|   │   PROS: Simple, no sync needed, clear ownership                     │    |
|   │   CONS: No knowledge sharing, duplication, silos                    │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 2: ORCHESTRATOR-LEVEL HOSTING (Blackboard Pattern)                 |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Central orchestrator holds shared memory                          │    |
|   │                                                                      │    |
|   │                    ┌─────────────────────┐                           │    |
|   │                    │    ORCHESTRATOR     │                           │    |
|   │                    │  ┌───────────────┐  │                           │    |
|   │                    │  │ SHARED MEMORY │  │                           │    |
|   │                    │  │ (Blackboard)  │  │                           │    |
|   │                    │  └───────────────┘  │                           │    |
|   │                    └─────────┬───────────┘                           │    |
|   │               ┌──────────────┼──────────────┐                        │    |
|   │               │              │              │                        │    |
|   │               ▼              ▼              ▼                        │    |
|   │        ┌──────────┐  ┌──────────┐  ┌──────────┐                     │    |
|   │        │ Agent A  │  │ Agent B  │  │ Agent C  │                     │    |
|   │        │ (no mem) │  │ (no mem) │  │ (no mem) │                     │    |
|   │        └──────────┘  └──────────┘  └──────────┘                     │    |
|   │                                                                      │    |
|   │   PROS: Single source of truth, easy sync                           │    |
|   │   CONS: Single point of failure, bottleneck                         │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 3: EXTERNAL HOSTING (Shared Database)                              |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   All agents query shared external storage                          │    |
|   │                                                                      │    |
|   │   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐               │    |
|   │   │  Agent A    │   │  Agent B    │   │  Agent C    │               │    |
|   │   └──────┬──────┘   └──────┬──────┘   └──────┬──────┘               │    |
|   │          │                 │                 │                       │    |
|   │          └─────────────────┼─────────────────┘                       │    |
|   │                            │                                         │    |
|   │                            ▼                                         │    |
|   │              ┌─────────────────────────────┐                         │    |
|   │              │     EXTERNAL MEMORY         │                         │    |
|   │              │  ┌─────────┐ ┌─────────┐   │                         │    |
|   │              │  │Vector DB│ │Graph DB │   │                         │    |
|   │              │  └─────────┘ └─────────┘   │                         │    |
|   │              └─────────────────────────────┘                         │    |
|   │                                                                      │    |
|   │   PROS: Scalable, persistent, queryable by any agent               │    |
|   │   CONS: Latency, complexity, eventual consistency                  │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 5.2 Two-Tier Memory Architecture (Recommended for Multi-Agent)

```
+===============================================================================+
|                    TWO-TIER MEMORY ARCHITECTURE                                |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   TIER 1: PRIVATE MEMORY (Per-agent)                                │    |
|   │   ────────────────────────────────────                              │    |
|   │   • Agent-specific knowledge                                        │    |
|   │   • Task execution history                                          │    |
|   │   • Role-based context                                              │    |
|   │   • Sensitive information                                           │    |
|   │                                                                      │    |
|   │   ┌───────────────┐   ┌───────────────┐   ┌───────────────┐        │    |
|   │   │   CODER       │   │   REVIEWER    │   │    TESTER     │        │    |
|   │   │   AGENT       │   │    AGENT      │   │    AGENT      │        │    |
|   │   │ ┌───────────┐ │   │ ┌───────────┐ │   │ ┌───────────┐ │        │    |
|   │   │ │ Private:  │ │   │ │ Private:  │ │   │ │ Private:  │ │        │    |
|   │   │ │ - Coding  │ │   │ │ - Review  │ │   │ │ - Test    │ │        │    |
|   │   │ │   patterns│ │   │ │   criteria│ │   │ │   results │ │        │    |
|   │   │ │ - Errors  │ │   │ │ - Past    │ │   │ │ - Coverage│ │        │    |
|   │   │ │   made    │ │   │ │   reviews │ │   │ │   data    │ │        │    |
|   │   │ └───────────┘ │   │ └───────────┘ │   │ └───────────┘ │        │    |
|   │   └───────┬───────┘   └───────┬───────┘   └───────┬───────┘        │    |
|   │           │                   │                   │                 │    |
|   │           └───────────────────┼───────────────────┘                 │    |
|   │                               │                                     │    |
|   │                               ▼                                     │    |
|   │   TIER 2: SHARED MEMORY (Team-wide)                                │    |
|   │   ────────────────────────────────                                 │    |
|   │   ┌─────────────────────────────────────────────────────────────┐  │    |
|   │   │                     SHARED MEMORY                            │  │    |
|   │   │                                                              │  │    |
|   │   │   • Project context (requirements, architecture)             │  │    |
|   │   │   • Current task state (TASK LEDGER from Magentic-One)      │  │    |
|   │   │   • Team decisions and agreements                            │  │    |
|   │   │   • Shared artifacts (code, documents)                       │  │    |
|   │   │                                                              │  │    |
|   │   └─────────────────────────────────────────────────────────────┘  │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 5.3 Synchronization Patterns

```
+===============================================================================+
|                    MEMORY SYNCHRONIZATION PATTERNS                             |
+===============================================================================+
|                                                                               |
|   PATTERN 1: PUBLISH-SUBSCRIBE                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Agents subscribe to memory topics, get notified on updates        │    |
|   │                                                                      │    |
|   │                    ┌─────────────────┐                               │    |
|   │                    │   EVENT BUS     │                               │    |
|   │                    │   (Pub/Sub)     │                               │    |
|   │                    └────────┬────────┘                               │    |
|   │          ┌─────────────────┼─────────────────┐                      │    |
|   │          │                 │                 │                      │    |
|   │          ▼                 ▼                 ▼                      │    |
|   │   Subscribe to:     Subscribe to:     Subscribe to:                │    |
|   │   - "plan_updates"  - "code_changes"  - "test_results"             │    |
|   │                                                                      │    |
|   │   USE CASE: Real-time collaboration, status updates                 │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 2: MESSAGE PASSING                                                  |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Agents explicitly pass context through messages                   │    |
|   │                                                                      │    |
|   │   Agent A ────[message + context]────► Agent B                     │    |
|   │                                                                      │    |
|   │   Message: {                                                         │    |
|   │     "task": "review this code",                                     │    |
|   │     "context": {                                                    │    |
|   │       "code": "...",                                                │    |
|   │       "requirements": "...",                                        │    |
|   │       "previous_feedback": "..."                                    │    |
|   │     }                                                                │    |
|   │   }                                                                  │    |
|   │                                                                      │    |
|   │   USE CASE: Sequential pipelines, explicit handoffs                 │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 3: BLACKBOARD (Shared State)                                       |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   All agents read/write to central shared state                     │    |
|   │                                                                      │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │                      BLACKBOARD                              │   │    |
|   │   │                                                              │   │    |
|   │   │   current_task: "implement feature X"                        │   │    |
|   │   │   status: "in_progress"                                      │   │    |
|   │   │   artifacts: [code.py, tests.py]                             │   │    |
|   │   │   decisions: ["use React", "REST API"]                       │   │    |
|   │   │                                                              │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │        ▲              ▲              ▲                              │    |
|   │        │ read/write   │ read/write   │ read/write                  │    |
|   │   ┌────┴────┐    ┌────┴────┐    ┌────┴────┐                        │    |
|   │   │ Agent A │    │ Agent B │    │ Agent C │                        │    |
|   │   └─────────┘    └─────────┘    └─────────┘                        │    |
|   │                                                                      │    |
|   │   USE CASE: Complex problem solving, dynamic collaboration          │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

---

## 6. Memory Management Techniques

### 6.1 Sliding Window Configuration

```
+===============================================================================+
|                    SLIDING WINDOW BEST PRACTICES                               |
+===============================================================================+
|                                                                               |
|   WINDOW SIZE RECOMMENDATIONS:                                                |
|                                                                               |
|   ┌────────────────────┬─────────────┬──────────────────────────────────┐    |
|   │ Use Case           │ Window Size │ Reasoning                        │    |
|   ├────────────────────┼─────────────┼──────────────────────────────────┤    |
|   │ Customer Support   │ 6-8 turns   │ Most issues resolve quickly      │    |
|   │ Task Assistant     │ 10-12 turns │ Need task context continuity     │    |
|   │ Code Assistant     │ 15-20 turns │ Code context is verbose          │    |
|   │ Conversation Bot   │ 8-10 turns  │ Balance memory vs relevance      │    |
|   │ Research Assistant │ 20-30 turns │ Complex multi-step research      │    |
|   └────────────────────┴─────────────┴──────────────────────────────────┘    |
|                                                                               |
|   IMPLEMENTATION PATTERN:                                                     |
|                                                                               |
|   ```python                                                                   |
|   class SlidingWindowMemory:                                                  |
|       def __init__(self, window_size: int = 10):                             |
|           self.window_size = window_size                                      |
|           self.messages = []                                                  |
|                                                                               |
|       def add_message(self, message: dict):                                   |
|           self.messages.append(message)                                       |
|           # Keep only last N messages                                         |
|           if len(self.messages) > self.window_size:                          |
|               self.messages = self.messages[-self.window_size:]              |
|                                                                               |
|       def get_context(self) -> list:                                         |
|           return self.messages                                                |
|   ```                                                                         |
|                                                                               |
+===============================================================================+
```

### 6.2 Summarization Strategies

```
+===============================================================================+
|                    SUMMARIZATION IMPLEMENTATION                                |
+===============================================================================+
|                                                                               |
|   STRATEGY 1: PERIODIC SUMMARIZATION                                         |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Trigger: Every N messages (e.g., every 10 turns)                  │    |
|   │                                                                      │    |
|   │   ```python                                                          │    |
|   │   class PeriodicSummarizer:                                          │    |
|   │       def __init__(self, llm, summarize_every: int = 10):           │    |
|   │           self.summarize_every = summarize_every                     │    |
|   │           self.messages = []                                         │    |
|   │           self.summaries = []                                        │    |
|   │                                                                      │    |
|   │       def add_message(self, msg):                                    │    |
|   │           self.messages.append(msg)                                  │    |
|   │           if len(self.messages) >= self.summarize_every:            │    |
|   │               summary = self.llm.summarize(self.messages)           │    |
|   │               self.summaries.append(summary)                         │    |
|   │               self.messages = []  # Clear after summarizing         │    |
|   │                                                                      │    |
|   │       def get_context(self):                                         │    |
|   │           return self.summaries + self.messages                     │    |
|   │   ```                                                                │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   STRATEGY 2: CONTEXTUAL SUMMARIZATION                                       |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   Keep recent messages verbatim, summarize older                    │    |
|   │                                                                      │    |
|   │   [SUMMARY of msgs 1-20] + [Verbatim msgs 21-30]                    │    |
|   │                                                                      │    |
|   │   Benefits:                                                          │    |
|   │   • Recent context preserved exactly                                │    |
|   │   • Older context compressed but available                          │    |
|   │   • Balances detail vs token efficiency                             │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   SUMMARIZATION PROMPT TEMPLATE:                                              |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   "Summarize the following conversation, preserving:                 │    |
|   │    - Key decisions made                                              │    |
|   │    - Important facts mentioned                                       │    |
|   │    - User preferences expressed                                      │    |
|   │    - Open questions or unresolved issues                            │    |
|   │                                                                      │    |
|   │    Keep the summary concise (max 200 words) but informative.        │    |
|   │                                                                      │    |
|   │    Conversation:                                                     │    |
|   │    {messages}"                                                       │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 6.3 Memory Formation (Fact Extraction)

```
+===============================================================================+
|                    MEMORY FORMATION PATTERN                                    |
+===============================================================================+
|                                                                               |
|   EXTRACTION PROMPT:                                                          |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   "From this conversation, extract important facts about the user:  │    |
|   │                                                                      │    |
|   │    Categories to look for:                                          │    |
|   │    - Personal info (name, location, profession)                     │    |
|   │    - Preferences (likes, dislikes, style)                           │    |
|   │    - Context (current project, goals, constraints)                  │    |
|   │    - Decisions made                                                  │    |
|   │                                                                      │    |
|   │    Return as JSON:                                                   │    |
|   │    {                                                                 │    |
|   │      'facts': [                                                      │    |
|   │        {'category': '...', 'fact': '...', 'confidence': 0.9}        │    |
|   │      ]                                                               │    |
|   │    }"                                                                │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   MEMORY UPDATE STRATEGIES:                                                   |
|                                                                               |
|   ┌─────────────────┬────────────────────────────────────────────────────┐   |
|   │ Strategy        │ Description                                        │   |
|   ├─────────────────┼────────────────────────────────────────────────────┤   |
|   │ ADDITIVE        │ Always add new facts                               │   |
|   │ OVERRIDE        │ New facts replace old if conflict                  │   |
|   │ VERSIONED       │ Keep history of fact changes                       │   |
|   │ CONFIDENCE-BASED│ Only update if new fact has higher confidence      │   |
|   └─────────────────┴────────────────────────────────────────────────────┘   |
|                                                                               |
+===============================================================================+
```

---

## 7. Vector Database Comparison

### 7.1 Database Selection Matrix

```
+===============================================================================+
|                    VECTOR DATABASE COMPARISON (2025)                           |
+===============================================================================+
|                                                                               |
|   ┌───────────┬────────────┬────────────┬────────────┬────────────┬────────┐ |
|   │ Database  │ Type       │ Best For   │ Scale      │ Latency    │ Cost   │ |
|   ├───────────┼────────────┼────────────┼────────────┼────────────┼────────┤ |
|   │ Pinecone  │ Managed    │ Production │ Billions   │ <100ms     │ $$$    │ |
|   │           │ Cloud      │ RAG        │ vectors    │            │        │ |
|   ├───────────┼────────────┼────────────┼────────────┼────────────┼────────┤ |
|   │ Weaviate  │ Open-source│ Hybrid     │ Millions   │ <50ms      │ $$     │ |
|   │           │ Self-host  │ Search     │ vectors    │            │        │ |
|   ├───────────┼────────────┼────────────┼────────────┼────────────┼────────┤ |
|   │ Milvus    │ Open-source│ Enterprise │ Billions   │ <10ms      │ $$     │ |
|   │           │ Distributed│ Scale      │ vectors    │            │        │ |
|   ├───────────┼────────────┼────────────┼────────────┼────────────┼────────┤ |
|   │ ChromaDB  │ Open-source│ Prototypes │ Millions   │ <20ms      │ $      │ |
|   │           │ Embedded   │ Quick Start│ vectors    │            │        │ |
|   ├───────────┼────────────┼────────────┼────────────┼────────────┼────────┤ |
|   │ Qdrant    │ Open-source│ Filtering  │ Millions   │ <10ms      │ $      │ |
|   │           │ Self-host  │ + Search   │ vectors    │            │        │ |
|   ├───────────┼────────────┼────────────┼────────────┼────────────┼────────┤ |
|   │ Redis     │ In-memory  │ Real-time  │ Millions   │ <5ms       │ $$     │ |
|   │           │ (RediSearch│ Low latency│ vectors    │            │        │ |
|   └───────────┴────────────┴────────────┴────────────┴────────────┴────────┘ |
|                                                                               |
|   RECOMMENDATION BY USE CASE:                                                 |
|                                                                               |
|   • STARTUP/PROTOTYPE: ChromaDB (simple, fast setup)                         |
|   • PRODUCTION SaaS: Pinecone (managed, reliable)                            |
|   • ENTERPRISE: Milvus or Weaviate (self-hosted, control)                    |
|   • REAL-TIME: Redis (lowest latency)                                        |
|   • HYBRID SEARCH: Weaviate (vector + keyword)                               |
|                                                                               |
+===============================================================================+
```

---

## 8. Memory Platform Comparison

### 8.1 Platform Benchmark (2025)

```
+===============================================================================+
|                    MEMORY PLATFORM BENCHMARK                                   |
+===============================================================================+
|                                                                               |
|   SOURCE: Mem0 AI Memory Benchmark (2025)                                    |
|                                                                               |
|   ┌─────────────┬──────────┬──────────┬──────────┬──────────┬──────────────┐ |
|   │ Platform    │ Accuracy │ Latency  │ Token    │ Multi-hop│ Best For     │ |
|   │             │          │ (p95)    │ Savings  │ Queries  │              │ |
|   ├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────────┤ |
|   │ Mem0        │ 66.9%    │ 1.4s     │ 91%      │ Good     │ Production   │ |
|   │             │          │          │          │          │ AI Agents    │ |
|   ├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────────┤ |
|   │ Mem0g       │ 69.2%    │ 1.6s     │ 89%      │ Best     │ Complex      │ |
|   │ (w/ Graph)  │          │          │          │          │ Reasoning    │ |
|   ├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────────┤ |
|   │ OpenAI      │ 53.1%    │ 2.1s     │ 80%      │ Medium   │ Consumer     │ |
|   │ Memory      │          │          │          │          │ ChatGPT      │ |
|   ├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────────┤ |
|   │ LangMem     │ 48.5%    │ 60s+     │ 75%      │ Poor     │ LangChain    │ |
|   │             │          │          │          │          │ Ecosystem    │ |
|   ├─────────────┼──────────┼──────────┼──────────┼──────────┼──────────────┤ |
|   │ MemGPT      │ 55.2%    │ 3.5s     │ 85%      │ Good     │ Research     │ |
|   │ (Letta)     │          │          │          │          │ Long context │ |
|   └─────────────┴──────────┴──────────┴──────────┴──────────┴──────────────┘ |
|                                                                               |
|   KEY FINDINGS:                                                               |
|   • Mem0 leads in accuracy (26% better than OpenAI Memory)                   |
|   • Mem0 91% lower p95 latency vs full-context approach                      |
|   • Graph memory (Mem0g) best for temporal/relational queries                |
|   • LangMem has latency issues at scale                                      |
|                                                                               |
+===============================================================================+
```

### 8.2 Platform Deep Dive

```
+===============================================================================+
|                    PLATFORM CHARACTERISTICS                                    |
+===============================================================================+
|                                                                               |
|   MEM0:                                                                       |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ • Memory scopes: User, Session, Agent                               │    |
|   │ • Selective fact extraction (not just compression)                  │    |
|   │ • Graph variant for complex relationships                           │    |
|   │ • Production-ready, raised $24M (Oct 2025)                         │    |
|   │ • Best for: Business-critical AI agents                            │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   MEMGPT (LETTA):                                                             |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ • OS-inspired hierarchical memory                                   │    |
|   │ • LLM controls its own memory (paging)                             │    |
|   │ • Academic origin (UC Berkeley)                                     │    |
|   │ • Best for: Research, infinite context simulation                  │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   LANGMEM (LANGCHAIN):                                                        |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ • Three memory types: Semantic, Procedural, Episodic               │    |
|   │ • Tight LangChain integration                                       │    |
|   │ • Developer-friendly if in LangChain ecosystem                     │    |
|   │ • Best for: LangChain projects needing memory                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   OPENAI MEMORY:                                                              |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ • Built into ChatGPT                                                │    |
|   │ • Frictionless user experience                                      │    |
|   │ • Limited customization                                             │    |
|   │ • Best for: Consumer apps, simple preference tracking              │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

---

## 9. Design Patterns by Use Case

### 9.1 Use Case Decision Matrix

```
+===============================================================================+
|                    USE CASE → MEMORY ARCHITECTURE MAPPING                      |
+===============================================================================+
|                                                                               |
|   ┌────────────────────────┬─────────────────────────────────────────────┐   |
|   │ USE CASE               │ RECOMMENDED ARCHITECTURE                     │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Simple FAQ Bot         │ Sliding Window (5-10 msgs)                  │   |
|   │                        │ No persistence needed                       │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Customer Support       │ Sliding Window + Ticket Context             │   |
|   │                        │ Per-ticket memory, cross-ticket RAG         │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Personal Assistant     │ Hybrid (Memory Formation + RAG)             │   |
|   │                        │ Long-term user preferences                  │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Code Assistant         │ RAG + Large Sliding Window                  │   |
|   │                        │ Codebase in vector DB                       │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Research Agent         │ Hierarchical (MemGPT-style)                 │   |
|   │                        │ Long research sessions                      │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Multi-Agent System     │ Two-Tier (Private + Shared)                 │   |
|   │                        │ Blackboard for coordination                 │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ E-commerce Advisor     │ Memory Formation + User Profile             │   |
|   │                        │ Preferences, purchase history               │   |
|   ├────────────────────────┼─────────────────────────────────────────────┤   |
|   │ Healthcare Bot         │ Hybrid + Strict Access Control              │   |
|   │                        │ HIPAA compliance, audit logs                │   |
|   └────────────────────────┴─────────────────────────────────────────────┘   |
|                                                                               |
+===============================================================================+
```

### 9.2 Architecture Examples

```
+===============================================================================+
|                    EXAMPLE 1: CUSTOMER SUPPORT BOT                             |
+===============================================================================+
|                                                                               |
|   REQUIREMENTS:                                                               |
|   • Quick resolution per ticket                                              |
|   • Access to knowledge base                                                  |
|   • Remember customer history (if returning)                                  |
|                                                                               |
|   ARCHITECTURE:                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   ┌─────────────────┐                                               │    |
|   │   │   USER QUERY    │                                               │    |
|   │   └────────┬────────┘                                               │    |
|   │            │                                                         │    |
|   │            ▼                                                         │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │                 CONTEXT ASSEMBLER                            │   │    |
|   │   │                                                              │   │    |
|   │   │   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │   │    |
|   │   │   │   SLIDING    │  │  KNOWLEDGE   │  │   CUSTOMER   │     │   │    |
|   │   │   │   WINDOW     │  │     BASE     │  │   PROFILE    │     │   │    |
|   │   │   │   (8 turns)  │  │    (RAG)     │  │  (Long-term) │     │   │    |
|   │   │   └──────────────┘  └──────────────┘  └──────────────┘     │   │    |
|   │   │                                                              │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │            │                                                         │    |
|   │            ▼                                                         │    |
|   │   ┌─────────────────┐                                               │    |
|   │   │      LLM        │ → Response                                    │    |
|   │   └─────────────────┘                                               │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+

+===============================================================================+
|                    EXAMPLE 2: MULTI-AGENT CODE REVIEW SYSTEM                   |
+===============================================================================+
|                                                                               |
|   REQUIREMENTS:                                                               |
|   • Multiple specialized agents                                              |
|   • Share context about current PR                                           |
|   • Learn from past reviews                                                  |
|                                                                               |
|   ARCHITECTURE:                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │                    ┌────────────────────────┐                        │    |
|   │                    │    SHARED CONTEXT      │                        │    |
|   │                    │    (Blackboard)        │                        │    |
|   │                    │                        │                        │    |
|   │                    │ • PR details           │                        │    |
|   │                    │ • Review status        │                        │    |
|   │                    │ • Findings so far      │                        │    |
|   │                    └───────────┬────────────┘                        │    |
|   │          ┌─────────────────────┼─────────────────────┐              │    |
|   │          │                     │                     │              │    |
|   │          ▼                     ▼                     ▼              │    |
|   │   ┌──────────────┐     ┌──────────────┐     ┌──────────────┐       │    |
|   │   │   SECURITY   │     │    CODE      │     │    STYLE     │       │    |
|   │   │   REVIEWER   │     │   REVIEWER   │     │   REVIEWER   │       │    |
|   │   │              │     │              │     │              │       │    |
|   │   │ Private:     │     │ Private:     │     │ Private:     │       │    |
|   │   │ - Security   │     │ - Best       │     │ - Style      │       │    |
|   │   │   patterns   │     │   practices  │     │   guides     │       │    |
|   │   │ - CVE DB     │     │ - Code smells│     │ - Linting    │       │    |
|   │   └──────────────┘     └──────────────┘     └──────────────┘       │    |
|   │          │                     │                     │              │    |
|   │          └─────────────────────┼─────────────────────┘              │    |
|   │                                │                                    │    |
|   │                                ▼                                    │    |
|   │                    ┌────────────────────────┐                        │    |
|   │                    │    LONG-TERM MEMORY    │                        │    |
|   │                    │    (Vector DB)         │                        │    |
|   │                    │                        │                        │    |
|   │                    │ • Past review patterns │                        │    |
|   │                    │ • Common issues found  │                        │    |
|   │                    │ • Team preferences     │                        │    |
|   │                    └────────────────────────┘                        │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

---

## 10. Implementation Decision Framework

### 10.1 Decision Tree

```
+===============================================================================+
|                    MEMORY ARCHITECTURE DECISION TREE                           |
+===============================================================================+
|                                                                               |
|   START: What is your primary use case?                                      |
|      │                                                                        |
|      ├─► Short conversations (<20 turns)?                                    |
|      │      │                                                                 |
|      │      └─► YES → SLIDING WINDOW (simple, effective)                     |
|      │                                                                        |
|      ├─► Need cross-session memory?                                          |
|      │      │                                                                 |
|      │      └─► YES → Need semantic search?                                  |
|      │             │                                                          |
|      │             ├─► YES → RAG + VECTOR DB                                 |
|      │             │                                                          |
|      │             └─► NO → MEMORY FORMATION (key facts)                     |
|      │                                                                        |
|      ├─► Very long conversations (100+ turns)?                               |
|      │      │                                                                 |
|      │      └─► YES → HIERARCHICAL (MemGPT-style)                            |
|      │              or SUMMARIZATION + RAG                                   |
|      │                                                                        |
|      ├─► Multi-agent system?                                                 |
|      │      │                                                                 |
|      │      └─► YES → TWO-TIER (Private + Shared)                            |
|      │              + SYNCHRONIZATION PATTERN                                |
|      │                                                                        |
|      └─► Production with cost/latency constraints?                           |
|             │                                                                 |
|             └─► YES → HYBRID APPROACH                                        |
|                      (Memory Formation + Sliding Window + RAG)               |
|                                                                               |
+===============================================================================+
```

### 10.2 Quick Start Recommendations

```
+===============================================================================+
|                    QUICK START GUIDE                                           |
+===============================================================================+
|                                                                               |
|   FOR PROTOTYPES:                                                             |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   1. Start with SLIDING WINDOW (10 messages)                        │    |
|   │   2. Add ChromaDB if need knowledge base                            │    |
|   │   3. Measure: Does context get lost?                                │    |
|   │      - YES → Add summarization or memory formation                  │    |
|   │      - NO → Keep simple                                             │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   FOR PRODUCTION:                                                             |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   1. Use Mem0 or similar production-ready platform                  │    |
|   │   2. Configure memory scopes (User, Session, Agent)                 │    |
|   │   3. Set up monitoring for:                                         │    |
|   │      - Token usage                                                  │    |
|   │      - Memory hit rate                                              │    |
|   │      - User satisfaction (context accuracy)                         │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   FOR MULTI-AGENT:                                                            |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                                                                      │    |
|   │   1. Design memory boundaries first:                                │    |
|   │      - What is private to each agent?                               │    |
|   │      - What must be shared?                                         │    |
|   │   2. Choose synchronization pattern                                 │    |
|   │   3. Implement TASK LEDGER (from Magentic-One) for coordination    │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

---

## 11. References

### Web Sources

1. [Conversational Memory for LLMs with Langchain - Pinecone](https://www.pinecone.io/learn/series/langchain/langchain-conversational-memory/)
2. [AI Chatbot Memory Management: Tools and Strategies 2025](https://python.plainenglish.io/memory-management-strategies-and-tools-for-ai-chatbots-and-agents-77ec9063ed92)
3. [Building AI Agents That Actually Remember - Medium](https://medium.com/@nomannayeem/building-ai-agents-that-actually-remember-a-developers-guide-to-memory-management-in-2025-062fd0be80a1)
4. [Collaborative Memory: Multi-User Memory Sharing in LLM Agents](https://arxiv.org/html/2505.18279v1)
5. [Why Multi-Agent Systems Need Memory Engineering - MongoDB](https://www.mongodb.com/company/blog/technical/why-multi-agent-systems-need-memory-engineering)
6. [LLM Chat History Summarization Guide 2025 - Mem0](https://mem0.ai/blog/llm-chat-history-summarization-guide-2025)
7. [AI Memory Benchmark: Mem0 vs OpenAI vs LangMem vs MemGPT](https://mem0.ai/blog/benchmarked-openai-memory-vs-langmem-vs-memgpt-vs-mem0-for-long-term-memory-here-s-how-they-stacked-up)
8. [Beyond Vector Databases: Architectures for True Long-Term AI Memory](https://vardhmanandroid2015.medium.com/beyond-vector-databases-architectures-for-true-long-term-ai-memory-0d4629d1a006)
9. [Cutting Through the Noise: Smarter Context Management - JetBrains](https://blog.jetbrains.com/research/2025/12/efficient-context-management/)
10. [Best Vector Databases for RAG: Complete 2025 Guide](https://latenode.com/blog/ai-frameworks-technical-infrastructure/vector-databases-embeddings/best-vector-databases-for-rag-complete-2025-comparison-guide)

### Related Documents

- [v1-explain-magentic-leader-plan.md](./v1-explain-magentic-leader-plan.md) - Dual Loop System & Task Ledger
- [v1-microsoft-agent-framework-research.md](./v1-microsoft-agent-framework-research.md) - MAF Overview
- [v2-microsoft-agent-framework-use-case-guide.md](./v2-microsoft-agent-framework-use-case-guide.md) - Use Cases & Memory Levels

### Academic Papers

- Mem0: Building Production-Ready AI Agents with Scalable Long-Term Memory (arXiv:2504.19413)
- MemGPT: Towards LLMs as Operating Systems (UC Berkeley)
- Recursively Summarizing Enables Long-Term Dialogue Memory (arXiv:2308.15022)
- Memory in LLM-based Multi-agent Systems: Mechanisms, Challenges, and Collective Intelligence

---

## Summary

### Key Takeaways

1. **Context Window ≠ Memory**: LLM context windows are limited; real memory requires external systems

2. **Memory Formation > Summarization**: Selective fact extraction outperforms compression (26% accuracy improvement, 80-90% token savings)

3. **Hybrid is Best for Production**: Combine sliding window (recent) + memory formation (facts) + RAG (semantic search)

4. **Multi-Agent needs Two-Tier**: Private memory for agent expertise + Shared memory for coordination

5. **Platform Choice Matters**: Mem0 leads benchmarks for production AI agents

### Quick Reference

```
┌─────────────────────────────────────────────────────────────┐
│                 MEMORY ARCHITECTURE CHEAT SHEET             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  SHORT CONVERSATION → Sliding Window (10 msgs)              │
│  PERSONALIZATION   → Memory Formation (Mem0)                │
│  KNOWLEDGE BASE    → RAG (Vector DB)                        │
│  LONG SESSIONS     → Hierarchical (MemGPT)                  │
│  MULTI-AGENT       → Two-Tier + Blackboard                  │
│  PRODUCTION        → Hybrid (all of the above)              │
│                                                             │
│  TOKEN SAVINGS:                                             │
│  • Memory Formation: 80-90%                                 │
│  • Summarization: 60-70%                                    │
│  • RAG: 70-80% (only relevant context)                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```
