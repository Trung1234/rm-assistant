# Giải thích chi tiết PLAN-v1: Multi-Agent Lab

**Version**: 1.0
**Created**: 2026-01-01
**Purpose**: Giải thích chi tiết kiến trúc và implementation của PLAN-v1-multi-agent-lab.md
**Related Files**:
- [PLAN-v1-multi-agent-lab.md](../plans/PLAN-v1-multi-agent-lab.md)
- [PLAN-v2-data-organization.md](../plans/PLAN-v2-data-organization.md)
- [v1-microsoft-agent-framework-research.md](../research/v1-microsoft-agent-framework-research.md)

---

## User Stories & Use Cases

### Actors (Các vai trò sử dụng hệ thống)

| Actor | Mô tả | Mục tiêu chính |
|-------|-------|----------------|
| **Giáo viên chủ nhiệm** | Quản lý 1 hoặc nhiều lớp, cần theo dõi kết quả học tập | Nắm bắt tình hình lớp, hỗ trợ học sinh yếu |
| **Giáo viên bộ môn** | Dạy 1 môn học cho nhiều lớp | Phân tích kết quả theo môn, so sánh giữa các lớp |
| **Ban Giám Hiệu** | Quản lý toàn trường | Tổng quan kết quả, báo cáo chiến lược |
| **Phụ huynh** | Nhận thông tin về con em | Hiểu rõ tình hình học tập của con |

---

### User Stories

#### US-01: Phân tích kết quả học tập của lớp

```
AS A      Giáo viên chủ nhiệm
I WANT    Phân tích chi tiết kết quả học tập của lớp tôi quản lý
SO THAT   Tôi có thể nắm bắt tình hình học tập và đưa ra biện pháp hỗ trợ phù hợp

ACCEPTANCE CRITERIA:
- Hiển thị điểm trung bình toàn lớp
- Hiển thị điểm trung bình theo từng môn học
- Thống kê tỷ lệ học sinh đạt/không đạt
- Xác định điểm cao nhất và thấp nhất
```

**Flow trong hệ thống:**
```
User Input: "Phân tích kết quả học tập lớp 10A"
    │
    ▼
┌─────────┐     ┌─────────┐     ┌─────────┐     ┌─────────┐
│ Analyst │ ──► │  Coder  │ ──► │Executor │ ──► │ Writer  │
│         │     │         │     │         │     │         │
│ Gọi     │     │ Viết    │     │ Chạy    │     │ Tạo     │
│ tools   │     │ code    │     │ code    │     │ báo cáo │
│ lấy     │     │ biểu đồ │     │ xuất    │     │ tổng    │
│ thống kê│     │         │     │ chart   │     │ hợp     │
└─────────┘     └─────────┘     └─────────┘     └─────────┘
    │
    ▼
Output:
- Phân tích chi tiết (text)
- Biểu đồ điểm số (PNG)
- Báo cáo tổng hợp (MD)
```

---

#### US-02: So sánh kết quả giữa các lớp

```
AS A      Ban Giám Hiệu / Giáo viên bộ môn
I WANT    So sánh kết quả học tập giữa các lớp
SO THAT   Tôi có thể đánh giá hiệu quả giảng dạy và phát hiện lớp cần hỗ trợ

ACCEPTANCE CRITERIA:
- So sánh điểm trung bình giữa các lớp
- So sánh theo từng môn học
- Trực quan hóa bằng biểu đồ so sánh
- Highlight lớp có điểm cao nhất/thấp nhất
```

**Flow trong hệ thống:**
```
User Input: "So sánh kết quả học tập giữa lớp 10A, 10B và 11A"
    │
    ▼
┌─────────┐
│ Analyst │ ──► Gọi tool_get_class_statistics cho mỗi lớp
│         │     ──► Phân tích điểm mạnh/yếu của từng lớp
│         │     ──► Đề xuất biểu đồ so sánh cho Coder
└─────────┘
    │
    ▼
┌─────────┐
│  Coder  │ ──► Viết code matplotlib tạo biểu đồ cột nhóm
│         │     ──► 3 màu cho 3 lớp, 7 cột cho 7 môn
└─────────┘
    │
    ▼
┌─────────┐
│Executor │ ──► Chạy code, xuất file PNG
└─────────┘
    │
    ▼
┌─────────┐
│ Writer  │ ──► Tổng hợp báo cáo so sánh
│         │     ──► Khuyến nghị cho lớp cần cải thiện
└─────────┘
    │
    ▼
Output: Biểu đồ so sánh + Báo cáo phân tích
```

---

#### US-03: Xác định học sinh cần hỗ trợ

```
AS A      Giáo viên chủ nhiệm / Giáo viên bộ môn
I WANT    Xác định danh sách học sinh có điểm thấp cần hỗ trợ
SO THAT   Tôi có thể đưa ra biện pháp phụ đạo kịp thời

ACCEPTANCE CRITERIA:
- Liệt kê học sinh có điểm dưới ngưỡng (mặc định 5.0)
- Hiển thị các môn yếu của từng học sinh
- Sắp xếp theo mức độ cần hỗ trợ (điểm thấp nhất trước)
- Có thể lọc theo môn học cụ thể
```

**Flow trong hệ thống:**
```
User Input: "Tìm học sinh yếu môn Toán trong lớp 10A"
    │
    ▼
┌─────────┐
│ Analyst │ ──► Gọi tool_get_students_needing_support
│         │     với tham số: class_name="10A", subject="Toán"
│         │     ──► Phân tích nguyên nhân tiềm năng
└─────────┘
    │
    ▼
Output: Danh sách học sinh + điểm số + phân tích
```

---

#### US-04: Tạo báo cáo gửi phụ huynh

```
AS A      Giáo viên chủ nhiệm
I WANT    Tạo báo cáo chi tiết về kết quả học tập gửi phụ huynh
SO THAT   Phụ huynh nắm được tình hình học tập của con em

ACCEPTANCE CRITERIA:
- Báo cáo có định dạng chuyên nghiệp
- Bao gồm điểm số các môn
- Đánh giá ưu điểm và cần cải thiện
- Đề xuất hướng hỗ trợ tại nhà
- Có thể gửi qua email
```

**Flow trong hệ thống:**
```
User Input: "Tạo báo cáo gửi phụ huynh cho học sinh Nguyễn Văn A lớp 10A"
    │
    ▼
┌─────────┐
│ Analyst │ ──► Lấy thông tin học sinh
│         │     ──► Phân tích điểm số chi tiết
│         │     ──► Đề xuất nội dung báo cáo
└─────────┘
    │
    ▼
┌─────────┐
│  Coder  │ ──► Viết code tạo biểu đồ radar điểm các môn
└─────────┘
    │
    ▼
┌─────────┐
│Executor │ ──► Xuất biểu đồ cá nhân
└─────────┘
    │
    ▼
┌─────────┐
│ Writer  │ ──► Gọi tool_save_report: Lưu báo cáo MD
│         │     ──► Gọi tool_create_parent_email: Tạo nội dung email
└─────────┘
    │
    ▼
Output: Báo cáo (MD) + Template email + Biểu đồ cá nhân
```

---

#### US-05: Tạo biểu đồ trực quan

```
AS A      Giáo viên / Ban Giám Hiệu
I WANT    Tạo biểu đồ trực quan về kết quả học tập
SO THAT   Tôi có thể trình bày trong cuộc họp hoặc báo cáo

ACCEPTANCE CRITERIA:
- Hỗ trợ nhiều loại biểu đồ (cột, đường, tròn, radar)
- Tùy chỉnh tiêu đề, nhãn, màu sắc
- Xuất file PNG chất lượng cao
- Hỗ trợ tiếng Việt
```

**Flow trong hệ thống:**
```
User Input: "Vẽ biểu đồ so sánh điểm các môn của lớp 10A dạng cột"
    │
    ▼
┌─────────┐
│ Analyst │ ──► Lấy dữ liệu điểm theo môn
│         │     ──► Chỉ định loại biểu đồ cho Coder
└─────────┘
    │
    ▼
┌─────────┐
│  Coder  │ ──► Viết code matplotlib với bar chart
│         │     ──► Thiết lập font tiếng Việt
│         │     ──► Thêm data labels, legend, grid
└─────────┘
    │
    ▼
┌─────────┐
│Executor │ ──► Chạy code xuất PNG
│         │     ──► Kiểm tra file đã tạo
└─────────┘
    │
    ▼
Output: File biểu đồ PNG trong thư mục charts/
```

---

### Use Cases Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    EDUCATION MULTI-AGENT SYSTEM                              │
│                         USE CASES DIAGRAM                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                          ┌─────────────────────┐                            │
│                          │   <<System>>         │                            │
│                          │   Multi-Agent Chat   │                            │
│                          └─────────────────────┘                            │
│                                    │                                         │
│         ┌──────────────────────────┼──────────────────────────┐             │
│         │                          │                          │             │
│         ▼                          ▼                          ▼             │
│  ┌─────────────┐          ┌─────────────┐          ┌─────────────┐         │
│  │  UC-01      │          │  UC-02      │          │  UC-03      │         │
│  │ Phân tích   │          │ So sánh     │          │ Tìm HS      │         │
│  │ lớp học    │          │ các lớp     │          │ cần hỗ trợ  │         │
│  └─────────────┘          └─────────────┘          └─────────────┘         │
│         │                          │                          │             │
│         │                          ▼                          │             │
│         │                 ┌─────────────┐                     │             │
│         │                 │  UC-04      │                     │             │
│         ├────────────────►│ Tạo báo cáo │◄────────────────────┤             │
│         │                 │ phụ huynh   │                     │             │
│         │                 └─────────────┘                     │             │
│         │                          │                          │             │
│         │                          ▼                          │             │
│         │                 ┌─────────────┐                     │             │
│         └────────────────►│  UC-05      │◄────────────────────┘             │
│                           │ Tạo biểu đồ │                                   │
│                           │ trực quan   │                                   │
│                           └─────────────┘                                   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  ACTORS                                                              │   │
│  │                                                                      │   │
│  │  👨‍🏫 Giáo viên chủ nhiệm ────────► UC-01, UC-03, UC-04               │   │
│  │  👩‍🏫 Giáo viên bộ môn ──────────► UC-01, UC-02, UC-03               │   │
│  │  🏫 Ban Giám Hiệu ─────────────► UC-02, UC-05                        │   │
│  │  👪 Phụ huynh ─────────────────► (Nhận output từ UC-04)              │   │
│  │                                                                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### Use Case Specifications

#### UC-01: Phân tích kết quả học tập lớp

| Attribute | Description |
|-----------|-------------|
| **Use Case ID** | UC-01 |
| **Use Case Name** | Phân tích kết quả học tập lớp |
| **Actor** | Giáo viên chủ nhiệm, Giáo viên bộ môn |
| **Description** | Phân tích chi tiết kết quả học tập của một lớp cụ thể |
| **Pre-conditions** | Dữ liệu điểm số của lớp đã có trong hệ thống |
| **Post-conditions** | Xuất được báo cáo phân tích và biểu đồ |
| **Trigger** | User gửi yêu cầu phân tích lớp qua chat interface |

**Main Flow:**
1. User gửi yêu cầu: "Phân tích lớp [tên lớp]"
2. **Analyst** gọi `tool_get_class_statistics` để lấy thống kê
3. **Analyst** phân tích và đề xuất biểu đồ cho **Coder**
4. **Coder** viết code Python tạo biểu đồ
5. **Executor** chạy code, xuất file PNG
6. **Writer** tổng hợp báo cáo cuối cùng
7. Hệ thống trả về kết quả cho User

**Alternative Flow:**
- 3a. Nếu lớp không tồn tại → Analyst thông báo lỗi
- 5a. Nếu code lỗi → Executor báo lỗi, flow quay lại Coder để sửa

---

#### UC-02: So sánh kết quả giữa các lớp

| Attribute | Description |
|-----------|-------------|
| **Use Case ID** | UC-02 |
| **Use Case Name** | So sánh kết quả giữa các lớp |
| **Actor** | Ban Giám Hiệu, Giáo viên bộ môn |
| **Description** | So sánh điểm số và hiệu suất học tập giữa nhiều lớp |
| **Pre-conditions** | Có ít nhất 2 lớp với dữ liệu điểm số |
| **Post-conditions** | Xuất được biểu đồ so sánh và báo cáo đối chiếu |
| **Trigger** | User gửi yêu cầu so sánh các lớp |

**Main Flow:**
1. User gửi yêu cầu: "So sánh lớp [A], [B], [C]"
2. **Analyst** gọi `tool_get_class_statistics` cho mỗi lớp
3. **Analyst** phân tích so sánh, xác định điểm mạnh/yếu
4. **Coder** viết code biểu đồ cột nhóm (grouped bar chart)
5. **Executor** chạy code, xuất PNG
6. **Writer** tổng hợp báo cáo so sánh với khuyến nghị
7. Trả kết quả cho User

---

#### UC-03: Tìm học sinh cần hỗ trợ

| Attribute | Description |
|-----------|-------------|
| **Use Case ID** | UC-03 |
| **Use Case Name** | Tìm học sinh cần hỗ trợ |
| **Actor** | Giáo viên chủ nhiệm, Giáo viên bộ môn |
| **Description** | Xác định học sinh có điểm thấp cần được phụ đạo |
| **Pre-conditions** | Dữ liệu điểm số học sinh đã có |
| **Post-conditions** | Danh sách học sinh cần hỗ trợ kèm phân tích |
| **Trigger** | User yêu cầu tìm học sinh yếu |

**Main Flow:**
1. User gửi yêu cầu: "Tìm học sinh yếu [môn] lớp [tên lớp]"
2. **Analyst** gọi `tool_get_students_needing_support`
3. **Analyst** phân tích nguyên nhân và đề xuất biện pháp
4. **Writer** tạo báo cáo danh sách với khuyến nghị
5. Trả kết quả cho User

---

#### UC-04: Tạo báo cáo gửi phụ huynh

| Attribute | Description |
|-----------|-------------|
| **Use Case ID** | UC-04 |
| **Use Case Name** | Tạo báo cáo gửi phụ huynh |
| **Actor** | Giáo viên chủ nhiệm |
| **Description** | Tạo báo cáo chi tiết về học sinh để gửi phụ huynh |
| **Pre-conditions** | Thông tin học sinh và điểm số có trong hệ thống |
| **Post-conditions** | File báo cáo MD + Template email được tạo |
| **Trigger** | User yêu cầu tạo báo cáo cho học sinh cụ thể |

**Main Flow:**
1. User yêu cầu: "Tạo báo cáo phụ huynh cho [học sinh] lớp [lớp]"
2. **Analyst** lấy thông tin học sinh và phân tích chi tiết
3. **Coder** viết code biểu đồ radar cá nhân (nếu cần)
4. **Executor** chạy code xuất biểu đồ
5. **Writer** gọi `tool_save_report` lưu báo cáo
6. **Writer** gọi `tool_create_parent_email` tạo template email
7. Trả kết quả cho User

---

#### UC-05: Tạo biểu đồ trực quan

| Attribute | Description |
|-----------|-------------|
| **Use Case ID** | UC-05 |
| **Use Case Name** | Tạo biểu đồ trực quan |
| **Actor** | Tất cả |
| **Description** | Tạo biểu đồ từ dữ liệu để trình bày hoặc báo cáo |
| **Pre-conditions** | Dữ liệu cần trực quan hóa đã có |
| **Post-conditions** | File PNG biểu đồ được tạo |
| **Trigger** | User yêu cầu vẽ biểu đồ cụ thể |

**Main Flow:**
1. User yêu cầu: "Vẽ biểu đồ [loại] cho [dữ liệu]"
2. **Analyst** chuẩn bị dữ liệu, chỉ định loại biểu đồ
3. **Coder** viết code matplotlib với specs từ Analyst
4. **Executor** chạy code, xuất file PNG
5. Trả URL file biểu đồ cho User

---

### Agents trong từng Use Case

| Use Case | Analyst | Coder | Executor | Writer |
|----------|:-------:|:-----:|:--------:|:------:|
| UC-01 Phân tích lớp | ✅ | ✅ | ✅ | ✅ |
| UC-02 So sánh lớp | ✅ | ✅ | ✅ | ✅ |
| UC-03 Tìm HS yếu | ✅ | ❌ | ❌ | ✅ |
| UC-04 Báo cáo PH | ✅ | ✅ | ✅ | ✅ |
| UC-05 Tạo biểu đồ | ✅ | ✅ | ✅ | ❌ |

---

### Tools sử dụng trong từng Use Case

| Use Case | get_class_list | get_class_statistics | get_students_needing_support | execute_python_code | save_report | create_parent_email |
|----------|:--------------:|:--------------------:|:----------------------------:|:-------------------:|:-----------:|:-------------------:|
| UC-01 | ❌ | ✅ | ❌ | ✅ | ✅ | ❌ |
| UC-02 | ✅ | ✅ | ❌ | ✅ | ✅ | ❌ |
| UC-03 | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ |
| UC-04 | ❌ | ✅ | ❌ | ✅ | ✅ | ✅ |
| UC-05 | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |

---

## Mục Lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Pattern đang sử dụng: Sequential Round-Robin](#2-pattern-đang-sử-dụng-sequential-round-robin)
3. [Component Architecture](#3-component-architecture)
4. [Data Flow giữa các Agents](#4-data-flow-giữa-các-agents)
5. [Speaker Selection Methods](#5-speaker-selection-methods)
6. [Message Flow trong GroupChat](#6-message-flow-trong-groupchat)
7. [So sánh với các Pattern khác](#7-so-sánh-với-các-pattern-khác)
8. [Tóm tắt](#8-tóm-tắt)

---

## 1. Tổng quan kiến trúc

### PLAN-v1 đang implement cái gì?

PLAN-v1 implement kiến trúc **AutoGen GroupChat** - một pattern **Sequential/Round-Robin** từ Microsoft AutoGen framework.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    KIẾN TRÚC PLAN-v1 ĐANG IMPLEMENT                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Framework:     AutoGen (Python - pyautogen)                                │
│  Pattern:       GroupChat với Round-Robin Speaker Selection                 │
│  Agents:        4 (Analyst → Coder → Executor → Writer)                     │
│  Termination:   Keyword-based ("TASK_COMPLETE")                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### So sánh với MAF Research

| Aspect | MAF Research (lý thuyết) | PLAN-v1 (thực hành) |
|--------|--------------------------|---------------------|
| **Framework** | Semantic Kernel + AutoGen | AutoGen only (Python) |
| **Pattern** | Sequential, Concurrent, Hand-off, Magentic-One | **Sequential/Round-Robin** |
| **Orchestrator** | GroupChatManager | GroupChatManager |
| **Language** | C# hoặc Python | Python (pyautogen) |

### Công thức MAF

Theo research document:

```
Microsoft Agent Framework = Semantic Kernel + AutoGen

• Semantic Kernel: "The Skill Builder" - tạo single agent với plugins, memory
• AutoGen: "The Collaborator" - điều phối multi-agent conversation
```

PLAN-v1 chỉ sử dụng **AutoGen** (phần multi-agent) mà chưa tích hợp Semantic Kernel.

---

## 2. Pattern đang sử dụng: Sequential Round-Robin

### Từ MAF Research - 4 Orchestration Patterns

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      AUTOGEN ORCHESTRATION PATTERNS                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   PATTERN 1: SEQUENTIAL  ← PLAN-v1 DÙNG CÁI NÀY                             │
│   ─────────────────────                                                      │
│                                                                              │
│       Agent A ──────► Agent B ──────► Agent C ──────► Output                │
│         │               │               │                                    │
│       [Step 1]       [Step 2]       [Step 3]                                │
│                                                                              │
│   ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│   PATTERN 2: CONCURRENT                                                      │
│   ─────────────────────                                                      │
│                                                                              │
│                    ┌──► Agent A ──┐                                         │
│                    │              │                                         │
│       Input ───────┼──► Agent B ──┼───► Aggregator ───► Output             │
│                    │              │                                         │
│                    └──► Agent C ──┘                                         │
│                                                                              │
│   ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│   PATTERN 3: HAND-OFF                                                        │
│   ─────────────────────                                                      │
│                                                                              │
│       Agent A ────┬────► Agent B                                            │
│          │        │         │                                               │
│          │   [Condition]    │                                               │
│          │        │         │                                               │
│          └────────┴─────────┼────► Agent C                                  │
│                             │                                               │
│                        [Condition]                                          │
│                                                                              │
│   ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│   PATTERN 4: MAGENTIC-ONE (Generalist Team)                                 │
│   ────────────────────────────────────────                                  │
│                                                                              │
│                    ┌─────────────────┐                                      │
│                    │   Orchestrator   │                                      │
│                    │   (Smart Router) │                                      │
│                    └────────┬────────┘                                      │
│                             │                                               │
│         ┌───────────────────┼───────────────────┐                          │
│         ▼                   ▼                   ▼                          │
│     WebSurfer           FileSurfer          Coder                          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### PLAN-v1 Implementation: Sequential

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PLAN-v1: SEQUENTIAL ROUND-ROBIN                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐            │
│    │ ANALYST  │───►│  CODER   │───►│ EXECUTOR │───►│  WRITER  │            │
│    │          │    │          │    │          │    │          │            │
│    │ Round 1  │    │ Round 2  │    │ Round 3  │    │ Round 4  │            │
│    └──────────┘    └──────────┘    └──────────┘    └──────────┘            │
│         │               │               │               │                   │
│         ▼               ▼               ▼               ▼                   │
│    Phân tích       Viết code       Chạy code      Viết báo cáo             │
│    dữ liệu         Python          matplotlib     + TASK_COMPLETE          │
│                                                                              │
│  Nếu chưa TASK_COMPLETE, quay lại Round 1 (Analyst)                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Component Architecture

### Kiến trúc 4 tầng

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PLAN-v1 COMPONENT ARCHITECTURE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                         ORCHESTRATION LAYER                            │  │
│  │                                                                        │  │
│  │   ┌─────────────────────────────────────────────────────────────┐     │  │
│  │   │                    GroupChatManager                          │     │  │
│  │   │                                                              │     │  │
│  │   │   • Quản lý conversation flow                               │     │  │
│  │   │   • Chọn speaker tiếp theo (round_robin)                    │     │  │
│  │   │   • Kiểm tra termination (TASK_COMPLETE)                    │     │  │
│  │   │   • Max rounds: 10                                          │     │  │
│  │   │                                                              │     │  │
│  │   └─────────────────────────────────────────────────────────────┘     │  │
│  │                                │                                       │  │
│  │                                ▼                                       │  │
│  │   ┌─────────────────────────────────────────────────────────────┐     │  │
│  │   │                    GroupChat                                 │     │  │
│  │   │                                                              │     │  │
│  │   │   agents: [UserProxy, Analyst, Coder, Executor, Writer]     │     │  │
│  │   │   messages: []  ← SHARED CONTEXT                            │     │  │
│  │   │   speaker_selection_method: "round_robin"                   │     │  │
│  │   │                                                              │     │  │
│  │   └─────────────────────────────────────────────────────────────┘     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                │                                             │
│                                ▼                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                          AGENTS LAYER                                  │  │
│  │                                                                        │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │  │
│  │  │   ANALYST   │  │    CODER    │  │  EXECUTOR   │  │   WRITER    │   │  │
│  │  │             │  │             │  │             │  │             │   │  │
│  │  │ Role:       │  │ Role:       │  │ Role:       │  │ Role:       │   │  │
│  │  │ Phân tích   │  │ Viết code   │  │ Chạy code   │  │ Viết báo    │   │  │
│  │  │ dữ liệu     │  │ Python      │  │ Python      │  │ cáo         │   │  │
│  │  │             │  │             │  │             │  │             │   │  │
│  │  │ Tools:      │  │ Tools:      │  │ Tools:      │  │ Tools:      │   │  │
│  │  │ -get_grades │  │ (none-chỉ   │  │ -execute_   │  │ -save_      │   │  │
│  │  │ -get_stats  │  │  gen code)  │  │  python_code│  │  report     │   │  │
│  │  │ -get_weak   │  │             │  │             │  │ -create_    │   │  │
│  │  │ -get_chart  │  │             │  │             │  │  email      │   │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘   │  │
│  │                                                                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                │                                             │
│                                ▼                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                          TOOLS LAYER                                   │  │
│  │                                                                        │  │
│  │   data_tools.py        code_tools.py         report_tools.py          │  │
│  │   ├─get_class_grades   ├─execute_python      ├─save_report            │  │
│  │   ├─get_class_stats    │  _code              └─create_parent_email    │  │
│  │   ├─get_students_      │                                               │  │
│  │   │ needing_support    │                                               │  │
│  │   └─get_chart_data     │                                               │  │
│  │                                                                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                │                                             │
│                                ▼                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                       SOURCE DATA LAYER                                │  │
│  │                                                                        │  │
│  │   data_generator (Global Instance)                                    │  │
│  │   ├── students: List[Student]  ← 90 học sinh (3 lớp x 30 HS)         │  │
│  │   └── grades: List[Grade]      ← 630 điểm số (90 HS x 7 môn)         │  │
│  │                                                                        │  │
│  │   Models:                                                              │  │
│  │   ├── Student: id, name, class_name, date_of_birth, parent_email     │  │
│  │   ├── Grade: student_id, subject, score, semester, recorded_at       │  │
│  │   └── ClassStatistics: class_name, avg, high, low, subject_averages  │  │
│  │                                                                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Chi tiết từng Agent

| Agent | Vai trò | System Prompt | Tools | Output |
|-------|---------|---------------|-------|--------|
| **Analyst** | Phân tích dữ liệu | "Bạn là CHUYÊN GIA PHÂN TÍCH DỮ LIỆU GIÁO DỤC..." | get_grades, get_stats, get_weak, get_chart | Text insights |
| **Coder** | Viết code | "Bạn là LẬP TRÌNH VIÊN chuyên viết code Python..." | None (chỉ generate code) | Python code block |
| **Executor** | Chạy code | "Bạn là EXECUTOR - chuyên chạy code Python..." | execute_python_code | Execution result + file paths |
| **Writer** | Viết báo cáo | "Bạn là CHUYÊN GIA VIẾT BÁO CÁO cho phụ huynh..." | save_report, create_email | Report + TASK_COMPLETE |

---

## 4. Data Flow giữa các Agents

### 4 loại Data trong hệ thống

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    TYPES OF DATA IN MULTI-AGENT SYSTEM                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1. SOURCE DATA (Dữ liệu nguồn)                                             │
│     ├── Raw data từ database, file, API                                     │
│     ├── Ví dụ: Điểm số học sinh, thông tin lớp học                         │
│     └── Đặc điểm: Không thay đổi trong quá trình conversation               │
│                                                                              │
│  2. CONVERSATION DATA (Dữ liệu hội thoại)                                   │
│     ├── Messages giữa các agents                                            │
│     ├── Ví dụ: "Analyst nói: Điểm TB lớp 10A là 7.5"                        │
│     └── Đặc điểm: Tích lũy qua từng round, mọi agent đều thấy               │
│                                                                              │
│  3. INTERMEDIATE DATA (Dữ liệu trung gian)                                  │
│     ├── Kết quả xử lý của mỗi agent                                         │
│     ├── Ví dụ: Biểu đồ PNG, file báo cáo MD                                 │
│     └── Đặc điểm: Output của agent này = Input của agent khác               │
│                                                                              │
│  4. AGENT STATE (Trạng thái nội bộ)                                         │
│     ├── Memory, context riêng của mỗi agent                                 │
│     ├── Ví dụ: "Tôi đã phân tích lớp 10A xong"                              │
│     └── Đặc điểm: Chỉ agent đó biết                                         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow chi tiết

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       DATA FLOW IN PLAN-v1                                   │
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
│  │  2. Extract code từ markdown block ```python ... ```                │    │
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

## 5. Speaker Selection Methods

### Round-Robin (PLAN-v1 sử dụng)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ROUND-ROBIN (Tuần tự) - PLAN-v1 ĐANG DÙNG                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  speaker_selection_method="round_robin"                                     │
│                                                                              │
│  Round 1 → Analyst                                                          │
│  Round 2 → Coder                                                            │
│  Round 3 → Executor                                                         │
│  Round 4 → Writer                                                           │
│  Round 5 → Analyst (nếu chưa TASK_COMPLETE)                                │
│  ...                                                                        │
│                                                                              │
│  ✅ Ưu điểm:                                                                │
│  ├── Đơn giản, dễ hiểu                                                     │
│  ├── Dễ debug (biết trước ai nói tiếp)                                     │
│  ├── Predictable flow                                                       │
│  └── Phù hợp cho learning/lab                                              │
│                                                                              │
│  ❌ Nhược điểm:                                                             │
│  ├── Không linh hoạt                                                        │
│  ├── Agent có thể skip turn không cần thiết                                │
│  └── Không xử lý error tốt (nếu Coder sai, vẫn đến Executor)              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Auto (Alternative)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  AUTO (LLM quyết định) - ALTERNATIVE                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  speaker_selection_method="auto"                                            │
│                                                                              │
│  Round 1 → LLM chọn: "Analyst cần phân tích trước"                         │
│  Round 2 → LLM chọn: "Coder cần viết code"                                 │
│  Round 3 → LLM chọn: "Có lỗi! Coder sửa lại" ← Quay lại Coder!            │
│  Round 4 → LLM chọn: "OK, Executor chạy"                                   │
│  ...                                                                        │
│                                                                              │
│  ✅ Ưu điểm:                                                                │
│  ├── Linh hoạt                                                              │
│  ├── Xử lý error tốt hơn                                                   │
│  └── Dynamic routing                                                        │
│                                                                              │
│  ❌ Nhược điểm:                                                             │
│  ├── Khó debug (không biết ai nói tiếp)                                    │
│  ├── Tốn tokens (LLM phải decide mỗi round)                                │
│  └── Không deterministic                                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### So sánh

| Aspect | Round-Robin | Auto |
|--------|-------------|------|
| **Predictability** | Cao | Thấp |
| **Flexibility** | Thấp | Cao |
| **Error handling** | Kém | Tốt |
| **Token cost** | Thấp | Cao |
| **Debug** | Dễ | Khó |
| **Use case** | Lab, Learning | Production |

---

## 6. Message Flow trong GroupChat

### GroupChat.messages[] là Shared Memory

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    MESSAGE FLOW: PLAN-v1                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  GroupChat.messages[] là SHARED MEMORY giữa tất cả agents                   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     GroupChat.messages                               │    │
│  ├─────────────────────────────────────────────────────────────────────┤    │
│  │  [0] User:     "Phân tích lớp 10A và tạo báo cáo"                   │    │
│  │       ↓                                                              │    │
│  │  [1] Analyst:  "📊 PHÂN TÍCH: Điểm TB 7.5, Toán cao nhất..."        │    │
│  │       ↓        (gọi tool: get_class_statistics)                     │    │
│  │  [2] Coder:    "```python\nimport matplotlib...\n```"               │    │
│  │       ↓        (đọc [1] để lấy data cho code)                       │    │
│  │  [3] Executor: "✅ File saved: charts/class_10a.png"                │    │
│  │       ↓        (gọi tool: execute_python_code với code từ [2])      │    │
│  │  [4] Writer:   "📋 BÁO CÁO:...\n\nTASK_COMPLETE"                    │    │
│  │                (đọc [0-3] để tổng hợp, gọi save_report)             │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  KEY INSIGHT:                                                               │
│  ═══════════                                                                │
│  • Mỗi agent ĐỌC TẤT CẢ messages trước đó                                  │
│  • Mỗi agent THÊM message mới vào cuối                                     │
│  • Agents KHÔNG nói chuyện trực tiếp - đều qua messages[]                  │
│  • GroupChatManager theo dõi và chọn speaker tiếp theo                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Cách Agents đọc message từ nhau

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

## 7. So sánh với các Pattern khác

### PLAN-v1 vs Các Pattern

| Pattern | Mô tả | PLAN-v1 dùng? | Khi nào dùng |
|---------|-------|---------------|--------------|
| **Sequential** | A → B → C → Output | ✅ CÓ | Task có workflow rõ ràng |
| **Concurrent** | A, B, C chạy song song | ❌ KHÔNG | Task có thể parallel |
| **Hand-off** | A → B hoặc C (conditional) | ❌ KHÔNG | Dynamic routing |
| **Magentic-One** | Team với Orchestrator thông minh | ❌ KHÔNG | Complex autonomous tasks |

### Khi nào nên dùng Pattern khác?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    KHI NÀO DÙNG PATTERN KHÁC?                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  CONCURRENT - Khi cần parallel processing                                   │
│  ─────────────────────────────────────────                                  │
│  Ví dụ: Phân tích 3 lớp cùng lúc                                           │
│                                                                              │
│       ┌──► Analyst (10A) ──┐                                               │
│       │                    │                                                │
│  User ┼──► Analyst (10B) ──┼──► Aggregator ──► Writer                      │
│       │                    │                                                │
│       └──► Analyst (11A) ──┘                                               │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  HAND-OFF - Khi cần conditional routing                                     │
│  ─────────────────────────────────────────                                  │
│  Ví dụ: Nếu điểm < 5 → gửi email, ngược lại → chỉ tạo report               │
│                                                                              │
│                    ┌──► EmailAgent (điểm < 5)                              │
│  Analyst ──────────┤                                                        │
│                    └──► ReportAgent (điểm >= 5)                            │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  MAGENTIC-ONE - Khi cần autonomous agent team                               │
│  ─────────────────────────────────────────────                              │
│  Ví dụ: Research agent tự động browse web, analyze, summarize              │
│                                                                              │
│                    ┌─────────────────┐                                      │
│                    │   Orchestrator   │                                      │
│                    │   (Plans tasks)  │                                      │
│                    └────────┬────────┘                                      │
│                             │                                               │
│         ┌───────────────────┼───────────────────┐                          │
│         ▼                   ▼                   ▼                          │
│     WebSurfer           FileSurfer          Coder                          │
│     (Browse)            (Read files)        (Write code)                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Tóm tắt

### PLAN-v1 Summary

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SUMMARY: PLAN-v1 ARCHITECTURE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  PLAN-v1 implement:                                                         │
│  ═══════════════════                                                        │
│                                                                              │
│  • Framework:  AutoGen (Python pyautogen)                                   │
│  • Pattern:    Sequential Round-Robin GroupChat                             │
│  • Agents:     4 chuyên biệt (Analyst, Coder, Executor, Writer)            │
│  • Tools:      Function calling qua LLM                                     │
│  • Data:       Shared via GroupChat.messages                                │
│  • Termination: Keyword "TASK_COMPLETE"                                     │
│                                                                              │
│  ĐÂY LÀ:                                                                    │
│  ═══════                                                                    │
│  • Pattern CƠ BẢN nhất của AutoGen                                          │
│  • Phù hợp cho LAB/LEARNING                                                 │
│  • Dễ debug, dễ hiểu flow                                                   │
│  • Good starting point                                                      │
│                                                                              │
│  CHƯA PHẢI:                                                                 │
│  ══════════                                                                 │
│  • Production-ready (cần error handling, retry)                             │
│  • Complex orchestration (conditional, concurrent)                          │
│  • Full MAF (chưa tích hợp Semantic Kernel)                                 │
│  • Enterprise-grade (cần logging, monitoring)                               │
│                                                                              │
│  NEXT STEPS:                                                                │
│  ═══════════                                                                │
│  1. Thêm error handling (retry loop khi Executor fail)                     │
│  2. Thử speaker_selection_method="auto"                                    │
│  3. Thêm concurrent pattern (analyze multiple classes)                     │
│  4. Tích hợp Semantic Kernel cho enterprise features                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Quick Reference

| Câu hỏi | Trả lời |
|---------|---------|
| **PLAN-v1 dùng framework gì?** | AutoGen (Python pyautogen) |
| **Pattern nào?** | Sequential Round-Robin GroupChat |
| **Có bao nhiêu agents?** | 4: Analyst, Coder, Executor, Writer |
| **Agents chia sẻ data như nào?** | Qua GroupChat.messages[] |
| **Khi nào kết thúc?** | Khi có "TASK_COMPLETE" trong message |
| **Speaker được chọn như nào?** | Round-robin (tuần tự) |
| **Có dùng Semantic Kernel không?** | Chưa - chỉ AutoGen |

---

## References

- [PLAN-v1-multi-agent-lab.md](../plans/PLAN-v1-multi-agent-lab.md) - Implementation plan
- [PLAN-v2-data-organization.md](../plans/PLAN-v2-data-organization.md) - Data organization details
- [v1-microsoft-agent-framework-research.md](../research/v1-microsoft-agent-framework-research.md) - MAF research
- [EX-v1-semantic-kernel.md](./EX-v1-semantic-kernel.md) - Semantic Kernel concepts
- [AutoGen Documentation](https://microsoft.github.io/autogen/)

---

**End of Document**
