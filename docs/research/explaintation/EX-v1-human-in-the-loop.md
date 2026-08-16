# EX-v1: Human-in-the-Loop (HITL) trong Microsoft Agent Framework + MCP

**Created**: 2026-01-21
**Version**: v1
**Status**: Research Complete
**Author**: Claude Code Research

---

## 1. Executive Summary

```
+==============================================================================+
|                         RESEARCH OVERVIEW                                     |
+==============================================================================+
|                                                                              |
|   WHAT: Human-in-the-Loop (HITL) là architectural pattern yêu cầu human     |
|         intervention tại các decision points trong AI workflow               |
|                                                                              |
|   WHY:  - EU AI Act Article 14 (2026): Bắt buộc human oversight              |
|         - Reduce AI hallucination risks                                      |
|         - Audit trail & compliance                                           |
|         - Trust building với end users                                       |
|                                                                              |
|   HOW:  Microsoft Agent Framework + MCP Protocol                             |
|         - Checkpoint-based state persistence                                 |
|         - RequestInfoExecutor cho human input                                |
|         - Deterministic routing cho risk-based decisions                     |
|                                                                              |
+==============================================================================+
```

---

## 2. Problem Decomposition (MECE Framework)

### 2.1 Issue Tree - HITL Requirements

```
Human-in-the-Loop Requirements
├── 1. WHEN to involve humans (Trigger Conditions)
│   ├── 1.1 Risk-based thresholds
│   ├── 1.2 Regulatory requirements
│   ├── 1.3 Confidence scores below threshold
│   └── 1.4 Destructive/irreversible actions
│
├── 2. HOW to pause execution (Technical Mechanism)
│   ├── 2.1 Checkpoint creation
│   ├── 2.2 State serialization
│   ├── 2.3 Request emission
│   └── 2.4 Async wait handling
│
├── 3. WHAT state to preserve (Data Requirements)
│   ├── 3.1 Workflow variables
│   ├── 3.2 Completed step outputs
│   ├── 3.3 Pending tasks queue
│   └── 3.4 Execution metadata
│
└── 4. WHERE humans interact (Interface Points)
    ├── 4.1 Approval UI/Dashboard
    ├── 4.2 Queue-based systems
    ├── 4.3 API endpoints
    └── 4.4 Notification systems
```

### 2.2 Mutually Exclusive Categories

| Category | Description | Overlap Check |
|----------|-------------|---------------|
| **Pre-execution HITL** | Human input trước khi AI chạy | No overlap với runtime |
| **Runtime HITL** | Pause mid-execution cho approval | No overlap với pre/post |
| **Post-execution HITL** | Review sau khi AI hoàn thành | No overlap với runtime |
| **Feedback Loop** | Training data từ human corrections | Separate from execution |

### 2.3 Collectively Exhaustive Verification

```
✅ All HITL touchpoints covered:
   - Before AI runs (configuration, constraints)
   - During AI execution (checkpoints, approvals)
   - After AI completes (review, corrections)
   - Continuous improvement (feedback loops)
```

---

## 3. First Principles Analysis

### 3.1 Fundamental Assumptions

| # | Assumption | Challenge | Validation |
|---|------------|-----------|------------|
| 1 | AI decisions need oversight | Not all decisions are equal | ✅ Risk-based filtering |
| 2 | Humans are always available | Delays can block workflows | ✅ Timeout + escalation |
| 3 | State can be serialized | Some states are transient | ✅ Checkpoint boundaries |
| 4 | Resume produces same result | Model non-determinism | ⚠️ Cached intermediate results |

### 3.2 Breaking Down to Fundamentals

```
HITL = State Preservation + Decision Routing + Human Interface

Where:
├── State Preservation
│   └── Checkpoint = Serialize(workflow_vars + outputs + pending_tasks + metadata)
│
├── Decision Routing
│   └── Route = f(risk_score, confidence, action_type, policy)
│
└── Human Interface
    └── Interface = RequestInfoEvent → Human → Response → Resume
```

### 3.3 Rebuild from Scratch

**Minimal Viable HITL System:**

```
1. Trigger Detection
   └── if (risk_score >= threshold OR action.is_destructive)

2. State Snapshot
   └── checkpoint.save(current_state)

3. Request Emission
   └── emit(RequestInfoEvent(action, context, options))

4. Wait Loop
   └── while (!response.received) { await; }

5. Resume Execution
   └── checkpoint.restore() → continue_workflow(response)
```

---

## 4. 5 Whys Analysis

### 4.1 Why do we need HITL in AI agents?

```
Why 1: AI agents can make mistakes that affect real systems
       ↓
Why 2: LLMs have hallucination and confidence calibration issues
       ↓
Why 3: Training data doesn't cover all edge cases
       ↓
Why 4: Real-world scenarios have nuances not captured in prompts
       ↓
Why 5: Human judgment is still superior for ambiguous situations
       ════════════════════════════════════════════════════════
       ROOT CAUSE: AI lacks contextual understanding that humans have
```

### 4.2 Why checkpoint-based approach over other methods?

```
Why 1: Need to pause workflow without losing progress
       ↓
Why 2: Re-execution is expensive and non-deterministic
       ↓
Why 3: LLM calls have cost and latency implications
       ↓
Why 4: Intermediate results may vary due to model randomness
       ↓
Why 5: Business requires reproducible audit trails
       ════════════════════════════════════════════════════════
       ROOT CAUSE: Checkpointing ensures deterministic resume
```

---

## 5. Microsoft Agent Framework HITL Architecture

### 5.1 Component Overview

```
+==============================================================================+
|                    MAF HITL ARCHITECTURE                                      |
+==============================================================================+

  ┌─────────────────────────────────────────────────────────────────────────┐
  │                         WORKFLOW RUNTIME                                 │
  │                                                                          │
  │  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐             │
  │  │  Agent A     │     │  Agent B     │     │  Agent C     │             │
  │  │  (Analysis)  │     │  (Scoring)   │     │  (Decision)  │             │
  │  └──────┬───────┘     └──────┬───────┘     └──────┬───────┘             │
  │         │                    │                    │                      │
  │         └────────────────────┼────────────────────┘                      │
  │                              ▼                                           │
  │                    ┌─────────────────┐                                   │
  │                    │   Aggregator    │                                   │
  │                    │   (Typed Risk)  │                                   │
  │                    └────────┬────────┘                                   │
  │                             │                                            │
  │              ┌──────────────┼──────────────┐                             │
  │              ▼              │              ▼                             │
  │     ┌──────────────┐       │      ┌──────────────┐                      │
  │     │ risk < 0.6   │       │      │ risk >= 0.6  │                      │
  │     │ AUTO-APPROVE │       │      │ HUMAN REVIEW │                      │
  │     └──────────────┘       │      └──────┬───────┘                      │
  │                            │             │                               │
  │                            │    ┌────────▼────────┐                      │
  │                            │    │ RequestInfo     │                      │
  │                            │    │ Executor        │──────────────────────┼──► RequestInfoEvent
  │                            │    └────────┬────────┘                      │
  │                            │             │                               │
  │                    ┌───────▼─────────────▼───────┐                       │
  │                    │      CHECKPOINT             │                       │
  │                    │  (Cosmos DB / In-Memory)    │                       │
  │                    └─────────────────────────────┘                       │
  │                                                                          │
  └──────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
                              ┌─────────────────┐
                              │  Human Review   │
                              │  Interface      │
                              │  (UI/Queue/API) │
                              └────────┬────────┘
                                       │
                                       ▼
                              ┌─────────────────┐
                              │  Response       │
                              │  (Approve/Edit/ │
                              │   Reject)       │
                              └─────────────────┘
```

### 5.2 Core Components

| Component | Purpose | Implementation |
|-----------|---------|----------------|
| **RequestInfoEvent** | Emit request cho human input | Framework event type |
| **RequestInfoExecutor** | Coordinator giữa workflow và human | Built-in executor |
| **CheckpointManager** | State persistence | InMemory / Cosmos DB |
| **InputPort** | Request/Response routing | Type-safe handler |

### 5.3 Code Implementation

#### Python - Request Human Input

```python
from agent_framework import Executor, WorkflowContext, handler, response_handler

class HumanReviewExecutor(Executor):
    """Executor that requests human approval for high-risk decisions."""

    @handler
    async def handle_risk_assessment(
        self,
        assessment: RiskAssessment,
        context: WorkflowContext
    ):
        if assessment.overall_risk_score >= 0.6:
            # Request human review
            await context.request_info(
                request_data=HumanReviewRequest(
                    case_id=assessment.case_id,
                    risk_score=assessment.overall_risk_score,
                    evidence=assessment.evidence,
                    recommended_action=assessment.recommendation
                ),
                response_type=HumanReviewResponse
            )
        else:
            # Auto-approve low risk
            await context.send_message(
                ApprovalResult(approved=True, reviewer="AUTO")
            )

    @response_handler
    async def handle_human_response(
        self,
        original_request: HumanReviewRequest,
        response: HumanReviewResponse,
        context: WorkflowContext
    ):
        # Process human decision
        await context.send_message(
            ApprovalResult(
                approved=response.approved,
                reviewer=response.reviewer_id,
                comments=response.comments
            )
        )
```

#### C# - Checkpoint with Human Review

```csharp
using Microsoft.Agents.AI.Workflows;

// Configure checkpoint storage
var checkpointManager = new CheckpointManager();
var checkpoints = new List<CheckpointInfo>();

// Run workflow with checkpointing
Checkpointed<StreamingRun> checkpointedRun = await InProcessExecution
    .StreamAsync(workflow, input, checkpointManager)
    .ConfigureAwait(false);

// Watch for events
await foreach (WorkflowEvent evt in checkpointedRun.Run.WatchStreamAsync())
{
    switch (evt)
    {
        case RequestInfoEvent requestEvt:
            // Human review required
            Console.WriteLine($"Human review needed: {requestEvt.Request}");

            // Get human decision (from UI/Queue/API)
            var humanResponse = await GetHumanDecision(requestEvt.Request);

            // Send response back to workflow
            var response = requestEvt.Request.CreateResponse<HumanReviewResponse>(
                humanResponse
            );
            await handle.SendResponseAsync(response);
            break;

        case SuperStepCompletedEvent superStepEvt:
            // Save checkpoint after each superstep
            if (superStepEvt.CompletionInfo?.Checkpoint != null)
            {
                checkpoints.Add(superStepEvt.CompletionInfo.Checkpoint);
            }
            break;
    }
}
```

#### Workflow Builder - Decision Routing

```python
from agent_framework import WorkflowBuilder, Case, Default

builder = WorkflowBuilder()

# Add executors
builder.set_start_executor(data_collector)
builder.add_edge(data_collector, risk_analyzer)
builder.add_edge(risk_analyzer, aggregator)

# Add conditional routing based on risk score
builder.add_switch_case_edge_group(
    aggregator,
    [
        Case(
            condition=lambda assessment: assessment.overall_risk_score >= 0.6,
            target=human_review_executor  # Route to HITL
        ),
        Default(target=auto_approve_executor),  # Auto-approve low risk
    ],
)

# Build with checkpointing
workflow = builder.with_checkpointing(
    InMemoryCheckpointStorage()  # Or CosmosDBCheckpointStorage for production
).build()
```

---

## 6. MCP Integration for HITL

### 6.1 MCP Tool Approval Flow

```
+==============================================================================+
|                    MCP TOOL APPROVAL ARCHITECTURE                             |
+==============================================================================+

  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
  │   Agent     │────►│ MCP Client  │────►│ MCP Server  │
  │             │     │             │     │   (Tools)   │
  └─────────────┘     └──────┬──────┘     └──────┬──────┘
                             │                   │
                             │ Tool Call Request │
                             │◄──────────────────┤
                             │                   │
                    ┌────────▼────────┐          │
                    │ Approval Policy │          │
                    │ Check           │          │
                    └────────┬────────┘          │
                             │                   │
              ┌──────────────┼──────────────┐    │
              │              │              │    │
              ▼              ▼              ▼    │
       ┌──────────┐   ┌──────────┐   ┌──────────┐
       │ "always" │   │ "never"  │   │ Callback │
       │ approve  │   │ approve  │   │ (custom) │
       └────┬─────┘   └────┬─────┘   └────┬─────┘
            │              │              │
            │              │         ┌────▼────┐
            │              │         │ Human   │
            │              │         │ Decision│
            │              │         └────┬────┘
            │              │              │
            └──────────────┼──────────────┘
                           ▼
                    ┌──────────────┐
                    │ Execute or   │
                    │ Reject Tool  │
                    └──────────────┘
```

### 6.2 MCP Approval Configuration

```python
# OpenAI Agents SDK Example
from openai_agents import Agent, MCPServer

mcp_server = MCPServer(
    name="database_tools",
    url="http://localhost:8080/mcp",
    tool_config={
        "require_approval": {
            # Per-tool approval policies
            "read_data": "never",        # Auto-approve reads
            "write_data": "always",      # Always require approval
            "delete_data": "always",     # Always require approval
            "query_stats": "never",      # Auto-approve
        }
    },
    on_approval_request=human_approval_callback  # Custom handler
)

async def human_approval_callback(tool_name: str, arguments: dict) -> bool:
    """Custom approval logic for sensitive operations."""
    # Log the request
    print(f"Approval requested for {tool_name}: {arguments}")

    # Get human decision (from UI, Slack, etc.)
    decision = await get_human_approval(tool_name, arguments)

    return decision.approved
```

### 6.3 Enterprise MCP Gateway (Agentforce Pattern)

```
┌─────────────────────────────────────────────────────────────────┐
│                    MCP GATEWAY (Enterprise)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Policy Engine:                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Role-based access (which users/agents can call tools)  │   │
│  │ • Time-based restrictions (business hours only)          │   │
│  │ • Data exposure limits (PII masking)                     │   │
│  │ • Approval workflows (high-risk → human review)          │   │
│  │ • Rate limiting (prevent abuse)                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  Audit Trail:                                                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Who requested (agent/user ID)                          │   │
│  │ • What was requested (tool, arguments)                   │   │
│  │ • When (timestamp)                                       │   │
│  │ • Decision (approved/rejected/edited)                    │   │
│  │ • Reviewer (human ID if HITL)                            │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Comparison Matrix

### 7.1 Framework Comparison

| Feature | AutoGen (Legacy) | MAF | LangGraph | Claude Code |
|---------|------------------|-----|-----------|-------------|
| **Checkpoint** | Custom code | ✅ Built-in | ✅ Built-in | Session-based |
| **Pause/Resume** | Blocking only | ✅ Async | ✅ interrupt() | Manual |
| **State Persistence** | None | Cosmos DB | PostgreSQL | Memory |
| **Request/Response** | Custom | RequestInfoExecutor | HumanInterrupt | Tool permission |
| **MCP Support** | Basic | ✅ Native | Plugin | ✅ Native |
| **Production Ready** | No | ✅ Yes | ✅ Yes | CLI only |

### 7.2 HITL Pattern Comparison

| Pattern | Use Case | Pros | Cons |
|---------|----------|------|------|
| **Pre-execution** | Configuration, constraints | Predictable | Limited flexibility |
| **Checkpoint-based** | Mid-workflow approval | Full state preservation | Complexity |
| **Interrupt-based** | Immediate pause | Simple | May lose context |
| **Queue-based** | Async review | Scalable | Latency |

---

## 8. Best Practices

### 8.1 Design Principles

```
+==============================================================================+
|                    HITL DESIGN PRINCIPLES                                     |
+==============================================================================+

1. RISK-BASED FILTERING
   ├── Not all actions need human review
   ├── Define clear thresholds (risk score, confidence, action type)
   └── Automate low-risk, review high-risk

2. CHECKPOINT BOUNDARIES
   ├── Create checkpoints at natural superstep boundaries
   ├── Avoid mid-computation checkpoints
   └── Ensure serializable state

3. TIMEOUT & ESCALATION
   ├── Define SLAs for human response
   ├── Implement escalation paths
   └── Have fallback decisions

4. AUDIT TRAIL
   ├── Log every decision point
   ├── Capture before/after states
   └── Record reviewer identity and reasoning

5. FEEDBACK LOOPS
   ├── Collect human corrections
   ├── Use for model improvement
   └── Track accuracy over time
```

### 8.2 Anti-Patterns to Avoid

| Anti-Pattern | Problem | Solution |
|--------------|---------|----------|
| Review everything | Defeats automation | Risk-based filtering |
| Checkpoint too often | Performance overhead | Superstep boundaries |
| No timeout | Workflows stuck forever | SLA + escalation |
| No audit trail | Compliance risk | Log all decisions |
| Ignore feedback | No improvement | Feedback loops |

---

## 9. Implementation Checklist

### 9.1 Pre-Implementation

- [ ] Define risk thresholds and policies
- [ ] Choose checkpoint storage (InMemory for dev, Cosmos DB for prod)
- [ ] Design human review interface (UI/Queue/API)
- [ ] Define timeout and escalation paths
- [ ] Set up audit logging

### 9.2 Implementation

- [ ] Implement RequestInfoExecutor for HITL points
- [ ] Configure conditional routing (switch_case_edge_group)
- [ ] Enable checkpointing on workflow builder
- [ ] Implement response handlers
- [ ] Add MCP tool approval policies

### 9.3 Post-Implementation

- [ ] Test checkpoint/resume scenarios
- [ ] Verify audit trail completeness
- [ ] Load test human review queue
- [ ] Monitor approval latencies
- [ ] Set up feedback collection

---

## 10. Conclusion

### 10.1 Core Points

1. **HITL là mandatory cho enterprise AI** - EU AI Act 2026 yêu cầu human oversight
2. **Checkpoint-based approach** là standard cho MAF - đảm bảo state preservation và deterministic resume
3. **MCP integration** cho phép fine-grained tool approval - read-only by default
4. **Risk-based routing** balance giữa automation và oversight

### 10.2 So What? (Implications)

```
For Architects:
└── Design workflows with explicit HITL checkpoints from the start

For Developers:
└── Use RequestInfoExecutor + checkpoint storage, not custom solutions

For Operations:
└── Plan for human reviewer availability and escalation paths

For Compliance:
└── HITL provides audit trail required by EU AI Act Article 14
```

---

## 11. References

### 11.1 Official Documentation

| Source | Link | Topic |
|--------|------|-------|
| Microsoft Learn | [Checkpoints](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/checkpoints) | Checkpoint API |
| Microsoft Learn | [Requests and Responses](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/requests-and-responses) | RequestInfoExecutor |
| Microsoft Learn | [Using MCP Tools](https://learn.microsoft.com/en-us/agent-framework/user-guide/model-context-protocol/using-mcp-tools) | MCP Integration |
| Microsoft TechCommunity | [Building HITL Workflows](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/building-human-in-the-loop-ai-workflows-with-microsoft-agent-framework/4460342) | HITL Patterns |

### 11.2 External Resources

| Source | Link | Topic |
|--------|------|-------|
| Permit.io | [HITL Best Practices](https://www.permit.io/blog/human-in-the-loop-for-ai-agents-best-practices-frameworks-use-cases-and-demo) | Best Practices |
| LangChain | [Human-in-the-Loop](https://docs.langchain.com/oss/python/langchain/human-in-the-loop) | LangGraph HITL |
| MCP Spec | [Specification](https://modelcontextprotocol.io/specification/2025-11-25) | MCP Protocol |
| OpenAI | [MCP Integration](https://openai.github.io/openai-agents-python/mcp/) | OpenAI Agents SDK |

### 11.3 Chat History Context

This document was created based on research conversation covering:
- Initial query về Microsoft Agent Framework MCP tools
- Deep dive vào HITL mechanisms
- Checkpoint-based workflow patterns
- MCP tool approval configurations

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| **HITL** | Human-in-the-Loop - pattern yêu cầu human intervention |
| **Checkpoint** | Saved state of workflow at a point in time |
| **Superstep** | Unit of execution between checkpoints |
| **RequestInfoEvent** | Event emitted khi workflow cần human input |
| **RequestInfoExecutor** | Executor xử lý HITL requests |
| **MCP** | Model Context Protocol - standard cho tool integration |
| **InputPort** | MAF component cho request/response routing |

---

## Appendix B: Decision Tree for HITL Implementation

```
                        ┌───────────────────┐
                        │ Does action have  │
                        │ real-world impact?│
                        └─────────┬─────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    │ No          │             │ Yes
                    ▼             │             ▼
             ┌──────────┐        │      ┌──────────────┐
             │ No HITL  │        │      │ Is action    │
             │ needed   │        │      │ reversible?  │
             └──────────┘        │      └──────┬───────┘
                                 │             │
                                 │   ┌─────────┼─────────┐
                                 │   │ Yes     │         │ No
                                 │   ▼         │         ▼
                                 │ ┌────────┐  │  ┌────────────┐
                                 │ │ Low    │  │  │ HITL       │
                                 │ │ risk?  │  │  │ REQUIRED   │
                                 │ └───┬────┘  │  └────────────┘
                                 │     │       │
                                 │ ┌───┼───┐   │
                                 │ │Yes│   │No │
                                 │ ▼   │   ▼   │
                                 │┌────┴─┐┌────┴────┐
                                 ││Auto- ││HITL     │
                                 ││approve││Optional │
                                 │└──────┘└─────────┘
```

---

*Document generated: 2026-01-21*
*Framework: Core Thinking (MECE + First Principles + 5 Whys)*
*Research depth: Comprehensive with multi-source validation*
