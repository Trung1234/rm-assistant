# Implementation Plan: Multi-Agent Conversation Lab (Python)

**Version**: 1.1
**Created**: 2026-01-01
**Updated**: 2026-01-01
**Purpose**: Xây dựng hệ thống lab để hiểu sâu về AutoGen Multi-Agent Conversation
**Language**: Python 3.11+
**Framework**: AutoGen (Microsoft) + Azure OpenAI

---

## 📋 Changelog

### v1.1 (2026-01-01)
- ✅ Thêm **Reviewer Agent** để kiểm tra code trước khi chạy
- ✅ Cập nhật flow: `Analyst → Coder → Reviewer → Executor → Writer`
- ✅ Reviewer kiểm tra: Syntax, Security, Imports, Completeness

### v1.0 (2026-01-01)
- Initial version với 4 agents

---

## Mục Lục

1. [Tổng quan hệ thống](#1-tổng-quan-hệ-thống)
2. [Kiến trúc Multi-Agent](#2-kiến-trúc-multi-agent)
3. [Cấu trúc Project](#3-cấu-trúc-project)
4. [Implementation Steps](#4-implementation-steps)
5. [Fake Data Setup](#5-fake-data-setup)
6. [Agent Definitions](#6-agent-definitions)
7. [Conversation Orchestration](#7-conversation-orchestration)
8. [Testing & Demo](#8-testing--demo)

---

## 1. Tổng quan hệ thống

### Mục tiêu

Xây dựng một **Education Analytics Multi-Agent System** với 5 agents chuyên biệt:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EDUCATION ANALYTICS MULTI-AGENT LAB                   │
│                                                                          │
│  INPUT: "Phân tích dữ liệu học sinh lớp 10A và tạo báo cáo cho phụ huynh"│
│                                                                          │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐           │
│  │   ANALYST    │ ───► │    CODER     │ ───► │   REVIEWER   │           │
│  │    Agent     │      │    Agent     │      │    Agent     │           │
│  │              │      │              │      │              │           │
│  │ Phân tích    │      │ Viết code    │      │ Kiểm tra     │           │
│  │ dữ liệu,     │      │ Python       │      │ syntax,      │           │
│  │ tìm insights │      │ visualization│      │ security     │           │
│  └──────────────┘      └──────────────┘      └──────┬───────┘           │
│                                                      │                   │
│                              ┌───────────────────────┤                   │
│                              │                       │                   │
│                           ✅ OK                   ❌ FAIL                │
│                              │                       │                   │
│                              ▼                       ▼                   │
│                     ┌──────────────┐         ┌──────────────┐           │
│                     │   EXECUTOR   │         │    CODER     │           │
│                     │    Agent     │         │   (sửa lại)  │           │
│                     │              │         └──────────────┘           │
│                     │ Chạy code    │                                    │
│                     │ tạo biểu đồ  │                                    │
│                     └──────┬───────┘                                    │
│                            │                                            │
│                            ▼                                            │
│                   ┌──────────────┐                                      │
│                   │    WRITER    │                                      │
│                   │    Agent     │                                      │
│                   │              │                                      │
│                   │ Viết báo cáo │                                      │
│                   │ cho phụ huynh│                                      │
│                   └──────────────┘                                      │
│                            │                                            │
│                            ▼                                            │
│  OUTPUT: Báo cáo Markdown với biểu đồ và lời khuyên                    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Use Case chính

| # | Use Case | Input | Output |
|---|----------|-------|--------|
| 1 | Phân tích lớp | Dữ liệu điểm lớp 10A | Báo cáo tổng hợp + biểu đồ |
| 2 | Cảnh báo học sinh yếu | Điểm < 5 | Danh sách + email template |
| 3 | So sánh tiến bộ | Điểm 2 học kỳ | Biểu đồ so sánh + nhận xét |

---

## 2. Kiến trúc Multi-Agent

### Agent Roles & Responsibilities

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         AGENT ARCHITECTURE (v1.1)                        │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                     SHARED COMPONENTS                               │ │
│  │                                                                     │ │
│  │  ┌──────────────────┐    ┌──────────────────┐                      │ │
│  │  │   Azure OpenAI   │    │   Fake Data      │                      │ │
│  │  │   LLM Config     │    │   (Pandas DF)    │                      │ │
│  │  └────────┬─────────┘    └────────┬─────────┘                      │ │
│  │           │                       │                                 │ │
│  └───────────┼───────────────────────┼─────────────────────────────────┘ │
│              │                       │                                   │
│              ▼                       ▼                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                          AGENTS LAYER (5 Agents)                     ││
│  │                                                                      ││
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌─────────┐││
│  │  │ ANALYST   │ │  CODER    │ │ REVIEWER  │ │ EXECUTOR  │ │ WRITER  │││
│  │  │           │ │           │ │           │ │           │ │         │││
│  │  │Functions: │ │Functions: │ │Functions: │ │Functions: │ │Functions│││
│  │  │-get_stats │ │-None      │ │-review    │ │-run_code  │ │-save    │││
│  │  │-get_grades│ │(code gen) │ │-check_syn │ │-exec_chart│ │-email   │││
│  │  │-compare   │ │           │ │-check_sec │ │           │ │         │││
│  │  └───────────┘ └───────────┘ └───────────┘ └───────────┘ └─────────┘││
│  │                                                                      ││
│  │  FLOW: Analyst → Coder → Reviewer → Executor → Writer               ││
│  │                              │                                       ││
│  │                    ❌ FAIL ──┴──► Coder (loop back)                  ││
│  │                                                                      ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                    │                                     │
│                                    ▼                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                      ORCHESTRATION LAYER                             ││
│  │                                                                      ││
│  │  ┌──────────────────────────────────────────────────────────────┐   ││
│  │  │                    GroupChat                                  │   ││
│  │  │                                                               │   ││
│  │  │  Speaker Selection: round_robin / auto                       │   ││
│  │  │  Max Rounds: 12 (thêm reviewer rounds)                       │   ││
│  │  │  Termination: "TASK_COMPLETE" keyword                        │   ││
│  │  │                                                               │   ││
│  │  └──────────────────────────────────────────────────────────────┘   ││
│  │                                                                      ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Reviewer Agent Details

| Check | Mô tả | Action nếu FAIL |
|-------|-------|-----------------|
| **Syntax** | Parse code bằng `ast.parse()` | → Coder sửa lỗi |
| **Security** | Kiểm tra `os.system`, `eval`, `subprocess` | → Coder viết lại |
| **Imports** | Chỉ cho phép `matplotlib`, `numpy`, `pandas` | → Coder đổi thư viện |
| **Completeness** | Kiểm tra `plt.savefig()`, không có `TODO` | → Coder hoàn thiện |

---

## 3. Cấu trúc Project

```
edu-multi-agent/
├── src/
│   └── multi_agent_lab/
│       ├── __init__.py
│       ├── main.py                      # Entry point
│       ├── config.py                    # Azure configuration
│       │
│       ├── agents/                      # Agent definitions (5 agents)
│       │   ├── __init__.py
│       │   ├── analyst_agent.py         # Data analyst
│       │   ├── coder_agent.py           # Code generator
│       │   ├── reviewer_agent.py        # Code reviewer (v1.1)
│       │   ├── executor_agent.py        # Code executor
│       │   └── writer_agent.py          # Report writer
│       │
│       ├── tools/                       # Agent tools/functions
│       │   ├── __init__.py
│       │   ├── data_tools.py            # get_grades, get_stats
│       │   ├── code_tools.py            # run_code
│       │   └── report_tools.py          # save_report
│       │
│       ├── orchestration/               # Multi-agent coordination
│       │   ├── __init__.py
│       │   └── group_chat.py            # GroupChat manager
│       │
│       └── data/                        # Fake data
│           ├── __init__.py
│           ├── fake_generator.py        # Generate fake data
│           └── models.py                # Data models
│
├── output/                              # Generated outputs
│   ├── charts/                          # Biểu đồ
│   └── reports/                         # Báo cáo
│
├── notebooks/                           # Jupyter notebooks for testing
│   └── lab_demo.ipynb
│
├── tests/                               # Unit tests
│   ├── test_agents.py
│   └── test_orchestration.py
│
├── requirements.txt                     # Dependencies
├── .env                                 # Azure credentials (gitignored)
├── .env.example                         # Example env file
│
├── plans/
│   └── PLAN-v1-multi-agent-lab.md      # This file
│
└── README.md
```

---

## 4. Implementation Steps

### Phase 1: Project Setup

#### Step 1.1: Tạo Project Structure

```bash
# Tạo thư mục
mkdir -p src/multi_agent_lab/{agents,tools,orchestration,data}
mkdir -p output/{charts,reports}
mkdir -p notebooks tests

# Tạo __init__.py files
touch src/multi_agent_lab/__init__.py
touch src/multi_agent_lab/agents/__init__.py
touch src/multi_agent_lab/tools/__init__.py
touch src/multi_agent_lab/orchestration/__init__.py
touch src/multi_agent_lab/data/__init__.py
```

#### Step 1.2: Tạo requirements.txt

```txt
# requirements.txt

# AutoGen - Microsoft Multi-Agent Framework
pyautogen>=0.2.0

# Azure OpenAI
openai>=1.0.0
azure-identity>=1.15.0

# Data Processing
pandas>=2.0.0
numpy>=1.24.0

# Visualization
matplotlib>=3.7.0
seaborn>=0.12.0

# Fake Data
faker>=20.0.0

# Utilities
python-dotenv>=1.0.0
pydantic>=2.0.0

# Development
jupyter>=1.0.0
ipykernel>=6.0.0
pytest>=7.0.0
```

#### Step 1.3: Tạo .env file

```bash
# .env.example (copy to .env and fill in)

AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=gpt-4
AZURE_OPENAI_API_VERSION=2024-02-15-preview
```

#### Step 1.4: Tạo config.py

```python
# src/multi_agent_lab/config.py

import os
from dotenv import load_dotenv

load_dotenv()

# Azure OpenAI Configuration
AZURE_CONFIG = {
    "model": os.getenv("AZURE_OPENAI_DEPLOYMENT", "gpt-4"),
    "api_key": os.getenv("AZURE_OPENAI_API_KEY"),
    "base_url": os.getenv("AZURE_OPENAI_ENDPOINT"),
    "api_type": "azure",
    "api_version": os.getenv("AZURE_OPENAI_API_VERSION", "2024-02-15-preview"),
}

# Agent Settings
AGENT_SETTINGS = {
    "max_rounds": 10,
    "termination_keyword": "TASK_COMPLETE",
}

# Output directories
OUTPUT_DIR = "./output"
CHARTS_DIR = f"{OUTPUT_DIR}/charts"
REPORTS_DIR = f"{OUTPUT_DIR}/reports"

# Ensure directories exist
os.makedirs(CHARTS_DIR, exist_ok=True)
os.makedirs(REPORTS_DIR, exist_ok=True)
```

---

### Phase 2: Fake Data Layer

#### Step 2.1: Data Models

```python
# src/multi_agent_lab/data/models.py

from pydantic import BaseModel
from datetime import datetime
from typing import List, Dict

class Student(BaseModel):
    id: str
    name: str
    class_name: str
    date_of_birth: datetime
    parent_email: str

class Grade(BaseModel):
    student_id: str
    subject: str
    score: float
    semester: str
    recorded_at: datetime

class ClassInfo(BaseModel):
    class_name: str
    total_students: int
    teacher_name: str

class ClassStatistics(BaseModel):
    class_name: str
    average_score: float
    highest_score: float
    lowest_score: float
    subject_averages: Dict[str, float]
```

#### Step 2.2: Fake Data Generator

```python
# src/multi_agent_lab/data/fake_generator.py

import random
from datetime import datetime, timedelta
from typing import List, Tuple
from faker import Faker
import pandas as pd

from .models import Student, Grade, ClassStatistics

# Vietnamese faker
fake = Faker('vi_VN')

SUBJECTS = ["Toán", "Lý", "Hóa", "Văn", "Anh", "Sử", "Địa"]
CLASS_NAMES = ["10A", "10B", "11A", "11B", "12A"]


class FakeDataGenerator:
    def __init__(self):
        self.students: List[Student] = []
        self.grades: List[Grade] = []
        self._generate_all_data()

    def _generate_all_data(self):
        """Generate fake data for all classes"""
        for class_name in ["10A", "10B", "11A"]:
            class_students = self._generate_students(class_name, 30)
            self.students.extend(class_students)

            class_grades = self._generate_grades(class_students, "HK1-2024")
            self.grades.extend(class_grades)

    def _generate_students(self, class_name: str, count: int = 30) -> List[Student]:
        """Generate fake students for a class"""
        students = []
        for i in range(count):
            student = Student(
                id=f"HS{random.randint(1000, 9999)}",
                name=fake.name(),
                class_name=class_name,
                date_of_birth=fake.date_of_birth(minimum_age=15, maximum_age=18),
                parent_email=fake.email()
            )
            students.append(student)
        return students

    def _generate_grades(self, students: List[Student], semester: str) -> List[Grade]:
        """Generate grades for students with realistic distribution"""
        grades = []

        for student in students:
            # Create a "student profile" - some students are generally better/worse
            student_base = random.gauss(6.5, 1.5)  # Base performance

            for subject in SUBJECTS:
                # Subject-specific variation
                subject_modifier = random.gauss(0, 1)
                score = student_base + subject_modifier

                # Clamp to valid range
                score = max(0, min(10, score))
                score = round(score, 1)

                grade = Grade(
                    student_id=student.id,
                    subject=subject,
                    score=score,
                    semester=semester,
                    recorded_at=datetime.now()
                )
                grades.append(grade)

        return grades

    def get_students_by_class(self, class_name: str) -> List[Student]:
        """Get all students in a class"""
        return [s for s in self.students if s.class_name == class_name]

    def get_grades_by_class(self, class_name: str) -> List[Grade]:
        """Get all grades for a class"""
        student_ids = {s.id for s in self.get_students_by_class(class_name)}
        return [g for g in self.grades if g.student_id in student_ids]

    def get_class_statistics(self, class_name: str) -> ClassStatistics:
        """Calculate statistics for a class"""
        grades = self.get_grades_by_class(class_name)

        if not grades:
            raise ValueError(f"No grades found for class {class_name}")

        scores = [g.score for g in grades]

        # Calculate subject averages
        subject_scores = {}
        for subject in SUBJECTS:
            subject_grades = [g.score for g in grades if g.subject == subject]
            if subject_grades:
                subject_scores[subject] = round(sum(subject_grades) / len(subject_grades), 2)

        return ClassStatistics(
            class_name=class_name,
            average_score=round(sum(scores) / len(scores), 2),
            highest_score=max(scores),
            lowest_score=min(scores),
            subject_averages=subject_scores
        )

    def get_students_needing_support(self, class_name: str, threshold: float = 5.0) -> List[Tuple[Student, float, List[str]]]:
        """Get students with average score below threshold"""
        students = self.get_students_by_class(class_name)
        grades = self.get_grades_by_class(class_name)

        result = []
        for student in students:
            student_grades = [g for g in grades if g.student_id == student.id]
            avg = sum(g.score for g in student_grades) / len(student_grades)

            if avg < threshold:
                weak_subjects = [g.subject for g in student_grades if g.score < 5.0]
                result.append((student, round(avg, 2), weak_subjects))

        return result

    def to_dataframe(self, class_name: str) -> pd.DataFrame:
        """Convert class data to pandas DataFrame for easy analysis"""
        students = self.get_students_by_class(class_name)
        grades = self.get_grades_by_class(class_name)

        # Create a pivot table: students x subjects
        data = []
        for student in students:
            row = {"student_id": student.id, "name": student.name}
            student_grades = {g.subject: g.score for g in grades if g.student_id == student.id}
            row.update(student_grades)
            data.append(row)

        df = pd.DataFrame(data)
        df["average"] = df[SUBJECTS].mean(axis=1).round(2)
        return df


# Global instance
data_generator = FakeDataGenerator()
```

---

### Phase 3: Agent Tools (Functions)

#### Step 3.1: Data Tools

```python
# src/multi_agent_lab/tools/data_tools.py

from typing import Annotated
from ..data.fake_generator import data_generator

def get_class_grades(
    class_name: Annotated[str, "Tên lớp, ví dụ: 10A, 11B"]
) -> str:
    """Lấy điểm số của tất cả học sinh trong một lớp"""

    try:
        df = data_generator.to_dataframe(class_name)

        result = f"📊 ĐIỂM SỐ LỚP {class_name}\n"
        result += f"Số học sinh: {len(df)}\n\n"

        # Top 5 students
        result += "🏆 TOP 5 HỌC SINH:\n"
        top5 = df.nlargest(5, 'average')[['name', 'average']]
        for _, row in top5.iterrows():
            result += f"  - {row['name']}: {row['average']}\n"

        result += "\n⚠️ 5 HỌC SINH CẦN HỖ TRỢ:\n"
        bottom5 = df.nsmallest(5, 'average')[['name', 'average']]
        for _, row in bottom5.iterrows():
            result += f"  - {row['name']}: {row['average']}\n"

        return result

    except Exception as e:
        return f"❌ Lỗi: {str(e)}"


def get_class_statistics(
    class_name: Annotated[str, "Tên lớp, ví dụ: 10A, 11B"]
) -> str:
    """Lấy thống kê tổng hợp của một lớp (điểm TB, cao nhất, thấp nhất, theo môn)"""

    try:
        stats = data_generator.get_class_statistics(class_name)

        result = f"📈 THỐNG KÊ LỚP {class_name}\n"
        result += f"Điểm trung bình: {stats.average_score}\n"
        result += f"Điểm cao nhất: {stats.highest_score}\n"
        result += f"Điểm thấp nhất: {stats.lowest_score}\n\n"

        result += "📊 ĐIỂM TB THEO MÔN:\n"
        # Sort by average descending
        sorted_subjects = sorted(stats.subject_averages.items(), key=lambda x: x[1], reverse=True)
        for subject, avg in sorted_subjects:
            bar = "█" * int(avg)
            result += f"  {subject:6}: {avg:.1f} {bar}\n"

        return result

    except Exception as e:
        return f"❌ Lỗi: {str(e)}"


def get_students_needing_support(
    class_name: Annotated[str, "Tên lớp"],
    threshold: Annotated[float, "Ngưỡng điểm (mặc định 5.0)"] = 5.0
) -> str:
    """Lấy danh sách học sinh có điểm TB dưới ngưỡng cần hỗ trợ"""

    try:
        students = data_generator.get_students_needing_support(class_name, threshold)

        result = f"⚠️ HỌC SINH CẦN HỖ TRỢ (Điểm TB < {threshold})\n"
        result += f"Tổng số: {len(students)} học sinh\n\n"

        for student, avg, weak_subjects in students:
            result += f"- {student.name} (TB: {avg})\n"
            result += f"  Môn yếu: {', '.join(weak_subjects)}\n"
            result += f"  Email PH: {student.parent_email}\n\n"

        return result

    except Exception as e:
        return f"❌ Lỗi: {str(e)}"


def get_chart_data(
    class_name: Annotated[str, "Tên lớp"]
) -> str:
    """Lấy dữ liệu để vẽ biểu đồ (JSON format)"""

    try:
        stats = data_generator.get_class_statistics(class_name)

        import json
        chart_data = {
            "class_name": class_name,
            "subjects": list(stats.subject_averages.keys()),
            "averages": list(stats.subject_averages.values()),
            "overall_average": stats.average_score
        }

        return json.dumps(chart_data, ensure_ascii=False, indent=2)

    except Exception as e:
        return f"❌ Lỗi: {str(e)}"
```

#### Step 3.2: Code Execution Tools

```python
# src/multi_agent_lab/tools/code_tools.py

import subprocess
import tempfile
import os
from typing import Annotated
from ..config import CHARTS_DIR


def execute_python_code(
    code: Annotated[str, "Code Python cần chạy"]
) -> str:
    """Chạy code Python và trả về kết quả. Hỗ trợ matplotlib cho biểu đồ."""

    # Create temp file
    with tempfile.NamedTemporaryFile(mode='w', suffix='.py', delete=False) as f:
        # Add imports và output directory
        full_code = f"""
import matplotlib
matplotlib.use('Agg')  # Non-interactive backend
import matplotlib.pyplot as plt
import numpy as np
import os

# Set output directory
OUTPUT_DIR = "{CHARTS_DIR}"
os.makedirs(OUTPUT_DIR, exist_ok=True)

# User code
{code}
"""
        f.write(full_code)
        temp_file = f.name

    try:
        # Run the code
        result = subprocess.run(
            ['python3', temp_file],
            capture_output=True,
            text=True,
            timeout=30,
            cwd=CHARTS_DIR
        )

        if result.returncode != 0:
            return f"❌ Lỗi khi chạy code:\n{result.stderr}"

        output = result.stdout.strip() if result.stdout else "Code chạy thành công (không có output)"

        # Check for generated files
        generated_files = []
        for f in os.listdir(CHARTS_DIR):
            if f.endswith(('.png', '.jpg', '.pdf')):
                generated_files.append(f"{CHARTS_DIR}/{f}")

        response = f"✅ CODE EXECUTED SUCCESSFULLY\n\n"
        response += f"📤 Output:\n{output}\n\n"

        if generated_files:
            response += "📁 Files created:\n"
            for f in generated_files[-3:]:  # Last 3 files
                response += f"  - {f}\n"

        return response

    except subprocess.TimeoutExpired:
        return "❌ Lỗi: Code chạy quá lâu (timeout 30s)"

    except Exception as e:
        return f"❌ Lỗi: {str(e)}"

    finally:
        # Cleanup
        os.unlink(temp_file)
```

#### Step 3.3: Report Tools

```python
# src/multi_agent_lab/tools/report_tools.py

import os
from datetime import datetime
from typing import Annotated
from ..config import REPORTS_DIR


def save_report(
    file_name: Annotated[str, "Tên file báo cáo (không cần extension)"],
    content: Annotated[str, "Nội dung báo cáo (Markdown)"]
) -> str:
    """Lưu báo cáo dạng Markdown"""

    try:
        file_path = os.path.join(REPORTS_DIR, f"{file_name}.md")

        # Add header
        full_content = f"""# Báo cáo: {file_name}
**Ngày tạo**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
**Tạo bởi**: Education Multi-Agent System

---

{content}

---
*Báo cáo được tạo tự động bởi AI*
"""

        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(full_content)

        return f"✅ Báo cáo đã lưu tại: {file_path}"

    except Exception as e:
        return f"❌ Lỗi: {str(e)}"


def create_parent_email(
    student_name: Annotated[str, "Tên học sinh"],
    average_score: Annotated[float, "Điểm trung bình"],
    weak_subjects: Annotated[str, "Các môn cần cải thiện (comma separated)"],
    comment: Annotated[str, "Lời nhận xét"]
) -> str:
    """Tạo nội dung email gửi phụ huynh"""

    email = f"""
Kính gửi Quý Phụ huynh em {student_name},

Trường xin gửi thông tin về tình hình học tập của em:

📊 ĐIỂM TRUNG BÌNH: {average_score:.1f}/10

📝 CÁC MÔN CẦN CẢI THIỆN:
{weak_subjects}

💡 NHẬN XÉT CỦA GIÁO VIÊN:
{comment}

Nhà trường mong Quý phụ huynh phối hợp để hỗ trợ em học tập tốt hơn.

Trân trọng,
Ban Giám hiệu
"""

    # Save email template
    safe_name = student_name.replace(" ", "_")
    file_path = os.path.join(REPORTS_DIR, f"email_{safe_name}_{datetime.now().strftime('%Y%m%d')}.txt")

    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(email)

    return f"✅ Email template đã tạo tại: {file_path}\n\n{email}"
```

---

### Phase 4: Agent Definitions

#### Step 4.1: Analyst Agent

```python
# src/multi_agent_lab/agents/analyst_agent.py

from autogen import AssistantAgent
from ..config import AZURE_CONFIG
from ..tools.data_tools import get_class_grades, get_class_statistics, get_students_needing_support, get_chart_data

ANALYST_SYSTEM_MESSAGE = """
Bạn là CHUYÊN GIA PHÂN TÍCH DỮ LIỆU GIÁO DỤC.

NHIỆM VỤ:
1. Phân tích điểm số và dữ liệu học sinh
2. Tìm patterns, trends, và insights
3. Xác định học sinh cần hỗ trợ
4. Đề xuất các visualization cần thiết

QUY TẮC:
- Luôn sử dụng function tools để lấy dữ liệu thực
- Trình bày insights rõ ràng với số liệu cụ thể
- Sau khi phân tích xong, hãy nói "Tôi đã phân tích xong. Coder, hãy viết code vẽ biểu đồ..."
- Sử dụng emoji để dễ đọc

OUTPUT FORMAT:
📊 PHÂN TÍCH: [tên lớp]

📈 INSIGHTS:
- [insight 1 với số liệu]
- [insight 2 với số liệu]

⚠️ CẦN CHÚ Ý:
- [vấn đề 1]
- [vấn đề 2]

Coder, hãy viết code vẽ biểu đồ cột hiển thị điểm TB các môn của lớp...
"""

def create_analyst_agent(llm_config: dict) -> AssistantAgent:
    """Create the Analyst agent with data tools"""

    return AssistantAgent(
        name="Analyst",
        system_message=ANALYST_SYSTEM_MESSAGE,
        llm_config=llm_config,
        function_map={
            "get_class_grades": get_class_grades,
            "get_class_statistics": get_class_statistics,
            "get_students_needing_support": get_students_needing_support,
            "get_chart_data": get_chart_data,
        }
    )
```

#### Step 4.2: Coder Agent

```python
# src/multi_agent_lab/agents/coder_agent.py

from autogen import AssistantAgent

CODER_SYSTEM_MESSAGE = """
Bạn là LẬP TRÌNH VIÊN chuyên viết code Python để tạo biểu đồ.

NHIỆM VỤ:
1. Nhận yêu cầu từ Analyst về biểu đồ cần tạo
2. Viết code Python sử dụng matplotlib
3. Code phải chạy được ngay

QUY TẮC:
- Chỉ sử dụng: matplotlib, numpy (đã có sẵn)
- Code phải có comments tiếng Việt
- Luôn save output vào file: plt.savefig('ten_file.png', dpi=150, bbox_inches='tight')
- Sử dụng tiếng Việt cho labels (matplotlib hỗ trợ Unicode)
- Sau khi viết code, nói "Reviewer, hãy kiểm tra code trên."

CODE TEMPLATE:
```python
import matplotlib.pyplot as plt
import numpy as np

# Dữ liệu
subjects = ['Toán', 'Lý', 'Hóa', 'Văn', 'Anh', 'Sử', 'Địa']
scores = [7.5, 6.8, 5.2, 7.0, 6.5, 6.2, 6.0]

# Vẽ biểu đồ
plt.figure(figsize=(10, 6))
plt.bar(subjects, scores, color='steelblue')
plt.xlabel('Môn học')
plt.ylabel('Điểm trung bình')
plt.title('Điểm TB các môn - Lớp 10A')
plt.ylim(0, 10)

# Thêm giá trị trên cột
for i, v in enumerate(scores):
    plt.text(i, v + 0.1, f'{v}', ha='center')

# Lưu file
plt.savefig('class_10a_chart.png', dpi=150, bbox_inches='tight')
print(f"Biểu đồ đã lưu: class_10a_chart.png")
```

Reviewer, hãy kiểm tra code trên.
"""

def create_coder_agent(llm_config: dict) -> AssistantAgent:
    """Create the Coder agent"""

    return AssistantAgent(
        name="Coder",
        system_message=CODER_SYSTEM_MESSAGE,
        llm_config=llm_config,
    )
```

#### Step 4.3: Reviewer Agent (v1.1)

```python
# src/multi_agent_lab/agents/reviewer_agent.py

import ast
import re
from typing import Tuple, List
from autogen import AssistantAgent

REVIEWER_SYSTEM_MESSAGE = """
Bạn là CODE REVIEWER - chuyên kiểm tra và verify code Python.

NHIỆM VỤ:
1. Nhận code từ Coder (trong markdown block ```python ... ```)
2. Kiểm tra code về: Syntax errors, Security issues, Logic completeness
3. Nếu code OK: Approve và chuyển cho Executor
4. Nếu có lỗi: Yêu cầu Coder sửa

TOOLS CÓ SẴN:
- review_code(code): Kiểm tra syntax và security của code
- check_syntax(code): Chỉ kiểm tra syntax
- check_security(code): Chỉ kiểm tra security

CHECKLIST REVIEW:
1. ✅ Syntax: Code có parse được không?
2. ✅ Imports: Chỉ dùng matplotlib, numpy, pandas?
3. ✅ Security: Không có os.system, subprocess, eval, exec?
4. ✅ Output: Có plt.savefig() hoặc print() không?
5. ✅ Completeness: Code có đầy đủ không (không có placeholder)?

OUTPUT FORMAT (APPROVED):
✅ CODE REVIEW PASSED

📋 Checklist:
- [x] Syntax: OK
- [x] Imports: Safe (matplotlib, numpy)
- [x] Security: No dangerous calls
- [x] Output: Has plt.savefig()

Executor, hãy chạy code này.

OUTPUT FORMAT (REJECTED):
❌ CODE REVIEW FAILED

🔴 Vấn đề phát hiện:
1. [Mô tả vấn đề]

💡 Đề xuất sửa:
- [Cách sửa cụ thể]

Coder, vui lòng sửa các vấn đề trên.
"""

# Dangerous patterns to check
DANGEROUS_PATTERNS = [
    (r'\\bimport\\s+os\\b', 'import os'),
    (r'\\bimport\\s+subprocess\\b', 'import subprocess'),
    (r'\\bos\\.system\\s*\\(', 'os.system()'),
    (r'\\beval\\s*\\(', 'eval()'),
    (r'\\bexec\\s*\\(', 'exec()'),
]


def check_syntax(code: str) -> Tuple[bool, str]:
    """Kiểm tra syntax của code Python"""
    try:
        ast.parse(code)
        return True, "Syntax OK"
    except SyntaxError as e:
        return False, f"Syntax Error at line {e.lineno}: {e.msg}"


def check_security(code: str) -> Tuple[bool, List[str]]:
    """Kiểm tra security issues trong code"""
    issues = []
    for pattern, description in DANGEROUS_PATTERNS:
        if re.search(pattern, code):
            issues.append(f"Dangerous pattern: {description}")
    return len(issues) == 0, issues


def review_code(code: str) -> str:
    """Review code hoàn chỉnh"""
    # Check syntax, imports, security, completeness
    # Return checklist with ✅ or ❌
    ...


def create_reviewer_agent(llm_config: dict) -> AssistantAgent:
    """Create the Reviewer agent with code review tools"""

    reviewer = AssistantAgent(
        name="Reviewer",
        system_message=REVIEWER_SYSTEM_MESSAGE,
        llm_config=llm_config,
    )

    # Register functions
    @reviewer.register_for_llm(description="Review code Python hoàn chỉnh")
    def tool_review_code(code: str) -> str:
        return review_code(code)

    @reviewer.register_for_llm(description="Kiểm tra syntax của code Python")
    def tool_check_syntax(code: str) -> str:
        is_valid, msg = check_syntax(code)
        return f"{'✅' if is_valid else '❌'} {msg}"

    return reviewer
```

---

#### Step 4.4: Executor Agent

```python
# src/multi_agent_lab/agents/executor_agent.py

from autogen import AssistantAgent
from ..tools.code_tools import execute_python_code

EXECUTOR_SYSTEM_MESSAGE = """
Bạn là EXECUTOR - chuyên chạy code Python và báo cáo kết quả.

NHIỆM VỤ:
1. Nhận code từ Coder (trong markdown code block)
2. Extract code và chạy bằng function execute_python_code
3. Báo cáo kết quả (thành công/lỗi)
4. Nếu lỗi, yêu cầu Coder sửa
5. Nếu thành công, nói "Writer, hãy viết báo cáo dựa trên phân tích của Analyst và biểu đồ tôi vừa tạo."

QUY TẮC:
- Chỉ extract code trong ```python ... ``` block
- Báo cáo đầy đủ output và file đã tạo
- Nếu có lỗi, giải thích rõ lỗi gì

OUTPUT FORMAT (thành công):
✅ CODE EXECUTED SUCCESSFULLY

📁 Files created:
- [đường dẫn file]

📤 Output:
[output từ code]

Writer, hãy viết báo cáo dựa trên phân tích của Analyst và biểu đồ tôi vừa tạo.

OUTPUT FORMAT (lỗi):
❌ CODE EXECUTION FAILED

Error: [mô tả lỗi]

Coder, vui lòng sửa lỗi sau: [chi tiết lỗi]
"""

def create_executor_agent(llm_config: dict) -> AssistantAgent:
    """Create the Executor agent with code execution tools"""

    return AssistantAgent(
        name="Executor",
        system_message=EXECUTOR_SYSTEM_MESSAGE,
        llm_config=llm_config,
        function_map={
            "execute_python_code": execute_python_code,
        }
    )
```

#### Step 4.5: Writer Agent

```python
# src/multi_agent_lab/agents/writer_agent.py

from autogen import AssistantAgent
from ..tools.report_tools import save_report, create_parent_email

WRITER_SYSTEM_MESSAGE = """
Bạn là CHUYÊN GIA VIẾT BÁO CÁO cho phụ huynh học sinh.

NHIỆM VỤ:
1. Tổng hợp thông tin từ Analyst
2. Kết hợp với biểu đồ từ Executor
3. Viết báo cáo thân thiện, dễ hiểu cho phụ huynh
4. Lưu báo cáo sử dụng function save_report

QUY TẮC:
- Ngôn ngữ thân thiện, không quá chuyên môn
- Tập trung vào điểm tích cực trước
- Đưa ra lời khuyên cụ thể, khả thi
- Sử dụng emoji phù hợp
- KẾT THÚC bằng "TASK_COMPLETE" khi hoàn thành

OUTPUT FORMAT:
📋 BÁO CÁO KẾT QUẢ HỌC TẬP

Kính gửi Quý Phụ huynh lớp [X],

📊 TỔNG QUAN:
[Tóm tắt ngắn gọn]

✨ ĐIỂM TÍCH CỰC:
- [điểm 1]
- [điểm 2]

📈 CẦN CẢI THIỆN:
- [môn/kỹ năng cần cải thiện]

💡 ĐỀ XUẤT CHO PHỤ HUYNH:
- [đề xuất cụ thể]

📎 ĐÍNH KÈM:
- Biểu đồ: [tên file]

Trân trọng,
Hệ thống Phân tích Học tập AI

TASK_COMPLETE
"""

def create_writer_agent(llm_config: dict) -> AssistantAgent:
    """Create the Writer agent with report tools"""

    return AssistantAgent(
        name="Writer",
        system_message=WRITER_SYSTEM_MESSAGE,
        llm_config=llm_config,
        function_map={
            "save_report": save_report,
            "create_parent_email": create_parent_email,
        }
    )
```

---

### Phase 5: Orchestration

#### Step 5.1: Group Chat Manager

```python
# src/multi_agent_lab/orchestration/group_chat.py

from autogen import GroupChat, GroupChatManager, UserProxyAgent
from typing import List
from ..config import AZURE_CONFIG, AGENT_SETTINGS
from ..agents.analyst_agent import create_analyst_agent
from ..agents.coder_agent import create_coder_agent
from ..agents.executor_agent import create_executor_agent
from ..agents.writer_agent import create_writer_agent


def create_llm_config() -> dict:
    """Create LLM configuration for Azure OpenAI"""
    return {
        "config_list": [{
            "model": AZURE_CONFIG["model"],
            "api_key": AZURE_CONFIG["api_key"],
            "base_url": AZURE_CONFIG["base_url"],
            "api_type": AZURE_CONFIG["api_type"],
            "api_version": AZURE_CONFIG["api_version"],
        }],
        "temperature": 0.7,
    }


def is_termination_message(msg: dict) -> bool:
    """Check if message contains termination keyword"""
    content = msg.get("content", "")
    if content is None:
        return False
    return AGENT_SETTINGS["termination_keyword"] in content


class EducationMultiAgentChat:
    def __init__(self):
        self.llm_config = create_llm_config()

        # Create agents
        self.analyst = create_analyst_agent(self.llm_config)
        self.coder = create_coder_agent(self.llm_config)
        self.executor = create_executor_agent(self.llm_config)
        self.writer = create_writer_agent(self.llm_config)

        # Create user proxy (to initiate conversation)
        self.user_proxy = UserProxyAgent(
            name="User",
            human_input_mode="NEVER",  # Fully automated
            max_consecutive_auto_reply=0,
            code_execution_config=False,
            is_termination_msg=is_termination_message,
        )

        # Create group chat
        self.agents = [self.analyst, self.coder, self.executor, self.writer]

        self.group_chat = GroupChat(
            agents=[self.user_proxy] + self.agents,
            messages=[],
            max_round=AGENT_SETTINGS["max_rounds"],
            speaker_selection_method="round_robin",  # or "auto"
        )

        # Create manager
        self.manager = GroupChatManager(
            groupchat=self.group_chat,
            llm_config=self.llm_config,
            is_termination_msg=is_termination_message,
        )

    def run(self, user_message: str) -> List[dict]:
        """Run the multi-agent conversation"""

        print("\n" + "="*60)
        print("🚀 BẮT ĐẦU MULTI-AGENT CONVERSATION")
        print("="*60)
        print(f"\n📝 User: {user_message}\n")
        print("-"*60)

        # Initiate chat
        self.user_proxy.initiate_chat(
            self.manager,
            message=user_message,
        )

        # Return conversation history
        return self.group_chat.messages

    def get_conversation_summary(self) -> str:
        """Get a summary of the conversation"""
        messages = self.group_chat.messages

        summary = f"\n📊 CONVERSATION SUMMARY\n"
        summary += f"Total messages: {len(messages)}\n"

        # Count by agent
        agent_counts = {}
        for msg in messages:
            name = msg.get("name", "Unknown")
            agent_counts[name] = agent_counts.get(name, 0) + 1

        summary += f"Messages by agent:\n"
        for name, count in agent_counts.items():
            summary += f"  - {name}: {count}\n"

        return summary


# Convenience function
def run_education_analysis(user_message: str) -> List[dict]:
    """Run education analysis with multi-agent system"""
    chat = EducationMultiAgentChat()
    return chat.run(user_message)
```

---

### Phase 6: Main Entry Point

```python
# src/multi_agent_lab/main.py

import sys
from .orchestration.group_chat import EducationMultiAgentChat


def main():
    """Main entry point"""

    print("╔══════════════════════════════════════════════════════════════╗")
    print("║       EDUCATION MULTI-AGENT LAB                              ║")
    print("║       Powered by AutoGen + Azure OpenAI                      ║")
    print("╠══════════════════════════════════════════════════════════════╣")
    print("║  Commands:                                                   ║")
    print("║  - Type your request and press Enter                         ║")
    print("║  - Type 'exit' to quit                                       ║")
    print("║  - Type 'demo' for demo request                              ║")
    print("╚══════════════════════════════════════════════════════════════╝")
    print()

    # Create chat instance
    chat = EducationMultiAgentChat()

    while True:
        try:
            user_input = input("📝 Your request: ").strip()

            if not user_input:
                continue

            if user_input.lower() == 'exit':
                print("👋 Goodbye!")
                break

            if user_input.lower() == 'demo':
                user_input = "Phân tích dữ liệu học sinh lớp 10A, tạo biểu đồ điểm theo môn, và viết báo cáo gửi phụ huynh"

            # Run conversation
            messages = chat.run(user_input)

            # Print summary
            print(chat.get_conversation_summary())

        except KeyboardInterrupt:
            print("\n👋 Goodbye!")
            break

        except Exception as e:
            print(f"\n❌ Error: {str(e)}")
            continue


if __name__ == "__main__":
    main()
```

---

## 5. Fake Data Sample

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         FAKE DATA OVERVIEW                               │
│                                                                          │
│  Classes: 10A, 10B, 11A                                                 │
│  Students per class: 30                                                 │
│  Total students: 90                                                     │
│  Subjects: Toán, Lý, Hóa, Văn, Anh, Sử, Địa                            │
│  Grades per student: 7                                                  │
│  Total grades: 630                                                      │
│                                                                          │
│  Sample DataFrame (Lớp 10A):                                            │
│  ┌──────────┬────────────────┬──────┬──────┬──────┬──────┬─────────┐   │
│  │ ID       │ Tên            │ Toán │ Lý   │ Hóa  │ ...  │ Average │   │
│  ├──────────┼────────────────┼──────┼──────┼──────┼──────┼─────────┤   │
│  │ HS1234   │ Nguyễn Văn An  │ 8.5  │ 7.0  │ 6.5  │ ...  │ 7.2     │   │
│  │ HS1235   │ Trần Thị Bình  │ 9.0  │ 8.5  │ 8.0  │ ...  │ 8.5     │   │
│  │ HS1236   │ Lê Hoàng Cường │ 4.5  │ 5.0  │ 3.5  │ ...  │ 4.3     │   │
│  └──────────┴────────────────┴──────┴──────┴──────┴──────┴─────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Agent Summary

| Agent | Role | Functions | Next Speaker |
|-------|------|-----------|--------------|
| **Analyst** | Phân tích dữ liệu | get_class_grades, get_class_statistics, get_students_needing_support | → Coder |
| **Coder** | Viết code visualization | None (generates code) | → Reviewer |
| **Reviewer** | Kiểm tra code trước khi chạy | review_code, check_syntax, check_security | → Executor (OK) hoặc → Coder (FAIL) |
| **Executor** | Chạy code Python | execute_python_code | → Writer (hoặc Coder nếu lỗi) |
| **Writer** | Viết báo cáo | save_report, create_parent_email | TASK_COMPLETE |

---

## 7. Testing & Demo

### Jupyter Notebook Demo

```python
# notebooks/lab_demo.ipynb

# Cell 1: Setup
%load_ext autoreload
%autoreload 2

import sys
sys.path.insert(0, '../src')

from multi_agent_lab.orchestration.group_chat import EducationMultiAgentChat

# Cell 2: Create chat
chat = EducationMultiAgentChat()

# Cell 3: Run demo
messages = chat.run("Phân tích dữ liệu học sinh lớp 10A và tạo báo cáo cho phụ huynh")

# Cell 4: View results
for msg in messages:
    print(f"\n{'='*60}")
    print(f"🤖 {msg['name']}:")
    print(msg['content'])
```

### Quick Start Commands

```bash
# 1. Install dependencies
pip install -r requirements.txt

# 2. Setup environment
cp .env.example .env
# Edit .env with your Azure credentials

# 3. Run the lab
cd src
python -m multi_agent_lab.main

# 4. Or run demo
# > demo
```

---

## 8. Expected Output Flow

```
📝 User: Phân tích dữ liệu lớp 10A và tạo báo cáo cho phụ huynh

🔄 Round 1 - Analyst:
📊 PHÂN TÍCH: Lớp 10A

📈 INSIGHTS:
- Điểm TB lớp: 6.8
- Môn mạnh nhất: Toán (TB 7.5)
- Môn yếu nhất: Hóa (TB 5.2)

⚠️ CẦN CHÚ Ý:
- 3 học sinh có điểm TB < 5.0

Coder, hãy viết code vẽ biểu đồ cột so sánh điểm TB các môn.

─────────────────────────────────────────────────────────────

🔄 Round 2 - Coder:
💻 CODE PYTHON:

```python
import matplotlib.pyplot as plt
...
plt.savefig('class_10a_subjects.png')
```

Reviewer, hãy kiểm tra code trên.

─────────────────────────────────────────────────────────────

🔄 Round 3 - Reviewer:
✅ CODE REVIEW PASSED

📋 Checklist:
- [x] Syntax: OK
- [x] Imports: Safe (matplotlib, numpy)
- [x] Security: No dangerous calls
- [x] Output: Has plt.savefig()
- [x] Completeness: Full code

Executor, hãy chạy code này.

─────────────────────────────────────────────────────────────

🔄 Round 4 - Executor:
✅ CODE EXECUTED SUCCESSFULLY

📁 Files created:
- output/charts/class_10a_subjects.png

Writer, hãy viết báo cáo dựa trên phân tích của Analyst.

─────────────────────────────────────────────────────────────

🔄 Round 5 - Writer:
📋 BÁO CÁO KẾT QUẢ HỌC TẬP

Kính gửi Quý Phụ huynh lớp 10A,

📊 TỔNG QUAN:
Điểm trung bình lớp đạt 6.8/10...

✨ ĐIỂM TÍCH CỰC:
- Môn Toán có kết quả tốt nhất...

📈 CẦN CẢI THIỆN:
- Môn Hóa cần được chú trọng hơn...

💡 ĐỀ XUẤT:
- Tăng cường ôn tập môn Hóa...

📎 ĐÍNH KÈM:
- Biểu đồ: class_10a_subjects.png

TASK_COMPLETE

─────────────────────────────────────────────────────────────

✅ CONVERSATION COMPLETED!
```

---

**End of Plan**
