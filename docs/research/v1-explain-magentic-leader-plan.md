# Magentic-One: Agent Leader Planning - Deep Dive

**Version**: 1.0
**Created**: 2026-01-02
**Status**: Notes from explanation session
**Related**: [v1-microsoft-agent-framework-research.md](./v1-microsoft-agent-framework-research.md)

---

## Table of Contents

1. [Dual Loop System Overview](#1-dual-loop-system-overview)
2. [Outer Loop - Strategic Planning](#2-outer-loop---strategic-planning)
3. [Techniques for Better Planning](#3-techniques-for-better-planning)
4. [Plan Evaluation Framework](#4-plan-evaluation-framework)
5. [Continuous Improvement](#5-continuous-improvement)

---

## 1. Dual Loop System Overview

Magentic-One sử dụng hệ thống **Dual Loop** để quản lý multi-agent orchestration:

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

### Key Insight

- **Outer Loop** = Người quản lý dự án (lập kế hoạch chiến lược)
- **Inner Loop** = Đội ngũ thực thi (làm công việc cụ thể)

---

## 2. Outer Loop - Strategic Planning

### 2.1 Task Ledger Components

| Component | Purpose | Example |
|-----------|---------|---------|
| **FACTS** | Thông tin đã xác minh | "User muốn sửa lỗi bảo mật" |
| **GUESSES** | Giả thuyết cần kiểm chứng | "Lỗi có thể ở authentication code" |
| **PLAN** | Kế hoạch từng bước | "1. Scan repo → 2. Analyze → 3. Fix → 4. Test" |

### 2.2 When Outer Loop Triggers

```
┌─────────────────────────────────────────────────────────────┐
│                    TRIGGER CONDITIONS                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   1. Progress Stalls                                        │
│      └── Agent không thể hoàn thành task                   │
│      └── Nhiều lần retry thất bại                          │
│                                                             │
│   2. Strategy Needs Revision                                │
│      └── Phát hiện thông tin mới thay đổi approach         │
│      └── GUESSES được verify là sai                        │
│                                                             │
│   3. Task Completion                                        │
│      └── Hoàn thành major milestone                        │
│      └── Cần evaluate và plan next phase                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 Execution Flow Example

```
+===============================================================================+
|            EXAMPLE: "Find and fix security bug in repo"                        |
+===============================================================================+
|                                                                               |
|   [OUTER LOOP] Creates initial Task Ledger:                                  |
|   ───────────────────────────────────────                                    |
|   FACTS:    - User wants security bug fixed                                  |
|   GUESSES:  - Bug might be in authentication code                            |
|   PLAN:     1. Browse repo structure                                         |
|             2. Search for security-related code                              |
|             3. Analyze potential vulnerabilities                             |
|             4. Write fix                                                     |
|             5. Test fix                                                      |
|                                                                               |
|   [INNER LOOP] Execution:                                                     |
|   ───────────────────────                                                    |
|                                                                               |
|   STEP 1: Assign to FileSurfer                                               |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   FileSurfer → Browses repo structure                               │    |
|   │   Returns:   → Found /src/auth/, /src/api/, /tests/                 │    |
|   │   Status:    → ✅ Complete                                          │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                              │                                                |
|                              ▼                                                |
|   STEP 2: Assign to Coder                                                    |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Coder     → Analyzes /src/auth/ for vulnerabilities              │    |
|   │   Returns:  → Found SQL injection in login.py line 45              │    |
|   │   Status:   → ✅ Complete                                          │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                              │                                                |
|                              ▼                                                |
|   STEP 3: Assign to Coder                                                    |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Coder     → Writes fix using parameterized queries               │    |
|   │   Returns:  → Created patch file                                   │    |
|   │   Status:   → ✅ Complete                                          │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                              │                                                |
|                              ▼                                                |
|   STEP 4: Assign to ComputerTerminal                                         |
|   ┌─────────────────────────────────────────────────────────────────────┐    |
|   │   Terminal  → Runs pytest /tests/test_auth.py                      │    |
|   │   Returns:  → All tests pass                                       │    |
|   │   Status:   → ✅ Complete                                          │    |
|   └─────────────────────────────────────────────────────────────────────┘    |
|                              │                                                |
|                              ▼                                                |
|   [OUTER LOOP] Final Update:                                                 |
|   ───────────────────────────                                                |
|   FACTS:    - SQL injection found and fixed in login.py                      |
|             - All tests passing                                              |
|   STATUS:   - ✅ TASK COMPLETE                                               |
|                                                                               |
+===============================================================================+
```

---

## 3. Techniques for Better Planning

### 3.1 Self-Correction Mechanism

```
┌─────────────────────────────────────────────────────────────┐
│                    SELF-CORRECTION MECHANISM                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   OUTER LOOP tự đánh giá qua:                              │
│                                                             │
│   1. FACTS tracking    → Plan có dựa trên sự thật không?   │
│   2. GUESSES validation → Giả thuyết đã được verify chưa?  │
│   3. PROGRESS check    → Có tiến triển không hay bị stuck? │
│                                                             │
│   Nếu stuck → Trigger REPLAN                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Plan Quality Criteria

| Criteria | Description | How to Measure |
|----------|-------------|----------------|
| **Completeness** | Plan có đủ bước để hoàn thành task? | Checklist coverage |
| **Feasibility** | Các bước có khả thi với agents hiện có? | Agent capability mapping |
| **Dependency clarity** | Thứ tự các bước có đúng? | DAG validation |
| **Measurable milestones** | Có thể đo tiến độ? | Progress indicators |
| **Fallback strategy** | Có plan B khi fail? | Error handling paths |

### 3.3 Chain-of-Thought (CoT) Planning

```
┌─────────────────────────────────────────────────────────────┐
│                    COT PLANNING EXAMPLE                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ❌ BAD (Vague):                                           │
│   "Sửa lỗi bảo mật"                                        │
│                                                             │
│   ✅ GOOD (CoT):                                            │
│   "Để sửa lỗi bảo mật, tôi cần:                            │
│    1. Hiểu loại lỗi → cần FileSurfer scan code             │
│    2. Xác định root cause → cần Coder analyze              │
│    3. Viết fix → cần Coder implement                       │
│    4. Verify fix → cần Terminal run tests                  │
│    Mỗi bước phụ thuộc kết quả bước trước."                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.4 ReAct Pattern (Reason + Act)

```
┌────────────────────────────────────────────────────────────┐
│                    REACT PATTERN FLOW                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  THOUGHT:      Cần tìm file chứa lỗi trước khi sửa        │

│  ACTION:       Giao FileSurfer scan /src/auth/            │
│  OBSERVATION:  Tìm thấy SQL injection ở login.py:45       │
│                                                            │
│  THOUGHT:      Đã xác định vị trí, cần phân tích chi tiết │
│  ACTION:       Giao Coder analyze login.py                │
│  OBSERVATION:  Line 45 dùng string concatenation cho SQL  │
│                                                            │
│  THOUGHT:      Cần fix bằng parameterized query           │
│  ACTION:       Giao Coder write fix                       │
│  OBSERVATION:  Fix completed, created patch               │
│                                                            │
│  THOUGHT:      Cần verify fix không break existing code   │
│  ACTION:       Giao Terminal run tests                    │
│  OBSERVATION:  All tests pass                             │
│                                                            │
│  CONCLUSION:   Task completed successfully                │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 3.5 Tree-of-Thoughts (ToT) for Complex Tasks

```
                        [Main Goal]
                            │
             ┌──────────────┼──────────────┐
             │              │              │
        [Approach A]   [Approach B]   [Approach C]
             │              │              │
        Score: 0.7     Score: 0.9     Score: 0.5
                            │
                       [Choose B]
                            │
                  [Continue planning...]
```

**Use ToT when:**
- Multiple valid approaches exist
- Need to evaluate trade-offs
- High-stakes decisions

---

## 4. Plan Evaluation Framework

### 4.1 Evaluation Metrics

```
+===============================================================+
|              PLAN QUALITY EVALUATION MATRIX                    |
+===============================================================+
|                                                               |
|   METRIC 1: Task Completion Rate (TCR)                        |
|   ─────────────────────────────────────                       |
|   Formula: Completed_Steps / Total_Steps × 100%               |
|   Target:  > 90%                                              |
|                                                               |
|   METRIC 2: Plan Revision Count (PRC)                         |
|   ─────────────────────────────────────                       |
|   Đo số lần phải replan                                       |
|   Target:  < 2 revisions per task                             |
|   Ít revision = plan ban đầu tốt                              |
|                                                               |
|   METRIC 3: Agent Utilization Efficiency (AUE)                |
|   ─────────────────────────────────────                       |
|   Đúng agent cho đúng việc?                                   |
|   Formula: Optimal_Assignments / Total_Assignments × 100%     |
|                                                               |
|   METRIC 4: Time-to-Completion (TTC)                          |
|   ─────────────────────────────────────                       |
|   So sánh với baseline                                        |
|   Target:  < 1.2x optimal time                                |
|                                                               |
+===============================================================+
```

### 4.2 Quality Scorecard

```
+===============================================================+
|                    PLAN QUALITY SCORECARD                      |
+===============================================================+
|                                                               |
|   DIMENSION          WEIGHT    SCORE (1-5)    WEIGHTED        |
|   ─────────────────────────────────────────────────────────   |
|   Completeness        25%         ___          ___            |
|   Feasibility         25%         ___          ___            |
|   Efficiency          20%         ___          ___            |
|   Robustness          15%         ___          ___            |
|   Clarity             15%         ___          ___            |
|   ─────────────────────────────────────────────────────────   |
|   TOTAL SCORE                                  ___/5.0        |
|                                                               |
|   RATING:                                                     |
|   4.5+ = Excellent    3.5-4.4 = Good                         |
|   2.5-3.4 = Needs Improvement    <2.5 = Poor                 |
|                                                               |
+===============================================================+
```

### 4.3 Dimension Definitions

| Dimension | Score 5 | Score 3 | Score 1 |
|-----------|---------|---------|---------|
| **Completeness** | Covers all required steps | Missing some steps | Major gaps |
| **Feasibility** | All steps executable | Some steps unclear | Many impossible steps |
| **Efficiency** | Optimal path | Some redundancy | Lots of waste |
| **Robustness** | Has fallbacks | Partial error handling | No error handling |
| **Clarity** | Crystal clear | Some ambiguity | Very confusing |

---

## 5. Continuous Improvement

### 5.1 Improvement Cycle

```
┌─────────────────────────────────────────────────────────────┐
│                  PLAN IMPROVEMENT CYCLE                      │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │   1. EXECUTE PLAN      │
              └───────────┬────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │   2. COLLECT METRICS   │
              │   - TCR, PRC, AUE, TTC │
              └───────────┬────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │   3. ANALYZE FAILURES  │
              │   - Why did step X fail│
              │   - Which agent unfit  │
              └───────────┬────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │   4. UPDATE KNOWLEDGE  │
              │   - Add to FACTS       │
              │   - Update heuristics  │
              └───────────┬────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │   5. REFINE PROMPTS    │
              │   - Improve planning   │
              │     instructions       │
              └───────────┬────────────┘
                          │
                          └──────────► Back to Step 1
```

### 5.2 Agent Leader System Prompt Template

```python
LEADER_SYSTEM_PROMPT = """
You are the Agent Leader. When planning, ALWAYS follow:

## PLANNING CHECKLIST:
□ List all known FACTS
□ Identify UNKNOWNS to investigate
□ Break task into small, measurable steps
□ Map each step to appropriate agent
□ Identify dependencies between steps
□ Define SUCCESS CRITERIA for each step
□ Prepare FALLBACK if step fails

## AGENT CAPABILITIES:
- FileSurfer: Browse files, navigate codebase
- Coder: Analyze code, write code, debug
- WebSurfer: Search web, fetch documentation
- ComputerTerminal: Run commands, execute tests

## OUTPUT FORMAT:
FACTS: [what is known]
UNKNOWNS: [needs investigation]
PLAN:
  Step 1: [action] → Agent: [name] → Success: [criteria]
  Step 2: [action] → Agent: [name] → Success: [criteria]
  ...
FALLBACK: [what to do if fail]
"""
```

### 5.3 Common Planning Pitfalls

| Pitfall | Symptom | Solution |
|---------|---------|----------|
| **Over-planning** | Too many micro-steps | Group related actions |
| **Under-planning** | Vague steps, agent confusion | Add specific criteria |
| **Wrong agent assignment** | Task fails or slow | Review agent capabilities |
| **Missing dependencies** | Steps executed out of order | Build dependency graph |
| **No fallback** | System stuck on failure | Add error handling paths |

### 5.4 Learning from Failures

```
┌─────────────────────────────────────────────────────────────┐
│                    FAILURE ANALYSIS TEMPLATE                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   FAILED STEP:    _________________________________         │
│                                                             │
│   ASSIGNED AGENT: _________________________________         │
│                                                             │
│   EXPECTED:       _________________________________         │
│                                                             │
│   ACTUAL:         _________________________________         │
│                                                             │
│   ROOT CAUSE:     [ ] Wrong agent                          │
│                   [ ] Unclear instructions                  │
│                   [ ] Missing prerequisite                  │
│                   [ ] External dependency                   │
│                   [ ] Other: _______________               │
│                                                             │
│   LESSON LEARNED: _________________________________         │
│                                                             │
│   KNOWLEDGE UPDATE:                                         │
│   - Add to FACTS: _________________________________         │
│   - Update HEURISTIC: _____________________________         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Summary

### Key Takeaways

1. **Dual Loop System** separates strategic planning (Outer) from tactical execution (Inner)

2. **Task Ledger** maintains FACTS, GUESSES, and PLAN for transparency

3. **Good plans** are Complete, Feasible, Efficient, Robust, and Clear

4. **Planning techniques**:
   - Chain-of-Thought (CoT) for step-by-step reasoning
   - ReAct for interleaved reasoning and action
   - Tree-of-Thoughts for complex multi-path decisions

5. **Continuous improvement** through metrics collection and failure analysis

### Quick Reference

```
Plan Quality = f(Completeness, Feasibility, Efficiency, Robustness, Clarity)

Good Plan Indicators:
✅ TCR > 90%
✅ PRC < 2
✅ AUE > 85%
✅ TTC < 1.2x optimal
```

---

## References

- [v1-microsoft-agent-framework-research.md](./v1-microsoft-agent-framework-research.md) - Original MAF research
- Magentic-One Paper - Microsoft Research
- ReAct: Synergizing Reasoning and Acting in Language Models
- Tree of Thoughts: Deliberate Problem Solving with Large Language Models