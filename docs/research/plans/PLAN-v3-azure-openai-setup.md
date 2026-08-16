# PLAN-v3: Hướng dẫn Setup Azure OpenAI cho Multi-Agent Lab

**Version**: 3.0
**Created**: 2026-01-01
**Purpose**: Hướng dẫn chi tiết cách lấy Azure OpenAI model về sử dụng trong code local
**Prerequisites**: Tài khoản Azure Portal với subscription ($150 hoặc tương đương)

---

## Mục Lục

1. [Tổng quan](#1-tổng-quan)
2. [Step 1: Tạo Azure OpenAI Resource](#2-step-1-tạo-azure-openai-resource)
3. [Step 2: Deploy GPT Model](#3-step-2-deploy-gpt-model)
4. [Step 3: Lấy Endpoint và API Key](#4-step-3-lấy-endpoint-và-api-key)
5. [Step 4: Cấu hình trong Project](#5-step-4-cấu-hình-trong-project)
6. [Step 5: Test Kết Nối](#6-step-5-test-kết-nối)
7. [Troubleshooting](#7-troubleshooting)
8. [Cost Optimization](#8-cost-optimization)

---

## 1. Tổng quan

### Setup Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    SETUP FLOW                                │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ STEP 1: Azure Portal                                 │  │
│  │ → Create Azure OpenAI resource                       │  │
│  │ → Chọn region: East US hoặc Sweden Central           │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ STEP 2: Azure AI Foundry                             │  │
│  │ → Deployments → Create deployment                   │  │
│  │ → Chọn gpt-4o-mini (rẻ nhất cho Lab)                │  │
│  │ → Đặt tên: "gpt-4o-mini"                            │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ STEP 3: Lấy credentials                              │  │
│  │ → Azure Portal → Keys and Endpoint                  │  │
│  │ → Copy: Endpoint + API Key                          │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ STEP 4: Cấu hình local                               │  │
│  │ → cp .env.example .env                               │  │
│  │ → Điền: ENDPOINT, API_KEY, DEPLOYMENT               │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ STEP 5: Test                                         │  │
│  │ → python test_azure_connection.py                   │  │
│  │ → Thấy ✅ = Sẵn sàng!                               │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Thông tin cần thu thập

| # | Thông tin | Lấy từ đâu | Ví dụ |
|---|-----------|------------|-------|
| 1 | Endpoint | Azure Portal → Keys and Endpoint | `https://edu-openai.openai.azure.com/` |
| 2 | API Key | Azure Portal → Keys and Endpoint | `sk-xxxxxxxxxxxxxxxx` |
| 3 | Deployment Name | Azure AI Foundry → Deployments | `gpt-4o-mini` |
| 4 | API Version | Cố định | `2024-02-15-preview` |

---

## 2. Step 1: Tạo Azure OpenAI Resource

### 2.1. Truy cập Azure Portal

```
https://portal.azure.com
```

### 2.2. Tạo Resource

```
┌─────────────────────────────────────────────────────────────┐
│  AZURE PORTAL                                                │
│                                                              │
│  1. Click "+ Create a resource"                             │
│                    │                                         │
│                    ▼                                         │
│  2. Search: "Azure OpenAI"                                  │
│                    │                                         │
│                    ▼                                         │
│  3. Click "Create"                                          │
│                    │                                         │
│                    ▼                                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  CREATE AZURE OPENAI                                 │   │
│  │                                                      │   │
│  │  Subscription: [Chọn subscription $150 của bạn]     │   │
│  │  Resource group: [Create new] → "edu-multi-agent-rg"│   │
│  │  Region: East US (hoặc Southeast Asia)              │   │
│  │  Name: "edu-openai-resource"                         │   │
│  │  Pricing tier: Standard S0                           │   │
│  │                                                      │   │
│  │  [Review + Create] → [Create]                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 2.3. Lưu ý quan trọng về Region

| Region | GPT-4 | GPT-4o | GPT-4o-mini | Recommend |
|--------|-------|--------|-------------|-----------|
| East US | ✅ | ✅ | ✅ | Tốt nhất |
| Sweden Central | ✅ | ✅ | ✅ | Tốt |
| Southeast Asia | ❌ | ✅ | ✅ | OK |
| West Europe | ✅ | ✅ | ✅ | OK |

**Tip**: Nếu gặp lỗi "model not available in this region", hãy thử region khác.

---

## 3. Step 2: Deploy GPT Model

### 3.1. Truy cập Azure AI Foundry

Sau khi tạo resource xong:
1. Click vào resource vừa tạo
2. Click **"Go to Azure AI Foundry portal"**

Hoặc truy cập trực tiếp:
```
https://ai.azure.com
```

### 3.2. Deploy Model

```
┌─────────────────────────────────────────────────────────────┐
│  AZURE AI FOUNDRY                                            │
│                                                              │
│  1. Menu trái → "Deployments"                               │
│                    │                                         │
│                    ▼                                         │
│  2. Click "+ Create deployment"                             │
│                    │                                         │
│                    ▼                                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  SELECT A MODEL                                      │   │
│  │                                                      │   │
│  │  ○ gpt-4                    ← Mạnh nhất, đắt nhất   │   │
│  │  ○ gpt-4o                   ← Nhanh hơn, rẻ hơn     │   │
│  │  ● gpt-4o-mini              ← Recommend cho Lab     │   │
│  │  ○ gpt-35-turbo             ← Legacy                │   │
│  │                                                      │   │
│  │  [Next]                                              │   │
│  └─────────────────────────────────────────────────────┘   │
│                    │                                         │
│                    ▼                                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  DEPLOYMENT CONFIGURATION                            │   │
│  │                                                      │   │
│  │  Deployment name: "gpt-4o-mini" ← GHI NHỚ TÊN NÀY! │   │
│  │  Model version: [Latest]                             │   │
│  │  Tokens per Minute: 10K      ← Đủ cho Lab           │   │
│  │                                                      │   │
│  │  [Deploy]                                            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3.3. So sánh Models và Chi phí

| Model | Input Cost/1M tokens | Output Cost/1M tokens | Recommend |
|-------|---------------------|----------------------|-----------|
| `gpt-4o-mini` | ~$0.15 | ~$0.60 | **Best cho Lab** (rẻ nhất) |
| `gpt-4o` | ~$2.50 | ~$10.00 | Tốt, cân bằng |
| `gpt-4` | ~$30.00 | ~$60.00 | Đắt, dùng khi cần quality cao |
| `gpt-35-turbo` | ~$0.50 | ~$1.50 | Legacy, không recommend |

**Ước tính chi phí với $150 budget:**

```
┌─────────────────────────────────────────────────────────────┐
│  BUDGET ESTIMATION ($150)                                    │
│                                                              │
│  Model: gpt-4o-mini                                         │
│  ├── Cost per 1M tokens: ~$0.75 (avg input+output)         │
│  ├── Tokens per request: ~500 (avg)                         │
│  ├── Requests possible: ~400,000 requests                   │
│  └── Kết luận: ĐỦ DÙNG RẤT LÂU cho Lab!                    │
│                                                              │
│  Model: gpt-4                                                │
│  ├── Cost per 1M tokens: ~$45 (avg input+output)           │
│  ├── Tokens per request: ~500 (avg)                         │
│  ├── Requests possible: ~6,600 requests                     │
│  └── Kết luận: Cẩn thận, hết nhanh!                        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Step 3: Lấy Endpoint và API Key

### 4.1. Trong Azure Portal

```
┌─────────────────────────────────────────────────────────────┐
│  AZURE PORTAL → Your OpenAI Resource                         │
│                                                              │
│  Menu trái → "Keys and Endpoint"                            │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  KEYS AND ENDPOINT                                   │   │
│  │                                                      │   │
│  │  KEY 1: sk-xxxxxxxxxxxxxxxxxxxxxxxx                 │   │
│  │         [Copy]  [Regenerate]                        │   │
│  │                                                      │   │
│  │  KEY 2: sk-yyyyyyyyyyyyyyyyyyyyyyyy                 │   │
│  │         [Copy]  [Regenerate]                        │   │
│  │                                                      │   │
│  │  Endpoint:                                           │   │
│  │  https://edu-openai-resource.openai.azure.com/      │   │
│  │         [Copy]                                       │   │
│  │                                                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 4.2. Checklist - 3 thông tin cần ghi lại

```
┌─────────────────────────────────────────────────────────────┐
│  ✅ CHECKLIST - GHI LẠI 3 THÔNG TIN NÀY:                    │
│                                                              │
│  1. ENDPOINT:                                                │
│     https://_____________________________.openai.azure.com/ │
│                                                              │
│  2. API KEY (KEY 1 hoặc KEY 2):                             │
│     sk-________________________________________________     │
│                                                              │
│  3. DEPLOYMENT NAME (từ Step 2):                            │
│     ________________________________________________        │
│     (ví dụ: gpt-4o-mini, gpt-4, gpt-4o)                     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Step 4: Cấu hình trong Project

### 5.1. File Structure

```
edu-multi-agent/
├── .env.example          # Template (commit được)
├── .env                  # Credentials thực (KHÔNG commit!)
├── .gitignore            # Bảo vệ .env
└── test_azure_connection.py
```

### 5.2. Tạo file .env

```bash
# Chạy lệnh này trong terminal
cd /home/duymd/src/edu-multi-agent
cp .env.example .env
```

### 5.3. Điền thông tin vào .env

```bash
# .env (KHÔNG commit file này lên git!)

# Endpoint từ Azure Portal → Keys and Endpoint
AZURE_OPENAI_ENDPOINT=https://edu-openai-resource.openai.azure.com/

# API Key từ Azure Portal → Keys and Endpoint (KEY 1 hoặc KEY 2)
AZURE_OPENAI_API_KEY=sk-xxxxxxxxxxxxxxxxxxxxxxxx

# Deployment name từ Azure AI Foundry → Deployments
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini

# API Version (giữ nguyên)
AZURE_OPENAI_API_VERSION=2024-02-15-preview
```

### 5.4. Bảo vệ credentials với .gitignore

```gitignore
# .gitignore

# Environment variables (QUAN TRỌNG: không commit API keys!)
.env

# Python
__pycache__/
*.py[cod]
venv/
.venv/

# Output
output/
```

---

## 6. Step 5: Test Kết Nối

### 6.1. Cài đặt dependencies

```bash
# 1. Tạo virtual environment (khuyến nghị)
cd /home/duymd/src/edu-multi-agent
python3 -m venv venv
source venv/bin/activate

# 2. Cài đặt packages cần thiết
pip install openai python-dotenv
```

### 6.2. Chạy test

```bash
python test_azure_connection.py
```

### 6.3. Kết quả mong đợi

```
============================================================
🔍 KIỂM TRA CẤU HÌNH AZURE OPENAI
============================================================

📋 Cấu hình:
   Endpoint: https://edu-openai-resourc...
   API Key: ********************...
   Deployment: gpt-4o-mini
   API Version: 2024-02-15-preview

------------------------------------------------------------
🚀 THỬ KẾT NỐI VÀ GỌI API...
------------------------------------------------------------

✅ KẾT NỐI THÀNH CÔNG!

🤖 Response từ gpt-4o-mini:
   "Xin chào! Tôi có thể giúp gì cho bạn hôm nay?"

📊 Thông tin sử dụng:
   Model: gpt-4o-mini
   Tokens: 45 (prompt: 30, completion: 15)

============================================================
✅ SẴN SÀNG SỬ DỤNG AZURE OPENAI TRONG MULTI-AGENT LAB!
============================================================
```

---

## 7. Troubleshooting

### 7.1. Lỗi thường gặp

| Error | Nguyên nhân | Giải pháp |
|-------|-------------|-----------|
| `401 Unauthorized` | API Key sai | Kiểm tra lại KEY 1 từ Azure Portal |
| `404 Not Found` | Deployment name sai | Kiểm tra tên trong Azure AI Foundry |
| `Endpoint not found` | Endpoint URL sai | Kiểm tra lại URL (phải có `/` cuối) |
| `Model not available` | Region không hỗ trợ | Đổi region (East US recommend) |
| `Quota exceeded` | Hết quota | Tăng TPM trong Azure AI Foundry |

### 7.2. Debug checklist

```
┌─────────────────────────────────────────────────────────────┐
│  DEBUG CHECKLIST                                             │
│                                                              │
│  □ File .env tồn tại (không phải .env.example)             │
│  □ Endpoint có dấu "/" ở cuối                               │
│  □ API Key không có space thừa                              │
│  □ Deployment name đúng chính xác (case-sensitive)         │
│  □ Resource đã được tạo thành công (status: Running)       │
│  □ Model đã được deploy (status: Succeeded)                │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 7.3. Kiểm tra nhanh với curl

```bash
# Test API với curl
curl -X POST "${AZURE_OPENAI_ENDPOINT}openai/deployments/${AZURE_OPENAI_DEPLOYMENT}/chat/completions?api-version=2024-02-15-preview" \
  -H "Content-Type: application/json" \
  -H "api-key: ${AZURE_OPENAI_API_KEY}" \
  -d '{
    "messages": [{"role": "user", "content": "Hello"}],
    "max_tokens": 50
  }'
```

---

## 8. Cost Optimization

### 8.1. Tips tiết kiệm chi phí

```
┌─────────────────────────────────────────────────────────────┐
│  COST OPTIMIZATION TIPS                                      │
│                                                              │
│  1. Chọn model phù hợp                                       │
│     ├── Lab/Development: gpt-4o-mini (rẻ nhất)             │
│     ├── Production: gpt-4o (cân bằng)                       │
│     └── Critical tasks: gpt-4 (khi cần quality cao)        │
│                                                              │
│  2. Giới hạn tokens                                          │
│     ├── Set max_tokens hợp lý (không quá 1000)             │
│     └── Prompt ngắn gọn, đủ ý                               │
│                                                              │
│  3. Caching                                                  │
│     ├── Cache responses cho queries giống nhau              │
│     └── Tránh gọi API lặp lại không cần thiết              │
│                                                              │
│  4. Monitoring                                               │
│     ├── Azure Portal → Cost Management                      │
│     └── Set budget alerts ($50, $100, $140)                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 8.2. Set Budget Alert

Trong Azure Portal:
1. Vào **Cost Management + Billing**
2. Chọn **Budgets** → **Add**
3. Set alerts:
   - 50% ($75) - Nhắc nhở
   - 80% ($120) - Cảnh báo
   - 95% ($142) - Cảnh báo nghiêm trọng

---

## 9. Kết nối với PLAN-v1 Multi-Agent Lab

Sau khi setup xong, bạn có thể sử dụng trong Multi-Agent Lab như sau:

### 9.1. Trong config.py

```python
# src/multi_agent_lab/config.py

import os
from dotenv import load_dotenv

load_dotenv()

# Azure OpenAI Configuration (lấy từ .env)
AZURE_CONFIG = {
    "model": os.getenv("AZURE_OPENAI_DEPLOYMENT", "gpt-4o-mini"),
    "api_key": os.getenv("AZURE_OPENAI_API_KEY"),
    "base_url": os.getenv("AZURE_OPENAI_ENDPOINT"),
    "api_type": "azure",
    "api_version": os.getenv("AZURE_OPENAI_API_VERSION", "2024-02-15-preview"),
}
```

### 9.2. Trong group_chat.py

```python
# src/multi_agent_lab/orchestration/group_chat.py

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
```

### 9.3. Flow tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│                    LLM CONNECTION FLOW                       │
│                                                              │
│  .env file                                                   │
│     │ AZURE_OPENAI_ENDPOINT                                 │
│     │ AZURE_OPENAI_API_KEY                                  │
│     │ AZURE_OPENAI_DEPLOYMENT                               │
│     ▼                                                        │
│  config.py (AZURE_CONFIG dict)                               │
│     │                                                        │
│     ▼                                                        │
│  create_llm_config() → {"config_list": [...]}               │
│     │                                                        │
│     ▼                                                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  4 Agents dùng chung 1 llm_config                   │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐ │    │
│  │  │ Analyst  │ │  Coder   │ │ Executor │ │ Writer │ │    │
│  │  └──────────┘ └──────────┘ └──────────┘ └────────┘ │    │
│  └─────────────────────────────────────────────────────┘    │
│                          │                                   │
│                          ▼                                   │
│                   Azure OpenAI API                           │
│                   (gpt-4o-mini model)                        │
└─────────────────────────────────────────────────────────────┘
```

---

## References

- [PLAN-v1-multi-agent-lab.md](./PLAN-v1-multi-agent-lab.md) - Kế hoạch implement Multi-Agent Lab
- [PLAN-v2-data-organization.md](./PLAN-v2-data-organization.md) - Tổ chức dữ liệu
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Azure AI Foundry](https://ai.azure.com)
- [OpenAI Python SDK](https://github.com/openai/openai-python)

---

**End of Document**
