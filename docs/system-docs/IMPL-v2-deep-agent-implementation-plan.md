# IMPL-v2: Deep Agent System Implementation Plan

**Document ID**: IMPL-v2-deep-agent-implementation-plan
**Created**: 2026-01-13
**Based On**: [Over-v2-deep-agent-architecture-redesign.md](Over-v2-deep-agent-architecture-redesign.md)
**Status**: Implementation Plan - Ready for Execution
**Version**: v2

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Implementation Overview](#2-implementation-overview)
3. [Phase 0: Foundation](#3-phase-0-foundation)
4. [Phase 1: Memory Layer](#4-phase-1-memory-layer)
5. [Phase 2: MCP Infrastructure](#5-phase-2-mcp-infrastructure)
6. [Phase 3: Leader Agent](#6-phase-3-leader-agent)
7. [Phase 4: Integration & Optimization](#7-phase-4-integration--optimization)
8. [Use Case Tests](#8-use-case-tests)
9. [Validation Criteria](#9-validation-criteria)
10. [Risk Mitigation](#10-risk-mitigation)
11. [References](#11-references)

---

## 1. Executive Summary

### 1.1 Purpose

This document provides a **detailed, step-by-step implementation plan** to transform the v1 edu_mul_agents system into v2 architecture. The plan addresses:

| Problem | v2 Solution | Implementation Phase |
|---------|-------------|---------------------|
| **Context Lost** | Three-Tier Memory System | Phase 1 |
| **Token Exhaustion** | MCP Server Architecture | Phase 2 |
| **No Intelligent Orchestration** | Leader Agent Pattern | Phase 3 |
| **High Latency** | Optimized Pipeline | Phase 4 |

### 1.2 Success Metrics

```
+==============================================================================+
|                         SUCCESS METRICS                                       |
+==============================================================================+

┌─────────────────────────────┬─────────────┬─────────────┬───────────────────┐
│ Metric                      │ v1 Current  │ v2 Target   │ Measurement       │
├─────────────────────────────┼─────────────┼─────────────┼───────────────────┤
│ Context Retention           │ ~5 turns    │ Unlimited   │ Turn coherence    │
│ Cross-Session Memory        │ None        │ Yes         │ Recall accuracy   │
│ Response Latency            │ ~2s         │ <1.5s       │ End-to-end timing │
│ Token Usage per Turn        │ ~12K        │ ~4K         │ API metrics       │
│ Tool Extensibility          │ Code change │ Add server  │ Deployment time   │
└─────────────────────────────┴─────────────┴─────────────┴───────────────────┘
```

### 1.3 Phase Dependencies

```
+==============================================================================+
|                    IMPLEMENTATION PHASE DEPENDENCIES                          |
+==============================================================================+

                              ┌─────────────────────┐
                              │   Phase 4: Full     │
                              │   Integration       │
                              │   + E2E Testing     │
                              └──────────┬──────────┘
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
                    ▼                    │                    │
          ┌─────────────────┐            │                    │
          │ Phase 3:        │◀───────────┴────────────────────┤
          │ Leader Agent    │                                 │
          └────────┬────────┘                                 │
                   │                                          │
        ┌──────────┴──────────┐                              │
        │ REQUIRES BOTH       │                              │
        ▼                     ▼                              │
┌─────────────────┐  ┌─────────────────┐                     │
│ Phase 1:        │  │ Phase 2:        │                     │
│ Memory Layer    │  │ MCP Servers     │                     │
└────────┬────────┘  └────────┬────────┘                     │
         │                    │                              │
         │ CAN RUN IN         │                              │
         │ PARALLEL           │                              │
         └─────────┬──────────┘                              │
                   │                                         │
                   ▼                                         │
         ┌─────────────────────────────┐                     │
         │ Phase 0: Foundation         │◀────────────────────┘
         │ (Refactor v1)               │
         └─────────────────────────────┘

CRITICAL PATH: Phase 0 → (Phase 1 || Phase 2) → Phase 3 → Phase 4
```

---

## 2. Implementation Overview

### 2.1 Technology Stack

```
+==============================================================================+
|                         v2 TECHNOLOGY STACK                                   |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│  COMPONENT         │  TECHNOLOGY              │  PURPOSE                     │
├────────────────────┼──────────────────────────┼──────────────────────────────┤
│  Web Framework     │  FastAPI 0.109+          │  Async API + WebSocket       │
│  Leader Agent      │  OpenAI GPT-4o / Claude  │  Orchestration & reasoning   │
│  Voice Interface   │  Azure OpenAI Realtime   │  STT + TTS + VAD             │
├────────────────────┼──────────────────────────┼──────────────────────────────┤
│  Session Memory    │  Redis 7.x               │  Fast key-value, 24h TTL     │
│  Long-term Memory  │  Qdrant / Pinecone       │  Vector DB, semantic search  │
│  Graph (Optional)  │  Neo4j                   │  Relationship queries        │
├────────────────────┼──────────────────────────┼──────────────────────────────┤
│  MCP Runtime       │  Python + FastMCP        │  MCP server implementation   │
│  Authentication    │  OAuth 2.1 / Entra ID    │  Secure MCP access           │
│  Education API     │  Microsoft Graph         │  Student data source         │
├────────────────────┼──────────────────────────┼──────────────────────────────┤
│  Testing           │  pytest + pytest-asyncio │  Unit + Integration tests    │
│  Load Testing      │  Locust / k6             │  Performance validation      │
│  Monitoring        │  OpenTelemetry           │  Distributed tracing         │
└────────────────────┴──────────────────────────┴──────────────────────────────┘
```

### 2.2 Directory Structure (Target)

```
edu_mul_agents_v2/
├── src/
│   ├── leader_agent/              # Phase 3
│   │   ├── __init__.py
│   │   ├── agent.py               # Main Leader Agent class
│   │   ├── intent_analyzer.py     # Intent analysis module
│   │   ├── memory_manager.py      # Memory query orchestration
│   │   ├── tool_selector.py       # MCP tool selection
│   │   └── response_builder.py    # Response synthesis
│   │
│   ├── memory/                    # Phase 1
│   │   ├── __init__.py
│   │   ├── working_memory.py      # In-context memory
│   │   ├── session_memory.py      # Redis-based session
│   │   ├── longterm_memory.py     # Vector DB operations
│   │   ├── summarizer.py          # Context summarization
│   │   └── models.py              # Memory data models
│   │
│   ├── mcp_servers/               # Phase 2
│   │   ├── education_mcp/
│   │   │   ├── __init__.py
│   │   │   ├── server.py          # MCP server entry
│   │   │   ├── tools.py           # Tool definitions
│   │   │   ├── resources.py       # Resource definitions
│   │   │   └── graph_client.py    # MS Graph integration
│   │   ├── memory_mcp/
│   │   │   ├── __init__.py
│   │   │   ├── server.py
│   │   │   └── tools.py
│   │   └── advice_mcp/
│   │       ├── __init__.py
│   │       ├── server.py
│   │       └── tools.py
│   │
│   ├── voice/                     # Phase 0 (refactored)
│   │   ├── __init__.py
│   │   ├── realtime_client.py     # Azure Realtime WebSocket
│   │   ├── delegate_tool.py       # delegate_to_leader function
│   │   └── audio_handler.py       # PCM16 audio processing
│   │
│   ├── api/                       # Phase 0 (refactored)
│   │   ├── __init__.py
│   │   ├── main.py                # FastAPI app
│   │   ├── routers/
│   │   │   ├── auth.py
│   │   │   ├── voice.py
│   │   │   └── health.py
│   │   └── websockets/
│   │       └── voice_ws.py
│   │
│   └── common/
│       ├── config.py              # Pydantic settings
│       ├── models.py              # Shared data models
│       └── logging.py             # Structured logging
│
├── tests/
│   ├── unit/
│   │   ├── test_memory/
│   │   ├── test_mcp/
│   │   └── test_leader/
│   ├── integration/
│   │   ├── test_memory_integration.py
│   │   ├── test_mcp_integration.py
│   │   └── test_leader_integration.py
│   └── e2e/
│       ├── test_voice_flow.py
│       ├── test_context_recovery.py
│       └── test_student_switching.py
│
├── scripts/
│   ├── setup_redis.sh
│   ├── setup_qdrant.sh
│   └── run_mcp_servers.sh
│
├── docker/
│   ├── docker-compose.yml
│   ├── Dockerfile.api
│   ├── Dockerfile.mcp-education
│   └── Dockerfile.mcp-memory
│
├── requirements.txt
├── requirements-dev.txt
└── pyproject.toml
```

---

## 3. Phase 0: Foundation

### 3.1 Phase Overview

```
+==============================================================================+
|                    PHASE 0: FOUNDATION                                        |
+==============================================================================+

OBJECTIVE: Prepare v1 codebase for v2 component integration

DELIVERABLES:
  • Modular code structure ready for new components
  • Test harness for voice workflow validation
  • Basic in-memory session storage
  • Extracted tool definitions

DURATION ESTIMATE: Foundation work (baseline)

DEPENDENCIES: None (starting point)
```

### 3.2 Step-by-Step Implementation

#### Step P0-S1: Code Structure Refactoring

**Objective**: Restructure v1 code into modular architecture

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P0.1 │ Create new directory structure per Section 2.2                      │
│ P0.2 │ Extract RealtimeVoiceService into voice/ module                     │
│ P0.3 │ Extract tool definitions into separate JSON/Python files            │
│ P0.4 │ Create common/ module for shared models and config                  │
│ P0.5 │ Set up dependency injection container                               │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation Details**:

```python
# P0.3: Tool definitions extraction
# File: src/voice/tools/education_tools.py

EDUCATION_TOOLS = [
    {
        "type": "function",
        "name": "get_student_profile",
        "description": "Get detailed profile for a student",
        "parameters": {
            "type": "object",
            "properties": {
                "student_id": {
                    "type": "string",
                    "description": "The student's unique identifier"
                }
            },
            "required": ["student_id"]
        }
    },
    # ... other tools
]

# This replaces inline tool definitions in every session.update call
```

#### Step P0-S2: Test Harness Setup

**Objective**: Create testing infrastructure for voice workflows

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P0.6 │ Set up pytest with pytest-asyncio                                   │
│ P0.7 │ Create mock Azure Realtime API client for testing                   │
│ P0.8 │ Create fixture for simulating voice conversations                   │
│ P0.9 │ Add baseline latency measurement tests                              │
│ P0.10│ Document test execution procedures                                  │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Test Fixture Example**:

```python
# File: tests/conftest.py

import pytest
from unittest.mock import AsyncMock

@pytest.fixture
def mock_realtime_client():
    """Mock Azure OpenAI Realtime API client"""
    client = AsyncMock()
    client.send_text = AsyncMock(return_value=None)
    client.receive_response = AsyncMock(return_value={
        "type": "response.done",
        "response": {
            "output": [{"content": [{"text": "Test response"}]}]
        }
    })
    return client

@pytest.fixture
def voice_conversation_simulator():
    """Simulate multi-turn voice conversation"""
    class ConversationSimulator:
        def __init__(self):
            self.turns = []

        async def say(self, text: str) -> str:
            """Simulate user speaking and getting response"""
            self.turns.append({"role": "user", "content": text})
            # ... simulation logic
            return "simulated response"

    return ConversationSimulator()
```

#### Step P0-S3: Basic Session Storage

**Objective**: Add in-memory session storage as stepping stone

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P0.11│ Create SessionStore interface (Protocol)                            │
│ P0.12│ Implement InMemorySessionStore                                      │
│ P0.13│ Add session_id generation and tracking                              │
│ P0.14│ Store conversation turns in session                                 │
│ P0.15│ Add TTL for session cleanup                                         │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Interface Definition**:

```python
# File: src/memory/interfaces.py

from typing import Protocol, Optional, List
from dataclasses import dataclass
from datetime import datetime

@dataclass
class ConversationTurn:
    role: str  # "user" or "assistant"
    content: str
    timestamp: datetime
    metadata: dict = None

class SessionStore(Protocol):
    """Interface for session storage implementations"""

    async def create_session(self, user_id: str) -> str:
        """Create new session, return session_id"""
        ...

    async def add_turn(self, session_id: str, turn: ConversationTurn) -> None:
        """Add conversation turn to session"""
        ...

    async def get_turns(self, session_id: str, limit: int = 10) -> List[ConversationTurn]:
        """Get recent conversation turns"""
        ...

    async def get_context(self, session_id: str) -> dict:
        """Get session context (active student, preferences, etc.)"""
        ...

    async def set_context(self, session_id: str, key: str, value: any) -> None:
        """Set session context value"""
        ...
```

### 3.3 Phase 0 Testing

```
+==============================================================================+
|                    PHASE 0: TEST CASES                                        |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T0-U1: Tool Definition Loading                                              │
│   Given: Tool definitions in external file                                  │
│   When: Application starts                                                  │
│   Then: All 11 tools loaded correctly with schemas                          │
│                                                                             │
│ T0-U2: Session Creation                                                     │
│   Given: User with valid ID                                                 │
│   When: create_session() called                                             │
│   Then: Unique session_id returned, session stored                          │
│                                                                             │
│ T0-U3: Turn Storage                                                         │
│   Given: Active session                                                     │
│   When: add_turn() called with ConversationTurn                             │
│   Then: Turn stored, retrievable via get_turns()                            │
│                                                                             │
│ T0-U4: Session TTL                                                          │
│   Given: Session with 1-hour TTL                                            │
│   When: TTL expires                                                         │
│   Then: Session automatically cleaned up                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Integration Tests                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T0-I1: Voice Flow Baseline                                                  │
│   Given: Mock Realtime API                                                  │
│   When: User speaks "Hello"                                                 │
│   Then: Response generated within 2s (baseline)                             │
│                                                                             │
│ T0-I2: Tool Execution                                                       │
│   Given: Loaded tool definitions                                            │
│   When: function_call event received                                        │
│   Then: Correct tool executed, result returned                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.4 Phase 0 Acceptance Criteria

```
+==============================================================================+
|                    PHASE 0: ACCEPTANCE CRITERIA                               |
+==============================================================================+

┌──────┬────────────────────────────────────────────────┬───────────┬─────────┐
│ ID   │ Criteria                                       │ Measure   │ Status  │
├──────┼────────────────────────────────────────────────┼───────────┼─────────┤
│ AC01 │ All tool definitions externalized              │ File count│ ⬜      │
│ AC02 │ pytest test suite runs successfully            │ Pass rate │ ⬜      │
│ AC03 │ SessionStore interface implemented             │ Code check│ ⬜      │
│ AC04 │ InMemorySessionStore passes all tests          │ Test pass │ ⬜      │
│ AC05 │ Voice flow baseline latency documented         │ < 2s      │ ⬜      │
│ AC06 │ Directory structure matches Section 2.2        │ Structure │ ⬜      │
└──────┴────────────────────────────────────────────────┴───────────┴─────────┘
```

---

## 4. Phase 1: Memory Layer

### 4.1 Phase Overview

```
+==============================================================================+
|                    PHASE 1: MEMORY LAYER                                      |
+==============================================================================+

OBJECTIVE: Implement Three-Tier Memory System to solve context loss

DELIVERABLES:
  • Working Memory management (in-context)
  • Session Memory with Redis
  • Long-term Memory with Vector DB
  • Summarization pipeline
  • memory-mcp server with basic tools

DEPENDENCIES: Phase 0 completed

PARALLEL: Can run alongside Phase 2

KEY BENEFIT: Solves "context lost" problem
```

### 4.2 Three-Tier Memory Architecture

```
+==============================================================================+
|                    THREE-TIER MEMORY IMPLEMENTATION                           |
+==============================================================================+

                    ┌─────────────────────────────────────────────────────────┐
                    │                    MEMORY MANAGER                        │
                    │              (Coordinates all memory operations)         │
                    └─────────────────────────────┬───────────────────────────┘
                                                  │
           ┌──────────────────────────────────────┼──────────────────────────┐
           │                                      │                          │
           ▼                                      ▼                          ▼
┌─────────────────────────┐       ┌─────────────────────────┐   ┌─────────────────────────┐
│     TIER 1: WORKING     │       │    TIER 2: SESSION      │   │    TIER 3: LONG-TERM    │
│        MEMORY           │       │       MEMORY            │   │        MEMORY           │
├─────────────────────────┤       ├─────────────────────────┤   ├─────────────────────────┤
│ STORAGE: In-Context     │       │ STORAGE: Redis          │   │ STORAGE: Qdrant         │
│ TTL: Current turn       │       │ TTL: 24 hours           │   │ TTL: Permanent          │
│ SIZE: ~2K tokens        │       │ SIZE: ~50 messages      │   │ SIZE: Unlimited         │
│ ACCESS: Synchronous     │       │ ACCESS: <10ms           │   │ ACCESS: <100ms          │
└─────────────────────────┘       └─────────────────────────┘   └─────────────────────────┘

READ PRIORITY:  Working → Session → Long-term (fast to slow)
WRITE TRIGGERS: End of turn → Session; Important fact → Long-term
```

### 4.3 Step-by-Step Implementation

#### Step P1-S1: Working Memory Module

**Objective**: Manage in-context memory for Leader Agent

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P1.1 │ Create WorkingMemory class with token budget tracking               │
│ P1.2 │ Implement context builder (formats memory for LLM prompt)           │
│ P1.3 │ Add active student tracking                                         │
│ P1.4 │ Add pending tool results buffer                                     │
│ P1.5 │ Implement context priority (what to include when near limit)        │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/memory/working_memory.py

from dataclasses import dataclass, field
from typing import Optional, List, Dict, Any
import tiktoken

@dataclass
class WorkingMemoryState:
    """Current working memory state"""
    user_id: str
    session_id: str
    active_student_id: Optional[str] = None
    active_student_name: Optional[str] = None
    current_message: Optional[str] = None
    pending_tool_results: List[Dict[str, Any]] = field(default_factory=list)
    recent_context_summary: Optional[str] = None
    user_preferences: Dict[str, Any] = field(default_factory=dict)

class WorkingMemory:
    """
    Tier 1: In-context memory for Leader Agent

    Token budget: ~2000 tokens
    Lifetime: Single turn
    """

    MAX_TOKENS = 2000

    def __init__(self):
        self._encoder = tiktoken.encoding_for_model("gpt-4")
        self._state: Optional[WorkingMemoryState] = None

    def initialize(self, user_id: str, session_id: str) -> None:
        """Initialize working memory for new request"""
        self._state = WorkingMemoryState(
            user_id=user_id,
            session_id=session_id
        )

    def set_active_student(self, student_id: str, student_name: str) -> None:
        """Set currently active student context"""
        self._state.active_student_id = student_id
        self._state.active_student_name = student_name

    def set_current_message(self, message: str) -> None:
        """Set current user message"""
        self._state.current_message = message

    def add_tool_result(self, tool_name: str, result: Any) -> None:
        """Add pending tool result"""
        self._state.pending_tool_results.append({
            "tool": tool_name,
            "result": result
        })

    def set_context_summary(self, summary: str) -> None:
        """Set recent conversation summary from session memory"""
        self._state.recent_context_summary = summary

    def build_context(self) -> str:
        """
        Build context string for Leader Agent prompt
        Respects token budget, prioritizes most important info
        """
        sections = []

        # Priority 1: Current request (always included)
        sections.append(f"## Current Request\n{self._state.current_message}")

        # Priority 2: Active student (critical for queries)
        if self._state.active_student_id:
            sections.append(
                f"## Active Student\n"
                f"- Name: {self._state.active_student_name}\n"
                f"- ID: {self._state.active_student_id}"
            )

        # Priority 3: Recent context summary
        if self._state.recent_context_summary:
            sections.append(
                f"## Recent Conversation\n{self._state.recent_context_summary}"
            )

        # Priority 4: User preferences
        if self._state.user_preferences:
            prefs = "\n".join(f"- {k}: {v}" for k, v in self._state.user_preferences.items())
            sections.append(f"## User Preferences\n{prefs}")

        # Build and check token count
        context = "\n\n".join(sections)
        tokens = len(self._encoder.encode(context))

        # Trim if over budget (remove lowest priority sections)
        while tokens > self.MAX_TOKENS and len(sections) > 2:
            sections.pop()  # Remove lowest priority
            context = "\n\n".join(sections)
            tokens = len(self._encoder.encode(context))

        return context

    def get_token_usage(self) -> int:
        """Get current token usage"""
        return len(self._encoder.encode(self.build_context()))
```

#### Step P1-S2: Session Memory with Redis

**Objective**: Implement Redis-based session storage

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P1.6 │ Set up Redis connection with connection pooling                     │
│ P1.7 │ Implement SessionMemory class with Redis backend                    │
│ P1.8 │ Add chat history storage (list of turns)                            │
│ P1.9 │ Add session state storage (active student, flags)                   │
│ P1.10│ Implement 24-hour TTL with automatic cleanup                        │
│ P1.11│ Add get_recent_turns() with configurable limit                      │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/memory/session_memory.py

import json
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any
import redis.asyncio as redis
from .models import ConversationTurn

class SessionMemory:
    """
    Tier 2: Session-based memory using Redis

    TTL: 24 hours
    Access time: <10ms
    Contents: Chat history, session state, temporary context
    """

    DEFAULT_TTL = 86400  # 24 hours in seconds
    MAX_TURNS = 50

    def __init__(self, redis_url: str = "redis://localhost:6379"):
        self._redis: Optional[redis.Redis] = None
        self._redis_url = redis_url

    async def connect(self) -> None:
        """Establish Redis connection"""
        self._redis = redis.from_url(
            self._redis_url,
            encoding="utf-8",
            decode_responses=True
        )

    async def close(self) -> None:
        """Close Redis connection"""
        if self._redis:
            await self._redis.close()

    def _session_key(self, session_id: str) -> str:
        return f"session:{session_id}"

    def _turns_key(self, session_id: str) -> str:
        return f"session:{session_id}:turns"

    def _state_key(self, session_id: str) -> str:
        return f"session:{session_id}:state"

    async def create_session(self, user_id: str, session_id: str) -> None:
        """Create new session"""
        session_data = {
            "user_id": user_id,
            "session_id": session_id,
            "created_at": datetime.utcnow().isoformat(),
            "turn_count": 0
        }
        await self._redis.hset(
            self._session_key(session_id),
            mapping=session_data
        )
        await self._redis.expire(self._session_key(session_id), self.DEFAULT_TTL)

    async def add_turn(self, session_id: str, turn: ConversationTurn) -> None:
        """Add conversation turn to session history"""
        turn_data = json.dumps({
            "role": turn.role,
            "content": turn.content,
            "timestamp": turn.timestamp.isoformat(),
            "metadata": turn.metadata
        })

        # Add to list (RPUSH for chronological order)
        await self._redis.rpush(self._turns_key(session_id), turn_data)

        # Trim to max turns (keep most recent)
        await self._redis.ltrim(self._turns_key(session_id), -self.MAX_TURNS, -1)

        # Refresh TTL
        await self._redis.expire(self._turns_key(session_id), self.DEFAULT_TTL)

        # Update turn count
        await self._redis.hincrby(self._session_key(session_id), "turn_count", 1)

    async def get_recent_turns(
        self,
        session_id: str,
        limit: int = 10
    ) -> List[ConversationTurn]:
        """Get recent conversation turns"""
        turns_data = await self._redis.lrange(
            self._turns_key(session_id),
            -limit,
            -1
        )

        turns = []
        for turn_json in turns_data:
            data = json.loads(turn_json)
            turns.append(ConversationTurn(
                role=data["role"],
                content=data["content"],
                timestamp=datetime.fromisoformat(data["timestamp"]),
                metadata=data.get("metadata")
            ))
        return turns

    async def set_state(self, session_id: str, key: str, value: Any) -> None:
        """Set session state value"""
        await self._redis.hset(
            self._state_key(session_id),
            key,
            json.dumps(value) if not isinstance(value, str) else value
        )
        await self._redis.expire(self._state_key(session_id), self.DEFAULT_TTL)

    async def get_state(self, session_id: str, key: str) -> Optional[Any]:
        """Get session state value"""
        value = await self._redis.hget(self._state_key(session_id), key)
        if value:
            try:
                return json.loads(value)
            except json.JSONDecodeError:
                return value
        return None

    async def get_all_state(self, session_id: str) -> Dict[str, Any]:
        """Get all session state"""
        state = await self._redis.hgetall(self._state_key(session_id))
        result = {}
        for k, v in state.items():
            try:
                result[k] = json.loads(v)
            except json.JSONDecodeError:
                result[k] = v
        return result
```

#### Step P1-S3: Long-term Memory with Vector DB

**Objective**: Implement semantic memory with Qdrant

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P1.12│ Set up Qdrant connection                                            │
│ P1.13│ Create embedding function (OpenAI text-embedding-3-small)           │
│ P1.14│ Implement LongTermMemory class                                      │
│ P1.15│ Add store_memory() with embeddings                                  │
│ P1.16│ Add search_memory() with semantic similarity                        │
│ P1.17│ Add salience scoring for importance detection                       │
│ P1.18│ Implement memory categories (preference, fact, summary)             │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/memory/longterm_memory.py

from datetime import datetime
from typing import List, Optional, Dict, Any
from dataclasses import dataclass
from enum import Enum
import openai
from qdrant_client import QdrantClient
from qdrant_client.models import (
    Distance, VectorParams, PointStruct, Filter, FieldCondition, MatchValue
)
import uuid

class MemoryCategory(Enum):
    PREFERENCE = "preference"     # User preferences
    FACT = "fact"                 # Important facts about students
    SUMMARY = "summary"           # Session summaries
    INTERACTION = "interaction"   # Notable interactions

@dataclass
class MemoryRecord:
    id: str
    user_id: str
    content: str
    category: MemoryCategory
    salience: float  # 0.0 to 1.0, importance score
    created_at: datetime
    metadata: Dict[str, Any]

class LongTermMemory:
    """
    Tier 3: Long-term memory using Vector DB

    TTL: Permanent
    Access time: <100ms
    Contents: User preferences, student profiles, session summaries
    """

    COLLECTION_NAME = "edu_agent_memory"
    EMBEDDING_MODEL = "text-embedding-3-small"
    EMBEDDING_DIM = 1536

    def __init__(
        self,
        qdrant_url: str = "http://localhost:6333",
        openai_api_key: str = None
    ):
        self._client = QdrantClient(url=qdrant_url)
        self._openai = openai.AsyncOpenAI(api_key=openai_api_key)
        self._initialized = False

    async def initialize(self) -> None:
        """Initialize collection if not exists"""
        collections = self._client.get_collections().collections
        if not any(c.name == self.COLLECTION_NAME for c in collections):
            self._client.create_collection(
                collection_name=self.COLLECTION_NAME,
                vectors_config=VectorParams(
                    size=self.EMBEDDING_DIM,
                    distance=Distance.COSINE
                )
            )
        self._initialized = True

    async def _get_embedding(self, text: str) -> List[float]:
        """Generate embedding for text"""
        response = await self._openai.embeddings.create(
            model=self.EMBEDDING_MODEL,
            input=text
        )
        return response.data[0].embedding

    async def store_memory(
        self,
        user_id: str,
        content: str,
        category: MemoryCategory,
        salience: float = 0.5,
        metadata: Dict[str, Any] = None
    ) -> str:
        """Store memory with embedding"""
        memory_id = str(uuid.uuid4())
        embedding = await self._get_embedding(content)

        point = PointStruct(
            id=memory_id,
            vector=embedding,
            payload={
                "user_id": user_id,
                "content": content,
                "category": category.value,
                "salience": salience,
                "created_at": datetime.utcnow().isoformat(),
                "metadata": metadata or {}
            }
        )

        self._client.upsert(
            collection_name=self.COLLECTION_NAME,
            points=[point]
        )

        return memory_id

    async def search_memory(
        self,
        user_id: str,
        query: str,
        category: Optional[MemoryCategory] = None,
        limit: int = 5,
        min_salience: float = 0.0
    ) -> List[MemoryRecord]:
        """Search memories by semantic similarity"""
        query_embedding = await self._get_embedding(query)

        # Build filter
        filter_conditions = [
            FieldCondition(key="user_id", match=MatchValue(value=user_id)),
            FieldCondition(key="salience", range={"gte": min_salience})
        ]
        if category:
            filter_conditions.append(
                FieldCondition(key="category", match=MatchValue(value=category.value))
            )

        results = self._client.search(
            collection_name=self.COLLECTION_NAME,
            query_vector=query_embedding,
            query_filter=Filter(must=filter_conditions),
            limit=limit
        )

        memories = []
        for result in results:
            payload = result.payload
            memories.append(MemoryRecord(
                id=result.id,
                user_id=payload["user_id"],
                content=payload["content"],
                category=MemoryCategory(payload["category"]),
                salience=payload["salience"],
                created_at=datetime.fromisoformat(payload["created_at"]),
                metadata=payload.get("metadata", {})
            ))

        return memories

    async def get_user_preferences(self, user_id: str) -> List[MemoryRecord]:
        """Get all user preferences"""
        return await self.search_memory(
            user_id=user_id,
            query="user preferences and settings",
            category=MemoryCategory.PREFERENCE,
            limit=10
        )

    async def get_last_session_summary(self, user_id: str) -> Optional[str]:
        """Get most recent session summary"""
        memories = await self.search_memory(
            user_id=user_id,
            query="last session conversation summary",
            category=MemoryCategory.SUMMARY,
            limit=1
        )
        return memories[0].content if memories else None
```

#### Step P1-S4: Summarization Pipeline

**Objective**: Implement context compression

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P1.19│ Create Summarizer class                                             │
│ P1.20│ Implement turn summarization (compress N turns to summary)          │
│ P1.21│ Implement session summarization (end-of-session)                    │
│ P1.22│ Add salience detection for important facts                          │
│ P1.23│ Integrate summarization triggers in memory flow                     │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/memory/summarizer.py

from typing import List, Tuple
import openai
from .models import ConversationTurn

class Summarizer:
    """
    Summarization pipeline for context compression

    Triggers:
    - Every N turns: Compress oldest messages
    - Session end: Create session summary
    - Important fact: Extract and store in long-term
    """

    SUMMARIZE_AFTER_TURNS = 5
    MAX_SUMMARY_TOKENS = 200

    def __init__(self, openai_api_key: str = None):
        self._openai = openai.AsyncOpenAI(api_key=openai_api_key)

    async def summarize_turns(
        self,
        turns: List[ConversationTurn],
        max_tokens: int = None
    ) -> str:
        """Compress conversation turns into summary"""
        max_tokens = max_tokens or self.MAX_SUMMARY_TOKENS

        conversation_text = "\n".join(
            f"{t.role}: {t.content}" for t in turns
        )

        response = await self._openai.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "system",
                    "content": (
                        "Summarize this conversation concisely. "
                        "Focus on: topics discussed, decisions made, "
                        "important information mentioned. "
                        "Keep it brief for context injection."
                    )
                },
                {
                    "role": "user",
                    "content": conversation_text
                }
            ],
            max_tokens=max_tokens
        )

        return response.choices[0].message.content

    async def create_session_summary(
        self,
        turns: List[ConversationTurn],
        user_name: str = None
    ) -> str:
        """Create end-of-session summary for long-term storage"""
        conversation_text = "\n".join(
            f"{t.role}: {t.content}" for t in turns
        )

        response = await self._openai.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "system",
                    "content": (
                        "Create a session summary that captures: "
                        "1. Main topics discussed "
                        "2. Any student-related information mentioned "
                        "3. User preferences observed "
                        "4. Unresolved questions or next steps. "
                        "This will be used to provide context in future sessions."
                    )
                },
                {
                    "role": "user",
                    "content": f"Session for {user_name or 'user'}:\n\n{conversation_text}"
                }
            ],
            max_tokens=300
        )

        return response.choices[0].message.content

    async def detect_salient_facts(
        self,
        content: str
    ) -> List[Tuple[str, float]]:
        """
        Detect important facts worth storing in long-term memory
        Returns list of (fact, salience_score) tuples
        """
        response = await self._openai.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "system",
                    "content": (
                        "Extract important facts from the conversation that should be "
                        "remembered long-term. Focus on: "
                        "- User preferences (language, format, interests) "
                        "- Student information (performance patterns, challenges) "
                        "- Explicit requests to remember something. "
                        "Return JSON array of {fact: string, salience: 0.0-1.0}. "
                        "Salience 1.0 = critically important, 0.0 = not important."
                    )
                },
                {
                    "role": "user",
                    "content": content
                }
            ],
            response_format={"type": "json_object"},
            max_tokens=500
        )

        import json
        result = json.loads(response.choices[0].message.content)
        return [(item["fact"], item["salience"]) for item in result.get("facts", [])]
```

#### Step P1-S5: Memory MCP Server

**Objective**: Create MCP server for memory operations

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P1.24│ Set up FastMCP server for memory operations                         │
│ P1.25│ Implement recall tool (semantic search)                             │
│ P1.26│ Implement store tool (add to long-term)                             │
│ P1.27│ Implement search tool (filtered queries)                            │
│ P1.28│ Implement summarize tool (manual trigger)                           │
│ P1.29│ Add resources: chat_history, user_preferences                       │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/mcp_servers/memory_mcp/server.py

from fastmcp import FastMCP
from typing import Optional, List
from datetime import datetime

from ...memory.session_memory import SessionMemory
from ...memory.longterm_memory import LongTermMemory, MemoryCategory
from ...memory.summarizer import Summarizer

# Initialize MCP server
mcp = FastMCP("memory-mcp")

# Initialize memory components (configured via environment)
session_memory = SessionMemory()
longterm_memory = LongTermMemory()
summarizer = Summarizer()

@mcp.tool()
async def recall(
    user_id: str,
    query: str,
    category: Optional[str] = None,
    limit: int = 5
) -> dict:
    """
    Recall memories relevant to a query using semantic search

    Args:
        user_id: The user's identifier
        query: What to search for (semantic)
        category: Filter by category (preference, fact, summary)
        limit: Maximum memories to return
    """
    category_enum = MemoryCategory(category) if category else None

    memories = await longterm_memory.search_memory(
        user_id=user_id,
        query=query,
        category=category_enum,
        limit=limit
    )

    return {
        "memories": [
            {
                "content": m.content,
                "category": m.category.value,
                "salience": m.salience,
                "created_at": m.created_at.isoformat()
            }
            for m in memories
        ],
        "count": len(memories)
    }

@mcp.tool()
async def store(
    user_id: str,
    content: str,
    category: str,
    salience: float = 0.5,
    metadata: Optional[dict] = None
) -> dict:
    """
    Store a new memory in long-term storage

    Args:
        user_id: The user's identifier
        content: The memory content to store
        category: Memory category (preference, fact, summary)
        salience: Importance score 0.0-1.0
        metadata: Additional metadata
    """
    memory_id = await longterm_memory.store_memory(
        user_id=user_id,
        content=content,
        category=MemoryCategory(category),
        salience=salience,
        metadata=metadata
    )

    return {
        "status": "stored",
        "memory_id": memory_id,
        "category": category,
        "salience": salience
    }

@mcp.tool()
async def summarize_conversation(
    session_id: str,
    turn_count: int = 10
) -> dict:
    """
    Summarize recent conversation turns

    Args:
        session_id: Current session ID
        turn_count: Number of turns to summarize
    """
    turns = await session_memory.get_recent_turns(session_id, limit=turn_count)
    summary = await summarizer.summarize_turns(turns)

    return {
        "summary": summary,
        "turns_summarized": len(turns)
    }

@mcp.resource("chat_history/{session_id}")
async def get_chat_history(session_id: str) -> str:
    """Get recent chat history for a session"""
    turns = await session_memory.get_recent_turns(session_id, limit=20)

    history = []
    for turn in turns:
        history.append(f"[{turn.timestamp.strftime('%H:%M')}] {turn.role}: {turn.content}")

    return "\n".join(history)

@mcp.resource("user_preferences/{user_id}")
async def get_user_preferences(user_id: str) -> str:
    """Get stored user preferences"""
    memories = await longterm_memory.get_user_preferences(user_id)

    prefs = []
    for m in memories:
        prefs.append(f"- {m.content} (salience: {m.salience})")

    return "\n".join(prefs) if prefs else "No preferences stored yet."

# Run server
if __name__ == "__main__":
    import asyncio
    asyncio.run(session_memory.connect())
    asyncio.run(longterm_memory.initialize())
    mcp.run()
```

### 4.4 Phase 1 Testing

```
+==============================================================================+
|                    PHASE 1: TEST CASES                                        |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Working Memory                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T1-U1: Token Budget Enforcement                                             │
│   Given: Working memory with 2000 token limit                               │
│   When: Context exceeds budget                                              │
│   Then: Lower priority sections removed                                     │
│                                                                             │
│ T1-U2: Context Building                                                     │
│   Given: Active student, recent summary, preferences                        │
│   When: build_context() called                                              │
│   Then: Formatted context string with all sections                          │
│                                                                             │
│ T1-U3: Tool Result Buffer                                                   │
│   Given: Multiple tool results pending                                      │
│   When: add_tool_result() called                                            │
│   Then: Results buffered in order                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Session Memory                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T1-U4: Session Creation                                                     │
│   Given: User ID                                                            │
│   When: create_session() called                                             │
│   Then: Session created in Redis with TTL                                   │
│                                                                             │
│ T1-U5: Turn Storage and Retrieval                                           │
│   Given: Active session                                                     │
│   When: 20 turns added                                                      │
│   Then: get_recent_turns(10) returns last 10                                │
│                                                                             │
│ T1-U6: State Management                                                     │
│   Given: Active session                                                     │
│   When: set_state("active_student", "lucy-123")                             │
│   Then: get_state("active_student") returns "lucy-123"                      │
│                                                                             │
│ T1-U7: TTL Expiration                                                       │
│   Given: Session with 1-second TTL (test mode)                              │
│   When: 2 seconds pass                                                      │
│   Then: Session data no longer retrievable                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Long-term Memory                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T1-U8: Memory Storage                                                       │
│   Given: User preference content                                            │
│   When: store_memory() called                                               │
│   Then: Memory stored with embedding in Qdrant                              │
│                                                                             │
│ T1-U9: Semantic Search                                                      │
│   Given: 10 stored memories about Math and Science                          │
│   When: search_memory("Math grades")                                        │
│   Then: Math-related memories ranked higher                                 │
│                                                                             │
│ T1-U10: Category Filtering                                                  │
│   Given: Memories in multiple categories                                    │
│   When: search with category=PREFERENCE                                     │
│   Then: Only preference memories returned                                   │
│                                                                             │
│ T1-U11: Salience Filtering                                                  │
│   Given: Memories with various salience scores                              │
│   When: search with min_salience=0.7                                        │
│   Then: Only high-salience memories returned                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Integration Tests                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T1-I1: Memory Flow Integration                                              │
│   Given: New user session                                                   │
│   When: User has 5-turn conversation                                        │
│   Then:                                                                     │
│     - All turns stored in session memory                                    │
│     - Summary generated after 5 turns                                       │
│     - Important facts stored in long-term                                   │
│                                                                             │
│ T1-I2: Context Recovery                                                     │
│   Given: User with previous session (ended yesterday)                       │
│   When: New session starts                                                  │
│   Then:                                                                     │
│     - Last session summary retrieved                                        │
│     - User preferences loaded                                               │
│     - Working memory populated correctly                                    │
│                                                                             │
│ T1-I3: Memory MCP Server                                                    │
│   Given: Running memory-mcp server                                          │
│   When: recall tool called with query                                       │
│   Then: Relevant memories returned via MCP protocol                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.5 Phase 1 Acceptance Criteria

```
+==============================================================================+
|                    PHASE 1: ACCEPTANCE CRITERIA                               |
+==============================================================================+

┌──────┬────────────────────────────────────────────────┬───────────┬─────────┐
│ ID   │ Criteria                                       │ Measure   │ Status  │
├──────┼────────────────────────────────────────────────┼───────────┼─────────┤
│ AC11 │ WorkingMemory respects 2K token budget         │ Unit test │ ⬜      │
│ AC12 │ SessionMemory stores/retrieves 50+ turns       │ Unit test │ ⬜      │
│ AC13 │ SessionMemory TTL works correctly              │ Unit test │ ⬜      │
│ AC14 │ LongTermMemory semantic search accuracy >80%   │ Eval test │ ⬜      │
│ AC15 │ Summarizer produces coherent summaries         │ Manual QA │ ⬜      │
│ AC16 │ memory-mcp server responds in <50ms            │ Perf test │ ⬜      │
│ AC17 │ Context recovery loads previous session        │ Int. test │ ⬜      │
│ AC18 │ Redis connection pool handles 100 concurrent   │ Load test │ ⬜      │
└──────┴────────────────────────────────────────────────┴───────────┴─────────┘
```

---

## 5. Phase 2: MCP Infrastructure

### 5.1 Phase Overview

```
+==============================================================================+
|                    PHASE 2: MCP INFRASTRUCTURE                                |
+==============================================================================+

OBJECTIVE: Replace v1 function calling with MCP server architecture

DELIVERABLES:
  • education-mcp server with Graph API integration
  • OAuth 2.1 authentication for MCP
  • MCP client in application
  • Tool migration from v1

DEPENDENCIES: Phase 0 completed

PARALLEL: Can run alongside Phase 1

KEY BENEFIT: Zero-token tool definitions + secure + extensible
```

### 5.2 MCP Server Architecture

```
+==============================================================================+
|                    MCP SERVER IMPLEMENTATION                                  |
+==============================================================================+

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                           LEADER AGENT (MCP CLIENT)                          │
  └─────────────────────────────────────┬───────────────────────────────────────┘
                                        │ MCP Protocol (JSON-RPC)
        ┌───────────────────────────────┼───────────────────────────────┐
        │                               │                               │
        ▼                               ▼                               ▼
┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
│ education-mcp-server  │   │   memory-mcp-server   │   │   advice-mcp-server   │
│ (Phase 2)             │   │   (Phase 1)           │   │   (Phase 4)           │
├───────────────────────┤   ├───────────────────────┤   ├───────────────────────┤
│ TOOLS:                │   │ TOOLS:                │   │ TOOLS:                │
│ • get_student_profile │   │ • recall              │   │ • generate_advice     │
│ • get_classes         │   │ • store               │   │ • analyze_performance │
│ • get_assignments     │   │ • search              │   │ • suggest_resources   │
│ • get_grades          │   │ • summarize           │   │                       │
│ • get_teachers        │   │                       │   │                       │
│ • get_schedule        │   │                       │   │                       │
├───────────────────────┤   ├───────────────────────┤   ├───────────────────────┤
│ AUTH: OAuth 2.1       │   │ AUTH: OAuth 2.1       │   │ AUTH: OAuth 2.1       │
│ BACKEND: MS Graph     │   │ BACKEND: Redis+Qdrant │   │ BACKEND: GPT-4o       │
└───────────────────────┘   └───────────────────────┘   └───────────────────────┘
```

### 5.3 Step-by-Step Implementation

#### Step P2-S1: Education MCP Server

**Objective**: Create MCP server for education data

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P2.1 │ Set up FastMCP server structure                                     │
│ P2.2 │ Create Microsoft Graph client with token management                 │
│ P2.3 │ Implement get_student_profile tool                                  │
│ P2.4 │ Implement get_classes tool                                          │
│ P2.5 │ Implement get_assignments tool                                      │
│ P2.6 │ Implement get_grades tool                                           │
│ P2.7 │ Implement get_teachers tool                                         │
│ P2.8 │ Implement get_schedule tool                                         │
│ P2.9 │ Add resources: student_roster, class_catalog                        │
│ P2.10│ Add prompts: format_grades, summarize_schedule                      │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/mcp_servers/education_mcp/server.py

from fastmcp import FastMCP
from typing import Optional, List
from datetime import datetime, date

from .graph_client import GraphClient

# Initialize MCP server
mcp = FastMCP("education-mcp")

# Initialize Graph client
graph_client = GraphClient()

@mcp.tool()
async def get_student_profile(
    student_id: str,
    fields: Optional[List[str]] = None
) -> dict:
    """
    Get detailed profile for a student

    Args:
        student_id: The student's unique identifier
        fields: Optional list of specific fields to return
    """
    profile = await graph_client.get_student(student_id)

    if fields:
        profile = {k: v for k, v in profile.items() if k in fields}

    return {
        "student": profile,
        "retrieved_at": datetime.utcnow().isoformat()
    }

@mcp.tool()
async def get_classes(
    student_id: str,
    include_teachers: bool = False,
    term: Optional[str] = None
) -> dict:
    """
    Get classes for a student

    Args:
        student_id: The student's identifier
        include_teachers: Include teacher info for each class
        term: Filter by academic term (e.g., "2025-Spring")
    """
    classes = await graph_client.get_student_classes(
        student_id,
        include_teachers=include_teachers
    )

    if term:
        classes = [c for c in classes if c.get("term") == term]

    return {
        "classes": classes,
        "count": len(classes),
        "student_id": student_id
    }

@mcp.tool()
async def get_assignments(
    student_id: str,
    class_id: Optional[str] = None,
    status: Optional[str] = None,
    due_before: Optional[str] = None,
    due_after: Optional[str] = None
) -> dict:
    """
    Get assignments for a student

    Args:
        student_id: The student's identifier
        class_id: Filter by specific class
        status: Filter by status (assigned, submitted, graded, late)
        due_before: ISO date string for due date filter
        due_after: ISO date string for due date filter
    """
    assignments = await graph_client.get_student_assignments(
        student_id,
        class_id=class_id
    )

    # Apply filters
    if status:
        assignments = [a for a in assignments if a.get("status") == status]

    if due_before:
        due_date = date.fromisoformat(due_before)
        assignments = [a for a in assignments
                      if date.fromisoformat(a.get("due_date", "9999-12-31")) <= due_date]

    if due_after:
        due_date = date.fromisoformat(due_after)
        assignments = [a for a in assignments
                      if date.fromisoformat(a.get("due_date", "1900-01-01")) >= due_date]

    return {
        "assignments": assignments,
        "count": len(assignments),
        "filters_applied": {
            "class_id": class_id,
            "status": status,
            "due_before": due_before,
            "due_after": due_after
        }
    }

@mcp.tool()
async def get_grades(
    student_id: str,
    class_id: Optional[str] = None,
    assignment_id: Optional[str] = None,
    subject: Optional[str] = None
) -> dict:
    """
    Get grades for a student

    Args:
        student_id: The student's identifier
        class_id: Filter by specific class
        assignment_id: Get grade for specific assignment
        subject: Filter by subject (e.g., "Math", "Science")
    """
    grades = await graph_client.get_student_grades(
        student_id,
        class_id=class_id,
        assignment_id=assignment_id
    )

    if subject:
        grades = [g for g in grades
                 if subject.lower() in g.get("class_name", "").lower()]

    # Calculate summary
    if grades:
        scores = [g.get("score", 0) for g in grades if g.get("score") is not None]
        avg_score = sum(scores) / len(scores) if scores else None
    else:
        avg_score = None

    return {
        "grades": grades,
        "count": len(grades),
        "average_score": avg_score,
        "student_id": student_id
    }

@mcp.tool()
async def get_teachers(
    class_id: Optional[str] = None,
    student_id: Optional[str] = None
) -> dict:
    """
    Get teachers for a class or all teachers for a student

    Args:
        class_id: Get teacher for specific class
        student_id: Get all teachers for a student's classes
    """
    if class_id:
        teachers = await graph_client.get_class_teachers(class_id)
    elif student_id:
        teachers = await graph_client.get_student_teachers(student_id)
    else:
        return {"error": "Either class_id or student_id required"}

    return {
        "teachers": teachers,
        "count": len(teachers)
    }

@mcp.tool()
async def get_schedule(
    student_id: str,
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    include_assignments: bool = False
) -> dict:
    """
    Get schedule for a student

    Args:
        student_id: The student's identifier
        date_from: Start date (ISO format)
        date_to: End date (ISO format)
        include_assignments: Include assignment due dates in schedule
    """
    schedule = await graph_client.get_student_schedule(
        student_id,
        date_from=date_from,
        date_to=date_to
    )

    if include_assignments:
        assignments = await graph_client.get_student_assignments(
            student_id,
            date_from=date_from,
            date_to=date_to
        )
        # Merge assignments into schedule
        for a in assignments:
            schedule.append({
                "type": "assignment_due",
                "title": a.get("title"),
                "date": a.get("due_date"),
                "class": a.get("class_name")
            })
        schedule.sort(key=lambda x: x.get("date", ""))

    return {
        "schedule": schedule,
        "date_range": {"from": date_from, "to": date_to},
        "student_id": student_id
    }

# Resources (read-only data)
@mcp.resource("student_roster/{parent_id}")
async def get_student_roster(parent_id: str) -> str:
    """Get list of students for a parent"""
    students = await graph_client.get_parent_students(parent_id)

    roster = []
    for s in students:
        roster.append(f"- {s['name']} (ID: {s['id']}, Grade: {s.get('grade', 'N/A')})")

    return "\n".join(roster) if roster else "No students found."

@mcp.resource("class_catalog/{student_id}")
async def get_class_catalog(student_id: str) -> str:
    """Get class catalog for a student"""
    classes = await graph_client.get_student_classes(student_id)

    catalog = []
    for c in classes:
        catalog.append(
            f"- {c['name']} ({c.get('subject', 'N/A')})\n"
            f"  Teacher: {c.get('teacher', 'TBD')}\n"
            f"  Schedule: {c.get('schedule', 'N/A')}"
        )

    return "\n\n".join(catalog) if catalog else "No classes found."

# Prompts (templates)
@mcp.prompt("format_grades")
async def format_grades_prompt(grades_data: dict) -> str:
    """Format grades data for voice output"""
    return f"""
Format the following grades for a parent in a conversational, easy-to-understand way:

Grades Data:
{grades_data}

Guidelines:
- Start with an overall summary
- Highlight any concerning or excellent grades
- Keep it concise for voice output (under 3 sentences unless asked for details)
"""

# Run server
if __name__ == "__main__":
    mcp.run()
```

#### Step P2-S2: Microsoft Graph Client

**Objective**: Create authenticated Graph API client

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P2.11│ Create GraphClient class with MSAL authentication                   │
│ P2.12│ Implement token caching and refresh                                 │
│ P2.13│ Add Education Roster API methods                                    │
│ P2.14│ Add Assignment API methods                                          │
│ P2.15│ Add error handling with retry logic                                 │
│ P2.16│ Add response caching for frequently accessed data                   │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/mcp_servers/education_mcp/graph_client.py

import httpx
from typing import Optional, List, Dict, Any
from datetime import datetime, timedelta
from msal import ConfidentialClientApplication
from functools import lru_cache
import asyncio

class GraphClient:
    """
    Microsoft Graph API client for Education endpoints

    Endpoints used:
    - /education/me/classes
    - /education/me/assignments
    - /education/classes/{id}/members
    - /education/users/{id}
    """

    BASE_URL = "https://graph.microsoft.com/v1.0"
    SCOPES = [
        "https://graph.microsoft.com/.default"
    ]

    def __init__(
        self,
        tenant_id: str = None,
        client_id: str = None,
        client_secret: str = None
    ):
        import os
        self._tenant_id = tenant_id or os.getenv("AZURE_TENANT_ID")
        self._client_id = client_id or os.getenv("AZURE_CLIENT_ID")
        self._client_secret = client_secret or os.getenv("AZURE_CLIENT_SECRET")

        self._msal_app = ConfidentialClientApplication(
            self._client_id,
            authority=f"https://login.microsoftonline.com/{self._tenant_id}",
            client_credential=self._client_secret
        )

        self._token_cache: Dict[str, Any] = {}
        self._data_cache: Dict[str, Any] = {}
        self._cache_ttl = timedelta(minutes=5)

    async def _get_token(self) -> str:
        """Get access token with caching"""
        cached = self._token_cache.get("access_token")
        if cached and cached["expires_at"] > datetime.utcnow():
            return cached["token"]

        result = self._msal_app.acquire_token_for_client(scopes=self.SCOPES)

        if "access_token" in result:
            self._token_cache["access_token"] = {
                "token": result["access_token"],
                "expires_at": datetime.utcnow() + timedelta(seconds=result["expires_in"] - 60)
            }
            return result["access_token"]
        else:
            raise Exception(f"Failed to acquire token: {result.get('error_description')}")

    async def _request(
        self,
        method: str,
        endpoint: str,
        params: Dict = None,
        use_cache: bool = True
    ) -> Dict[str, Any]:
        """Make authenticated request to Graph API"""
        cache_key = f"{method}:{endpoint}:{params}"

        # Check cache
        if use_cache and cache_key in self._data_cache:
            cached = self._data_cache[cache_key]
            if cached["expires_at"] > datetime.utcnow():
                return cached["data"]

        token = await self._get_token()

        async with httpx.AsyncClient() as client:
            response = await client.request(
                method=method,
                url=f"{self.BASE_URL}{endpoint}",
                headers={"Authorization": f"Bearer {token}"},
                params=params,
                timeout=30.0
            )
            response.raise_for_status()
            data = response.json()

        # Cache response
        if use_cache:
            self._data_cache[cache_key] = {
                "data": data,
                "expires_at": datetime.utcnow() + self._cache_ttl
            }

        return data

    async def get_student(self, student_id: str) -> Dict[str, Any]:
        """Get student profile"""
        result = await self._request("GET", f"/education/users/{student_id}")
        return {
            "id": result.get("id"),
            "name": result.get("displayName"),
            "email": result.get("mail"),
            "grade": result.get("grade"),
            "school": result.get("school", {}).get("displayName")
        }

    async def get_student_classes(
        self,
        student_id: str,
        include_teachers: bool = False
    ) -> List[Dict[str, Any]]:
        """Get classes for a student"""
        result = await self._request("GET", f"/education/users/{student_id}/classes")

        classes = []
        for c in result.get("value", []):
            class_data = {
                "id": c.get("id"),
                "name": c.get("displayName"),
                "subject": c.get("description"),
                "term": c.get("term", {}).get("displayName")
            }

            if include_teachers:
                teachers = await self.get_class_teachers(c.get("id"))
                class_data["teachers"] = teachers

            classes.append(class_data)

        return classes

    async def get_student_assignments(
        self,
        student_id: str,
        class_id: Optional[str] = None,
        date_from: Optional[str] = None,
        date_to: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """Get assignments for a student"""
        if class_id:
            endpoint = f"/education/classes/{class_id}/assignments"
        else:
            endpoint = f"/education/users/{student_id}/assignments"

        result = await self._request("GET", endpoint)

        assignments = []
        for a in result.get("value", []):
            assignments.append({
                "id": a.get("id"),
                "title": a.get("displayName"),
                "class_id": a.get("classId"),
                "class_name": a.get("className"),
                "due_date": a.get("dueDateTime"),
                "status": a.get("status"),
                "instructions": a.get("instructions", {}).get("content")
            })

        return assignments

    async def get_student_grades(
        self,
        student_id: str,
        class_id: Optional[str] = None,
        assignment_id: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """Get grades for a student"""
        # Get submissions with grades
        if assignment_id and class_id:
            endpoint = f"/education/classes/{class_id}/assignments/{assignment_id}/submissions"
            params = {"$filter": f"submittedBy/user/id eq '{student_id}'"}
        else:
            # Get all submissions for student
            assignments = await self.get_student_assignments(student_id, class_id)
            grades = []
            for a in assignments:
                if a.get("status") == "graded":
                    submission = await self._request(
                        "GET",
                        f"/education/classes/{a['class_id']}/assignments/{a['id']}/submissions",
                        params={"$filter": f"submittedBy/user/id eq '{student_id}'"}
                    )
                    if submission.get("value"):
                        sub = submission["value"][0]
                        grades.append({
                            "assignment_id": a["id"],
                            "assignment_title": a["title"],
                            "class_name": a.get("class_name"),
                            "score": sub.get("outcomes", [{}])[0].get("points", {}).get("points"),
                            "max_score": sub.get("outcomes", [{}])[0].get("points", {}).get("maximum"),
                            "graded_at": sub.get("returnedDateTime")
                        })
            return grades

    async def get_class_teachers(self, class_id: str) -> List[Dict[str, Any]]:
        """Get teachers for a class"""
        result = await self._request("GET", f"/education/classes/{class_id}/teachers")
        return [
            {
                "id": t.get("id"),
                "name": t.get("displayName"),
                "email": t.get("mail")
            }
            for t in result.get("value", [])
        ]

    async def get_student_teachers(self, student_id: str) -> List[Dict[str, Any]]:
        """Get all teachers for a student's classes"""
        classes = await self.get_student_classes(student_id)
        teachers = []
        seen_ids = set()

        for c in classes:
            class_teachers = await self.get_class_teachers(c["id"])
            for t in class_teachers:
                if t["id"] not in seen_ids:
                    t["classes"] = [c["name"]]
                    teachers.append(t)
                    seen_ids.add(t["id"])
                else:
                    # Add class to existing teacher
                    for existing in teachers:
                        if existing["id"] == t["id"]:
                            existing["classes"].append(c["name"])

        return teachers

    async def get_student_schedule(
        self,
        student_id: str,
        date_from: Optional[str] = None,
        date_to: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """Get schedule for a student"""
        classes = await self.get_student_classes(student_id)

        # Note: Full calendar integration would use /users/{id}/calendar/events
        # For education, we return class schedule
        schedule = []
        for c in classes:
            schedule.append({
                "type": "class",
                "title": c["name"],
                "subject": c.get("subject"),
                "schedule": c.get("schedule", "See class details")
            })

        return schedule

    async def get_parent_students(self, parent_id: str) -> List[Dict[str, Any]]:
        """Get students associated with a parent"""
        # Note: This typically requires tenant-specific configuration
        # Using relatedContacts for education
        result = await self._request("GET", f"/education/users/{parent_id}/students")
        return [
            {
                "id": s.get("id"),
                "name": s.get("displayName"),
                "grade": s.get("grade")
            }
            for s in result.get("value", [])
        ]
```

#### Step P2-S3: OAuth 2.1 Authentication

**Objective**: Implement secure MCP authentication

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P2.17│ Create OAuth middleware for MCP servers                             │
│ P2.18│ Implement token validation                                          │
│ P2.19│ Add scope checking for tools                                        │
│ P2.20│ Implement rate limiting per user                                    │
│ P2.21│ Add audit logging for tool invocations                              │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/mcp_servers/common/auth.py

from functools import wraps
from typing import List, Optional, Callable
import jwt
from datetime import datetime
import httpx

class MCPAuthMiddleware:
    """
    OAuth 2.1 authentication middleware for MCP servers

    Validates tokens and checks scopes before tool execution
    """

    REQUIRED_SCOPES = {
        "get_student_profile": ["education.read", "education.roster"],
        "get_classes": ["education.read"],
        "get_assignments": ["education.read"],
        "get_grades": ["education.read"],
        "get_teachers": ["education.roster"],
        "get_schedule": ["education.read"],
        "recall": ["memory.read"],
        "store": ["memory.write"],
        "search": ["memory.read"],
        "summarize": ["memory.write"],
    }

    def __init__(
        self,
        tenant_id: str,
        client_id: str,
        issuer: str = None
    ):
        self._tenant_id = tenant_id
        self._client_id = client_id
        self._issuer = issuer or f"https://login.microsoftonline.com/{tenant_id}/v2.0"
        self._jwks_cache = None
        self._jwks_expires = None

    async def _get_jwks(self) -> dict:
        """Fetch JWKS for token validation"""
        if self._jwks_cache and self._jwks_expires > datetime.utcnow():
            return self._jwks_cache

        async with httpx.AsyncClient() as client:
            response = await client.get(
                f"{self._issuer}/.well-known/openid-configuration"
            )
            config = response.json()

            jwks_response = await client.get(config["jwks_uri"])
            self._jwks_cache = jwks_response.json()
            self._jwks_expires = datetime.utcnow() + timedelta(hours=1)

        return self._jwks_cache

    async def validate_token(self, token: str) -> dict:
        """Validate OAuth token and return claims"""
        jwks = await self._get_jwks()

        # Decode header to get key ID
        header = jwt.get_unverified_header(token)
        kid = header.get("kid")

        # Find matching key
        key = None
        for k in jwks.get("keys", []):
            if k.get("kid") == kid:
                key = jwt.algorithms.RSAAlgorithm.from_jwk(k)
                break

        if not key:
            raise ValueError("Token signing key not found")

        # Validate token
        claims = jwt.decode(
            token,
            key,
            algorithms=["RS256"],
            audience=self._client_id,
            issuer=self._issuer
        )

        return claims

    def check_scopes(self, tool_name: str, token_scopes: List[str]) -> bool:
        """Check if token has required scopes for tool"""
        required = self.REQUIRED_SCOPES.get(tool_name, [])
        return all(scope in token_scopes for scope in required)

    def require_auth(self, tool_name: str):
        """Decorator to require authentication for MCP tool"""
        def decorator(func: Callable):
            @wraps(func)
            async def wrapper(*args, **kwargs):
                # Extract token from MCP context
                # (Implementation depends on FastMCP context handling)
                token = kwargs.pop("_auth_token", None)

                if not token:
                    return {"error": "Authentication required", "code": 401}

                try:
                    claims = await self.validate_token(token)
                    token_scopes = claims.get("scp", "").split()

                    if not self.check_scopes(tool_name, token_scopes):
                        return {
                            "error": "Insufficient permissions",
                            "required_scopes": self.REQUIRED_SCOPES.get(tool_name),
                            "code": 403
                        }

                    # Add user context to kwargs
                    kwargs["_user_id"] = claims.get("oid")
                    kwargs["_user_name"] = claims.get("name")

                    return await func(*args, **kwargs)

                except jwt.ExpiredSignatureError:
                    return {"error": "Token expired", "code": 401}
                except jwt.InvalidTokenError as e:
                    return {"error": f"Invalid token: {str(e)}", "code": 401}

            return wrapper
        return decorator
```

#### Step P2-S4: MCP Client Integration

**Objective**: Create MCP client for Leader Agent

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P2.22│ Create MCPClient class                                              │
│ P2.23│ Implement tool discovery                                            │
│ P2.24│ Implement tool invocation with JSON-RPC                             │
│ P2.25│ Add connection pooling for MCP servers                              │
│ P2.26│ Add retry logic with exponential backoff                            │
│ P2.27│ Implement circuit breaker pattern                                   │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/leader_agent/mcp_client.py

import httpx
from typing import Dict, Any, List, Optional
from dataclasses import dataclass
from datetime import datetime, timedelta
import asyncio
import json

@dataclass
class MCPServer:
    name: str
    url: str
    tools: List[Dict[str, Any]] = None
    resources: List[Dict[str, Any]] = None
    last_discovery: datetime = None

class CircuitBreaker:
    """Circuit breaker for MCP server calls"""

    def __init__(self, failure_threshold: int = 5, reset_timeout: int = 60):
        self._failure_count = 0
        self._failure_threshold = failure_threshold
        self._reset_timeout = reset_timeout
        self._last_failure: Optional[datetime] = None
        self._state = "closed"  # closed, open, half-open

    def record_failure(self):
        self._failure_count += 1
        self._last_failure = datetime.utcnow()
        if self._failure_count >= self._failure_threshold:
            self._state = "open"

    def record_success(self):
        self._failure_count = 0
        self._state = "closed"

    def can_execute(self) -> bool:
        if self._state == "closed":
            return True
        if self._state == "open":
            if datetime.utcnow() - self._last_failure > timedelta(seconds=self._reset_timeout):
                self._state = "half-open"
                return True
            return False
        return True  # half-open

class MCPClient:
    """
    MCP Client for Leader Agent

    Connects to MCP servers and invokes tools
    """

    def __init__(self):
        self._servers: Dict[str, MCPServer] = {}
        self._circuit_breakers: Dict[str, CircuitBreaker] = {}
        self._http_client: Optional[httpx.AsyncClient] = None

    async def connect(self):
        """Initialize HTTP client"""
        self._http_client = httpx.AsyncClient(timeout=30.0)

    async def close(self):
        """Close HTTP client"""
        if self._http_client:
            await self._http_client.aclose()

    def register_server(self, name: str, url: str):
        """Register an MCP server"""
        self._servers[name] = MCPServer(name=name, url=url)
        self._circuit_breakers[name] = CircuitBreaker()

    async def discover_tools(self, server_name: str) -> List[Dict[str, Any]]:
        """Discover available tools from MCP server"""
        server = self._servers.get(server_name)
        if not server:
            raise ValueError(f"Unknown server: {server_name}")

        # Check if discovery is recent
        if server.tools and server.last_discovery:
            if datetime.utcnow() - server.last_discovery < timedelta(minutes=5):
                return server.tools

        # JSON-RPC call to discover tools
        response = await self._http_client.post(
            f"{server.url}/jsonrpc",
            json={
                "jsonrpc": "2.0",
                "method": "tools/list",
                "id": 1
            }
        )
        response.raise_for_status()
        result = response.json()

        server.tools = result.get("result", {}).get("tools", [])
        server.last_discovery = datetime.utcnow()

        return server.tools

    async def invoke_tool(
        self,
        server_name: str,
        tool_name: str,
        arguments: Dict[str, Any],
        auth_token: Optional[str] = None,
        retry_count: int = 3
    ) -> Dict[str, Any]:
        """
        Invoke a tool on an MCP server

        Args:
            server_name: Name of the MCP server
            tool_name: Name of the tool to invoke
            arguments: Tool arguments
            auth_token: OAuth token for authentication
            retry_count: Number of retries on failure
        """
        server = self._servers.get(server_name)
        if not server:
            raise ValueError(f"Unknown server: {server_name}")

        circuit_breaker = self._circuit_breakers[server_name]

        if not circuit_breaker.can_execute():
            return {
                "error": f"Server {server_name} temporarily unavailable",
                "code": 503
            }

        headers = {}
        if auth_token:
            headers["Authorization"] = f"Bearer {auth_token}"

        for attempt in range(retry_count):
            try:
                response = await self._http_client.post(
                    f"{server.url}/jsonrpc",
                    json={
                        "jsonrpc": "2.0",
                        "method": "tools/call",
                        "params": {
                            "name": tool_name,
                            "arguments": arguments
                        },
                        "id": 1
                    },
                    headers=headers
                )

                if response.status_code == 401:
                    return {"error": "Authentication required", "code": 401}
                if response.status_code == 403:
                    return {"error": "Permission denied", "code": 403}

                response.raise_for_status()
                result = response.json()

                circuit_breaker.record_success()

                if "error" in result:
                    return {"error": result["error"]["message"], "code": result["error"]["code"]}

                return result.get("result", {})

            except httpx.TimeoutException:
                circuit_breaker.record_failure()
                if attempt < retry_count - 1:
                    await asyncio.sleep(2 ** attempt)  # Exponential backoff
                continue

            except httpx.HTTPStatusError as e:
                circuit_breaker.record_failure()
                return {"error": str(e), "code": e.response.status_code}

        return {"error": f"Failed after {retry_count} attempts", "code": 500}

    def get_all_tools(self) -> Dict[str, List[Dict[str, Any]]]:
        """Get all discovered tools from all servers"""
        return {
            name: server.tools or []
            for name, server in self._servers.items()
        }
```

### 5.4 Phase 2 Testing

```
+==============================================================================+
|                    PHASE 2: TEST CASES                                        |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - MCP Server                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T2-U1: Tool Definition                                                      │
│   Given: education-mcp server                                               │
│   When: tools/list called                                                   │
│   Then: All 6 tools returned with schemas                                   │
│                                                                             │
│ T2-U2: get_student_profile Tool                                             │
│   Given: Valid student_id                                                   │
│   When: get_student_profile invoked                                         │
│   Then: Student profile returned with correct fields                        │
│                                                                             │
│ T2-U3: get_grades with Filters                                              │
│   Given: Student with grades in Math and Science                            │
│   When: get_grades called with subject="Math"                               │
│   Then: Only Math grades returned                                           │
│                                                                             │
│ T2-U4: Resource Retrieval                                                   │
│   Given: Parent ID                                                          │
│   When: student_roster resource requested                                   │
│   Then: Formatted roster returned                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Authentication                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T2-U5: Token Validation                                                     │
│   Given: Valid OAuth token                                                  │
│   When: validate_token called                                               │
│   Then: Claims extracted correctly                                          │
│                                                                             │
│ T2-U6: Expired Token Rejection                                              │
│   Given: Expired OAuth token                                                │
│   When: Tool invoked                                                        │
│   Then: 401 error returned                                                  │
│                                                                             │
│ T2-U7: Scope Checking                                                       │
│   Given: Token with education.read scope                                    │
│   When: get_grades invoked (requires education.read)                        │
│   Then: Access granted                                                      │
│                                                                             │
│ T2-U8: Insufficient Scopes                                                  │
│   Given: Token with only memory.read scope                                  │
│   When: get_grades invoked (requires education.read)                        │
│   Then: 403 error with required scopes                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - MCP Client                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T2-U9: Tool Discovery                                                       │
│   Given: Registered MCP server                                              │
│   When: discover_tools called                                               │
│   Then: Tools cached and returned                                           │
│                                                                             │
│ T2-U10: Circuit Breaker Open                                                │
│   Given: 5 consecutive failures                                             │
│   When: invoke_tool called                                                  │
│   Then: 503 returned without actual call                                    │
│                                                                             │
│ T2-U11: Retry Logic                                                         │
│   Given: First call fails, second succeeds                                  │
│   When: invoke_tool called with retry_count=3                               │
│   Then: Result from second call returned                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Integration Tests                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T2-I1: End-to-End MCP Flow                                                  │
│   Given: Running education-mcp server                                       │
│   When: MCPClient invokes get_classes                                       │
│   Then: Classes returned via JSON-RPC                                       │
│                                                                             │
│ T2-I2: Graph API Integration                                                │
│   Given: Valid Graph API credentials                                        │
│   When: get_student_profile called                                          │
│   Then: Real student data from Graph API                                    │
│                                                                             │
│ T2-I3: OAuth Flow                                                           │
│   Given: Unauthenticated request                                            │
│   When: Tool invoked                                                        │
│   Then: 401 with WWW-Authenticate header                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.5 Phase 2 Acceptance Criteria

```
+==============================================================================+
|                    PHASE 2: ACCEPTANCE CRITERIA                               |
+==============================================================================+

┌──────┬────────────────────────────────────────────────┬───────────┬─────────┐
│ ID   │ Criteria                                       │ Measure   │ Status  │
├──────┼────────────────────────────────────────────────┼───────────┼─────────┤
│ AC21 │ education-mcp server exposes 6 tools           │ Discovery │ ⬜      │
│ AC22 │ All tools return data within <300ms            │ Perf test │ ⬜      │
│ AC23 │ OAuth 2.1 authentication works correctly       │ Auth test │ ⬜      │
│ AC24 │ Scope checking enforced for all tools          │ Security  │ ⬜      │
│ AC25 │ Graph API integration returns real data        │ Int. test │ ⬜      │
│ AC26 │ Circuit breaker prevents cascading failures    │ Chaos test│ ⬜      │
│ AC27 │ MCP client handles retries correctly           │ Unit test │ ⬜      │
│ AC28 │ Resources return formatted data                │ Unit test │ ⬜      │
└──────┴────────────────────────────────────────────────┴───────────┴─────────┘
```

---

## 6. Phase 3: Leader Agent

### 6.1 Phase Overview

```
+==============================================================================+
|                    PHASE 3: LEADER AGENT                                      |
+==============================================================================+

OBJECTIVE: Implement intelligent orchestration layer

DELIVERABLES:
  • Leader Agent service with GPT-4o
  • Intent analysis pipeline
  • Memory query integration
  • MCP tool selection and execution
  • Response synthesis for voice
  • delegate_to_leader tool for Realtime API

DEPENDENCIES: Phase 1 (Memory) + Phase 2 (MCP) completed

KEY BENEFIT: Intelligent decision-making + context-aware responses
```

### 6.2 Leader Agent Architecture

```
+==============================================================================+
|                    LEADER AGENT PROCESSING PIPELINE                           |
+==============================================================================+

  ┌──────────────────┐
  │ User Message     │
  │ (from Realtime)  │
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │  PHASE 1: INTENT ANALYSIS                                                 │
  │  ─────────────────────────                                                │
  │  • Parse user intent (query, command, clarification)                      │
  │  • Extract entities (student, subject, time range)                        │
  │  • Detect ambiguity requiring clarification                               │
  └────────┬─────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │  PHASE 2: MEMORY QUERY                                                    │
  │  ─────────────────────                                                    │
  │  • Check working memory (current context)                                 │
  │  • Query session memory (recent conversation)                             │
  │  • Search long-term memory (relevant history)                             │
  └────────┬─────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │  PHASE 3: TOOL SELECTION                                                  │
  │  ───────────────────────────────────                                      │
  │  • Map intent to MCP tools                                                │
  │  • Determine tool execution order                                         │
  │  • Identify parallelizable calls                                          │
  └────────┬─────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │  PHASE 4: TOOL EXECUTION                                                  │
  │  ─────────────────────────                                                │
  │  • Execute MCP tools (parallel where safe)                                │
  │  • Handle failures with fallbacks                                         │
  │  • Collect results                                                        │
  └────────┬─────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │  PHASE 5: RESPONSE SYNTHESIS                                              │
  │  ─────────────────────────────                                            │
  │  • Combine tool results with memory context                               │
  │  • Format for voice output (concise)                                      │
  │  • Add follow-up suggestions                                              │
  └────────┬─────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │  PHASE 6: MEMORY UPDATE                                                   │
  │  ──────────────────────                                                   │
  │  • Store conversation turn in session                                     │
  │  • Detect and store important facts                                       │
  │  • Update working memory for next turn                                    │
  └────────┬─────────────────────────────────────────────────────────────────┘
           │
           ▼
  ┌──────────────────┐
  │ Voice Response   │
  │ (to Realtime)    │
  └──────────────────┘
```

### 6.3 Step-by-Step Implementation

#### Step P3-S1: Leader Agent Core

**Objective**: Create main Leader Agent class

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P3.1 │ Create LeaderAgent class with processing pipeline                   │
│ P3.2 │ Implement process_message() main entry point                        │
│ P3.3 │ Create system prompt builder                                        │
│ P3.4 │ Add GPT-4o API integration                                          │
│ P3.5 │ Implement response streaming for low latency                        │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/leader_agent/agent.py

from typing import Optional, Dict, Any, List
import openai
from dataclasses import dataclass
from datetime import datetime

from .intent_analyzer import IntentAnalyzer, Intent
from .memory_manager import MemoryManager
from .tool_selector import ToolSelector
from .response_builder import ResponseBuilder
from ..mcp_servers.common.mcp_client import MCPClient
from ..memory.working_memory import WorkingMemory

@dataclass
class AgentResponse:
    """Response from Leader Agent"""
    text: str
    confidence: float
    tools_used: List[str]
    memory_updated: bool
    processing_time_ms: int

class LeaderAgent:
    """
    Leader Agent - Orchestrator for the Deep Agent System

    Responsibilities:
    1. Analyze user intent
    2. Query relevant memory
    3. Select and execute MCP tools
    4. Synthesize responses
    5. Update memory with new information
    """

    SYSTEM_PROMPT_TEMPLATE = """You are an educational assistant helping parents access their children's education data.

## Your Role
- Help parents check grades, assignments, schedules, and teacher information
- Provide insights about student performance
- Be concise and conversational (responses will be spoken aloud)

## Current Context
{context}

## Available Tools
You can use the following tools via MCP:
{tools}

## Guidelines
1. Always confirm which student you're discussing if ambiguous
2. Keep voice responses under 3 sentences unless asked for details
3. Offer follow-up options (e.g., "Would you like to see her assignments too?")
4. If information is from memory, mention when it's from (e.g., "Last time you asked...")
5. Store important new information about student patterns or parent preferences

## Response Format
Respond naturally as if speaking to the parent. Do not use markdown or bullet points in the response."""

    def __init__(
        self,
        openai_api_key: str = None,
        model: str = "gpt-4o"
    ):
        self._openai = openai.AsyncOpenAI(api_key=openai_api_key)
        self._model = model

        # Initialize components
        self._intent_analyzer = IntentAnalyzer()
        self._memory_manager = MemoryManager()
        self._tool_selector = ToolSelector()
        self._response_builder = ResponseBuilder()
        self._mcp_client = MCPClient()
        self._working_memory = WorkingMemory()

    async def initialize(self):
        """Initialize all components"""
        await self._memory_manager.initialize()
        await self._mcp_client.connect()

        # Register MCP servers
        self._mcp_client.register_server("education", "http://localhost:8001")
        self._mcp_client.register_server("memory", "http://localhost:8002")
        self._mcp_client.register_server("advice", "http://localhost:8003")

        # Discover tools
        await self._mcp_client.discover_tools("education")
        await self._mcp_client.discover_tools("memory")

    async def process_message(
        self,
        user_id: str,
        session_id: str,
        message: str,
        auth_token: str = None
    ) -> AgentResponse:
        """
        Process a user message through the full pipeline

        Args:
            user_id: User identifier
            session_id: Current session identifier
            message: User's message text
            auth_token: OAuth token for MCP calls
        """
        start_time = datetime.utcnow()
        tools_used = []

        # Phase 1: Intent Analysis
        intent = await self._intent_analyzer.analyze(message)

        # Phase 2: Memory Query
        self._working_memory.initialize(user_id, session_id)
        self._working_memory.set_current_message(message)

        memory_context = await self._memory_manager.get_relevant_context(
            user_id=user_id,
            session_id=session_id,
            query=message,
            intent=intent
        )

        # Update working memory with retrieved context
        if memory_context.active_student:
            self._working_memory.set_active_student(
                memory_context.active_student["id"],
                memory_context.active_student["name"]
            )
        if memory_context.summary:
            self._working_memory.set_context_summary(memory_context.summary)

        # Phase 3: Tool Selection
        selected_tools = await self._tool_selector.select_tools(
            intent=intent,
            context=memory_context,
            available_tools=self._mcp_client.get_all_tools()
        )

        # Phase 4: Tool Execution
        tool_results = {}
        for tool in selected_tools:
            result = await self._mcp_client.invoke_tool(
                server_name=tool["server"],
                tool_name=tool["name"],
                arguments=tool["arguments"],
                auth_token=auth_token
            )
            tool_results[tool["name"]] = result
            tools_used.append(f"{tool['server']}.{tool['name']}")
            self._working_memory.add_tool_result(tool["name"], result)

        # Phase 5: Response Synthesis
        context_string = self._working_memory.build_context()
        tools_string = self._format_available_tools()

        system_prompt = self.SYSTEM_PROMPT_TEMPLATE.format(
            context=context_string,
            tools=tools_string
        )

        response_text = await self._generate_response(
            system_prompt=system_prompt,
            user_message=message,
            tool_results=tool_results,
            memory_context=memory_context
        )

        # Phase 6: Memory Update
        memory_updated = await self._memory_manager.update_memory(
            user_id=user_id,
            session_id=session_id,
            user_message=message,
            assistant_response=response_text,
            tool_results=tool_results
        )

        # Calculate processing time
        end_time = datetime.utcnow()
        processing_time_ms = int((end_time - start_time).total_seconds() * 1000)

        return AgentResponse(
            text=response_text,
            confidence=intent.confidence,
            tools_used=tools_used,
            memory_updated=memory_updated,
            processing_time_ms=processing_time_ms
        )

    async def _generate_response(
        self,
        system_prompt: str,
        user_message: str,
        tool_results: Dict[str, Any],
        memory_context: Any
    ) -> str:
        """Generate response using GPT-4o"""
        messages = [
            {"role": "system", "content": system_prompt}
        ]

        # Add tool results as context
        if tool_results:
            results_text = "## Tool Results\n"
            for name, result in tool_results.items():
                results_text += f"\n{name}:\n{result}\n"
            messages.append({
                "role": "system",
                "content": results_text
            })

        # Add user message
        messages.append({"role": "user", "content": user_message})

        response = await self._openai.chat.completions.create(
            model=self._model,
            messages=messages,
            max_tokens=500,  # Keep responses concise for voice
            temperature=0.7
        )

        return response.choices[0].message.content

    def _format_available_tools(self) -> str:
        """Format available tools for system prompt"""
        all_tools = self._mcp_client.get_all_tools()
        formatted = []

        for server, tools in all_tools.items():
            formatted.append(f"### {server}")
            for tool in tools:
                formatted.append(f"- {tool['name']}: {tool.get('description', '')}")

        return "\n".join(formatted)
```

#### Step P3-S2: Intent Analyzer

**Objective**: Create intent analysis module

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P3.6 │ Create Intent dataclass                                             │
│ P3.7 │ Implement IntentAnalyzer class                                      │
│ P3.8 │ Add entity extraction (student, subject, dates)                     │
│ P3.9 │ Add ambiguity detection                                             │
│ P3.10│ Implement confidence scoring                                        │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/leader_agent/intent_analyzer.py

from dataclasses import dataclass, field
from typing import List, Dict, Any, Optional
from enum import Enum
import openai

class IntentType(Enum):
    QUERY_GRADES = "query_grades"
    QUERY_ASSIGNMENTS = "query_assignments"
    QUERY_SCHEDULE = "query_schedule"
    QUERY_TEACHERS = "query_teachers"
    QUERY_CLASSES = "query_classes"
    SWITCH_STUDENT = "switch_student"
    COMPARE = "compare"
    GET_ADVICE = "get_advice"
    CLARIFICATION = "clarification"
    GREETING = "greeting"
    UNKNOWN = "unknown"

@dataclass
class Entity:
    """Extracted entity from user message"""
    type: str  # student, subject, date, assignment
    value: str
    confidence: float

@dataclass
class Intent:
    """Analyzed user intent"""
    type: IntentType
    confidence: float
    entities: List[Entity] = field(default_factory=list)
    requires_data: bool = True
    requires_memory: bool = True
    is_ambiguous: bool = False
    clarification_needed: Optional[str] = None

class IntentAnalyzer:
    """
    Analyze user intent from message

    Uses structured output from GPT-4o-mini for fast intent classification
    """

    INTENT_PROMPT = """Analyze the user's message and extract:
1. Intent type (one of: query_grades, query_assignments, query_schedule, query_teachers, query_classes, switch_student, compare, get_advice, clarification, greeting, unknown)
2. Entities mentioned (student names, subjects, dates)
3. Whether the request is ambiguous
4. Confidence score (0.0-1.0)

User message: {message}

Previous context (if relevant): {context}

Respond in JSON format:
{
    "intent": "query_grades",
    "confidence": 0.9,
    "entities": [
        {"type": "student", "value": "Lucy", "confidence": 0.95},
        {"type": "subject", "value": "Math", "confidence": 0.8}
    ],
    "requires_data": true,
    "requires_memory": true,
    "is_ambiguous": false,
    "clarification_needed": null
}"""

    def __init__(self, openai_api_key: str = None):
        self._openai = openai.AsyncOpenAI(api_key=openai_api_key)

    async def analyze(
        self,
        message: str,
        context: str = None
    ) -> Intent:
        """Analyze user message to extract intent and entities"""
        prompt = self.INTENT_PROMPT.format(
            message=message,
            context=context or "No previous context"
        )

        response = await self._openai.chat.completions.create(
            model="gpt-4o-mini",  # Fast model for intent analysis
            messages=[{"role": "user", "content": prompt}],
            response_format={"type": "json_object"},
            max_tokens=300
        )

        import json
        result = json.loads(response.choices[0].message.content)

        # Parse entities
        entities = [
            Entity(
                type=e["type"],
                value=e["value"],
                confidence=e.get("confidence", 0.5)
            )
            for e in result.get("entities", [])
        ]

        # Determine intent type
        intent_type = IntentType.UNKNOWN
        try:
            intent_type = IntentType(result.get("intent", "unknown"))
        except ValueError:
            pass

        return Intent(
            type=intent_type,
            confidence=result.get("confidence", 0.5),
            entities=entities,
            requires_data=result.get("requires_data", True),
            requires_memory=result.get("requires_memory", True),
            is_ambiguous=result.get("is_ambiguous", False),
            clarification_needed=result.get("clarification_needed")
        )

    def get_entity(self, intent: Intent, entity_type: str) -> Optional[str]:
        """Get entity value by type from intent"""
        for entity in intent.entities:
            if entity.type == entity_type:
                return entity.value
        return None
```

#### Step P3-S3: Memory Manager

**Objective**: Create memory coordination module

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P3.11│ Create MemoryManager class                                          │
│ P3.12│ Implement get_relevant_context()                                    │
│ P3.13│ Implement update_memory()                                           │
│ P3.14│ Add context prioritization logic                                    │
│ P3.15│ Integrate with memory-mcp server                                    │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/leader_agent/memory_manager.py

from dataclasses import dataclass, field
from typing import Optional, List, Dict, Any
from datetime import datetime

from ..memory.session_memory import SessionMemory
from ..memory.longterm_memory import LongTermMemory, MemoryCategory
from ..memory.summarizer import Summarizer
from ..memory.models import ConversationTurn
from .intent_analyzer import Intent

@dataclass
class MemoryContext:
    """Aggregated memory context for Leader Agent"""
    active_student: Optional[Dict[str, str]] = None
    recent_turns: List[Dict[str, str]] = field(default_factory=list)
    summary: Optional[str] = None
    user_preferences: List[str] = field(default_factory=list)
    relevant_memories: List[str] = field(default_factory=list)
    last_session_summary: Optional[str] = None

class MemoryManager:
    """
    Coordinates memory operations across all tiers

    Read priority: Working → Session → Long-term
    Write triggers: Turn end → Session; Important → Long-term
    """

    def __init__(self):
        self._session_memory = SessionMemory()
        self._longterm_memory = LongTermMemory()
        self._summarizer = Summarizer()

    async def initialize(self):
        """Initialize memory connections"""
        await self._session_memory.connect()
        await self._longterm_memory.initialize()

    async def get_relevant_context(
        self,
        user_id: str,
        session_id: str,
        query: str,
        intent: Intent
    ) -> MemoryContext:
        """
        Get relevant context from all memory tiers

        Queries are prioritized based on intent type
        """
        context = MemoryContext()

        # Get active student from session state
        active_student_id = await self._session_memory.get_state(
            session_id, "active_student_id"
        )
        active_student_name = await self._session_memory.get_state(
            session_id, "active_student_name"
        )
        if active_student_id:
            context.active_student = {
                "id": active_student_id,
                "name": active_student_name or "Unknown"
            }

        # Get recent conversation turns
        recent_turns = await self._session_memory.get_recent_turns(session_id, limit=5)
        context.recent_turns = [
            {"role": t.role, "content": t.content}
            for t in recent_turns
        ]

        # Generate summary of recent turns if > 3
        if len(recent_turns) > 3:
            context.summary = await self._summarizer.summarize_turns(recent_turns)

        # Get user preferences from long-term memory
        preferences = await self._longterm_memory.get_user_preferences(user_id)
        context.user_preferences = [p.content for p in preferences]

        # Search for relevant memories based on query
        if intent.requires_memory:
            relevant = await self._longterm_memory.search_memory(
                user_id=user_id,
                query=query,
                limit=3
            )
            context.relevant_memories = [m.content for m in relevant]

        # Get last session summary if this is a new session
        turn_count = len(recent_turns)
        if turn_count == 0:
            context.last_session_summary = await self._longterm_memory.get_last_session_summary(user_id)

        return context

    async def update_memory(
        self,
        user_id: str,
        session_id: str,
        user_message: str,
        assistant_response: str,
        tool_results: Dict[str, Any] = None
    ) -> bool:
        """
        Update memory after a conversation turn

        Returns True if any long-term memory was created
        """
        created_longterm = False

        # Store user turn
        user_turn = ConversationTurn(
            role="user",
            content=user_message,
            timestamp=datetime.utcnow(),
            metadata={"tool_results": tool_results}
        )
        await self._session_memory.add_turn(session_id, user_turn)

        # Store assistant turn
        assistant_turn = ConversationTurn(
            role="assistant",
            content=assistant_response,
            timestamp=datetime.utcnow()
        )
        await self._session_memory.add_turn(session_id, assistant_turn)

        # Detect salient facts to store in long-term memory
        conversation = f"User: {user_message}\nAssistant: {assistant_response}"
        salient_facts = await self._summarizer.detect_salient_facts(conversation)

        for fact, salience in salient_facts:
            if salience >= 0.7:  # Only store highly salient facts
                await self._longterm_memory.store_memory(
                    user_id=user_id,
                    content=fact,
                    category=MemoryCategory.FACT,
                    salience=salience,
                    metadata={
                        "session_id": session_id,
                        "extracted_from": "conversation"
                    }
                )
                created_longterm = True

        return created_longterm

    async def set_active_student(
        self,
        session_id: str,
        student_id: str,
        student_name: str
    ):
        """Set the active student for the session"""
        await self._session_memory.set_state(session_id, "active_student_id", student_id)
        await self._session_memory.set_state(session_id, "active_student_name", student_name)

    async def end_session(self, user_id: str, session_id: str):
        """Create session summary and store in long-term memory"""
        all_turns = await self._session_memory.get_recent_turns(session_id, limit=50)

        if len(all_turns) > 0:
            summary = await self._summarizer.create_session_summary(all_turns)

            await self._longterm_memory.store_memory(
                user_id=user_id,
                content=summary,
                category=MemoryCategory.SUMMARY,
                salience=0.8,
                metadata={
                    "session_id": session_id,
                    "turn_count": len(all_turns),
                    "ended_at": datetime.utcnow().isoformat()
                }
            )
```

#### Step P3-S4: Tool Selector

**Objective**: Create tool selection logic

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P3.16│ Create ToolSelector class                                           │
│ P3.17│ Implement intent-to-tool mapping                                    │
│ P3.18│ Add argument inference from context                                 │
│ P3.19│ Implement tool dependency ordering                                  │
│ P3.20│ Add parallel execution detection                                    │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/leader_agent/tool_selector.py

from typing import List, Dict, Any, Optional
from dataclasses import dataclass

from .intent_analyzer import Intent, IntentType
from .memory_manager import MemoryContext

@dataclass
class SelectedTool:
    """A tool selected for execution"""
    server: str
    name: str
    arguments: Dict[str, Any]
    priority: int  # Lower = execute first
    can_parallel: bool  # Can run in parallel with same-priority tools

class ToolSelector:
    """
    Select appropriate MCP tools based on intent and context

    Maps intents to tools and infers arguments from context
    """

    INTENT_TOOL_MAPPING = {
        IntentType.QUERY_GRADES: [
            {"server": "education", "name": "get_grades", "priority": 1}
        ],
        IntentType.QUERY_ASSIGNMENTS: [
            {"server": "education", "name": "get_assignments", "priority": 1}
        ],
        IntentType.QUERY_SCHEDULE: [
            {"server": "education", "name": "get_schedule", "priority": 1}
        ],
        IntentType.QUERY_TEACHERS: [
            {"server": "education", "name": "get_teachers", "priority": 1}
        ],
        IntentType.QUERY_CLASSES: [
            {"server": "education", "name": "get_classes", "priority": 1}
        ],
        IntentType.COMPARE: [
            {"server": "education", "name": "get_grades", "priority": 1},
            {"server": "memory", "name": "recall", "priority": 0}  # Get comparison data first
        ],
        IntentType.GET_ADVICE: [
            {"server": "education", "name": "get_grades", "priority": 1},
            {"server": "advice", "name": "generate_advice", "priority": 2}
        ],
    }

    async def select_tools(
        self,
        intent: Intent,
        context: MemoryContext,
        available_tools: Dict[str, List[Dict[str, Any]]]
    ) -> List[SelectedTool]:
        """
        Select tools based on intent and available tools

        Returns list of SelectedTool with inferred arguments
        """
        selected = []

        # Get tool mapping for intent
        tool_config = self.INTENT_TOOL_MAPPING.get(intent.type, [])

        for config in tool_config:
            server = config["server"]
            tool_name = config["name"]

            # Check if tool is available
            server_tools = available_tools.get(server, [])
            tool_def = next((t for t in server_tools if t["name"] == tool_name), None)

            if not tool_def:
                continue

            # Infer arguments from intent and context
            arguments = self._infer_arguments(
                tool_name=tool_name,
                tool_def=tool_def,
                intent=intent,
                context=context
            )

            selected.append(SelectedTool(
                server=server,
                name=tool_name,
                arguments=arguments,
                priority=config.get("priority", 1),
                can_parallel=config.get("can_parallel", True)
            ))

        # Sort by priority
        selected.sort(key=lambda t: t.priority)

        return selected

    def _infer_arguments(
        self,
        tool_name: str,
        tool_def: Dict[str, Any],
        intent: Intent,
        context: MemoryContext
    ) -> Dict[str, Any]:
        """Infer tool arguments from intent entities and context"""
        arguments = {}

        # Get required parameters from tool definition
        params = tool_def.get("parameters", {}).get("properties", {})
        required = tool_def.get("parameters", {}).get("required", [])

        # Student ID - from context or entity
        if "student_id" in params:
            student_id = None

            # Check intent entities first
            for entity in intent.entities:
                if entity.type == "student":
                    # Would need to resolve name to ID
                    student_id = entity.value
                    break

            # Fall back to active student from context
            if not student_id and context.active_student:
                student_id = context.active_student["id"]

            if student_id:
                arguments["student_id"] = student_id

        # Subject filter
        if "subject" in params:
            for entity in intent.entities:
                if entity.type == "subject":
                    arguments["subject"] = entity.value
                    break

        # Class ID
        if "class_id" in params:
            for entity in intent.entities:
                if entity.type == "class":
                    arguments["class_id"] = entity.value
                    break

        # Date filters
        if "due_before" in params or "due_after" in params:
            for entity in intent.entities:
                if entity.type == "date":
                    # Parse and assign date
                    arguments["due_before"] = entity.value
                    break

        # Memory-specific arguments
        if tool_name == "recall":
            arguments["user_id"] = context.active_student.get("id") if context.active_student else None
            arguments["query"] = " ".join(e.value for e in intent.entities)

        return arguments
```

#### Step P3-S5: Delegate Tool for Realtime API

**Objective**: Create the delegate_to_leader function

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P3.21│ Create delegate_to_leader tool definition                           │
│ P3.22│ Implement tool handler that calls Leader Agent                      │
│ P3.23│ Add context passing from Realtime to Leader                         │
│ P3.24│ Handle response format for TTS                                      │
│ P3.25│ Add error handling and fallbacks                                    │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

**Implementation**:

```python
# File: src/voice/delegate_tool.py

from typing import Dict, Any
import asyncio

from ..leader_agent.agent import LeaderAgent

# Global Leader Agent instance
_leader_agent: LeaderAgent = None

async def initialize_leader_agent():
    """Initialize the Leader Agent singleton"""
    global _leader_agent
    _leader_agent = LeaderAgent()
    await _leader_agent.initialize()

def get_delegate_tool_definition() -> Dict[str, Any]:
    """Get tool definition for Azure Realtime API"""
    return {
        "type": "function",
        "name": "delegate_to_leader",
        "description": (
            "Delegate the user's request to the Leader Agent for processing. "
            "The Leader Agent will analyze intent, query memory, execute tools, "
            "and generate an appropriate response."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "user_message": {
                    "type": "string",
                    "description": "The user's spoken message transcribed to text"
                },
                "context_summary": {
                    "type": "string",
                    "description": "Brief summary of recent conversation context"
                }
            },
            "required": ["user_message"]
        }
    }

async def handle_delegate_to_leader(
    user_id: str,
    session_id: str,
    arguments: Dict[str, Any],
    auth_token: str = None
) -> Dict[str, Any]:
    """
    Handle delegate_to_leader function call from Realtime API

    Args:
        user_id: Current user's ID
        session_id: Current session ID
        arguments: Tool arguments (user_message, context_summary)
        auth_token: OAuth token for MCP calls

    Returns:
        Response dict with text for TTS
    """
    global _leader_agent

    if not _leader_agent:
        await initialize_leader_agent()

    user_message = arguments.get("user_message", "")

    if not user_message:
        return {
            "response": "I didn't catch that. Could you please repeat?",
            "error": None
        }

    try:
        # Process through Leader Agent
        result = await _leader_agent.process_message(
            user_id=user_id,
            session_id=session_id,
            message=user_message,
            auth_token=auth_token
        )

        return {
            "response": result.text,
            "tools_used": result.tools_used,
            "processing_time_ms": result.processing_time_ms,
            "error": None
        }

    except Exception as e:
        # Fallback response on error
        return {
            "response": (
                "I'm having trouble processing that request right now. "
                "Could you try asking again?"
            ),
            "error": str(e)
        }

# Tool registration for Realtime API
REALTIME_TOOLS = [
    get_delegate_tool_definition()
]
```

### 6.4 Phase 3 Testing

```
+==============================================================================+
|                    PHASE 3: TEST CASES                                        |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Intent Analyzer                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T3-U1: Grade Query Intent                                                   │
│   Given: Message "What are Lucy's Math grades?"                             │
│   When: analyze() called                                                    │
│   Then: Intent=QUERY_GRADES, entities=[student:Lucy, subject:Math]          │
│                                                                             │
│ T3-U2: Ambiguous Intent                                                     │
│   Given: Message "How about the other one?"                                 │
│   When: analyze() called                                                    │
│   Then: is_ambiguous=True, clarification_needed set                         │
│                                                                             │
│ T3-U3: Compare Intent                                                       │
│   Given: Message "Compare their grades"                                     │
│   When: analyze() called with context showing two students discussed        │
│   Then: Intent=COMPARE, requires_memory=True                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Memory Manager                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T3-U4: Context Retrieval                                                    │
│   Given: Session with 5 turns and active student                            │
│   When: get_relevant_context() called                                       │
│   Then: MemoryContext populated with all tiers                              │
│                                                                             │
│ T3-U5: Memory Update with Salience                                          │
│   Given: Conversation mentioning user preference                            │
│   When: update_memory() called                                              │
│   Then: High-salience fact stored in long-term                              │
│                                                                             │
│ T3-U6: Session End Summary                                                  │
│   Given: Session with 10 turns                                              │
│   When: end_session() called                                                │
│   Then: Summary generated and stored in long-term                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Unit Tests - Tool Selector                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T3-U7: Grade Query Tool Selection                                           │
│   Given: Intent=QUERY_GRADES with student entity                            │
│   When: select_tools() called                                               │
│   Then: education.get_grades selected with student_id argument              │
│                                                                             │
│ T3-U8: Compare Tool Selection                                               │
│   Given: Intent=COMPARE                                                     │
│   When: select_tools() called                                               │
│   Then: memory.recall + education.get_grades selected                       │
│                                                                             │
│ T3-U9: Argument Inference                                                   │
│   Given: Intent with subject entity "Math"                                  │
│   When: select_tools() for QUERY_GRADES                                     │
│   Then: subject="Math" in tool arguments                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Integration Tests                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ T3-I1: Full Pipeline                                                        │
│   Given: "Show me Lucy's Math grades" with active session                   │
│   When: process_message() called                                            │
│   Then:                                                                     │
│     - Intent analyzed correctly                                             │
│     - Memory queried                                                        │
│     - education.get_grades called                                           │
│     - Response synthesized                                                  │
│     - Memory updated                                                        │
│                                                                             │
│ T3-I2: Context Continuity                                                   │
│   Given: Turn 1: "Show Lucy's grades", Turn 2: "What about assignments?"    │
│   When: Turn 2 processed                                                    │
│   Then: Lucy inferred from context, assignments returned                    │
│                                                                             │
│ T3-I3: Student Switching                                                    │
│   Given: Active student is Lucy                                             │
│   When: "What about Tom?" processed                                         │
│   Then: Active student switched to Tom, correct data returned               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.5 Phase 3 Acceptance Criteria

```
+==============================================================================+
|                    PHASE 3: ACCEPTANCE CRITERIA                               |
+==============================================================================+

┌──────┬────────────────────────────────────────────────────────────────┬───────┬──────┐
│ ID   │ Criteria                                                       │Measure│Status│
├──────┼────────────────────────────────────────────────────────────────┼───────┼──────┤
│ AC31 │ Intent analysis accuracy >90% on test set                      │ Eval  │ ⬜   │
│ AC32 │ Full pipeline processes in <500ms (excl. MCP)                  │ Perf  │ ⬜   │
│ AC33 │ Context continuity works across 10+ turns                      │ Test  │ ⬜   │
│ AC34 │ Student switching works correctly                              │ Test  │ ⬜   │
│ AC35 │ Memory updates capture important facts                         │ Manual│ ⬜   │
│ AC36 │ delegate_to_leader integrates with Realtime                    │ Int.  │ ⬜   │
│ AC37 │ Responses are concise (<3 sentences for voice)                 │ Manual│ ⬜   │
│ AC38 │ Error handling provides graceful fallbacks                     │ Test  │ ⬜   │
└──────┴────────────────────────────────────────────────────────────────┴───────┴──────┘
```

---

## 7. Phase 4: Integration & Optimization

### 7.1 Phase Overview

```
+==============================================================================+
|                    PHASE 4: INTEGRATION & OPTIMIZATION                        |
+==============================================================================+

OBJECTIVE: End-to-end integration, testing, and latency optimization

DELIVERABLES:
  • Full voice conversation flow working
  • Latency optimized to <1.5s target
  • E2E test suite
  • advice-mcp server
  • Production-ready deployment

DEPENDENCIES: All previous phases completed
```

### 7.2 Step-by-Step Implementation

```
TASK LIST:
┌──────┬─────────────────────────────────────────────────────────────────────┐
│ ID   │ Task                                                                │
├──────┼─────────────────────────────────────────────────────────────────────┤
│ P4.1 │ Integrate Realtime API with delegate_to_leader                      │
│ P4.2 │ Implement advice-mcp server                                         │
│ P4.3 │ Add response caching for common queries                             │
│ P4.4 │ Implement parallel MCP calls                                        │
│ P4.5 │ Add latency monitoring and tracing                                  │
│ P4.6 │ Create E2E test suite                                               │
│ P4.7 │ Performance optimization iteration                                  │
│ P4.8 │ Create Docker deployment configuration                              │
│ P4.9 │ Document API and deployment procedures                              │
│ P4.10│ User acceptance testing                                             │
└──────┴─────────────────────────────────────────────────────────────────────┘
```

### 7.3 Latency Optimization

```
+==============================================================================+
|                    LATENCY OPTIMIZATION TARGETS                               |
+==============================================================================+

TARGET: <1.5s end-to-end response time

LATENCY BREAKDOWN:
┌─────────────────────────┬─────────────┬─────────────┬─────────────────────┐
│ Component               │ Target      │ Current     │ Optimization        │
├─────────────────────────┼─────────────┼─────────────┼─────────────────────┤
│ Audio capture + STT     │ 200ms       │ ~200ms      │ (Realtime baseline) │
│ Leader Agent processing │ 400ms       │ TBD         │ Streaming, caching  │
│   - Intent analysis     │ 50ms        │             │ gpt-4o-mini         │
│   - Memory query        │ 50ms        │             │ Redis <10ms         │
│   - Tool selection      │ 10ms        │             │ Local logic         │
│   - MCP execution       │ 300ms       │             │ Parallel calls      │
│ Response + TTS          │ 500ms       │ ~500ms      │ (Realtime baseline) │
│ Buffer                  │ 400ms       │             │ Safety margin       │
├─────────────────────────┼─────────────┼─────────────┼─────────────────────┤
│ TOTAL                   │ <1500ms     │             │                     │
└─────────────────────────┴─────────────┴─────────────┴─────────────────────┘

OPTIMIZATION STRATEGIES:
1. Use gpt-4o-mini for intent analysis (faster)
2. Redis session memory (<10ms access)
3. Parallel MCP calls when independent
4. Cache frequent education data (student roster, class list)
5. Pre-fetch likely data on session start
6. Response streaming from Leader Agent
```

### 7.4 Phase 4 Testing

```
+==============================================================================+
|                    PHASE 4: E2E TEST CASES                                    |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: End-to-End Voice Flow                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ E2E-1: Basic Query Flow                                                     │
│   Given: User authenticated, session started                                │
│   When: User speaks "What are Lucy's grades?"                               │
│   Then:                                                                     │
│     - Speech transcribed correctly                                          │
│     - Leader Agent processes request                                        │
│     - Grades fetched via MCP                                                │
│     - Response spoken back                                                  │
│     - Total time <1.5s                                                      │
│                                                                             │
│ E2E-2: Multi-turn Conversation                                              │
│   Given: Previous turn discussed Lucy's Math grades                         │
│   When: User says "How about Science?"                                      │
│   Then:                                                                     │
│     - Context maintained (Lucy implied)                                     │
│     - Science grades returned                                               │
│     - No clarification needed                                               │
│                                                                             │
│ E2E-3: Context Recovery                                                     │
│   Given: User had conversation yesterday about Tom's assignments            │
│   When: User returns and says "Any updates on that?"                        │
│   Then:                                                                     │
│     - Last session summary retrieved                                        │
│     - "that" resolved to Tom's assignments                                  │
│     - Updates provided                                                      │
│                                                                             │
│ E2E-4: Student Switching                                                    │
│   Given: Discussing Lucy's grades                                           │
│   When: User says "What about Tom?"                                         │
│   Then:                                                                     │
│     - Active student switched to Tom                                        │
│     - Tom's grades returned                                                 │
│     - Switch logged in session                                              │
│                                                                             │
│ E2E-5: Comparison Query                                                     │
│   Given: Lucy and Tom both discussed in session                             │
│   When: User says "Compare their Math grades"                               │
│   Then:                                                                     │
│     - Both students' grades retrieved                                       │
│     - Comparison synthesized                                                │
│     - Clear voice response                                                  │
│                                                                             │
│ E2E-6: Error Handling                                                       │
│   Given: MCP server temporarily unavailable                                 │
│   When: User makes query                                                    │
│   Then:                                                                     │
│     - Graceful error message spoken                                         │
│     - Circuit breaker activated                                             │
│     - No system crash                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ TEST CATEGORY: Performance Tests                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ PERF-1: Latency Under Load                                                  │
│   Given: 10 concurrent users                                                │
│   When: Each makes queries                                                  │
│   Then: P95 latency <2s                                                     │
│                                                                             │
│ PERF-2: Memory System Performance                                           │
│   Given: User with 100 stored memories                                      │
│   When: Semantic search performed                                           │
│   Then: Results in <100ms                                                   │
│                                                                             │
│ PERF-3: MCP Server Throughput                                               │
│   Given: 50 concurrent MCP requests                                         │
│   When: Load test run                                                       │
│   Then: All requests complete, no timeouts                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.5 Phase 4 Acceptance Criteria

```
+==============================================================================+
|                    PHASE 4: ACCEPTANCE CRITERIA                               |
+==============================================================================+

┌──────┬────────────────────────────────────────────────┬───────────┬─────────┐
│ ID   │ Criteria                                       │ Measure   │ Status  │
├──────┼────────────────────────────────────────────────┼───────────┼─────────┤
│ AC41 │ E2E voice flow works end-to-end               │ E2E test  │ ⬜      │
│ AC42 │ P95 latency <1.5s for simple queries          │ Perf test │ ⬜      │
│ AC43 │ Context maintained across 15+ turns           │ E2E test  │ ⬜      │
│ AC44 │ Cross-session memory works correctly          │ E2E test  │ ⬜      │
│ AC45 │ advice-mcp server generates useful advice     │ Manual QA │ ⬜      │
│ AC46 │ System handles 10 concurrent users            │ Load test │ ⬜      │
│ AC47 │ Docker deployment works correctly             │ Deploy    │ ⬜      │
│ AC48 │ All E2E tests pass                            │ CI/CD     │ ⬜      │
└──────┴────────────────────────────────────────────────┴───────────┴─────────┘
```

---

## 8. Use Case Tests

### 8.1 Primary Use Cases

```
+==============================================================================+
|                    USE CASE TEST SCENARIOS                                    |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ UC-1: FIRST-TIME USER CONVERSATION                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ SCENARIO:                                                                   │
│   Parent: "Hello, I'd like to check on my daughter's schoolwork"            │
│   System: (Identifies 2 children: Lucy and Tom)                             │
│   System: "Hello! I can help you with that. I see you have two children,   │
│            Lucy and Tom. Which one would you like to start with?"          │
│   Parent: "Lucy please"                                                     │
│   System: (Sets Lucy as active student)                                     │
│   System: "Sure! What would you like to know about Lucy? I can show you    │
│            her grades, assignments, schedule, or teacher contacts."         │
│   Parent: "Her Math grades"                                                 │
│   System: (Calls education-mcp.get_grades with student=Lucy, subject=Math) │
│   System: "Lucy has two graded Math assignments. She scored 85% on the     │
│            Algebra Quiz and 72% on the Geometry Test. Would you like       │
│            some advice on improving her Geometry scores?"                   │
│                                                                             │
│ VALIDATIONS:                                                                │
│   ✓ Greeting handled appropriately                                          │
│   ✓ Multi-child scenario handled with clarification                         │
│   ✓ Active student set correctly                                            │
│   ✓ Query processed with correct filters                                    │
│   ✓ Response concise and conversational                                     │
│   ✓ Follow-up offered proactively                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ UC-2: RETURNING USER WITH CONTEXT RECOVERY                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ SETUP:                                                                      │
│   - User had conversation 2 days ago                                        │
│   - Discussed Lucy's overdue Science project                                │
│   - User preference: prefers brief responses                                │
│                                                                             │
│ SCENARIO:                                                                   │
│   Parent: "Hi, any updates?"                                                │
│   System: (Retrieves last session summary and preferences)                  │
│   System: "Welcome back! Since we last talked about Lucy's Science project,│
│            she's submitted it and received an 88%. Would you like details?" │
│   Parent: "Yes please"                                                      │
│   System: (Retrieves full assignment details)                               │
│   System: "The project on ecosystems was submitted on Monday. Teacher      │
│            feedback says excellent research, needs better formatting.       │
│            Her next Science assignment is due Friday."                      │
│                                                                             │
│ VALIDATIONS:                                                                │
│   ✓ Previous session context recovered                                      │
│   ✓ "updates" correctly interpreted with context                            │
│   ✓ Brief responses (user preference applied)                               │
│   ✓ Follow-up question handled                                              │
│   ✓ Proactive information about upcoming work                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ UC-3: STUDENT COMPARISON                                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ SCENARIO:                                                                   │
│   Parent: "Show me Lucy's grades"                                           │
│   System: "Lucy's grades: Math 85%, Science 92%, English 88%."              │
│   Parent: "And Tom?"                                                        │
│   System: (Switches to Tom, retrieves grades)                               │
│   System: "Tom's grades: Math 78%, Science 88%, English 91%."               │
│   Parent: "Compare them in Math"                                            │
│   System: (Recalls Lucy's Math from session memory)                         │
│   System: "In Math, Lucy is at 85% and Tom is at 78%. Lucy is 7 points     │
│            ahead. Tom might benefit from extra Math practice. Would you    │
│            like some suggestions?"                                          │
│                                                                             │
│ VALIDATIONS:                                                                │
│   ✓ Student switching works ("And Tom?" implies grades)                     │
│   ✓ Session memory retains Lucy's grades for comparison                     │
│   ✓ Comparison synthesized correctly                                        │
│   ✓ Advice offered proactively                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ UC-4: LONG CONVERSATION WITHOUT CONTEXT LOSS                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ SCENARIO: 15-turn conversation                                              │
│   Turn 1:  "Show Lucy's classes"                                            │
│   Turn 2:  "What about her Math teacher?"                                   │
│   Turn 3:  "What's his email?"                                              │
│   Turn 4:  "Show her Math assignments"                                      │
│   Turn 5:  "Any overdue?"                                                   │
│   Turn 6:  "When is the next one due?"                                      │
│   Turn 7:  "Switch to Tom"                                                  │
│   Turn 8:  "His Math assignments"                                           │
│   Turn 9:  "Compare their homework completion"                              │
│   Turn 10: "Back to Lucy"                                                   │
│   Turn 11: "Her Science grades"                                             │
│   Turn 12: "What about the project we discussed last time?"                 │
│   Turn 13: "Teacher feedback on it?"                                        │
│   Turn 14: "Any other subjects struggling?"                                 │
│   Turn 15: "Thanks, that's all for now"                                     │
│                                                                             │
│ VALIDATIONS:                                                                │
│   ✓ All 15 turns complete without memory errors                             │
│   ✓ Pronouns resolved correctly (her, his, it)                              │
│   ✓ Student switching maintains context                                     │
│   ✓ Cross-session memory retrieved (Turn 12)                                │
│   ✓ Summarization keeps context within budget                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ UC-5: ERROR RECOVERY                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ SCENARIO 5A: MCP Server Unavailable                                         │
│   Parent: "Show Lucy's grades"                                              │
│   System: (education-mcp server is down)                                    │
│   System: "I'm having trouble accessing the school system right now.       │
│            Let me try again... Unfortunately, the grades aren't available  │
│            at the moment. Would you like me to tell you what I remember    │
│            from our last conversation about Lucy's grades?"                 │
│                                                                             │
│ SCENARIO 5B: Ambiguous Request                                              │
│   Parent: "Show the grades"                                                 │
│   System: (No active student set)                                           │
│   System: "I'd be happy to show grades. Which child would you like to      │
│            check - Lucy or Tom?"                                            │
│                                                                             │
│ SCENARIO 5C: Unknown Intent                                                 │
│   Parent: "What's the weather like?"                                        │
│   System: "I specialize in helping with your children's education info.    │
│            I can show grades, assignments, schedules, or teacher contacts. │
│            What would you like to know?"                                    │
│                                                                             │
│ VALIDATIONS:                                                                │
│   ✓ Graceful degradation on service failure                                 │
│   ✓ Fallback to cached/memory data offered                                  │
│   ✓ Clarification requested for ambiguous input                             │
│   ✓ Out-of-scope requests handled politely                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 9. Validation Criteria

### 9.1 Overall System Validation

```
+==============================================================================+
|                    SYSTEM VALIDATION MATRIX                                   |
+==============================================================================+

┌───────────────────────┬────────────────────────────────────────┬────────────┐
│ Requirement           │ Validation Method                       │ Status    │
├───────────────────────┼────────────────────────────────────────┼────────────┤
│ Context Lost Solved   │ UC-2, UC-4 pass without repeat queries │ ⬜         │
│ MCP Integration       │ All MCP tools callable via JSON-RPC    │ ⬜         │
│ Low Latency (<1.5s)   │ P95 latency test passes                │ ⬜         │
│ Leader Agent Works    │ UC-1 through UC-5 all pass             │ ⬜         │
│ Memory Persistence    │ Cross-session context recovery works   │ ⬜         │
│ Security (OAuth 2.1)  │ Unauthorized requests rejected         │ ⬜         │
│ Scalability           │ 10 concurrent users handled            │ ⬜         │
│ Error Handling        │ UC-5 error scenarios pass              │ ⬜         │
└───────────────────────┴────────────────────────────────────────┴────────────┘
```

### 9.2 Definition of Done

```
+==============================================================================+
|                    DEFINITION OF DONE                                         |
+==============================================================================+

PHASE COMPLETION CRITERIA:

  Phase 0: Foundation
    □ All AC01-AC06 met
    □ Code review completed
    □ Test coverage >80%

  Phase 1: Memory Layer
    □ All AC11-AC18 met
    □ Redis + Qdrant deployed and tested
    □ memory-mcp server operational
    □ Integration tests pass

  Phase 2: MCP Infrastructure
    □ All AC21-AC28 met
    □ education-mcp server operational
    □ OAuth 2.1 working
    □ Graph API integration tested

  Phase 3: Leader Agent
    □ All AC31-AC38 met
    □ Full pipeline tested
    □ Realtime integration working
    □ Intent accuracy validated

  Phase 4: Integration
    □ All AC41-AC48 met
    □ E2E tests pass
    □ Performance targets met
    □ User acceptance completed

OVERALL SYSTEM:
    □ All use cases (UC-1 to UC-5) pass
    □ Documentation complete
    □ Deployment procedures documented
    □ Monitoring and alerting configured
```

---

## 10. Risk Mitigation

```
+==============================================================================+
|                    RISK MITIGATION STRATEGIES                                 |
+==============================================================================+

┌─────────────────────────────┬───────────────────────────────────────────────┐
│ Risk                        │ Mitigation                                     │
├─────────────────────────────┼───────────────────────────────────────────────┤
│ Memory retrieval too slow   │ - Redis for session (<10ms)                   │
│                             │ - Pre-fetch on session start                   │
│                             │ - Query caching                                │
├─────────────────────────────┼───────────────────────────────────────────────┤
│ MCP server bottleneck       │ - Horizontal scaling                           │
│                             │ - Response caching                             │
│                             │ - Circuit breaker pattern                      │
├─────────────────────────────┼───────────────────────────────────────────────┤
│ Leader Agent poor decisions │ - Comprehensive system prompt                  │
│                             │ - Confidence scoring                           │
│                             │ - Clarification questions                      │
│                             │ - User feedback loop                           │
├─────────────────────────────┼───────────────────────────────────────────────┤
│ Memory staleness            │ - TTL on cached education data                 │
│                             │ - Cache invalidation hooks                     │
│                             │ - "Refresh" option for users                   │
├─────────────────────────────┼───────────────────────────────────────────────┤
│ Integration complexity      │ - Phased rollout                               │
│                             │ - Feature flags                                │
│                             │ - Comprehensive logging                        │
├─────────────────────────────┼───────────────────────────────────────────────┤
│ Graph API rate limits       │ - Request batching                             │
│                             │ - Caching layer                                │
│                             │ - Exponential backoff                          │
└─────────────────────────────┴───────────────────────────────────────────────┘
```

---

## 11. References

### 11.1 Architecture Documents

| Document | Location | Purpose |
|----------|----------|---------|
| v2 Architecture | [Over-v2-deep-agent-architecture-redesign.md](Over-v2-deep-agent-architecture-redesign.md) | Source design document |
| v1 Analysis | [Over-v1-architecture-analysis.md](Over-v1-architecture-analysis.md) | Current state analysis |
| Task Tracker | [TASK-v1-deep-agent-system-tracker.md](TASK-v1-deep-agent-system-tracker.md) | Progress tracking |

### 11.2 External References

| Resource | URL | Purpose |
|----------|-----|---------|
| MCP Specification | [modelcontextprotocol.io](https://modelcontextprotocol.io/specification/2025-11-25) | Protocol reference |
| FastMCP Documentation | [fastmcp.dev](https://fastmcp.dev) | MCP server framework |
| OpenAI Realtime API | [platform.openai.com](https://platform.openai.com/docs/guides/realtime-conversations) | Voice interface |
| Qdrant Documentation | [qdrant.tech](https://qdrant.tech/documentation/) | Vector DB |
| Redis Documentation | [redis.io](https://redis.io/docs/) | Session storage |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v2-draft | 2026-01-13 | Claude Code | Initial implementation plan |

---

*Generated with assistance from Claude Code - Deep Agent System Lab*
