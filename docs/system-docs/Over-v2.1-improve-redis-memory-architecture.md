# Over-v2.1: Redis Memory Architecture Summary

**Purpose**: Quick reference for Redis implementation in Phase 1
**Status**: 📋 Planned (reminder for future implementation)

---

## 1. Three-Tier Memory Overview

```
+==============================================================================+
|                      THREE-TIER MEMORY SYSTEM                                 |
+==============================================================================+
|                                                                              |
|   TIER 1: WORKING MEMORY          TIER 2: SESSION MEMORY                     |
|   (In-Context)                    (Redis)                                    |
|   ┌─────────────────────┐         ┌─────────────────────┐                   |
|   │  ~2K tokens         │         │  Redis 7.x          │                   |
|   │  Current turn only  │ ──────► │  TTL: 24 hours      │                   |
|   │  GPT-4o context     │         │  Max: 50 turns      │                   |
|   └─────────────────────┘         └──────────┬──────────┘                   |
|                                              │                               |
|                                              │ (Summarize & Embed)           |
|                                              ▼                               |
|                                   ┌─────────────────────┐                   |
|                                   │  TIER 3: LONG-TERM  │                   |
|                                   │  (Qdrant)           │                   |
|                                   │  Permanent storage  │                   |
|                                   │  Vector similarity  │                   |
|                                   └─────────────────────┘                   |
|                                                                              |
+==============================================================================+
```

---

## 2. Redis Key Structure

```
+==============================================================================+
|                         REDIS KEY DESIGN                                      |
+==============================================================================+

  SESSION DATA (Hash)
  ───────────────────
  Key: session:{session_id}

  ┌─────────────────────────────────────────────────────────────┐
  │  HSET session:abc123                                        │
  │    user_id        "user_001"                                │
  │    student_id     "student_123"                             │
  │    created_at     "2025-01-13T10:00:00Z"                    │
  │    last_activity  "2025-01-13T10:30:00Z"                    │
  │    turn_count     "15"                                      │
  └─────────────────────────────────────────────────────────────┘


  CONVERSATION TURNS (List)
  ─────────────────────────
  Key: session:{session_id}:turns

  ┌─────────────────────────────────────────────────────────────┐
  │  RPUSH session:abc123:turns                                 │
  │    [0] {"role":"user","content":"...","ts":"..."}           │
  │    [1] {"role":"assistant","content":"...","ts":"..."}      │
  │    ...                                                      │
  │    [49] (max 50 turns, LTRIM auto-cleanup)                  │
  └─────────────────────────────────────────────────────────────┘


  SESSION STATE (Hash)
  ────────────────────
  Key: session:{session_id}:state

  ┌─────────────────────────────────────────────────────────────┐
  │  HSET session:abc123:state                                  │
  │    current_intent   "check_grades"                          │
  │    active_tool      "education_mcp"                         │
  │    pending_action   null                                    │
  └─────────────────────────────────────────────────────────────┘

```

---

## 3. Redis Operations Flow

```
+==============================================================================+
|                      REDIS OPERATIONS FLOW                                    |
+==============================================================================+

  USER MESSAGE                          REDIS OPERATIONS
  ─────────────                         ─────────────────
       │
       ▼
  ┌─────────────┐     1. GET session    ┌─────────────────────────────────────┐
  │  New Turn   │ ──────────────────────│  HGETALL session:{id}               │
  └──────┬──────┘                       │  LRANGE session:{id}:turns -10 -1   │
         │                              └─────────────────────────────────────┘
         │
         ▼
  ┌─────────────┐     2. PROCESS        ┌─────────────────────────────────────┐
  │  Leader     │                       │  (In-memory processing)             │
  │  Agent      │                       │  Context = last 10 turns            │
  └──────┬──────┘                       └─────────────────────────────────────┘
         │
         ▼
  ┌─────────────┐     3. SAVE turn      ┌─────────────────────────────────────┐
  │  Response   │ ──────────────────────│  RPUSH session:{id}:turns {turn}    │
  └──────┬──────┘                       │  LTRIM session:{id}:turns -50 -1    │
         │                              │  EXPIRE session:{id}:turns 86400    │
         │                              └─────────────────────────────────────┘
         │
         ▼
  ┌─────────────┐     4. UPDATE meta    ┌─────────────────────────────────────┐
  │  Update     │ ──────────────────────│  HSET session:{id}                  │
  │  Session    │                       │    last_activity {now}              │
  └─────────────┘                       │    turn_count {n+1}                 │
                                        │  EXPIRE session:{id} 86400          │
                                        └─────────────────────────────────────┘

```

---

## 4. Implementation Pattern

```python
# Phase 1: Redis Session Memory (Future Implementation)

class RedisSessionMemory:
    """Redis-backed session storage."""

    DEFAULT_TTL = 86400   # 24 hours
    MAX_TURNS = 50        # Rolling window

    def __init__(self, redis: Redis):
        self._redis = redis

    # Key builders
    def _session_key(self, sid: str) -> str:
        return f"session:{sid}"

    def _turns_key(self, sid: str) -> str:
        return f"session:{sid}:turns"

    def _state_key(self, sid: str) -> str:
        return f"session:{sid}:state"

    # Core operations
    async def add_turn(self, sid: str, turn: dict) -> None:
        """Add turn with auto-cleanup."""
        key = self._turns_key(sid)
        await self._redis.rpush(key, json.dumps(turn))
        await self._redis.ltrim(key, -self.MAX_TURNS, -1)
        await self._redis.expire(key, self.DEFAULT_TTL)

    async def get_context(self, sid: str, n: int = 10) -> list[dict]:
        """Get last N turns for context."""
        key = self._turns_key(sid)
        turns = await self._redis.lrange(key, -n, -1)
        return [json.loads(t) for t in turns]
```

---

## 5. Phase Comparison

```
+==============================================================================+
|                      PHASE 0 vs PHASE 1 COMPARISON                            |
+==============================================================================+

  ┌─────────────────────────────────────────────────────────────────────────┐
  │  PHASE 0 (Current)              │  PHASE 1 (Future)                     │
  ├─────────────────────────────────┼───────────────────────────────────────┤
  │  InMemorySessionStore           │  RedisSessionMemory                   │
  │                                 │                                       │
  │  ┌───────────────────┐          │  ┌───────────────────┐                │
  │  │  Python dict      │          │  │  Redis Server     │                │
  │  │  (RAM only)       │          │  │  (Persistent)     │                │
  │  │                   │          │  │                   │                │
  │  │  self._sessions = │          │  │  session:{id}     │                │
  │  │    {}             │          │  │  session:{id}:... │                │
  │  └───────────────────┘          │  └───────────────────┘                │
  │                                 │                                       │
  │  ❌ Lost on restart             │  ✅ Survives restart                  │
  │  ❌ Single process only         │  ✅ Multi-process shared              │
  │  ❌ No TTL                       │  ✅ Auto-expire (24h)                 │
  │  ❌ Unbounded growth             │  ✅ Max 50 turns                      │
  └─────────────────────────────────┴───────────────────────────────────────┘

```

---

## 6. Long-term Memory (Qdrant)

```
+==============================================================================+
|                      QDRANT VECTOR STORAGE                                    |
+==============================================================================+

  SESSION END                           QDRANT OPERATIONS
  ───────────                           ─────────────────
       │
       ▼
  ┌─────────────┐                       ┌─────────────────────────────────────┐
  │  Summarize  │     1. Summarize      │  GPT-4o: "Summarize this session"   │
  │  Session    │ ──────────────────────│  → "User checked grades for Math,   │
  └──────┬──────┘                       │     asked about assignment #5"      │
         │                              └─────────────────────────────────────┘
         │
         ▼
  ┌─────────────┐     2. Embed          ┌─────────────────────────────────────┐
  │  Create     │ ──────────────────────│  text-embedding-3-small             │
  │  Embedding  │                       │  → [0.12, -0.45, 0.78, ...]         │
  └──────┬──────┘                       │     (1536 dimensions)               │
         │                              └─────────────────────────────────────┘
         │
         ▼
  ┌─────────────┐     3. Store          ┌─────────────────────────────────────┐
  │  Upsert to  │ ──────────────────────│  qdrant.upsert(                     │
  │  Qdrant     │                       │    collection="memories",           │
  └─────────────┘                       │    points=[Point(                   │
                                        │      id=uuid,                       │
                                        │      vector=embedding,              │
                                        │      payload={                      │
                                        │        "user_id": "...",            │
                                        │        "summary": "...",            │
                                        │        "timestamp": "..."           │
                                        │      }                              │
                                        │    )]                               │
                                        │  )                                  │
                                        └─────────────────────────────────────┘

```

---

## 7. Quick Reference

| Component | Technology | Key/Collection | TTL |
|-----------|------------|----------------|-----|
| Session Data | Redis Hash | `session:{id}` | 24h |
| Turns | Redis List | `session:{id}:turns` | 24h |
| State | Redis Hash | `session:{id}:state` | 24h |
| Long-term | Qdrant | `memories` | Permanent |

---

## 8. Implementation Checklist (Phase 1)

- [ ] Install `redis` and `qdrant-client` packages
- [ ] Create `RedisSessionMemory` class
- [ ] Implement key builders (`_session_key`, `_turns_key`, `_state_key`)
- [ ] Implement CRUD operations with TTL
- [ ] Add `LTRIM` for rolling window (max 50 turns)
- [ ] Create `QdrantMemoryStore` class
- [ ] Implement session summarization on close
- [ ] Add embedding generation (OpenAI)
- [ ] Test Redis persistence across restarts
- [ ] Test multi-process session sharing

---

*Document created: 2025-01-13*
*To be implemented in: Phase 1 - Memory System*
