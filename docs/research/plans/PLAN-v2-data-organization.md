# Data Organization for Multi-Agent Systems (v2)

**Version**: 2.0
**Created**: 2026-01-01
**Purpose**: Làm rõ cách tổ chức dữ liệu cho hệ thống Multi-Agent
**Base**: PLAN-v1-multi-agent-lab.md

---

## Mục Lục

1. [Tổng quan về Data trong Multi-Agent](#1-tổng-quan-về-data-trong-multi-agent)
2. [Data Layers Architecture](#2-data-layers-architecture)
3. [Data Flow giữa các Agents](#3-data-flow-giữa-các-agents)
4. [Các Pattern tổ chức Data](#4-các-pattern-tổ-chức-data)
5. [Chi tiết Implementation](#5-chi-tiết-implementation)
6. [Best Practices](#6-best-practices)

---

## 1. Tổng quan về Data trong Multi-Agent

### Câu hỏi cốt lõi

> "Trong hệ thống Multi-Agent, dữ liệu nằm ở đâu? Agents truy cập như thế nào? Làm sao để agents chia sẻ thông tin?"

### Các loại Data trong Multi-Agent System

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    TYPES OF DATA IN MULTI-AGENT SYSTEM                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1️⃣ SOURCE DATA (Dữ liệu nguồn)                                             │
│     ├── Raw data từ database, file, API                                     │
│     ├── Ví dụ: Điểm số học sinh, thông tin lớp học                         │
│     └── Đặc điểm: Không thay đổi trong quá trình conversation               │
│                                                                              │
│  2️⃣ CONVERSATION DATA (Dữ liệu hội thoại)                                   │
│     ├── Messages giữa các agents                                            │
│     ├── Ví dụ: "Analyst nói: Điểm TB lớp 10A là 7.5"                        │
│     └── Đặc điểm: Tích lũy qua từng round, mọi agent đều thấy               │
│                                                                              │
│  3️⃣ INTERMEDIATE DATA (Dữ liệu trung gian)                                  │
│     ├── Kết quả xử lý của mỗi agent                                         │
│     ├── Ví dụ: Biểu đồ PNG, file báo cáo MD                                 │
│     └── Đặc điểm: Output của agent này = Input của agent khác               │
│                                                                              │
│  4️⃣ AGENT STATE (Trạng thái nội bộ)                                         │
│     ├── Memory, context riêng của mỗi agent                                 │
│     ├── Ví dụ: "Tôi đã phân tích lớp 10A xong"                              │
│     └── Đặc điểm: Chỉ agent đó biết                                         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Data Layers Architecture

### Kiến trúc 4 tầng

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      DATA LAYERS ARCHITECTURE                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  LAYER 4: OUTPUT LAYER                                               │    │
│  │  ════════════════════                                                │    │
│  │  Nơi lưu kết quả cuối cùng                                          │    │
│  │                                                                      │    │
│  │  output/                                                             │    │
│  │  ├── charts/           ← Biểu đồ PNG, PDF                           │    │
│  │  │   ├── class_10a_subjects.png                                     │    │
│  │  │   └── class_10a_progress.png                                     │    │
│  │  │                                                                   │    │
│  │  └── reports/          ← Báo cáo Markdown                           │    │
│  │      ├── report_10a_2024.md                                         │    │
│  │      └── email_nguyen_van_a.txt                                     │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                  ▲                                          │
│                                  │ Writer Agent saves                       │
│                                  │                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  LAYER 3: CONVERSATION LAYER (Shared Context)                        │    │
│  │  ════════════════════════════════════════════                        │    │
│  │  Nơi các agents "nói chuyện" và chia sẻ thông tin                   │    │
│  │                                                                      │    │
│  │  ┌─────────────────────────────────────────────────────────────┐    │    │
│  │  │  GroupChat.messages[]                                        │    │    │
│  │  │                                                              │    │    │
│  │  │  [0] User:     "Phân tích lớp 10A"                          │    │    │
│  │  │  [1] Analyst:  "Điểm TB: 7.5, Toán cao nhất..."             │    │    │
│  │  │  [2] Coder:    "```python\nimport matplotlib...```"          │    │    │
│  │  │  [3] Executor: "✅ Chart saved: class_10a.png"               │    │    │
│  │  │  [4] Writer:   "📋 BÁO CÁO: ... TASK_COMPLETE"              │    │    │
│  │  │                                                              │    │    │
│  │  │  ⚡ TẤT CẢ AGENTS ĐỀU THẤY TOÀN BỘ MESSAGES                 │    │    │
│  │  └─────────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                  ▲                                          │
│                                  │ Agents read/write                        │
│                                  │                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  LAYER 2: TOOLS LAYER (Data Access)                                  │    │
│  │  ════════════════════════════════════                                │    │
│  │  Functions mà agents gọi để lấy/xử lý data                          │    │
│  │                                                                      │    │
│  │  tools/                                                              │    │
│  │  ├── data_tools.py     ← get_class_grades(), get_statistics()       │    │
│  │  ├── code_tools.py     ← execute_python_code()                      │    │
│  │  └── report_tools.py   ← save_report(), create_email()              │    │
│  │                                                                      │    │
│  │  ⚡ AGENTS KHÔNG TRUY CẬP TRỰC TIẾP DATABASE                        │    │
│  │  ⚡ AGENTS GỌI TOOLS → TOOLS TRUY CẬP DATA                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                  ▲                                          │
│                                  │ Tools access                             │
│                                  │                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  LAYER 1: SOURCE DATA LAYER                                          │    │
│  │  ══════════════════════════════                                      │    │
│  │  Dữ liệu nguồn (fake hoặc thật)                                     │    │
│  │                                                                      │    │
│  │  data/                                                               │    │
│  │  ├── fake_generator.py ← FakeDataGenerator class                    │    │
│  │  ├── models.py         ← Student, Grade, ClassStatistics           │    │
│  │  │                                                                   │    │
│  │  │  ┌──────────────────────────────────────────────────────────┐   │    │
│  │  │  │  data_generator (global instance)                         │   │    │
│  │  │  │                                                           │   │    │
│  │  │  │  students: List[Student]   ← 90 học sinh                 │   │    │
│  │  │  │  grades: List[Grade]       ← 630 điểm số                 │   │    │
│  │  │  │                                                           │   │    │
│  │  │  │  Hoặc kết nối thật:                                      │   │    │
│  │  │  │  - PostgreSQL database                                    │   │    │
│  │  │  │  - REST API endpoint                                      │   │    │
│  │  │  │  - CSV/Excel files                                        │   │    │
│  │  │  └──────────────────────────────────────────────────────────┘   │    │
│  │  │                                                                   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Tóm tắt mỗi Layer

| Layer | Tên | Mục đích | Ví dụ |
|-------|-----|----------|-------|
| **1** | Source Data | Lưu trữ dữ liệu gốc | Database, CSV, Fake data |
| **2** | Tools | Cung cấp API truy cập data | `get_grades()`, `save_report()` |
| **3** | Conversation | Chia sẻ thông tin giữa agents | GroupChat.messages |
| **4** | Output | Kết quả cuối cùng | Charts, Reports |

---

## 3. Data Flow giữa các Agents

### Flow diagram chi tiết

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       DATA FLOW IN MULTI-AGENT                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  USER INPUT: "Phân tích lớp 10A và tạo báo cáo"                             │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  ROUND 1: ANALYST                                                    │    │
│  │                                                                      │    │
│  │  1. Đọc user request từ GroupChat.messages                          │    │
│  │  2. Gọi tool: get_class_statistics("10A")                           │    │
│  │                                                                      │    │
│  │     ┌──────────────────────────────────────────────────────┐        │    │
│  │     │  Tool: get_class_statistics                          │        │    │
│  │     │                                                       │        │    │
│  │     │  Step 1: data_generator.get_class_statistics("10A")  │        │    │
│  │     │  Step 2: Calculate averages from grades list         │        │    │
│  │     │  Step 3: Return formatted string                     │        │    │
│  │     │                                                       │        │    │
│  │     │  Return: "📈 THỐNG KÊ LỚP 10A                         │        │    │
│  │     │           Điểm TB: 7.5                                │        │    │
│  │     │           Cao nhất: 9.8..."                           │        │    │
│  │     └──────────────────────────────────────────────────────┘        │    │
│  │                                                                      │    │
│  │  3. Thêm message vào GroupChat:                                     │    │
│  │     "Analyst: 📊 PHÂN TÍCH: Điểm TB lớp 10A là 7.5..."              │    │
│  │                                                                      │    │
│  │  OUTPUT: Text analysis → GroupChat.messages[1]                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  ROUND 2: CODER                                                      │    │
│  │                                                                      │    │
│  │  1. Đọc Analyst output từ GroupChat.messages[1]                     │    │
│  │  2. Extract data: subjects=["Toán","Lý"...], scores=[7.5,6.8...]    │    │
│  │  3. Generate Python code với matplotlib                             │    │
│  │                                                                      │    │
│  │  OUTPUT: Python code string → GroupChat.messages[2]                 │    │
│  │                                                                      │    │
│  │  ⚠️ CODER KHÔNG GỌI TOOL - chỉ generate code text                   │    │
│  │  ⚠️ Data đến từ ANALYST MESSAGE, không từ database                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  ROUND 3: EXECUTOR                                                   │    │
│  │                                                                      │    │
│  │  1. Đọc Coder output từ GroupChat.messages[2]                       │    │
│  │  2. Extract code từ markdown block                                  │    │
│  │  3. Gọi tool: execute_python_code(code)                             │    │
│  │                                                                      │    │
│  │     ┌──────────────────────────────────────────────────────┐        │    │
│  │     │  Tool: execute_python_code                            │        │    │
│  │     │                                                       │        │    │
│  │     │  Step 1: Write code to temp file                     │        │    │
│  │     │  Step 2: subprocess.run(python temp.py)              │        │    │
│  │     │  Step 3: Check output/charts/ for new files          │        │    │
│  │     │  Step 4: Return result + file paths                  │        │    │
│  │     │                                                       │        │    │
│  │     │  SIDE EFFECT: Creates output/charts/class_10a.png    │        │    │
│  │     └──────────────────────────────────────────────────────┘        │    │
│  │                                                                      │    │
│  │  OUTPUT: File path → GroupChat.messages[3]                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  ROUND 4: WRITER                                                     │    │
│  │                                                                      │    │
│  │  1. Đọc TẤT CẢ messages từ GroupChat                                │    │
│  │     - messages[0]: User request                                     │    │
│  │     - messages[1]: Analyst insights                                 │    │
│  │     - messages[2]: Coder code                                       │    │
│  │     - messages[3]: Executor chart path                              │    │
│  │                                                                      │    │
│  │  2. Tổng hợp thành báo cáo                                          │    │
│  │  3. Gọi tool: save_report("report_10a", content)                    │    │
│  │                                                                      │    │
│  │     ┌──────────────────────────────────────────────────────┐        │    │
│  │     │  Tool: save_report                                    │        │    │
│  │     │                                                       │        │    │
│  │     │  SIDE EFFECT: Creates output/reports/report_10a.md   │        │    │
│  │     └──────────────────────────────────────────────────────┘        │    │
│  │                                                                      │    │
│  │  OUTPUT: "TASK_COMPLETE" → Kết thúc conversation                    │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  FINAL OUTPUT:                                                              │
│  ├── output/charts/class_10a.png                                           │
│  └── output/reports/report_10a.md                                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Bảng Data Flow

| Round | Agent | Input (đọc từ đâu) | Process | Output (ghi vào đâu) |
|-------|-------|---------------------|---------|----------------------|
| 0 | User | Keyboard | - | GroupChat.messages[0] |
| 1 | Analyst | messages[0] + Source Data (via tool) | Phân tích | messages[1] = insights text |
| 2 | Coder | messages[1] | Generate code | messages[2] = code block |
| 3 | Executor | messages[2] | Run code | messages[3] + charts/*.png |
| 4 | Writer | messages[0-3] | Tổng hợp | messages[4] + reports/*.md |

---

## 4. Các Pattern tổ chức Data

### Pattern 1: Shared Data Store (Global Instance)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PATTERN 1: SHARED DATA STORE                                                │
│  Dùng khi: Data cần được nhiều tools truy cập                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │  data/fake_generator.py                                         │         │
│  │                                                                  │         │
│  │  class FakeDataGenerator:                                       │         │
│  │      def __init__(self):                                        │         │
│  │          self.students = []  # populated on init               │         │
│  │          self.grades = []    # populated on init               │         │
│  │                                                                  │         │
│  │      def get_students_by_class(self, class_name): ...          │         │
│  │      def get_grades_by_class(self, class_name): ...            │         │
│  │      def get_class_statistics(self, class_name): ...           │         │
│  │                                                                  │         │
│  │  # 🔑 GLOBAL INSTANCE - tất cả tools dùng chung                │         │
│  │  data_generator = FakeDataGenerator()                           │         │
│  └────────────────────────────────────────────────────────────────┘         │
│                                                                              │
│       ┌───────────────────┬───────────────────┬───────────────────┐         │
│       │                   │                   │                   │         │
│       ▼                   ▼                   ▼                   ▼         │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐  │
│  │  data_tools │    │  data_tools │    │  data_tools │    │  data_tools │  │
│  │  .get_grades│    │  .get_stats │    │  .get_weak  │    │  .get_chart │  │
│  │             │    │             │    │  _students  │    │  _data      │  │
│  └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘  │
│                                                                              │
│  ✅ Ưu điểm: Đơn giản, data consistency                                     │
│  ❌ Nhược điểm: Không thread-safe, khó scale                               │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Code minh họa:**

```python
# data/fake_generator.py

class FakeDataGenerator:
    def __init__(self):
        self.students: List[Student] = []
        self.grades: List[Grade] = []
        self._generate_all_data()  # Populate on init

    def get_students_by_class(self, class_name: str) -> List[Student]:
        return [s for s in self.students if s.class_name == class_name]

# Global instance - shared across all tools
data_generator = FakeDataGenerator()


# tools/data_tools.py

from ..data.fake_generator import data_generator  # Import global

def get_class_grades(class_name: str) -> str:
    # Use shared instance
    df = data_generator.to_dataframe(class_name)
    return format_grades(df)
```

---

### Pattern 2: Message Passing (Via GroupChat)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PATTERN 2: MESSAGE PASSING                                                  │
│  Dùng khi: Agents cần chia sẻ intermediate results                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │               GroupChat.messages[] (SHARED MEMORY)              │         │
│  │                                                                  │         │
│  │  [0] {"name": "User",     "content": "Phân tích lớp 10A"}       │         │
│  │  [1] {"name": "Analyst",  "content": "Điểm TB: 7.5, ..."}       │         │
│  │  [2] {"name": "Coder",    "content": "```python...```"}         │         │
│  │  [3] {"name": "Executor", "content": "File: chart.png"}         │         │
│  └────────────────────────────────────────────────────────────────┘         │
│                                                                              │
│       ▲                ▲                ▲                ▲                  │
│       │ read           │ read           │ read           │ read             │
│       │ write          │ write          │ write          │ write            │
│       │                │                │                │                  │
│  ┌─────────┐      ┌─────────┐      ┌─────────┐      ┌─────────┐            │
│  │ Analyst │      │  Coder  │      │Executor │      │ Writer  │            │
│  │         │      │         │      │         │      │         │            │
│  │ Writes: │      │ Reads:  │      │ Reads:  │      │ Reads:  │            │
│  │ insights│      │ insights│      │ code    │      │ ALL     │            │
│  │         │      │         │      │         │      │ messages│            │
│  │ Does NOT│      │ Writes: │      │ Writes: │      │         │            │
│  │ read    │      │ code    │      │ result  │      │ Writes: │            │
│  │ others  │      │         │      │         │      │ report  │            │
│  └─────────┘      └─────────┘      └─────────┘      └─────────┘            │
│                                                                              │
│  ✅ Ưu điểm: Tự nhiên, transparent, dễ debug                               │
│  ❌ Nhược điểm: Data trong message là unstructured text                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Cách Agents đọc message từ nhau:**

```python
# Coder đọc output của Analyst
# KHÔNG phải qua code trực tiếp, mà qua LLM context

# System prompt của Coder:
CODER_SYSTEM_MESSAGE = """
Bạn là LẬP TRÌNH VIÊN.

QUY TẮC:
- Đọc phân tích từ Analyst (message trước đó)
- Extract số liệu: subjects, scores
- Viết code matplotlib
"""

# AutoGen automatically passes all previous messages to each agent
# Coder sees:
#   [0] User: "Phân tích lớp 10A"
#   [1] Analyst: "📊 PHÂN TÍCH: Điểm TB: 7.5, Toán: 8.0, Lý: 6.5..."
#
# LLM (GPT-4) extracts data from Analyst message and generates code
```

---

### Pattern 3: File-Based Transfer

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PATTERN 3: FILE-BASED TRANSFER                                              │
│  Dùng khi: Data quá lớn hoặc là binary (images, PDF)                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  FILESYSTEM (Shared Storage)                                         │    │
│  │                                                                      │    │
│  │  output/                                                             │    │
│  │  ├── charts/                                                         │    │
│  │  │   └── class_10a_subjects.png  ← Executor writes                  │    │
│  │  │                                ← Writer references               │    │
│  │  │                                                                   │    │
│  │  └── reports/                                                        │    │
│  │      └── report_10a.md           ← Writer writes                    │    │
│  │                                   ← User reads                       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│                            ┌─────────────────────────────┐                  │
│                            │      GroupChat.messages     │                  │
│                            │                             │                  │
│                            │ [3] Executor: "File saved:  │                  │
│                            │      charts/class_10a.png"  │ ← Path only     │
│                            │                             │                  │
│                            │ [4] Writer: "Report at:     │                  │
│                            │      reports/report_10a.md" │ ← Path only     │
│                            └─────────────────────────────┘                  │
│                                                                              │
│  ✅ Ưu điểm: Xử lý binary data, không giới hạn size                        │
│  ❌ Nhược điểm: Cần quản lý filesystem, cleanup                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Code minh họa:**

```python
# tools/code_tools.py

def execute_python_code(code: str) -> str:
    # ... run code ...

    # Check for generated files
    generated_files = []
    for f in os.listdir(CHARTS_DIR):
        if f.endswith(('.png', '.pdf')):
            generated_files.append(f"{CHARTS_DIR}/{f}")

    # Return file PATHS (not file content)
    response = "✅ CODE EXECUTED\n"
    response += "📁 Files created:\n"
    for f in generated_files:
        response += f"  - {f}\n"  # ← Path only

    return response


# tools/report_tools.py

def save_report(file_name: str, content: str) -> str:
    file_path = os.path.join(REPORTS_DIR, f"{file_name}.md")

    with open(file_path, 'w') as f:
        f.write(content)

    # Return path, not content
    return f"✅ Report saved: {file_path}"
```

---

### Pattern 4: Tool Return (Structured Data via Functions)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PATTERN 4: TOOL RETURN                                                      │
│  Dùng khi: Agent cần structured data từ external source                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  ANALYST AGENT                                                       │    │
│  │                                                                      │    │
│  │  LLM decides to call: get_class_statistics("10A")                   │    │
│  │       │                                                              │    │
│  │       ▼                                                              │    │
│  │  ┌─────────────────────────────────────────────────────────────┐    │    │
│  │  │  AutoGen Function Calling                                    │    │    │
│  │  │                                                              │    │    │
│  │  │  1. Extract function name + args from LLM response          │    │    │
│  │  │  2. Call Python function: get_class_statistics("10A")       │    │    │
│  │  │  3. Return result to LLM context                            │    │    │
│  │  └─────────────────────────────────────────────────────────────┘    │    │
│  │       │                                                              │    │
│  │       ▼                                                              │    │
│  │  Tool returns: "📈 THỐNG KÊ LỚP 10A\nĐiểm TB: 7.5\n..."             │    │
│  │       │                                                              │    │
│  │       ▼                                                              │    │
│  │  LLM receives result and formulates response                        │    │
│  │       │                                                              │    │
│  │       ▼                                                              │    │
│  │  Agent message: "📊 PHÂN TÍCH: Dựa trên dữ liệu, lớp 10A có..."     │    │
│  │                                                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  DATA FLOW:                                                                 │
│  Database → Tool function → String result → LLM → Agent message            │
│                                                                              │
│  ✅ Ưu điểm: Real-time data, LLM-friendly format                           │
│  ❌ Nhược điểm: Cần format output cho LLM comprehension                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### Tổng hợp các Pattern

| Pattern | Khi nào dùng | Data Type | Ví dụ |
|---------|--------------|-----------|-------|
| **Shared Store** | Multi-tool cần cùng source | Structured | Database, DataFrame |
| **Message Passing** | Agent-to-agent communication | Text | Insights, Analysis |
| **File Transfer** | Large/binary data | Files | Charts, PDFs |
| **Tool Return** | Agent cần external data | String | API response, Query result |

---

## 5. Chi tiết Implementation

### 5.1 Source Data Layer

```python
# src/multi_agent_lab/data/models.py

from pydantic import BaseModel
from datetime import datetime
from typing import List, Dict

class Student(BaseModel):
    """Đại diện cho 1 học sinh"""
    id: str
    name: str
    class_name: str
    date_of_birth: datetime
    parent_email: str

class Grade(BaseModel):
    """Đại diện cho 1 điểm số"""
    student_id: str
    subject: str
    score: float
    semester: str
    recorded_at: datetime

class ClassStatistics(BaseModel):
    """Thống kê tổng hợp của 1 lớp"""
    class_name: str
    average_score: float
    highest_score: float
    lowest_score: float
    subject_averages: Dict[str, float]
```

**Relationship Diagram:**

```
┌──────────────────────────────────────────────────────────────────┐
│                    DATA MODELS RELATIONSHIP                       │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐          ┌─────────────────┐                │
│  │     Student     │          │      Grade      │                │
│  ├─────────────────┤          ├─────────────────┤                │
│  │ id: str         │──────────│ student_id: str │ FK             │
│  │ name: str       │    1:N   │ subject: str    │                │
│  │ class_name: str │          │ score: float    │                │
│  │ date_of_birth   │          │ semester: str   │                │
│  │ parent_email    │          │ recorded_at     │                │
│  └─────────────────┘          └─────────────────┘                │
│         │                                                         │
│         │ aggregated                                              │
│         ▼                                                         │
│  ┌─────────────────────┐                                         │
│  │  ClassStatistics    │                                         │
│  ├─────────────────────┤                                         │
│  │ class_name: str     │                                         │
│  │ average_score       │ ← Calculated from grades               │
│  │ highest_score       │ ← MAX(grades.score)                    │
│  │ lowest_score        │ ← MIN(grades.score)                    │
│  │ subject_averages    │ ← GROUP BY subject                     │
│  └─────────────────────┘                                         │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 5.2 Data Generator (Fake Data)

```python
# src/multi_agent_lab/data/fake_generator.py

class FakeDataGenerator:
    """
    Singleton-like class để tạo và quản lý fake data.

    Tất cả data được tạo 1 lần khi init và lưu trong memory.
    Tools access data qua các method helper.
    """

    def __init__(self):
        self.students: List[Student] = []
        self.grades: List[Grade] = []
        self._generate_all_data()

    def _generate_all_data(self):
        """Tạo data cho 3 lớp, mỗi lớp 30 HS, mỗi HS 7 môn"""
        for class_name in ["10A", "10B", "11A"]:
            students = self._generate_students(class_name, 30)
            self.students.extend(students)

            grades = self._generate_grades(students, "HK1-2024")
            self.grades.extend(grades)

    # Query methods for tools
    def get_students_by_class(self, class_name: str) -> List[Student]:
        """Tool: Lấy danh sách học sinh theo lớp"""
        return [s for s in self.students if s.class_name == class_name]

    def get_grades_by_class(self, class_name: str) -> List[Grade]:
        """Tool: Lấy điểm số theo lớp"""
        student_ids = {s.id for s in self.get_students_by_class(class_name)}
        return [g for g in self.grades if g.student_id in student_ids]

    def to_dataframe(self, class_name: str) -> pd.DataFrame:
        """Tool: Convert to pandas DataFrame for analysis"""
        # ... implementation ...

# Global instance - shared by all tools
data_generator = FakeDataGenerator()
```

### 5.3 Tools Layer (Data Access)

```python
# src/multi_agent_lab/tools/data_tools.py

from typing import Annotated
from ..data.fake_generator import data_generator

def get_class_grades(
    class_name: Annotated[str, "Tên lớp, ví dụ: 10A"]
) -> str:
    """
    Tool function for Analyst agent.

    Input: class_name từ LLM function calling
    Output: Formatted string cho LLM đọc hiểu

    ⚠️ QUAN TRỌNG: Return type là STR, không phải DataFrame
    LLM cần đọc text, không parse binary data
    """
    df = data_generator.to_dataframe(class_name)

    # Format cho LLM-friendly output
    result = f"📊 ĐIỂM SỐ LỚP {class_name}\n"
    result += f"Số học sinh: {len(df)}\n\n"

    result += "🏆 TOP 5:\n"
    for _, row in df.nlargest(5, 'average').iterrows():
        result += f"  - {row['name']}: {row['average']}\n"

    return result
```

### 5.4 Conversation Layer (GroupChat)

```python
# src/multi_agent_lab/orchestration/group_chat.py

class EducationMultiAgentChat:
    def __init__(self):
        # Create agents
        self.analyst = create_analyst_agent(llm_config)
        self.coder = create_coder_agent(llm_config)
        self.executor = create_executor_agent(llm_config)
        self.writer = create_writer_agent(llm_config)

        # GroupChat holds ALL messages - THIS IS THE SHARED CONTEXT
        self.group_chat = GroupChat(
            agents=[self.user_proxy, self.analyst, self.coder,
                    self.executor, self.writer],
            messages=[],  # ← Shared message history
            max_round=10,
            speaker_selection_method="round_robin",
        )

    def run(self, user_message: str):
        """
        Run conversation.

        Data flow:
        1. User message → messages[0]
        2. Analyst reads [0], writes insights → messages[1]
        3. Coder reads [0,1], writes code → messages[2]
        4. Executor reads [2], runs code → messages[3]
        5. Writer reads [0-3], writes report → messages[4]
        """
        self.user_proxy.initiate_chat(self.manager, message=user_message)
        return self.group_chat.messages  # Return all messages
```

---

## 6. Best Practices

### 6.1 Data Format cho LLM

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  BEST PRACTICE: FORMAT DATA CHO LLM ĐỌC HIỂU                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ❌ BAD: Return raw data                                                     │
│  ────────────────────────                                                   │
│  def get_grades(class_name):                                                │
│      return data_generator.grades  # List[Grade] objects                   │
│  # LLM nhận: [Grade(student_id='HS1234', score=8.5, ...), ...]             │
│  # LLM không hiểu!                                                          │
│                                                                              │
│  ❌ BAD: Return JSON only                                                    │
│  ───────────────────────                                                    │
│  def get_grades(class_name):                                                │
│      return json.dumps([g.dict() for g in grades])                         │
│  # LLM nhận: {"student_id": "HS1234", "score": 8.5...}                     │
│  # Có thể đọc nhưng khó phân tích nhanh                                    │
│                                                                              │
│  ✅ GOOD: Return formatted text with structure                              │
│  ─────────────────────────────────────────────                              │
│  def get_grades(class_name):                                                │
│      result = f"📊 ĐIỂM SỐ LỚP {class_name}\n"                             │
│      result += f"Số học sinh: {len(students)}\n"                           │
│      result += f"Điểm TB: {avg_score}\n\n"                                 │
│                                                                              │
│      result += "🏆 TOP 5:\n"                                                │
│      for s in top_5:                                                        │
│          result += f"  - {s.name}: {s.avg}\n"                              │
│                                                                              │
│      return result                                                          │
│  # LLM dễ đọc, có cấu trúc, có context!                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Khi nào dùng Tool vs Message

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  DECISION: TOOL vs MESSAGE PASSING                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SỬ DỤNG TOOL khi:                                                          │
│  ────────────────                                                           │
│  ✅ Cần lấy data từ external source (database, API)                         │
│  ✅ Cần perform action (save file, send email)                              │
│  ✅ Cần real-time data                                                      │
│  ✅ Data quá phức tạp để embed trong prompt                                 │
│                                                                              │
│  Ví dụ:                                                                      │
│  - Analyst gọi get_class_grades() ← Data từ database                       │
│  - Executor gọi execute_python_code() ← Perform action                      │
│  - Writer gọi save_report() ← Persist to disk                              │
│                                                                              │
│  ────────────────────────────────────────────────────────────────────────   │
│                                                                              │
│  SỬ DỤNG MESSAGE PASSING khi:                                               │
│  ───────────────────────────                                                │
│  ✅ Truyền kết quả xử lý của 1 agent cho agent khác                        │
│  ✅ Chia sẻ insights, analysis                                              │
│  ✅ Agent cần đọc output của agent trước                                    │
│                                                                              │
│  Ví dụ:                                                                      │
│  - Coder đọc insights từ Analyst message (không gọi tool)                  │
│  - Writer đọc tất cả messages để tổng hợp (không gọi tool)                 │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.3 Data Isolation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  BEST PRACTICE: DATA ISOLATION                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  NGUYÊN TẮC: Agent KHÔNG truy cập trực tiếp data                            │
│  ────────────────────────────────────────────────                           │
│                                                                              │
│  ❌ BAD: Agent trực tiếp access database                                     │
│                                                                              │
│      Agent ─────────────────────────────────────► Database                  │
│             Direct SQL query (nguy hiểm!)                                   │
│                                                                              │
│  ────────────────────────────────────────────────────────────────────────   │
│                                                                              │
│  ✅ GOOD: Agent gọi tool, tool access database                              │
│                                                                              │
│      Agent ────► Tool Function ────► Data Layer ────► Database             │
│            call   validate          query             return                │
│            func   & format                                                  │
│                                                                              │
│  ƯU ĐIỂM:                                                                    │
│  ├── Security: Tool kiểm soát query, prevent injection                     │
│  ├── Validation: Tool validate input trước khi query                       │
│  ├── Format: Tool format output cho LLM-friendly                           │
│  └── Logging: Tool log mọi data access                                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.4 Checklist tổ chức Data

| # | Câu hỏi | Trả lời | Pattern |
|---|---------|---------|---------|
| 1 | Source data lưu ở đâu? | Database/File/Memory | Shared Store |
| 2 | Agents access data như nào? | Qua Tool functions | Tool Return |
| 3 | Agents chia sẻ kết quả như nào? | Qua GroupChat.messages | Message Passing |
| 4 | Binary output (charts) lưu đâu? | Filesystem + path in message | File Transfer |
| 5 | Tool return format gì? | LLM-friendly string | Tool Return |
| 6 | Ai cleanup output files? | Manual hoặc scheduled job | File Transfer |

---

## Summary

### Tóm tắt kiến trúc Data

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SUMMARY: DATA ORGANIZATION                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  📦 SOURCE DATA                                                             │
│  ├── Lưu trong: data_generator (global instance)                           │
│  ├── Access bởi: Tools only (không phải agents)                            │
│  └── Format: Pydantic models (Student, Grade, ClassStatistics)             │
│                                                                              │
│  🔧 TOOLS LAYER                                                             │
│  ├── Chức năng: Bridge giữa Agents và Data                                 │
│  ├── Input: LLM function call với typed parameters                         │
│  └── Output: LLM-friendly formatted string                                 │
│                                                                              │
│  💬 CONVERSATION LAYER                                                      │
│  ├── Chứa: GroupChat.messages[]                                            │
│  ├── Access: All agents (read + write)                                     │
│  └── Mục đích: Agents chia sẻ insights, code, results                      │
│                                                                              │
│  📁 OUTPUT LAYER                                                            │
│  ├── Charts: output/charts/*.png                                           │
│  ├── Reports: output/reports/*.md                                          │
│  └── Reference: File paths stored in messages                              │
│                                                                              │
│  🔑 KEY PRINCIPLES:                                                         │
│  1. Agents KHÔNG access data trực tiếp → Dùng Tools                        │
│  2. Tools return STRING, không return objects                              │
│  3. Agents đọc nhau qua GroupChat.messages                                 │
│  4. Binary files lưu disk, chỉ pass path qua messages                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

**End of Document**
