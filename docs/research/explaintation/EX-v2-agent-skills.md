# EX-v2: Agent Skills - Open Standard for AI Agent Expertise

**Created**: 2026-01-21
**Version**: v2
**Status**: Research Complete
**Author**: Claude Code Research
**Related**: [EX-v1-human-in-the-loop.md](./EX-v1-human-in-the-loop.md)

---

## 1. Executive Summary

```
+==============================================================================+
|                         AGENT SKILLS RESEARCH OVERVIEW                        |
+==============================================================================+
|                                                                              |
|   WHAT: Agent Skills = Folders of instructions, scripts, resources           |
|         that agents discover and load dynamically to perform tasks           |
|         more accurately and efficiently                                      |
|                                                                              |
|   WHY:  - Package domain expertise into reusable components                  |
|         - Avoid repeating instructions across conversations                  |
|         - Enable cross-platform portability (Claude, Copilot, Codex)         |
|         - Optimize context window usage via progressive disclosure           |
|                                                                              |
|   HOW:  SKILL.md file with YAML frontmatter + instructions                   |
|         Progressive disclosure: Metadata → Instructions → Resources          |
|                                                                              |
|   KEY INSIGHT:                                                               |
|   "MCP provides the PLUMBING for tool access,                                |
|    Skills provide the BRAIN for how to use those tools effectively"          |
|                                   — Simon Willison                           |
|                                                                              |
+==============================================================================+
```

---

## 2. Definition & Core Concepts

### 2.1 What Are Agent Skills?

Agent Skills are **organized folders of instructions, scripts, and resources** that agents can discover and load dynamically to perform better at specific tasks.

**Analogy**: Building a skill is like creating an **onboarding guide for a new employee**:
- Không cần dạy từ đầu mỗi lần
- Có sẵn best practices và procedures
- Biết cách làm việc theo quy trình công ty

### 2.2 Origin & Timeline

```
Timeline:
─────────────────────────────────────────────────────────────────────────────
Oct 2025     │ Anthropic introduces Agent Skills internally
             │
Dec 18, 2025 │ Anthropic releases Agent Skills as OPEN STANDARD
             │ Launch partners: Atlassian, Canva, Figma, Notion,
             │                  Cloudflare, Zapier, Stripe, Vercel
             │
Dec 2025     │ GitHub Copilot adds Agent Skills support (VS Code Insiders)
             │
Jan 2026     │ Agent Skills available in VS Code stable release
             │ Supported: Claude, Copilot, Codex, Gemini CLI
─────────────────────────────────────────────────────────────────────────────
```

### 2.3 Key Characteristics

| Characteristic | Description |
|----------------|-------------|
| **Portable** | Works across Claude, GitHub Copilot, OpenAI Codex, Gemini CLI |
| **File-based** | Simple folder structure with SKILL.md |
| **Progressive** | Loads content on-demand, not all at once |
| **Composable** | Skills can reference other skills |
| **Version-controlled** | Can be stored in Git repositories |

---

## 3. Problem Decomposition (MECE Framework)

### 3.1 Issue Tree - Why Agent Skills Exist

```
Problem: AI Agents Need Domain Expertise
├── 1. KNOWLEDGE GAP
│   ├── 1.1 LLMs have general knowledge, not domain-specific
│   ├── 1.2 Organization procedures not in training data
│   ├── 1.3 Best practices change faster than model updates
│   └── 1.4 Context-specific nuances missed
│
├── 2. EFFICIENCY PROBLEMS
│   ├── 2.1 Repeating same instructions every conversation
│   ├── 2.2 Context window wasted on boilerplate
│   ├── 2.3 Inconsistent outputs across sessions
│   └── 2.4 No reusability of expertise
│
├── 3. PORTABILITY ISSUES
│   ├── 3.1 Custom prompts locked to one platform
│   ├── 3.2 No standardization across AI tools
│   ├── 3.3 Vendor lock-in concerns
│   └── 3.4 Duplication of effort
│
└── 4. SCALABILITY CHALLENGES
    ├── 4.1 Large context windows still have limits
    ├── 4.2 Loading everything upfront is wasteful
    ├── 4.3 Enterprise needs 100s of specialized workflows
    └── 4.4 Team knowledge hard to capture
```

### 3.2 Mutually Exclusive Categories

| Category | What It Provides | Example |
|----------|------------------|---------|
| **Procedural Knowledge** | How to do tasks | "When reviewing contracts, check X, Y, Z" |
| **Domain Expertise** | Specialized knowledge | Legal terminology, API patterns |
| **Organizational Context** | Company-specific info | Brand guidelines, coding standards |
| **Workflow Automation** | Multi-step processes | CI/CD pipelines, release procedures |

### 3.3 Collectively Exhaustive Verification

```
✅ All agent capability needs covered:
   - WHAT to do (instructions)
   - HOW to do it (procedures, scripts)
   - WITH WHAT context (resources, templates)
   - WHEN to apply (triggers, conditions)
```

---

## 4. First Principles Analysis

### 4.1 Fundamental Question

> "What is the minimum structure needed to package reusable expertise for AI agents?"

**Answer**: A simple folder with:
1. **Metadata** (name, description) - for discovery
2. **Instructions** (markdown) - for guidance
3. **Resources** (optional) - for execution

### 4.2 Breaking Down to Fundamentals

```
Agent Skill = Metadata + Instructions + Resources

Where:
├── Metadata
│   └── YAML frontmatter: name, description (for agent discovery)
│
├── Instructions
│   └── Markdown content: procedures, guidelines, examples
│
└── Resources (optional)
    ├── Scripts (Python, bash, etc.)
    ├── Templates (document formats)
    ├── Reference docs (API specs, schemas)
    └── Examples (input/output samples)
```

### 4.3 Why File-Based Architecture?

| Approach | Pros | Cons |
|----------|------|------|
| **Database** | Searchable, scalable | Complex setup, vendor lock-in |
| **API** | Real-time updates | Network dependency, latency |
| **File-based** ✅ | Simple, portable, version-controlled | Manual discovery |

**Decision**: File-based wins because it's **simple, portable, and git-friendly**.

---

## 5. 5 Whys Analysis

### 5.1 Why do agents need Skills (not just prompts)?

```
Why 1: Agents repeat the same mistakes without domain knowledge
       ↓
Why 2: Prompts are ephemeral and lost after conversation
       ↓
Why 3: No mechanism to persist expertise across sessions
       ↓
Why 4: LLMs optimized for general knowledge, not specific procedures
       ↓
Why 5: Human expertise needs explicit packaging to be reusable
       ════════════════════════════════════════════════════════════
       ROOT CAUSE: Knowledge must be externalized and structured
                   for consistent agent performance
```

### 5.2 Why progressive disclosure (not load everything)?

```
Why 1: Loading all skills wastes context window tokens
       ↓
Why 2: Context windows have limits (even 200K+ tokens)
       ↓
Why 3: Most skills are irrelevant for current task
       ↓
Why 4: Token cost scales with context size
       ↓
Why 5: Efficient resource usage requires lazy loading
       ════════════════════════════════════════════════════════════
       ROOT CAUSE: "Largest context ≠ best context"
                   Quality > Quantity for LLM performance
```

---

## 6. MCP vs Agent Skills - Deep Comparison

### 6.1 Core Distinction

```
+==============================================================================+
|                         MCP vs AGENT SKILLS                                   |
+==============================================================================+
|                                                                              |
|   ┌─────────────────────────────────┐  ┌─────────────────────────────────┐   |
|   │         MCP (Tools)             │  │       AGENT SKILLS              │   |
|   │         "PLUMBING"              │  │         "BRAIN"                 │   |
|   ├─────────────────────────────────┤  ├─────────────────────────────────┤   |
|   │                                 │  │                                 │   |
|   │  WHAT tools are available       │  │  HOW to use tools effectively   │   |
|   │                                 │  │                                 │   |
|   │  • Connect to APIs              │  │  • Procedural knowledge         │   |
|   │  • Query databases              │  │  • Domain expertise             │   |
|   │  • Execute actions              │  │  • Best practices               │   |
|   │  • Real-time data access        │  │  • Organizational context       │   |
|   │                                 │  │                                 │   |
|   │  Network: ✅ Full access        │  │  Network: ❌ Local only          │   |
|   │  Loading: Always available      │  │  Loading: Progressive/lazy      │   |
|   │                                 │  │                                 │   |
|   └─────────────────────────────────┘  └─────────────────────────────────┘   |
|                                                                              |
|   ════════════════════════════════════════════════════════════════════════   |
|                                                                              |
|   ANALOGY:                                                                   |
|   MCP = Giving someone a toolbox (hammer, screwdriver, saw)                  |
|   Skills = Teaching them carpentry (how to build a table)                    |
|                                                                              |
+==============================================================================+
```

### 6.2 Detailed Comparison Matrix

| Aspect | MCP (Model Context Protocol) | Agent Skills |
|--------|------------------------------|--------------|
| **Primary Purpose** | Connect to external systems | Package knowledge & workflows |
| **Provides** | Tool connectivity, actions | Procedural knowledge, expertise |
| **Network Access** | ✅ Full network capability | ❌ Local execution only |
| **Architecture** | Protocol-based (JSON-RPC 2.0) | File-system based |
| **Loading** | Always available in context | Progressive disclosure |
| **Context Usage** | Consumes tokens on init | Lazy loading, minimal overhead |
| **State** | Stateless (per call) | Can reference prior context |
| **Best For** | External integrations, real-time data | Internal workflows, standardization |
| **Example** | `database_query()`, `send_email()` | "How to review contracts" |

### 6.3 When to Use Which?

```
DECISION TREE: MCP vs Skills
─────────────────────────────────────────────────────────────────────────────

                    ┌───────────────────────┐
                    │ Does task need        │
                    │ external data/action? │
                    └───────────┬───────────┘
                                │
              ┌─────────────────┼─────────────────┐
              │ Yes             │                 │ No
              ▼                 │                 ▼
       ┌──────────────┐        │          ┌──────────────┐
       │ Use MCP      │        │          │ Does task    │
       │              │        │          │ need special │
       │ Examples:    │        │          │ expertise?   │
       │ • API calls  │        │          └──────┬───────┘
       │ • Database   │        │                 │
       │ • File I/O   │        │      ┌──────────┼──────────┐
       └──────────────┘        │      │ Yes      │          │ No
                               │      ▼          │          ▼
                               │ ┌──────────┐    │    ┌──────────┐
                               │ │ Use      │    │    │ Use      │
                               │ │ Skills   │    │    │ Prompts  │
                               │ └──────────┘    │    └──────────┘
                               │                 │
                               │    ┌────────────┘
                               │    │
                               ▼    ▼
                        ┌──────────────────┐
                        │ Use BOTH         │
                        │                  │
                        │ MCP for data     │
                        │ Skills for how   │
                        └──────────────────┘
```

### 6.4 Use Cases Comparison

| Scenario | MCP Role | Skills Role |
|----------|----------|-------------|
| **Contract Review** | Fetch document from Google Drive | Teach how to analyze clauses, flag risks |
| **Code Review** | Access GitHub PR, run linters | Apply coding standards, security checks |
| **Data Analysis** | Query database, fetch datasets | Teach analysis methodology, visualization |
| **CI/CD Pipeline** | Trigger builds, deploy | Follow release procedures, generate reports |
| **Customer Support** | Query CRM, send emails | Follow support scripts, escalation rules |

### 6.5 Combined Architecture

```
+==============================================================================+
|                    MCP + SKILLS COMBINED ARCHITECTURE                         |
+==============================================================================+

  ┌─────────────────────────────────────────────────────────────────────────┐
  │                           AI AGENT                                       │
  │                                                                          │
  │  ┌─────────────────────────────────────────────────────────────────┐    │
  │  │                     SKILLS (Brain)                               │    │
  │  │                                                                  │    │
  │  │  "When analyzing sales data:                                    │    │
  │  │   1. Filter by date range first                                 │    │
  │  │   2. Group by region                                            │    │
  │  │   3. Calculate YoY growth                                       │    │
  │  │   4. Flag anomalies > 2 std dev"                                │    │
  │  │                                                                  │    │
  │  └──────────────────────────────┬──────────────────────────────────┘    │
  │                                 │                                        │
  │                                 │ Guides HOW                             │
  │                                 ▼                                        │
  │  ┌─────────────────────────────────────────────────────────────────┐    │
  │  │                      MCP (Plumbing)                              │    │
  │  │                                                                  │    │
  │  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │    │
  │  │  │ Database │  │ GitHub   │  │ Slack    │  │ Google   │        │    │
  │  │  │ Server   │  │ Server   │  │ Server   │  │ Drive    │        │    │
  │  │  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘        │    │
  │  │       │             │             │             │               │    │
  │  └───────┼─────────────┼─────────────┼─────────────┼───────────────┘    │
  │          │             │             │             │                     │
  └──────────┼─────────────┼─────────────┼─────────────┼─────────────────────┘
             │             │             │             │
             ▼             ▼             ▼             ▼
       ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
       │ Postgres │  │ GitHub   │  │ Slack    │  │ Google   │
       │    DB    │  │   API    │  │   API    │  │ Drive    │
       └──────────┘  └──────────┘  └──────────┘  └──────────┘
```

---

## 7. Agent Skills Architecture

### 7.1 Folder Structure

```
my-skill/
├── SKILL.md                    # Required: Main skill definition
│   ├── ---                     #   YAML frontmatter (metadata)
│   │   ├── name: my-skill
│   │   └── description: "..."
│   ├── ---
│   └── [Instructions]          #   Markdown content
│
├── reference.md                # Optional: Detailed reference
├── examples/                   # Optional: Usage examples
│   ├── input-1.json
│   └── output-1.json
├── scripts/                    # Optional: Executable code
│   ├── validate.py
│   └── transform.sh
└── templates/                  # Optional: Document templates
    └── report-template.docx
```

### 7.2 SKILL.md Format

```yaml
---
name: contract-review
description: >
  Analyze legal contracts for risks and compliance issues.
  Use when reviewing agreements, NDAs, or vendor contracts.
  Identifies problematic clauses, missing terms, and suggests revisions.
version: 1.0.0
author: Legal Team
---

# Contract Review Skill

## When to Use
- Reviewing new vendor agreements
- Analyzing customer contracts
- NDA compliance checks

## Procedure

### Step 1: Initial Scan
- Identify contract type
- Note parties involved
- Check for standard clauses

### Step 2: Risk Analysis
- Look for unlimited liability
- Check indemnification terms
- Review termination clauses

### Step 3: Compliance Check
- Verify data protection clauses
- Check jurisdiction
- Ensure regulatory compliance

## Red Flags
- Missing limitation of liability
- One-sided indemnification
- Auto-renewal without notice
- Unfavorable governing law

## Output Format
Provide structured analysis with:
1. Executive summary
2. Risk assessment (High/Medium/Low)
3. Specific clause concerns
4. Recommended changes
```

### 7.3 Progressive Disclosure Mechanism

```
+==============================================================================+
|                    PROGRESSIVE DISCLOSURE LEVELS                              |
+==============================================================================+

  Context Window Usage
  ────────────────────

  Level 1: DISCOVERY (~50-100 tokens per skill)
  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
  │
  │  Agent loads: name + description only
  │  Example: "contract-review: Analyze legal contracts for risks..."
  │
  │  Purpose: Know WHEN to use each skill
  │
  ▼

  Level 2: ACTIVATION (~2,000-5,000 tokens)
  ░░░░░░░░░░░░░░░░░░░░████████████████░░░░░░░░░░░░░░
  │
  │  Agent loads: Full SKILL.md content
  │  Triggered: When user request matches skill description
  │
  │  Purpose: Know HOW to perform the task
  │
  ▼

  Level 3: EXECUTION (Variable, as needed)
  ░░░░░░░░░░░░░░░░░░░░████████████████████████████░░
  │
  │  Agent loads: Referenced files (scripts, templates, docs)
  │  Triggered: When specific resources needed
  │
  │  Purpose: Access detailed reference material
  │
  ▼

  KEY INSIGHT:
  ─────────────────────────────────────────────────────────────────────────
  • Scripts execute OUTSIDE context window (only output returned)
  • Bundled files have NO PENALTY until accessed
  • Effectively UNBOUNDED context through lazy loading
```

### 7.4 Platform Support

| Platform | Skill Location | Status |
|----------|----------------|--------|
| **Claude.ai** | Pre-built + custom upload | ✅ Available |
| **Claude Code** | `.claude/skills/` | ✅ Available |
| **GitHub Copilot** | `.github/skills/` or `.claude/skills/` | ✅ Stable (Jan 2026) |
| **OpenAI Codex** | `~/.codex/skills/` | ✅ Available |
| **Gemini CLI** | Community support | 🔄 Beta |

---

## 8. Comparison: Skills vs Other Approaches

### 8.1 Skills vs Prompts vs Projects vs MCP vs Subagents

```
+==============================================================================+
|                    COMPREHENSIVE COMPARISON MATRIX                            |
+==============================================================================+

┌───────────────┬───────────────┬───────────────┬───────────────┬─────────────┐
│    Feature    │    Skills     │    Prompts    │   Projects    │     MCP     │
├───────────────┼───────────────┼───────────────┼───────────────┼─────────────┤
│   Provides    │ Procedural    │ Moment-to-    │ Background    │ Tool        │
│               │ knowledge     │ moment guide  │ knowledge     │ connectivity│
├───────────────┼───────────────┼───────────────┼───────────────┼─────────────┤
│  Persistence  │ Across        │ Single        │ Within        │ Continuous  │
│               │ conversations │ conversation  │ project       │             │
├───────────────┼───────────────┼───────────────┼───────────────┼─────────────┤
│   Best For    │ Specialized,  │ Quick,        │ Centralized   │ Data        │
│               │ reusable      │ one-off       │ reference     │ access      │
├───────────────┼───────────────┼───────────────┼───────────────┼─────────────┤
│    Loading    │ Dynamically,  │ Each turn     │ Always in     │ Always      │
│               │ as needed     │               │ project       │ available   │
├───────────────┼───────────────┼───────────────┼───────────────┼─────────────┤
│ Context Cost  │ Progressive   │ Per-message   │ Fixed         │ Per-tool    │
│               │ (lazy)        │               │ overhead      │ definition  │
├───────────────┼───────────────┼───────────────┼───────────────┼─────────────┤
│  Portability  │ ✅ Cross-     │ ❌ Single     │ ❌ Platform   │ ✅ Protocol │
│               │ platform      │ platform      │ specific      │ standard    │
└───────────────┴───────────────┴───────────────┴───────────────┴─────────────┘
```

### 8.2 Decision Guide

```
WHEN TO USE WHAT:
─────────────────────────────────────────────────────────────────────────────

Use PROMPTS when:
├── Quick, one-off request
├── Conversational refinement
└── Real-time guidance

Use SKILLS when:
├── Repeating same instructions multiple times
├── Need consistent output format
├── Domain expertise required
└── Want cross-platform portability

Use PROJECTS when:
├── Ongoing initiative with fixed context
├── Need persistent background knowledge
└── Working within single workspace

Use MCP when:
├── Need external data access
├── Executing actions on real systems
└── Real-time integrations required

Use SUBAGENTS when:
├── Task requires isolation
├── Parallel processing needed
└── Different tool permissions required

COMBINE THEM:
├── Project + Skills = Background knowledge + Dynamic expertise
├── MCP + Skills = Data access + Procedural knowledge
└── Subagents + Skills = Specialized agents with portable expertise
```

---

## 9. Use Cases & Examples

### 9.1 Document Skills (Anthropic Official)

| Skill | Purpose | Key Capabilities |
|-------|---------|------------------|
| **pdf** | PDF manipulation | Extract text, merge/split, fill forms |
| **xlsx** | Excel analysis | Formulas, pivot tables, charts |
| **docx** | Word documents | Create, edit, format |
| **pptx** | PowerPoint | Create presentations, add slides |

### 9.2 Enterprise Use Cases

#### Legal Review Skill

```yaml
---
name: contract-analysis
description: >
  Analyze contracts against company policies and flag risks.
  Use for vendor agreements, NDAs, and customer contracts.
---

# Contract Analysis

## Procedure
1. Identify contract type and parties
2. Extract key terms (duration, value, liability)
3. Compare against approved templates
4. Flag deviations and risks
5. Generate compliance report

## Red Flags
- Unlimited liability clauses
- Missing indemnification
- Unfavorable payment terms
- Auto-renewal without notice

## Output
- Risk score (High/Medium/Low)
- Clause-by-clause analysis
- Recommended changes
```

**Impact**: Law departments handle 3-4x more contract volume with same team size.

#### Code Review Skill

```yaml
---
name: code-review
description: >
  Review code changes for quality, security, and standards compliance.
  Use when reviewing PRs, refactoring, or security audits.
---

# Code Review Skill

## Security Checks
- [ ] No hardcoded secrets
- [ ] Input validation present
- [ ] SQL injection prevention
- [ ] XSS protection

## Quality Checks
- [ ] Clear naming conventions
- [ ] Appropriate error handling
- [ ] Test coverage adequate
- [ ] Documentation updated

## Performance
- [ ] No N+1 queries
- [ ] Appropriate caching
- [ ] No memory leaks
```

### 9.3 Combined MCP + Skills Example

**Scenario**: CI/CD Dashboard Analysis

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CI/CD ANALYSIS: MCP + SKILLS                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   MCP SERVERS (Data Collection):                                            │
│   ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐                   │
│   │ GitHub   │  │ CircleCI │  │ Slack    │  │ Metrics  │                   │
│   │ (PRs)    │  │ (builds) │  │ (alerts) │  │ (DB)     │                   │
│   └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘                   │
│        │             │             │             │                          │
│        └─────────────┼─────────────┼─────────────┘                          │
│                      ▼                                                       │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                    SKILLS (Analysis)                                 │   │
│   │                                                                      │   │
│   │   build-trend-analysis:                                             │   │
│   │   "Analyze build success rates, identify flaky tests,               │   │
│   │    calculate deployment frequency, flag regressions"                │   │
│   │                                                                      │   │
│   │   release-report:                                                   │   │
│   │   "Generate release notes, summarize changes,                       │   │
│   │    identify breaking changes, create changelog"                     │   │
│   │                                                                      │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   OUTPUT: Weekly release report with trend analysis                         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Best Practices

### 10.1 Skill Authoring Guidelines

```
+==============================================================================+
|                    SKILL AUTHORING BEST PRACTICES                             |
+==============================================================================+

1. DESCRIPTION IS CRITICAL
   ├── Agent uses description to SELECT correct skill from 100+ options
   ├── Include: what it does, when to use, specific triggers
   ├── Put ALL "when to use" info in frontmatter (not body)
   └── Body only loaded AFTER triggering

2. KEEP SKILL.md FOCUSED
   ├── Under 500 lines recommended
   ├── Split into separate files when approaching limit
   ├── Mutually exclusive contexts → separate files
   └── Reduces token usage

3. USE SCRIPTS FOR EXECUTION
   ├── Script code never loads into context window
   ├── Only script OUTPUT consumes tokens
   ├── More efficient than inline code generation
   └── Deterministic execution

4. INCLUDE EXAMPLES
   ├── Show expected input/output
   ├── Cover edge cases
   ├── Demonstrate proper formatting
   └── Help agent understand intent

5. TEST THOROUGHLY
   ├── Test with multiple compatible agents
   ├── Verify against specification
   ├── Include validation outputs
   └── Document expected behavior
```

### 10.2 Anti-Patterns to Avoid

| Anti-Pattern | Problem | Solution |
|--------------|---------|----------|
| **"When to use" in body** | Agent can't see it before activation | Put in description frontmatter |
| **Monolithic SKILL.md** | Context bloat | Split into referenced files |
| **Hardcoded values** | Not portable | Use variables, templates |
| **No examples** | Agent guesses | Include clear examples |
| **Overlapping skills** | Agent confused | Clear, distinct descriptions |

### 10.3 Progressive Disclosure Optimization

```
CONTEXT EFFICIENCY TIPS:
─────────────────────────────────────────────────────────────────────────────

DO:
✅ Keep metadata concise (~100 words)
✅ Use scripts for computation (output only)
✅ Reference files instead of inline content
✅ Split mutually exclusive contexts

DON'T:
❌ Load everything upfront
❌ Embed large code blocks in SKILL.md
❌ Duplicate content across skills
❌ Ignore context window limits
```

---

## 11. Chat History Context Analysis

### 11.1 Key Insights from Research Conversation

```
CHAT HISTORY SUMMARY:
─────────────────────────────────────────────────────────────────────────────

Q1: "dùng tool mcp nào của Microsoft Agent Framework có approved by user?"
    → Discovered: No MCP tools configured in current project
    → Found: serena MCP server used in other projects

Q2: "Human in the loop MCP MAF?"
    → Deep dive into HITL patterns in Microsoft Agent Framework
    → Created: EX-v1-human-in-the-loop.md

Q3: "giải thích cái này agentic skill?"
    → Initial explanation of Agent Skills concept
    → Key insight: "MCP = Plumbing, Skills = Brain"
    → Comparison matrix created

Q4: Current request - Full research document
    → Comprehensive MECE analysis
    → First Principles breakdown
    → 5 Whys root cause analysis
    → MCP vs Skills deep comparison
```

### 11.2 Connection to HITL (EX-v1)

```
RELATIONSHIP: HITL + MCP + SKILLS
─────────────────────────────────────────────────────────────────────────────

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   SKILLS teach:     "When deleting user data, always backup first"          │
│                              ↓                                               │
│   MCP executes:     delete_user_data() → triggers HITL approval             │
│                              ↓                                               │
│   HITL ensures:     Human reviews before irreversible action                │
│                              ↓                                               │
│   RESULT:           Skill-guided, tool-executed, human-approved             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

KEY INSIGHT:
Skills REDUCE need for HITL by teaching agents to do things correctly,
but high-risk actions still require human oversight via checkpoints.
```

---

## 12. Conclusion

### 12.1 Core Points

1. **Agent Skills = Packaged Expertise**
   - Folders of instructions, scripts, resources
   - Discovered and loaded dynamically by agents
   - Open standard with cross-platform support

2. **MCP vs Skills: Complementary, Not Competing**
   - MCP = Plumbing (WHAT tools available)
   - Skills = Brain (HOW to use tools effectively)
   - Use BOTH for sophisticated agents

3. **Progressive Disclosure = Efficient Context Usage**
   - Metadata → Instructions → Resources
   - Lazy loading prevents context bloat
   - Scripts execute outside context window

4. **Open Standard = Portability**
   - Works across Claude, Copilot, Codex, Gemini
   - Version-controlled in Git
   - Enterprise-ready with launch partners

### 12.2 So What? (Implications)

```
For AI Engineers:
└── Package domain expertise into Skills for consistent agent behavior
    Stop repeating instructions; create reusable Skills instead

For Architects:
└── Design systems with MCP for data + Skills for procedures
    "Teams that invest only in skills end up with agents that
     think brilliantly but can't actually do anything"

For Enterprise:
└── Skills enable capturing organizational knowledge
    Legal, compliance, and operational procedures become portable

For the Ecosystem:
└── Open standard prevents vendor lock-in
    Skills created for Claude work with Copilot, Codex, and more
```

---

## 13. References

### 13.1 Official Documentation

| Source | Link | Topic |
|--------|------|-------|
| Anthropic Engineering | [Equipping agents with Skills](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills) | Architecture |
| Claude Blog | [Skills Explained](https://claude.com/blog/skills-explained) | Comparison |
| Agent Skills Spec | [agentskills.io](https://agentskills.io/home) | Specification |
| GitHub | [anthropics/skills](https://github.com/anthropics/skills) | Official Skills |
| VS Code | [Agent Skills](https://code.visualstudio.com/docs/copilot/customization/agent-skills) | Copilot Integration |

### 13.2 Comparison Resources

| Source | Link | Topic |
|--------|------|-------|
| ByteBridge | [MCP vs Agent Skills](https://bytebridge.medium.com/model-context-protocol-mcp-vs-agent-skills-empowering-ai-agents-with-tools-and-expertise-3062acafd4f7) | Detailed Comparison |
| IntuitionLabs | [Claude Skills vs MCP](https://intuitionlabs.ai/articles/claude-skills-vs-mcp) | Technical Comparison |
| Goose Blog | [Did Skills Kill MCP?](https://block.github.io/goose/blog/2025/12/22/agent-skills-vs-mcp/) | Analysis |
| Armin Ronacher | [Skills vs Dynamic MCP](https://lucumr.pocoo.org/2025/12/13/skills-vs-mcp/) | Architecture |

### 13.3 Enterprise & Use Cases

| Source | Link | Topic |
|--------|------|-------|
| Claude Blog | [Enterprise AI Agents 2026](https://claude.com/blog/how-enterprises-are-building-ai-agents-in-2026) | Enterprise |
| VentureBeat | [Anthropic Enterprise Skills](https://venturebeat.com/technology/anthropic-launches-enterprise-agent-skills-and-opens-the-standard) | Launch |
| Sema4.ai | [AI Agent Use Cases](https://sema4.ai/blog/ai-agent-use-cases/) | Use Cases |

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| **Agent Skill** | Folder of instructions, scripts, resources that agents load dynamically |
| **SKILL.md** | Required markdown file defining a skill (YAML frontmatter + instructions) |
| **Progressive Disclosure** | Loading mechanism: metadata → instructions → resources as needed |
| **MCP** | Model Context Protocol - standard for connecting AI to external tools |
| **Frontmatter** | YAML metadata at start of SKILL.md (name, description required) |
| **Context Window** | Maximum tokens an LLM can process in single interaction |

---

## Appendix B: Quick Reference Card

```
+==============================================================================+
|                    AGENT SKILLS QUICK REFERENCE                               |
+==============================================================================+

CREATE A SKILL:
  1. Create folder: .claude/skills/my-skill/
  2. Add SKILL.md with:
     ---
     name: my-skill
     description: What it does and when to use
     ---
     # Instructions here

USE A SKILL:
  • Skills auto-activate when request matches description
  • No explicit invocation needed

MCP vs SKILLS:
  • MCP = Connect to external systems (APIs, databases)
  • Skills = Teach how to use those systems

PROGRESSIVE DISCLOSURE:
  Level 1: name + description (~50 tokens)
  Level 2: Full SKILL.md (~2-5K tokens)
  Level 3: Referenced files (as needed)

BEST PRACTICES:
  ✅ Put "when to use" in description
  ✅ Keep SKILL.md under 500 lines
  ✅ Use scripts for computation
  ✅ Include examples
  ❌ Don't embed large code blocks
  ❌ Don't duplicate across skills

PLATFORMS:
  Claude.ai, Claude Code, GitHub Copilot, OpenAI Codex, Gemini CLI

+==============================================================================+
```

---

*Document generated: 2026-01-21*
*Framework: Core Thinking (MECE + First Principles + 5 Whys)*
*Research depth: Comprehensive with multi-source validation*
*Related: EX-v1-human-in-the-loop.md*
