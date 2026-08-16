# Microsoft Agent Framework (MAF) - Use Case Driven Guide

**Version**: 2.0
**Created**: 2026-01-02
**Based on**: v1-microsoft-agent-framework-research.md
**Approach**: Progressive Use Cases (Simple → Complex)
**Status**: 🚧 In Progress

---

## Table of Contents

1. [Introduction: Learning Path](#1-introduction-learning-path)
2. [Level 1: Hello World - Single Agent](#2-level-1-hello-world---single-agent)
3. [Level 2: Agent with Tools/Plugins](#3-level-2-agent-with-toolsplugins)
4. [Level 3: Agent with Memory (RAG)](#4-level-3-agent-with-memory-rag)
5. [Level 4: Multi-Agent Sequential](#5-level-4-multi-agent-sequential)
6. [Level 5: Multi-Agent Concurrent](#6-level-5-multi-agent-concurrent)
7. [Level 6: Human-in-the-Loop](#7-level-6-human-in-the-loop)
8. [Level 7: Dynamic Orchestration (Magentic-One)](#8-level-7-dynamic-orchestration-magentic-one)
9. [Level 8: Production-Ready System](#9-level-8-production-ready-system)
10. [Comprehensive Comparison Table](#10-comprehensive-comparison-table)
11. [Decision Framework](#11-decision-framework)
12. [Appendix: MCP Integration Details](#12-appendix-mcp-integration-details)

---

## 1. Introduction: Learning Path

### 1.1 Philosophy: Problem → Solution → New Problem

Tài liệu này được thiết kế theo nguyên tắc **progressive complexity**:

```
+===============================================================================+
|                         LEARNING PATH OVERVIEW                                 |
+===============================================================================+
|                                                                               |
|   Level 1: Single Agent          "Tôi cần chatbot đơn giản"                  |
|      │                                                                        |
|      │  Problem: Không có khả năng thực hiện hành động                       |
|      ▼                                                                        |
|   Level 2: Agent + Tools         "Tôi cần agent gọi API"                     |
|      │                                                                        |
|      │  Problem: Không nhớ context giữa các session                          |
|      ▼                                                                        |
|   Level 3: Agent + Memory        "Tôi cần agent nhớ thông tin"               |
|      │                                                                        |
|      │  Problem: Một agent không đủ expertise                                |
|      ▼                                                                        |
|   Level 4: Sequential Agents     "Tôi cần pipeline xử lý"                    |
|      │                                                                        |
|      │  Problem: Pipeline quá chậm, cần parallel                             |
|      ▼                                                                        |
|   Level 5: Concurrent Agents     "Tôi cần xử lý song song"                   |
|      │                                                                        |
|      │  Problem: Cần approval từ người dùng                                  |
|      ▼                                                                        |
|   Level 6: Human-in-Loop         "Tôi cần kiểm duyệt"                        |
|      │                                                                        |
|      │  Problem: Vấn đề quá phức tạp, không biết trước solution              |
|      ▼                                                                        |
|   Level 7: Dynamic Orchestration "Tôi cần giải quyết open-ended problems"    |
|      │                                                                        |
|      │  Problem: Cần enterprise features                                      |
|      ▼                                                                        |
|   Level 8: Production System     "Tôi cần deploy production"                 |
|                                                                               |
+===============================================================================+
```

### 1.2 Technology Stack Overview

```
+===============================================================================+
|                    MICROSOFT AGENT FRAMEWORK STACK                             |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                     MICROSOFT AGENT FRAMEWORK                        │    |
|   │                    (Unified SDK - Python/.NET)                       │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                    │                                          |
|           ┌────────────────────────┴────────────────────────┐                 |
|           │                                                 │                 |
|           ▼                                                 ▼                 |
|   ┌───────────────────────┐                 ┌───────────────────────┐        |
|   │   SEMANTIC KERNEL     │                 │       AUTOGEN         │        |
|   │   (The Skill Builder) │                 │   (The Collaborator)  │        |
|   │                       │                 │                       │        |
|   │   • LLM Integration   │                 │   • Multi-Agent Chat  │        |
|   │   • Plugins/Tools     │                 │   • Orchestration     │        |
|   │   • Memory/RAG        │                 │   • Event-Driven      │        |
|   │   • Function Calling  │                 │   • Distributed       │        |
|   └───────────────────────┘                 └───────────────────────┘        |
|                                    │                                          |
|                                    ▼                                          |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                          MCP CLIENTS                                 │    |
|   │              (Model Context Protocol - Tool Integration)             │    |
|   │                                                                      │    |
|   │   ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐           │    |
|   │   │ Database │  │ Browser  │  │Filesystem│  │   APIs   │           │    |
|   │   └──────────┘  └──────────┘  └──────────┘  └──────────┘           │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

---

## 2. Level 1: Hello World - Single Agent

### 2.1 Use Case: Simple Q&A Chatbot

**Scenario**: Công ty XYZ cần một chatbot đơn giản để trả lời FAQ về sản phẩm.

**Requirements**:
- Trả lời câu hỏi từ người dùng
- Không cần nhớ context giữa các lần hỏi
- Không cần thực hiện hành động nào

```
+===============================================================================+
|                    USE CASE: FAQ CHATBOT                                       |
+===============================================================================+
|                                                                               |
|   USER: "Giá sản phẩm A là bao nhiêu?"                                       |
|          │                                                                    |
|          ▼                                                                    |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                        SINGLE AGENT                                   │   |
|   │                                                                       │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │      LLM      │ ← GPT-4, Claude, Gemini                          │   |
|   │   │   (Reasoning) │                                                   │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │           ▼                                                           │   |
|   │   System Prompt: "Bạn là trợ lý bán hàng của XYZ..."                 │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|          │                                                                    |
|          ▼                                                                    |
|   ASSISTANT: "Sản phẩm A có giá 500.000 VNĐ..."                             |
|                                                                               |
+===============================================================================+
```

### 2.2 Implementation Concepts

| Concept | Description | MAF Component |
|---------|-------------|---------------|
| **Agent** | Đơn vị AI xử lý input → reasoning → output | `ChatCompletionAgent` |
| **LLM** | Brain của agent, thực hiện reasoning | Model Client |
| **System Prompt** | Instructions cho agent về behavior | Agent Configuration |
| **User Prompt** | Input từ người dùng | Agent Thread |

### 2.3 Code Pattern (Conceptual)

```python
# Semantic Kernel Approach
from semantic_kernel import Kernel
from semantic_kernel.agents import ChatCompletionAgent

# 1. Setup LLM
kernel = Kernel()
kernel.add_chat_completion_service("gpt-4", ...)

# 2. Create Agent
agent = ChatCompletionAgent(
    kernel=kernel,
    name="FAQ_Bot",
    instructions="Bạn là trợ lý bán hàng của XYZ..."
)

# 3. Handle User Input
response = await agent.invoke("Giá sản phẩm A là bao nhiêu?")
```

### 2.4 Limitations → Next Level

```
+===============================================================================+
|                    LEVEL 1 LIMITATIONS                                         |
+===============================================================================+
|                                                                               |
|   ❌ KHÔNG THỂ:                                                               |
|   ├── Thực hiện hành động (đặt hàng, gửi email)                              |
|   ├── Truy cập dữ liệu real-time (giá cổ phiếu, thời tiết)                   |
|   ├── Tương tác với hệ thống bên ngoài (database, API)                       |
|   └── Nhớ thông tin từ conversation trước                                     |
|                                                                               |
|   → SOLUTION: Thêm Tools/Plugins (Level 2)                                   |
|                                                                               |
+===============================================================================+
```

---

## 3. Level 2: Agent with Tools/Plugins

### 3.1 Use Case: Smart Home Controller

**Scenario**: Người dùng muốn điều khiển nhà thông minh bằng giọng nói.

**New Problem từ Level 1**:
- Agent cần thực hiện hành động thực tế (bật/tắt đèn)
- Agent cần truy cập dữ liệu real-time (trạng thái thiết bị)

**Solution**: **Plugins/Tools** - Cho phép agent gọi functions bên ngoài

```
+===============================================================================+
|                    USE CASE: SMART HOME CONTROLLER                             |
+===============================================================================+
|                                                                               |
|   USER: "Bật đèn phòng khách"                                                |
|          │                                                                    |
|          ▼                                                                    |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                     AGENT + PLUGINS                                   │   |
|   │                                                                       │   |
|   │   ┌───────────────┐          ┌───────────────────────┐               │   |
|   │   │      LLM      │ ───────► │    PLUGIN REGISTRY     │               │   |
|   │   │   (Decides)   │          │                        │               │   |
|   │   └───────────────┘          │  ┌─────────────────┐  │               │   |
|   │                              │  │  get_lights()   │  │               │   |
|   │   LLM receives:              │  │  change_state() │  │               │   |
|   │   - User message             │  │  toggle_all()   │  │               │   |
|   │   - Available functions      │  └─────────────────┘  │               │   |
|   │                              └───────────────────────┘               │   |
|   │                                                                       │   |
|   │   FUNCTION CALLING FLOW:                                              │   |
|   │   1. LLM sees: get_lights(), change_state(id, is_on)                 │   |
|   │   2. LLM decides: Call get_lights() first                            │   |
|   │   3. Returns: [{id:1, name:"Phòng khách", is_on:false}]              │   |
|   │   4. LLM decides: Call change_state(1, true)                         │   |
|   │   5. Returns: Success                                                 │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|          │                                                                    |
|          ▼                                                                    |
|   ASSISTANT: "Đã bật đèn phòng khách."                                       |
|                                                                               |
+===============================================================================+
```

### 3.2 Key Concepts Introduced

| Concept | Description | Why Important |
|---------|-------------|---------------|
| **Plugin** | Collection of related functions | Tổ chức tools theo domain |
| **Function/Tool** | Một action cụ thể agent có thể gọi | Mở rộng khả năng của agent |
| **Function Calling** | LLM quyết định gọi function nào | Core mechanism cho tool use |
| **Semantic Description** | Mô tả function để LLM hiểu | LLM dựa vào đây để quyết định |

### 3.3 Plugin Anatomy

```
+===============================================================================+
|                         PLUGIN STRUCTURE                                       |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                      LIGHTS PLUGIN                                   │    |
|   │                                                                      │    |
|   │   @kernel_function                                                   │    |
|   │   def get_lights(self) -> List[Light]:                              │    |
|   │       """Gets a list of lights and their current state."""          │    |
|   │       return self.light_service.get_all()                           │    |
|   │                         │                                            │    |
|   │                         └── Semantic Description (cho LLM đọc)       │    |
|   │                                                                      │    |
|   │   @kernel_function                                                   │    |
|   │   def change_state(self, light_id: int, is_on: bool) -> Light:      │    |
|   │       """Changes the state of a specific light."""                  │    |
|   │       return self.light_service.set_state(light_id, is_on)          │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   BEST PRACTICES:                                                             |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ ✅ Use snake_case for function names                                 │    |
|   │ ✅ Keep parameters minimal (prefer primitives)                       │    |
|   │ ✅ Write clear semantic descriptions                                 │    |
|   │ ✅ Limit to ≤10 tools per API call (OpenAI recommendation)          │    |
|   │ ❌ Don't pass sensitive data through LLM (use ID-based approach)    │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 3.4 Three Ways to Import Plugins

```
+===============================================================================+
|                    PLUGIN IMPORT METHODS                                       |
+===============================================================================+
|                                                                               |
|   METHOD 1: NATIVE CODE (Python/C#)                                          |
|   ─────────────────────────────────                                          |
|   Best for: Custom business logic, internal APIs                             |
|                                                                               |
|   @kernel_function                                                           |
|   async def send_email(self, to: str, subject: str, body: str):             |
|       """Sends an email to the specified recipient."""                      |
|       return await self.email_service.send(to, subject, body)               |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   METHOD 2: OPENAPI SPECIFICATION                                            |
|   ───────────────────────────────                                            |
|   Best for: External APIs, third-party services                              |
|                                                                               |
|   openapi: 3.0.0                                                             |
|   paths:                                                                      |
|     /weather:                                                                 |
|       get:                                                                    |
|         summary: Get current weather                                         |
|         operationId: getWeather                                              |
|         parameters:                                                           |
|           - name: city                                                        |
|             in: query                                                         |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   METHOD 3: MCP SERVER ⭐ (Model Context Protocol)                           |
|   ─────────────────────                                                       |
|   Best for: Complex integrations, standardized tool access                   |
|                                                                               |
|   MCP Server exposes:                                                        |
|   ├── Tools: [database_query, file_read, browser_navigate]                  |
|   ├── Resources: [documents, schemas, configs]                              |
|   └── Prompts: [templates, instructions]                                    |
|                                                                               |
+===============================================================================+
```

### 3.5 MCP - Model Context Protocol Deep Dive

```
+===============================================================================+
|                    MCP ARCHITECTURE                                            |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                         MCP OVERVIEW                                 │    |
|   │                                                                      │    |
|   │   What: Standardized protocol for AI-tool communication             │    |
|   │   Why: Unified way to expose capabilities to any AI agent           │    |
|   │   Who: Anthropic (creator), Microsoft (adopter in MAF)              │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                      MCP COMPONENTS                                  │    |
|   │                                                                      │    |
|   │   ┌───────────────┐                    ┌───────────────┐            │    |
|   │   │  MCP CLIENT   │◄──── JSON-RPC ────►│  MCP SERVER   │            │    |
|   │   │  (in Agent)   │                    │  (Tool Host)  │            │    |
|   │   └───────────────┘                    └───────────────┘            │    |
|   │                                                │                     │    |
|   │                                                ▼                     │    |
|   │                                   ┌─────────────────────┐           │    |
|   │                                   │    CAPABILITIES      │           │    |
|   │                                   │                      │           │    |
|   │                                   │  TOOLS:              │           │    |
|   │                                   │  - database_query    │           │    |
|   │                                   │  - file_operations   │           │    |
|   │                                   │  - browser_actions   │           │    |
|   │                                   │                      │           │    |
|   │                                   │  RESOURCES:          │           │    |
|   │                                   │  - documents         │           │    |
|   │                                   │  - schemas           │           │    |
|   │                                   │                      │           │    |
|   │                                   │  PROMPTS:            │           │    |
|   │                                   │  - templates         │           │    |
|   │                                   └─────────────────────┘           │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   TRANSPORT OPTIONS:                                                          |
|   ├── STDIO: Local process communication                                     |
|   └── SSE (Server-Sent Events): Remote server communication                 |
|                                                                               |
+===============================================================================+
```

### 3.6 Limitations → Next Level

```
+===============================================================================+
|                    LEVEL 2 LIMITATIONS                                         |
+===============================================================================+
|                                                                               |
|   ❌ KHÔNG THỂ:                                                               |
|   ├── Nhớ conversation history giữa các sessions                             |
|   ├── Học từ interactions trước đó                                           |
|   ├── Tìm kiếm semantic trong documents                                      |
|   └── Personalize based on user preferences                                  |
|                                                                               |
|   EXAMPLE PROBLEM:                                                            |
|   User (Session 1): "Tôi thích màu xanh"                                     |
|   User (Session 2): "Gợi ý màu cho phòng khách"                              |
|   Agent: ??? (không nhớ user thích màu xanh)                                 |
|                                                                               |
|   → SOLUTION: Thêm Memory System (Level 3)                                   |
|                                                                               |
+===============================================================================+
```

---

## 4. Level 3: Agent with Memory (RAG)

### 4.1 Use Case: Personal Finance Advisor

**Scenario**: Ứng dụng tư vấn tài chính cá nhân cần nhớ thông tin user và tìm kiếm trong knowledge base.

**New Problem từ Level 2**:
- Agent cần nhớ thông tin user giữa các sessions
- Agent cần tìm kiếm semantic trong documents (regulations, policies)

**Solution**: **Memory System** - Short-term, Long-term, và Semantic Memory

```
+===============================================================================+
|                    USE CASE: FINANCE ADVISOR WITH MEMORY                       |
+===============================================================================+
|                                                                               |
|   SESSION 1:                                                                  |
|   User: "Thu nhập hàng tháng của tôi là 30 triệu"                            |
|   Agent: Stores → Long-term Memory: {income: 30000000}                       |
|                                                                               |
|   SESSION 2 (Next day):                                                       |
|   User: "Tôi nên tiết kiệm bao nhiêu?"                                       |
|                                                                               |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                     AGENT + MEMORY SYSTEM                             │   |
|   │                                                                       │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │      LLM      │                                                   │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │   ┌───────┴───────┬───────────────────┬───────────────────┐          │   |
|   │   │               │                   │                   │          │   |
|   │   ▼               ▼                   ▼                   ▼          │   |
|   │ ┌──────────┐ ┌──────────┐     ┌──────────────┐    ┌─────────────┐   │   |
|   │ │SHORT-TERM│ │LONG-TERM │     │   SEMANTIC   │    │   PLUGINS   │   │   |
|   │ │  MEMORY  │ │  MEMORY  │     │    MEMORY    │    │   (Tools)   │   │   |
|   │ │          │ │          │     │  (Vector DB) │    │             │   │   |
|   │ │ Current  │ │User data:│     │              │    │ calculate() │   │   |
|   │ │ convo    │ │income:   │     │ Regulations  │    │ get_rates() │   │   |
|   │ │ context  │ │30M       │     │ Policies     │    │             │   │   |
|   │ └──────────┘ └──────────┘     │ Guidelines   │    └─────────────┘   │   |
|   │                               └──────────────┘                       │   |
|   │                                                                       │   |
|   │   FLOW:                                                               │   |
|   │   1. Retrieve income from Long-term Memory                           │   |
|   │   2. Search "tiết kiệm" in Semantic Memory → find guidelines         │   |
|   │   3. Use Plugin to calculate optimal savings                          │   |
|   │   4. Generate response with context                                   │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|                                                                               |
|   Agent: "Với thu nhập 30 triệu, theo quy tắc 50/30/20,                      |
|           bạn nên tiết kiệm 6 triệu/tháng (20%)..."                          |
|                                                                               |
+===============================================================================+
```

### 4.2 Memory Types (MECE)

```
+===============================================================================+
|                    MEMORY SYSTEM ARCHITECTURE                                  |
+===============================================================================+
|                                                                               |
|   ┌───────────────────────────────────────────────────────────────────────┐  |
|   │                       MEMORY TYPES                                     │  |
|   │                                                                        │  |
|   │   ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐    │  |
|   │   │   SHORT-TERM    │   │   LONG-TERM     │   │    SEMANTIC     │    │  |
|   │   │                 │   │                 │   │                 │    │  |
|   │   │ • Chat history  │   │ • User prefs    │   │ • Vector DB     │    │  |
|   │   │ • Session vars  │   │ • Facts         │   │ • Embeddings    │    │  |
|   │   │ • Temp context  │   │ • Knowledge     │   │ • RAG           │    │  |
|   │   │                 │   │                 │   │                 │    │  |
|   │   │ Lifecycle:      │   │ Lifecycle:      │   │ Lifecycle:      │    │  |
|   │   │ Session-based   │   │ Persistent      │   │ Indexed         │    │  |
|   │   │ (lost on end)   │   │ (survives)      │   │ (searchable)    │    │  |
|   │   └─────────────────┘   └─────────────────┘   └─────────────────┘    │  |
|   │                                                                        │  |
|   └───────────────────────────────────────────────────────────────────────┘  |
|                                                                               |
|   VECTOR STORE OPTIONS:                                                       |
|   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    |
|   │ Azure AI     │  │   Pinecone   │  │    Qdrant    │  │   Chroma     │    |
|   │ Search       │  │              │  │              │  │              │    |
|   └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    |
|                                                                               |
+===============================================================================+
```

### 4.3 RAG (Retrieval Augmented Generation) Flow

```
+===============================================================================+
|                    RAG ARCHITECTURE                                            |
+===============================================================================+
|                                                                               |
|   INDEXING PHASE (Offline):                                                   |
|   ─────────────────────────                                                   |
|                                                                               |
|   Documents ──► Chunking ──► Embedding ──► Vector Store                      |
|   (PDFs,         (Split      (text →       (Store &                          |
|    Docs,          into        vector)       Index)                            |
|    Web)           parts)                                                      |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   QUERY PHASE (Online):                                                       |
|   ─────────────────────                                                       |
|                                                                               |
|   User Query: "Quy định về tiết kiệm hưu trí?"                               |
|        │                                                                      |
|        ▼                                                                      |
|   ┌────────────────┐                                                         |
|   │ 1. EMBED QUERY │ → [0.023, -0.041, 0.087, ...]                          |
|   └────────────────┘                                                         |
|        │                                                                      |
|        ▼                                                                      |
|   ┌────────────────┐                                                         |
|   │ 2. SIMILARITY  │ → Find top-k nearest neighbors                         |
|   │    SEARCH      │                                                         |
|   └────────────────┘                                                         |
|        │                                                                      |
|        ▼                                                                      |
|   ┌────────────────┐    Retrieved Context:                                   |
|   │ 3. RETRIEVE    │ → "Theo quy định 401k, nhân viên                       |
|   │    CONTEXT     │    có thể đóng góp tối đa 23,000$/năm..."              |
|   └────────────────┘                                                         |
|        │                                                                      |
|        ▼                                                                      |
|   ┌────────────────┐                                                         |
|   │ 4. AUGMENT     │ → Combine query + context → Send to LLM                |
|   │    PROMPT      │                                                         |
|   └────────────────┘                                                         |
|        │                                                                      |
|        ▼                                                                      |
|   ┌────────────────┐                                                         |
|   │ 5. GENERATE    │ → LLM produces informed response                       |
|   └────────────────┘                                                         |
|                                                                               |
+===============================================================================+
```

### 4.4 Limitations → Next Level

```
+===============================================================================+
|                    LEVEL 3 LIMITATIONS                                         |
+===============================================================================+
|                                                                               |
|   ❌ KHÔNG THỂ:                                                               |
|   ├── Một agent xử lý tasks đa chuyên môn (code + review + test)            |
|   ├── Chất lượng giảm khi một agent làm quá nhiều việc                       |
|   ├── Không có separation of concerns                                        |
|   └── Bottleneck khi workload tăng                                           |
|                                                                               |
|   EXAMPLE PROBLEM:                                                            |
|   Task: "Viết code → Review → Fix bugs → Write tests"                        |
|   Single Agent: Quality suffers, no specialized expertise                    |
|                                                                               |
|   → SOLUTION: Multi-Agent với chuyên môn hóa (Level 4)                       |
|                                                                               |
+===============================================================================+
```

---

## 5. Level 4: Multi-Agent Sequential

### 5.1 Use Case: Code Review Pipeline

**Scenario**: Team cần automated code review với multiple stages.

**New Problem từ Level 3**:
- Một agent không thể làm tốt nhiều tasks chuyên biệt
- Cần chuyên môn hóa: Coder, Reviewer, Tester

**Solution**: **Sequential Multi-Agent** - Agents chạy theo thứ tự

```
+===============================================================================+
|                    USE CASE: CODE REVIEW PIPELINE                              |
+===============================================================================+
|                                                                               |
|   User Request: "Implement login feature with security best practices"        |
|                                                                               |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                   SEQUENTIAL MULTI-AGENT                              │   |
|   │                                                                       │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   CODER       │                                                   │   |
|   │   │   AGENT       │  → Writes initial implementation                  │   |
|   │   │               │  → Has: code_write, file_create plugins           │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │           ▼ Pass code artifact                                        │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   SECURITY    │                                                   │   |
|   │   │   REVIEWER    │  → Checks for vulnerabilities                     │   |
|   │   │   AGENT       │  → Has: static_analysis, vulnerability_scan       │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │           ▼ Pass review feedback                                      │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   CODER       │                                                   │   |
|   │   │   AGENT       │  → Fixes issues based on feedback                 │   |
|   │   │   (Again)     │                                                   │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │           ▼ Pass fixed code                                           │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   TESTER      │                                                   │   |
|   │   │   AGENT       │  → Writes and runs tests                          │   |
|   │   │               │  → Has: test_write, test_run plugins              │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │           ▼                                                           │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   FINAL       │                                                   │   |
|   │   │   OUTPUT      │  → Complete, reviewed, tested code                │   |
|   │   └───────────────┘                                                   │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|                                                                               |
+===============================================================================+
```

### 5.2 Key Concepts Introduced

| Concept | Description | MAF Component |
|---------|-------------|---------------|
| **Orchestrator** | Điều phối agents theo sequence | AutoGen Runtime |
| **Agent Specialization** | Mỗi agent có role/expertise riêng | Agent Definition |
| **Message Passing** | Agents giao tiếp qua messages | AgentThread |
| **Sequential Pattern** | A → B → C → Output | GroupChat (sequential) |

### 5.3 AutoGen Architecture

```
+===============================================================================+
|                    AUTOGEN MULTI-AGENT ARCHITECTURE                            |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                         ORCHESTRATOR                                 │    |
|   │                   (Conversation Manager)                             │    |
|   │                                                                      │    |
|   │   Responsibilities:                                                  │    |
|   │   • Determine which agent speaks next                                │    |
|   │   • Manage conversation flow                                         │    |
|   │   • Handle termination conditions                                    │    |
|   └───────────────────────────────────────────────────────────────────── │    |
|                              │                                                |
|           ┌──────────────────┼──────────────────┐                             |
|           │                  │                  │                             |
|           ▼                  ▼                  ▼                             |
|   ┌───────────────┐  ┌───────────────┐  ┌───────────────┐                    |
|   │   AGENT A     │  │  USER PROXY   │  │   AGENT B     │                    |
|   │   (Coder)     │  │    AGENT      │  │  (Reviewer)   │                    |
|   │               │  │               │  │               │                    |
|   │ Role:         │  │ Role:         │  │ Role:         │                    |
|   │ - Write code  │  │ - Human gate  │  │ - Review code │                    |
|   │ - Debug       │  │ - Approval    │  │ - Find bugs   │                    |
|   │               │  │ - Input relay │  │ - Suggest fix │                    |
|   │               │  │               │  │               │                    |
|   │ Tools:        │  │ Tools:        │  │ Tools:        │                    |
|   │ - code_exec   │  │ - user_input  │  │ - static_scan │                    |
|   │ - file_write  │  │ - approval    │  │ - test_run    │                    |
|   └───────┬───────┘  └───────┬───────┘  └───────┬───────┘                    |
|           │                  │                  │                             |
|           └──────────────────┴──────────────────┘                             |
|                              │                                                |
|                              ▼                                                |
|                    MESSAGE EXCHANGE                                           |
|                                                                               |
+===============================================================================+
```

### 5.4 Limitations → Next Level

```
+===============================================================================+
|                    LEVEL 4 LIMITATIONS                                         |
+===============================================================================+
|                                                                               |
|   ❌ KHÔNG THỂ:                                                               |
|   ├── Chạy parallel các tasks độc lập → slow                                 |
|   ├── Tận dụng multiple perspectives cùng lúc                                |
|   ├── Reduce latency cho time-sensitive tasks                                |
|   └── Scale với high throughput                                              |
|                                                                               |
|   EXAMPLE PROBLEM:                                                            |
|   Task: Analyze stock from 4 perspectives:                                   |
|   - Fundamental Analysis                                                      |
|   - Technical Analysis                                                        |
|   - Sentiment Analysis                                                        |
|   - ESG Analysis                                                              |
|                                                                               |
|   Sequential: 4 × 30s = 120s                                                 |
|   Parallel:   max(30s, 30s, 30s, 30s) = 30s                                 |
|                                                                               |
|   → SOLUTION: Concurrent Multi-Agent (Level 5)                               |
|                                                                               |
+===============================================================================+
```

---

## 6. Level 5: Multi-Agent Concurrent

### 6.1 Use Case: Comprehensive Stock Analysis

**Scenario**: Platform phân tích cổ phiếu cần multiple perspectives nhanh chóng.

**New Problem từ Level 4**:
- Sequential quá chậm cho real-time analysis
- Cần aggregate multiple independent perspectives

**Solution**: **Concurrent Multi-Agent** - Agents chạy song song, kết quả được aggregate

```
+===============================================================================+
|                    USE CASE: STOCK ANALYSIS                                    |
+===============================================================================+
|                                                                               |
|   User Request: "Analyze AAPL stock"                                         |
|                                                                               |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                   CONCURRENT MULTI-AGENT                              │   |
|   │                                                                       │   |
|   │                        ┌─────────────┐                                │   |
|   │                        │   SPLITTER  │                                │   |
|   │                        │  (Dispatch) │                                │   |
|   │                        └──────┬──────┘                                │   |
|   │                               │                                       │   |
|   │        ┌──────────────────────┼──────────────────────┐               │   |
|   │        │           │          │          │           │               │   |
|   │        ▼           ▼          ▼          ▼           ▼               │   |
|   │   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                   │   |
|   │   │FUNDAMENT│ │TECHNICAL│ │SENTIMENT│ │   ESG   │  Run in           │   |
|   │   │  AGENT  │ │  AGENT  │ │  AGENT  │ │  AGENT  │  PARALLEL         │   |
|   │   │         │ │         │ │         │ │         │                   │   |
|   │   │Revenue, │ │Charts,  │ │News,    │ │Environ, │                   │   |
|   │   │Earnings,│ │RSI,     │ │Social,  │ │Social,  │                   │   |
|   │   │P/E Ratio│ │MACD     │ │Reddit   │ │Govern   │                   │   |
|   │   └────┬────┘ └────┬────┘ └────┬────┘ └────┬────┘                   │   |
|   │        │           │          │          │                           │   |
|   │        └───────────┴──────────┴──────────┘                           │   |
|   │                         │                                             │   |
|   │                         ▼                                             │   |
|   │                  ┌─────────────┐                                      │   |
|   │                  │  AGGREGATOR │                                      │   |
|   │                  │    AGENT    │                                      │   |
|   │                  │             │                                      │   |
|   │                  │ Combines:   │                                      │   |
|   │                  │ - All scores│                                      │   |
|   │                  │ - Conflicts │                                      │   |
|   │                  │ - Final rec │                                      │   |
|   │                  └─────────────┘                                      │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|                                                                               |
|   Output: "AAPL - BUY (Score: 8.2/10)                                        |
|            Fundamental: Strong ✓                                             |
|            Technical: Bullish ✓                                              |
|            Sentiment: Positive ✓                                             |
|            ESG: Above average ✓"                                             |
|                                                                               |
+===============================================================================+
```

### 6.2 Concurrent Pattern Architecture

```
+===============================================================================+
|                    CONCURRENT PATTERN DETAILS                                  |
+===============================================================================+
|                                                                               |
|   ARCHITECTURE:                                                               |
|                                                                               |
|                       ┌──► Agent A ──┐                                       |
|                       │              │                                       |
|        Input ─────────┼──► Agent B ──┼───► Aggregator ───► Output            |
|                       │              │                                       |
|                       └──► Agent C ──┘                                       |
|                                                                               |
|   CHARACTERISTICS:                                                            |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ ✅ USE WHEN:                  │ ❌ AVOID WHEN:                       │    |
|   │ • Tasks are independent       │ • Agents depend on each other       │    |
|   │ • Need multiple perspectives  │ • Specific order required           │    |
|   │ • Time-sensitive (latency)    │ • Resource constraints exist        │    |
|   │ • High throughput needed      │ • Sequential logic required         │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   AGGREGATION STRATEGIES:                                                     |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │ 1. VOTING        → Majority wins                                    │    |
|   │ 2. WEIGHTED      → Based on agent confidence/expertise              │    |
|   │ 3. SYNTHESIS     → LLM combines all perspectives                    │    |
|   │ 4. CONSENSUS     → All must agree (or escalate)                     │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 6.3 Limitations → Next Level

```
+===============================================================================+
|                    LEVEL 5 LIMITATIONS                                         |
+===============================================================================+
|                                                                               |
|   ❌ KHÔNG THỂ:                                                               |
|   ├── Yêu cầu approval từ human trước khi thực hiện action                   |
|   ├── Handle sensitive operations (financial transactions)                   |
|   ├── Compliance với regulations cần human oversight                         |
|   └── Escalate khi vượt quá authority của AI                                 |
|                                                                               |
|   EXAMPLE PROBLEM:                                                            |
|   Task: "Transfer $10,000 to account XYZ"                                    |
|   AI alone: ❌ Dangerous, cần human approval                                 |
|                                                                               |
|   → SOLUTION: Human-in-the-Loop pattern (Level 6)                            |
|                                                                               |
+===============================================================================+
```

---

## 7. Level 6: Human-in-the-Loop

### 7.1 Use Case: Content Moderation System

**Scenario**: Platform cần moderate content với AI suggestions nhưng human final decision.

**New Problem từ Level 5**:
- Một số decisions quá risky cho AI tự quyết
- Cần human oversight cho sensitive content
- Regulatory compliance requirements

**Solution**: **User Proxy Agent** - Gateway cho human interaction

```
+===============================================================================+
|                    USE CASE: CONTENT MODERATION                                |
+===============================================================================+
|                                                                               |
|   Content: "Offensive post detected..."                                       |
|                                                                               |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                   HUMAN-IN-THE-LOOP                                   │   |
|   │                                                                       │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   ANALYZER    │                                                   │   |
|   │   │   AGENT       │  → Analyzes content, detects violations           │   |
|   │   │               │  → Confidence: 75%                                │   |
|   │   └───────┬───────┘                                                   │   |
|   │           │                                                           │   |
|   │           ▼                                                           │   |
|   │   ┌───────────────┐                                                   │   |
|   │   │   DECISION    │                                                   │   |
|   │   │   ROUTER      │                                                   │   |
|   │   │               │                                                   │   |
|   │   │ if confidence │                                                   │   |
|   │   │   >= 95%:     │                                                   │   |
|   │   │   auto-remove │                                                   │   |
|   │   │               │                                                   │   |
|   │   │ if confidence │                                                   │   |
|   │   │   < 95%:      │───────────────┐                                   │   |
|   │   │   escalate    │               │                                   │   |
|   │   └───────────────┘               │                                   │   |
|   │                                   ▼                                   │   |
|   │                          ┌───────────────┐                            │   |
|   │                          │  USER PROXY   │                            │   |
|   │                          │    AGENT      │                            │   |
|   │                          │               │                            │   |
|   │                          │ Presents:     │                            │   |
|   │                          │ - Content     │                            │   |
|   │                          │ - AI analysis │                            │   |
|   │                          │ - Suggested   │                            │   |
|   │                          │   action      │                            │   |
|   │                          │               │                            │   |
|   │                          │ Awaits:       │                            │   |
|   │                          │ - Human       │                            │   |
|   │                          │   decision    │                            │   |
|   │                          └───────┬───────┘                            │   |
|   │                                  │                                    │   |
|   │                                  ▼                                    │   |
|   │                     ┌─────────────────────────┐                       │   |
|   │                     │     HUMAN MODERATOR     │                       │   |
|   │                     │                         │                       │   |
|   │                     │  Options:               │                       │   |
|   │                     │  [✓] Remove content     │                       │   |
|   │                     │  [ ] Warn user          │                       │   |
|   │                     │  [ ] No action          │                       │   |
|   │                     │  [ ] Escalate further   │                       │   |
|   │                     └─────────────────────────┘                       │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|                                                                               |
+===============================================================================+
```

### 7.2 User Proxy Agent Pattern

```
+===============================================================================+
|                    USER PROXY AGENT DETAILS                                    |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                     USER PROXY AGENT                                 │    |
|   │                                                                      │    |
|   │   PURPOSE: Bridge between AI agents and human users                  │    |
|   │                                                                      │    |
|   │   CAPABILITIES:                                                      │    |
|   │   ├── Present information to humans                                  │    |
|   │   ├── Collect human decisions/input                                  │    |
|   │   ├── Relay human feedback to agents                                 │    |
|   │   └── Handle approval workflows                                      │    |
|   │                                                                      │    |
|   │   TRIGGERS FOR HUMAN INVOLVEMENT:                                    │    |
|   │   ├── Confidence below threshold                                     │    |
|   │   ├── High-risk operations (financial, legal)                        │    |
|   │   ├── Explicit agent request for help                                │    |
|   │   ├── Periodic check-ins (every N steps)                             │    |
|   │   └── Conflict between agents                                        │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   IMPLEMENTATION PATTERNS:                                                    |
|                                                                               |
|   Pattern 1: APPROVAL GATE                                                   |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Agent A ──► [Proposed Action] ──► User Proxy ──► Human            │    |
|   │                                          │                           │    |
|   │                                          ▼                           │    |
|   │                               [Approved?] ──► Agent B (execute)      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   Pattern 2: ESCALATION                                                      |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Agent A ──► [Can't solve] ──► User Proxy ──► Human ──► Solution   │    |
|   │                                          │                           │    |
|   │                                          ▼                           │    |
|   │                               Agent A continues with solution        │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   Pattern 3: COLLABORATIVE                                                   |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Agent A ◄──► User Proxy ◄──► Human (ongoing dialogue)             │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 7.3 Limitations → Next Level

```
+===============================================================================+
|                    LEVEL 6 LIMITATIONS                                         |
+===============================================================================+
|                                                                               |
|   ❌ KHÔNG THỂ:                                                               |
|   ├── Giải quyết problems không có solution path rõ ràng                     |
|   ├── Tự adapt strategy khi gặp unexpected obstacles                         |
|   ├── Coordinate complex, multi-domain problems                              |
|   └── Self-correct và replan khi approach không work                         |
|                                                                               |
|   EXAMPLE PROBLEM:                                                            |
|   Task: "Fix production incident - unknown root cause"                       |
|   - Don't know which systems are affected                                    |
|   - Don't know what to investigate first                                     |
|   - Need to adapt as new information emerges                                 |
|                                                                               |
|   → SOLUTION: Dynamic Orchestration - Magentic-One (Level 7)                 |
|                                                                               |
+===============================================================================+
```

---

## 8. Level 7: Dynamic Orchestration (Magentic-One)

### 8.1 Use Case: Production Incident Response

**Scenario**: SRE team cần AI system tự investigate và resolve production incidents.

**New Problem từ Level 6**:
- Không biết trước solution path
- Cần dynamic planning và re-planning
- Multi-domain expertise required

**Solution**: **Magentic-One** - Generalist multi-agent system với dynamic orchestration

```
+===============================================================================+
|                    USE CASE: INCIDENT RESPONSE                                 |
+===============================================================================+
|                                                                               |
|   Alert: "API latency increased 500% - unknown cause"                        |
|                                                                               |
|   ┌──────────────────────────────────────────────────────────────────────┐   |
|   │                      MAGENTIC-ONE SYSTEM                              │   |
|   │                                                                       │   |
|   │   ┌───────────────────────────────────────────────────────────────┐   │   |
|   │   │                     ORCHESTRATOR                               │   │   |
|   │   │                   (Team Leader/PM)                             │   │   |
|   │   │                                                                │   │   |
|   │   │   Manages:                                                     │   │   |
|   │   │   • TASK LEDGER (strategic planning)                          │   │   |
|   │   │   • PROGRESS LEDGER (tactical execution)                      │   │   |
|   │   │                                                                │   │   |
|   │   │   Initial Task Ledger:                                        │   │   |
|   │   │   FACTS:   - API latency 500% higher                          │   │   |
|   │   │   GUESSES: - Could be DB, could be network, could be code     │   │   |
|   │   │   PLAN:    1. Check monitoring dashboards                     │   │   |
|   │   │            2. Analyze logs                                     │   │   |
|   │   │            3. Identify root cause                             │   │   |
|   │   │            4. Apply fix                                        │   │   |
|   │   │            5. Verify resolution                               │   │   |
|   │   └───────────────────────────────────────────────────────────────┘   │   |
|   │                              │                                        │   |
|   │        ┌─────────────────────┼─────────────────────┐                 │   |
|   │        │          │          │          │          │                 │   |
|   │        ▼          ▼          ▼          ▼          ▼                 │   |
|   │   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐       │   |
|   │   │WEBSURFER│ │FILESURFER│ │  CODER  │ │TERMINAL │ │ HUMAN  │       │   |
|   │   │         │ │         │ │         │ │         │ │ESCALATE│       │   |
|   │   │Browse   │ │Read logs│ │Analyze  │ │Run cmds │ │        │       │   |
|   │   │dashbords│ │Navigate │ │Code     │ │Execute  │ │Escalate│       │   |
|   │   │Extract  │ │Search   │ │Debug    │ │Install  │ │to human│       │   |
|   │   └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘       │   |
|   │                                                                       │   |
|   └──────────────────────────────────────────────────────────────────────┘   |
|                                                                               |
+===============================================================================+
```

### 8.2 Two-Loop Architecture

```
+===============================================================================+
|                    MAGENTIC-ONE: DUAL LOOP SYSTEM                              |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                         OUTER LOOP                                   │    |
|   │                    (Strategic Planning)                              │    |
|   │                                                                      │    |
|   │   Manages: TASK LEDGER                                               │    |
|   │   ┌─────────────────────────────────────────────────────────┐       │    |
|   │   │                                                         │       │    |
|   │   │   TASK LEDGER CONTENTS:                                │       │    |
|   │   │   ├── FACTS    → Verified information                  │       │    |
|   │   │   ├── GUESSES  → Hypotheses to verify                  │       │    |
|   │   │   └── PLAN     → Step-by-step strategy                 │       │    |
|   │   │                                                         │       │    |
|   │   └─────────────────────────────────────────────────────────┘       │    |
|   │                                                                      │    |
|   │   Triggers: When progress stalls or strategy needs revision         │    |
|   │   Action:   Updates strategic direction, revises plan               │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                              │                                                |
|                              ▼                                                |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                         INNER LOOP                                   │    |
|   │                    (Tactical Execution)                              │    |
|   │                                                                      │    |
|   │   Manages: PROGRESS LEDGER                                           │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │                                                             │   │    |
|   │   │   PROGRESS LEDGER CONTENTS:                                │   │    |
|   │   │   ├── CURRENT_PROGRESS  → What's been accomplished         │   │    |
|   │   │   ├── TASK_ASSIGNMENT   → Which agent does what            │   │    |
|   │   │   └── COMPLETION_STATUS → Is task done? Ready for next?    │   │    |
|   │   │                                                             │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                                                                      │    |
|   │   Cycle:                                                             │    |
|   │   1. Assign task to agent                                           │    |
|   │   2. Agent executes                                                 │    |
|   │   3. Check completion status                                        │    |
|   │   4. Decide: Next task OR Replan (→ Outer Loop)                     │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 8.3 Execution Flow Example

```
+===============================================================================+
|            INCIDENT RESPONSE: STEP-BY-STEP EXECUTION                           |
+===============================================================================+
|                                                                               |
|   [OUTER LOOP] Creates initial Task Ledger:                                  |
|   ───────────────────────────────────────                                    |
|   FACTS:    - API latency 500% higher                                        |
|   GUESSES:  - DB issue? Network? Code deployment?                            |
|   PLAN:     1. Check Grafana dashboard                                       |
|             2. Review recent deployments                                     |
|             3. Analyze application logs                                      |
|             4. Identify root cause                                           |
|             5. Apply fix                                                     |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   [INNER LOOP] Step 1: Assign to WebSurfer                                   |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   WebSurfer → Opens Grafana, navigates to API dashboard             │    |
|   │   Returns:  → DB query latency normal, but connection pool exhausted│    |
|   │   Status:   → ✅ Complete                                           │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   [OUTER LOOP] Updates Task Ledger:                                          |
|   ───────────────────────────────────                                        |
|   FACTS:    - API latency 500% higher                                        |
|             - DB query latency normal                                        |
|             - Connection pool exhausted ← NEW                                |
|   GUESSES:  - Connection leak in new code?                                   |
|   PLAN:     → REVISED:                                                       |
|             1. ✅ Check Grafana dashboard                                    |
|             2. Check recent deployment for connection handling               |
|             3. Review connection pool settings                               |
|             4. Apply hotfix or rollback                                      |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   [INNER LOOP] Step 2: Assign to FileSurfer + Coder                          |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   FileSurfer → Reviews deployment history                           │    |
|   │   Returns:   → Found deployment 2 hours ago                         │    |
|   │                                                                      │    |
|   │   Coder     → Analyzes diff in deployed code                        │    |
|   │   Returns:  → Found: DB connection not closed in error path         │    |
|   │   Status:   → ✅ ROOT CAUSE IDENTIFIED                              │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   [INNER LOOP] Step 3: Assign to Coder + Terminal                            |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Coder    → Writes hotfix: Add finally block to close connection   │    |
|   │   Terminal → Deploys hotfix                                         │    |
|   │   Status:  → ✅ Fix applied                                         │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   [INNER LOOP] Step 4: Verify                                                |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   WebSurfer → Checks Grafana                                        │    |
|   │   Returns:  → Latency back to normal, pool utilization dropping     │    |
|   │   Status:   → ✅ INCIDENT RESOLVED                                  │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
|   [OUTER LOOP] Final Status:                                                 |
|   ──────────────────────────                                                 |
|   FACTS:    - Root cause: Connection leak in error handling                  |
|             - Fix: Added finally block                                       |
|             - Verification: Metrics back to normal                           |
|   STATUS:   → ✅ TASK COMPLETE                                               |
|                                                                               |
+===============================================================================+
```

### 8.4 All Orchestration Patterns Summary

```
+===============================================================================+
|                    ORCHESTRATION PATTERNS COMPARISON                           |
+===============================================================================+
|                                                                               |
|   PATTERN 1: SEQUENTIAL                                                       |
|   ─────────────────────                                                       |
|       Agent A ───► Agent B ───► Agent C ───► Output                          |
|                                                                               |
|   Use: Pipelines, transformations, progressive refinement                    |
|   Example: Contract Generation (Template → Customize → Review → Risk)        |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 2: CONCURRENT                                                       |
|   ─────────────────────                                                       |
|              ┌──► Agent A ──┐                                                |
|   Input ─────┼──► Agent B ──┼───► Aggregator ───► Output                     |
|              └──► Agent C ──┘                                                |
|                                                                               |
|   Use: Multiple perspectives, time-sensitive analysis                        |
|   Example: Stock Analysis (Fundamental + Technical + Sentiment + ESG)        |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 3: GROUP CHAT                                                       |
|   ─────────────────────                                                       |
|        Agent A ◄───► Agent B ◄───► Agent C                                   |
|                   (discussion)                                                |
|                                                                               |
|   Use: Brainstorming, creative tasks, debate                                 |
|   Best Practice: Limit to ≤3 agents                                          |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 4: HANDOFF                                                          |
|   ─────────────────                                                           |
|                    ┌─────────────┐                                           |
|                    │   Triage    │                                           |
|                    └──────┬──────┘                                           |
|           ┌───────────────┼───────────────┐                                  |
|           ▼               ▼               ▼                                  |
|     [Technical]     [Billing]      [Human]                                   |
|                                                                               |
|   Use: Customer support, specialist routing                                  |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   PATTERN 5: MAGENTIC (Dynamic)                                              |
|   ─────────────────────────────                                              |
|          ┌─────────────────────────┐                                         |
|          │  ORCHESTRATOR (Dynamic) │                                         |
|          │  Task Ledger + Planning │                                         |
|          └────────────┬────────────┘                                         |
|      ┌────────────────┼────────────────┐                                     |
|      ▼        ▼       ▼        ▼       ▼                                     |
|   [Agent1] [Agent2] [Agent3] [Agent4] [Human]                                |
|                                                                               |
|   Use: Complex open-ended problems, SRE, research                            |
|                                                                               |
+===============================================================================+
```

---

## 9. Level 8: Production-Ready System

### 9.1 Use Case: Enterprise Customer Service Platform

**Scenario**: Enterprise cần full-featured customer service platform với AI.

**New Problem từ Level 7**:
- Need enterprise-grade security
- Need observability và monitoring
- Need scalability
- Need compliance

**Solution**: **Full MAF Stack** với enterprise features

```
+===============================================================================+
|                    PRODUCTION ARCHITECTURE                                     |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                         LOAD BALANCER                                │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                    │                                          |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                         API GATEWAY                                  │    |
|   │                  (Authentication, Rate Limiting)                     │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                    │                                          |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                     AGENT ORCHESTRATOR                               │    |
|   │                                                                      │    |
|   │   ┌───────────────────────────────────────────────────────────────┐ │    |
|   │   │                      MIDDLEWARE LAYER                          │ │    |
|   │   │                                                                │ │    |
|   │   │   ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │ │    |
|   │   │   │ Logging  │  │ Security │  │   Rate   │  │ Content  │     │ │    |
|   │   │   │Telemetry │  │Validation│  │ Limiting │  │ Filter   │     │ │    |
|   │   │   └──────────┘  └──────────┘  └──────────┘  └──────────┘     │ │    |
|   │   │                                                                │ │    |
|   │   └───────────────────────────────────────────────────────────────┘ │    |
|   │                                                                      │    |
|   │   ┌───────────────────────────────────────────────────────────────┐ │    |
|   │   │                      AGENT POOL                                │ │    |
|   │   │                                                                │ │    |
|   │   │   ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐        │ │    |
|   │   │   │ Triage  │  │Technical│  │ Billing │  │  Sales  │        │ │    |
|   │   │   │  Agent  │  │ Support │  │ Support │  │  Agent  │        │ │    |
|   │   │   └─────────┘  └─────────┘  └─────────┘  └─────────┘        │ │    |
|   │   │                                                                │ │    |
|   │   └───────────────────────────────────────────────────────────────┘ │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                    │                                          |
|       ┌────────────────────────────┼────────────────────────────┐             |
|       │                            │                            │             |
|       ▼                            ▼                            ▼             |
|   ┌──────────────┐         ┌──────────────┐         ┌──────────────┐         |
|   │   MODEL      │         │   MEMORY     │         │    MCP       │         |
|   │   CLIENTS    │         │   STORES     │         │   SERVERS    │         |
|   │              │         │              │         │              │         |
|   │ Azure OpenAI │         │ Redis        │         │ Database     │         |
|   │ OpenAI       │         │ Vector DB    │         │ CRM          │         |
|   │ Azure AI     │         │ Cosmos DB    │         │ Ticketing    │         |
|   └──────────────┘         └──────────────┘         └──────────────┘         |
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                     OBSERVABILITY STACK                              │    |
|   │                                                                      │    |
|   │   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │    |
|   │   │   Metrics    │  │   Tracing    │  │   Logging    │              │    |
|   │   │ (Prometheus) │  │ (OpenTelemetry)│ │ (ELK Stack) │              │    |
|   │   └──────────────┘  └──────────────┘  └──────────────┘              │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 9.2 Enterprise Features Checklist

```
+===============================================================================+
|                    PRODUCTION CHECKLIST                                        |
+===============================================================================+
|                                                                               |
|   SECURITY:                                                                   |
|   ├── □ Authentication/Authorization (OAuth2, JWT)                           |
|   ├── □ Input validation và sanitization                                     |
|   ├── □ Sensitive data handling (ID-based, not through LLM)                  |
|   ├── □ Rate limiting per user/tenant                                        |
|   ├── □ Content filtering (harmful content detection)                        |
|   └── □ Audit logging                                                        |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   RELIABILITY:                                                                |
|   ├── □ Retry logic với exponential backoff                                  |
|   ├── □ Circuit breaker pattern                                              |
|   ├── □ Graceful degradation                                                 |
|   ├── □ Health checks                                                        |
|   └── □ Disaster recovery plan                                               |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   SCALABILITY:                                                                |
|   ├── □ Horizontal scaling (stateless agents)                                |
|   ├── □ Connection pooling                                                   |
|   ├── □ Caching (responses, embeddings)                                      |
|   ├── □ Queue-based processing                                               |
|   └── □ Load balancing                                                       |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   OBSERVABILITY:                                                              |
|   ├── □ Structured logging                                                   |
|   ├── □ Distributed tracing (correlation IDs)                                |
|   ├── □ Metrics (latency, throughput, errors)                                |
|   ├── □ Alerting                                                             |
|   └── □ Dashboard                                                            |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|   COST MANAGEMENT:                                                            |
|   ├── □ Token usage tracking                                                 |
|   ├── □ Cost allocation per tenant                                           |
|   ├── □ Caching để reduce API calls                                          |
|   ├── □ Model selection strategy (cheaper models cho simple tasks)           |
|   └── □ Usage quotas                                                         |
|                                                                               |
+===============================================================================+
```

---

## 10. Comprehensive Comparison Table

### 10.1 Level Comparison

```
+===============================================================================================================+
|                                    LEVEL COMPARISON TABLE                                                      |
+===============================================================================================================+
|                                                                                                               |
| Level | Use Case              | Components               | Complexity | When to Use                          |
|-------|----------------------|--------------------------|------------|--------------------------------------|
|   1   | Simple Q&A           | Agent + LLM              | ⭐          | FAQ bots, simple chat                |
|   2   | Tool-enabled Agent   | Agent + Plugins/MCP      | ⭐⭐         | Actions needed (API, DB, files)      |
|   3   | Memory-enabled Agent | Agent + Memory + RAG     | ⭐⭐⭐        | Personalization, knowledge base      |
|   4   | Sequential Multi     | Multiple Agents          | ⭐⭐⭐        | Pipelines, workflows                 |
|   5   | Concurrent Multi     | Parallel Agents          | ⭐⭐⭐⭐       | Multiple perspectives, speed         |
|   6   | Human-in-Loop        | Agents + User Proxy      | ⭐⭐⭐⭐       | Approval, sensitive ops              |
|   7   | Dynamic Orchestrate  | Magentic-One             | ⭐⭐⭐⭐⭐      | Open-ended, unknown solutions        |
|   8   | Production           | Full Stack + Enterprise  | ⭐⭐⭐⭐⭐      | Enterprise deployment                |
|                                                                                                               |
+===============================================================================================================+
```

### 10.2 Component Selection Matrix

```
+===============================================================================================================+
|                                    COMPONENT SELECTION MATRIX                                                  |
+===============================================================================================================+
|                                                                                                               |
| Requirement                    | Semantic Kernel | AutoGen | Both (MAF) | Notes                              |
|--------------------------------|-----------------|---------|------------|-------------------------------------|
| Single capable agent           |       ✓         |         |            | SK is optimized for single agent   |
| Plugin/Tool integration        |       ✓         |         |            | SK has native plugin system        |
| Memory/RAG                     |       ✓         |         |            | SK has built-in memory             |
| Multi-agent conversation       |                 |    ✓    |            | AG specializes in multi-agent      |
| Dynamic orchestration          |                 |    ✓    |            | AG has flexible orchestration      |
| Production enterprise          |       ✓         |         |            | SK is production-ready             |
| Research/Experimentation       |                 |    ✓    |            | AG is research-oriented            |
| Complex workflows              |                 |         |     ✓      | Combine SK skills + AG orchestration|
| MCP integration                |       ✓         |    ✓    |     ✓      | Both support MCP                   |
|                                                                                                               |
+===============================================================================================================+
```

### 10.3 Orchestration Pattern Selection

```
+===============================================================================================================+
|                                    ORCHESTRATION PATTERN SELECTION                                             |
+===============================================================================================================+
|                                                                                                               |
| Situation                              | Pattern      | Agents | Example                                     |
|----------------------------------------|--------------|--------|---------------------------------------------|
| Clear linear dependencies              | Sequential   |  2-5   | Contract: Template→Customize→Review→Approve |
| Independent parallel tasks             | Concurrent   |  2-10  | Analysis: Fundamental+Technical+Sentiment   |
| Iterative refinement needed            | Group Chat   |  2-3   | Brainstorming: Designer+Developer+PM        |
| Dynamic routing by expertise           | Handoff      |  3-5   | Support: Triage→Technical/Billing/Sales     |
| Unknown solution path                  | Magentic     |  4-6   | Incident: Orchestrator+WebSurfer+Coder+Term |
| Human approval required                | Any + Proxy  |  +1    | Any pattern + UserProxyAgent                |
|                                                                                                               |
+===============================================================================================================+
```

### 10.4 MCP vs Other Integration Methods

```
+===============================================================================================================+
|                                    INTEGRATION METHODS COMPARISON                                              |
+===============================================================================================================+
|                                                                                                               |
| Method         | Best For                          | Pros                    | Cons                         |
|----------------|-----------------------------------|-------------------------|------------------------------|
| Native Code    | Custom business logic             | Full control, type-safe | Language-specific            |
| OpenAPI        | External APIs, third-party        | Cross-platform, standard| Needs spec maintenance       |
| MCP Server     | Complex integrations, standard    | Unified protocol, rich  | Learning curve, newer        |
|                |                                   | capabilities            |                              |
|                                                                                                               |
| MCP ADVANTAGES:                                                                                               |
| ├── Standardized protocol (works with any MCP-compatible agent)                                              |
| ├── Rich capability model (Tools + Resources + Prompts)                                                      |
| ├── Transport flexibility (STDIO for local, SSE for remote)                                                  |
| ├── Growing ecosystem of pre-built servers                                                                   |
| └── Microsoft native support in MAF                                                                          |
|                                                                                                               |
+===============================================================================================================+
```

### 10.5 Memory Types Comparison

```
+===============================================================================================================+
|                                    MEMORY TYPES COMPARISON                                                     |
+===============================================================================================================+
|                                                                                                               |
| Type          | Lifecycle        | Use Case                    | Storage                | Search Type      |
|---------------|------------------|-----------------------------|-----------------------|------------------|
| Short-term    | Session          | Current conversation        | In-memory             | None (sequential)|
| Long-term     | Persistent       | User preferences, facts     | Key-value store       | Exact match      |
| Semantic      | Persistent       | Knowledge base, documents   | Vector DB             | Similarity       |
|                                                                                                               |
| WHEN TO USE WHICH:                                                                                            |
| ├── Short-term: Chat context, temporary variables                                                            |
| ├── Long-term: User data that needs to persist (preferences, history)                                        |
| └── Semantic: Large document collections, need semantic search (RAG)                                         |
|                                                                                                               |
+===============================================================================================================+
```

---

## 11. Decision Framework

### 11.1 Architecture Decision Tree

```
+===============================================================================+
|                    ARCHITECTURE DECISION TREE                                  |
+===============================================================================+
|                                                                               |
|                         [Start: What are you building?]                       |
|                                      │                                        |
|            ┌─────────────────────────┼─────────────────────────┐              |
|            │                         │                         │              |
|       Single Agent?          Multi-Agent?              Workflow?              |
|            │                         │                         │              |
|    ┌───────┴───────┐         ┌──────┴──────┐          ┌──────┴──────┐        |
|    │               │         │             │          │             │        |
| Simple?       Complex?    Dynamic?    Structured?  Graph?    Sequential?     |
|    │               │         │             │          │             │        |
|    │               │         │             │          │             │        |
|    ▼               ▼         ▼             ▼          ▼             ▼        |
| Level 1        Level 2-3   Level 7      Level 4-6  Level 7      Level 4      |
| (SK Agent)   (SK+Plugin)  (Magentic)   (AutoGen)  (Magentic)  (Sequential)   |
|              (SK+Memory)                                                      |
|                                                                               |
|   ─────────────────────────────────────────────────────────────────────────  |
|                                                                               |
|                    [Need human approval?]                                     |
|                            │                                                  |
|              ┌─────────────┴─────────────┐                                    |
|              │                           │                                    |
|             YES                          NO                                   |
|              │                           │                                    |
|              ▼                           │                                    |
|      Add UserProxyAgent                  │                                    |
|        (Level 6)                         │                                    |
|              │                           │                                    |
|              └───────────────────────────┘                                    |
|                            │                                                  |
|                            ▼                                                  |
|                    [Enterprise deployment?]                                   |
|                            │                                                  |
|              ┌─────────────┴─────────────┐                                    |
|              │                           │                                    |
|             YES                          NO                                   |
|              │                           │                                    |
|              ▼                           ▼                                    |
|         Level 8                    Current Level                              |
|   (Full Production Stack)          is sufficient                              |
|                                                                               |
+===============================================================================+
```

### 11.2 Quick Selection Guide

```
+===============================================================================+
|                    QUICK SELECTION GUIDE                                       |
+===============================================================================+
|                                                                               |
|   "I want to..."                                    → Use                     |
|   ──────────────────────────────────────────────────────────────────────────  |
|   Build a simple chatbot                            → Level 1 (SK Agent)      |
|   Make my agent call APIs                           → Level 2 (SK + Plugins)  |
|   Add memory and document search                    → Level 3 (SK + Memory)   |
|   Create a code review pipeline                     → Level 4 (Sequential)    |
|   Get multiple analyses in parallel                 → Level 5 (Concurrent)    |
|   Add human approval for sensitive actions          → Level 6 (Human-in-Loop) |
|   Solve complex, undefined problems                 → Level 7 (Magentic-One)  |
|   Deploy to production with enterprise features     → Level 8 (Full Stack)    |
|                                                                               |
+===============================================================================+
```

---

## 12. Appendix: MCP Integration Details

### 12.1 MCP in MAF Context

```
+===============================================================================+
|                    MCP INTEGRATION IN MAF                                      |
+===============================================================================+
|                                                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                    MICROSOFT AGENT FRAMEWORK                         │    |
|   │                                                                      │    |
|   │   ┌─────────────────────────────────────────────────────────────┐   │    |
|   │   │                    MCP CLIENT                                │   │    |
|   │   │              (Built into MAF Runtime)                        │   │    |
|   │   │                                                              │   │    |
|   │   │   Features:                                                  │   │    |
|   │   │   • Automatic tool discovery                                 │   │    |
|   │   │   • Resource access                                          │   │    |
|   │   │   • Prompt templates                                         │   │    |
|   │   │   • Transport abstraction (STDIO/SSE)                        │   │    |
|   │   │                                                              │   │    |
|   │   └─────────────────────────────────────────────────────────────┘   │    |
|   │                              │                                       │    |
|   └──────────────────────────────│───────────────────────────────────────┘    |
|                                  │                                            |
|                                  ▼                                            |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │                        MCP SERVERS                                   │    |
|   │                                                                      │    |
|   │   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │    |
|   │   │   Database   │  │   Browser    │  │   Custom     │              │    |
|   │   │   Server     │  │   Server     │  │   Server     │              │    |
|   │   │              │  │              │  │              │              │    |
|   │   │ Tools:       │  │ Tools:       │  │ Tools:       │              │    |
|   │   │ - query      │  │ - navigate   │  │ - your_tool  │              │    |
|   │   │ - insert     │  │ - click      │  │ - your_tool2 │              │    |
|   │   │ - update     │  │ - extract    │  │              │              │    |
|   │   │              │  │              │  │ Resources:   │              │    |
|   │   │ Resources:   │  │ Resources:   │  │ - your_data  │              │    |
|   │   │ - schema     │  │ - screenshot │  │              │              │    |
|   │   └──────────────┘  └──────────────┘  └──────────────┘              │    |
|   │                                                                      │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                                                                               |
+===============================================================================+
```

### 12.2 MCP Server Implementation Pattern

```
+===============================================================================+
|                    MCP SERVER STRUCTURE                                        |
+===============================================================================+
|                                                                               |
|   MCP Server "my-database-server":                                           |
|   │                                                                           |
|   ├── TOOLS (Actions the LLM can invoke)                                     |
|   │   │                                                                       |
|   │   ├── query_database                                                     |
|   │   │   ├── description: "Execute SQL query"                               |
|   │   │   ├── parameters: {sql: string, limit?: number}                      |
|   │   │   └── returns: QueryResult[]                                         |
|   │   │                                                                       |
|   │   └── get_table_schema                                                   |
|   │       ├── description: "Get schema of a table"                           |
|   │       ├── parameters: {table_name: string}                               |
|   │       └── returns: Schema                                                |
|   │                                                                           |
|   ├── RESOURCES (Data the LLM can access)                                    |
|   │   │                                                                       |
|   │   ├── database://schemas                                                 |
|   │   │   └── All table schemas                                              |
|   │   │                                                                       |
|   │   └── database://stats                                                   |
|   │       └── Database statistics                                            |
|   │                                                                           |
|   └── PROMPTS (Templates for common operations)                              |
|       │                                                                       |
|       └── analyze_query_performance                                          |
|           └── Template for query analysis                                    |
|                                                                               |
+===============================================================================+
```

### 12.3 Popular MCP Servers for MAF

```
+===============================================================================+
|                    POPULAR MCP SERVERS                                         |
+===============================================================================+
|                                                                               |
| Server          | Purpose                    | Tools                          |
|-----------------|----------------------------|--------------------------------|
| @modelcontextprotocol/server-filesystem                                       |
|                 | File system access         | read, write, list, search      |
|                                                                               |
| @modelcontextprotocol/server-postgres                                         |
|                 | PostgreSQL database        | query, schema, tables          |
|                                                                               |
| @anthropic/mcp-server-puppeteer                                               |
|                 | Browser automation         | navigate, click, type, screenshot|
|                                                                               |
| @anthropic/mcp-server-github                                                  |
|                 | GitHub integration         | repos, issues, PRs, actions    |
|                                                                               |
| Custom Server   | Your business logic        | Any tools you define           |
|                                                                               |
+===============================================================================+
```

---

## Glossary

| Term | Definition |
|------|------------|
| **Agent** | AI entity: input → reasoning → action → output |
| **AutoGen** | Microsoft Research framework for multi-agent orchestration |
| **Concurrent** | Multiple agents running in parallel |
| **Function Calling** | LLM capability to request function execution |
| **Handoff** | Transfer control from one agent to another |
| **Human-in-Loop** | Pattern requiring human approval/input |
| **Magentic-One** | Dynamic multi-agent system with dual-loop architecture |
| **MAF** | Microsoft Agent Framework (SK + AutoGen) |
| **MCP** | Model Context Protocol - standardized tool integration |
| **Orchestrator** | Agent that coordinates other agents |
| **Plugin** | Collection of functions for agent use |
| **RAG** | Retrieval Augmented Generation |
| **Semantic Kernel** | Microsoft SDK for single agent + plugins + memory |
| **Sequential** | Agents running one after another |
| **Task Ledger** | Strategic planning document in Magentic-One |
| **User Proxy** | Agent pattern for human interaction |
| **Vector Store** | Database for semantic similarity search |

---

## References

| Resource | URL |
|----------|-----|
| Microsoft Agent Framework | [learn.microsoft.com](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview) |
| Semantic Kernel Blog | [devblogs.microsoft.com/semantic-kernel](https://devblogs.microsoft.com/semantic-kernel/) |
| AutoGen Blog | [devblogs.microsoft.com/autogen](https://devblogs.microsoft.com/autogen/) |
| MCP Specification | [modelcontextprotocol.io](https://modelcontextprotocol.io/) |
| MCP Servers | [github.com/modelcontextprotocol/servers](https://github.com/modelcontextprotocol/servers) |

---

**End of Document**
