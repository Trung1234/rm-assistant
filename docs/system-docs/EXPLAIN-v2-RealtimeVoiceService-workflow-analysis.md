# EXPLAIN-v2: RealtimeVoiceService Workflow Deep Analysis

**Document**: EXPLAIN-v2-RealtimeVoiceService-workflow-analysis.md
**Created**: 2026-01-12
**Last Updated**: 2026-01-12
**Version**: 2.0
**Source File**: `src/edu_mul_agents.Infrastructure/Services/RealtimeVoiceService.cs` (1102 lines)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Component Architecture Overview](#2-component-architecture-overview)
3. [WebSocket Protocol Deep Dive](#3-websocket-protocol-deep-dive)
4. [Server VAD (Voice Activity Detection)](#4-server-vad-voice-activity-detection)
5. [Session Management Lifecycle](#5-session-management-lifecycle)
6. [Message Processing Pipeline](#6-message-processing-pipeline)
7. [Function Calling Workflow](#7-function-calling-workflow)
8. [Tool Implementations](#8-tool-implementations)
9. [Streaming Response Architecture](#9-streaming-response-architecture)
10. [State Management](#10-state-management)
11. [Event-Driven Communication](#11-event-driven-communication)
12. [Error Handling Patterns](#12-error-handling-patterns)
13. [Security Considerations](#13-security-considerations)
14. [Performance Analysis](#14-performance-analysis)
15. [Best Practices Comparison](#15-best-practices-comparison)
16. [Quick Reference](#16-quick-reference)

---

## 1. Executive Summary

```
+==============================================================================+
|                 REALTIMEVOICESERVICE - EXECUTIVE SUMMARY                      |
+==============================================================================+
|                                                                              |
|  PURPOSE: Real-time voice-to-voice communication with Azure OpenAI          |
|  PROTOCOL: WebSocket (wss://) + JSON message exchange                        |
|  PATTERN: Event-driven async with background receive loop                    |
|                                                                              |
|  KEY FEATURES:                                                               |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ [1] Server VAD - Automatic speech boundary detection                    │  |
|  │ [2] Function Calling - 3 education tools (data, advice, switch)         │  |
|  │ [3] Streaming TTS - Sentence-by-sentence for low latency                │  |
|  │ [4] Interruption Handling - Cancel AI response on user speech           │  |
|  │ [5] Context Management - Parent/Student session state                   │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                                                              |
|  AUDIO FORMAT: PCM16, 24kHz, Mono (Base64 encoded over WebSocket)           |
|  AI MODEL: gpt-4o-realtime-preview (Voice) + gpt-5-mini (Advice)            |
|                                                                              |
+==============================================================================+
```

### Service Statistics

| Metric | Value | Notes |
|--------|-------|-------|
| **Total Lines** | 1102 | Including comments |
| **Methods** | 23 | Public + Private |
| **Events** | 5 | OnAudio, OnTranscript, OnError, OnStudentSwitched, OnPlaybackInterrupted |
| **Tools** | 3 | get_education_data, get_advice, switch_student |
| **Dependencies** | 4 | IStudentService, IConfiguration, ILogger, ChatClient |

---

## 2. Component Architecture Overview

```
+==============================================================================+
|                    REALTIMEVOICESERVICE ARCHITECTURE                          |
+==============================================================================+

                           BLAZOR UI LAYER
  ┌──────────────────────────────────────────────────────────────────────────┐
  │                         VoiceMode.razor.cs                                │
  │  ┌───────────────┐   ┌───────────────┐   ┌───────────────┐               │
  │  │ JS Interop    │   │ Event         │   │ UI State      │               │
  │  │ Audio Capture │   │ Handlers      │   │ Management    │               │
  │  └───────┬───────┘   └───────┬───────┘   └───────────────┘               │
  └──────────┼───────────────────┼────────────────────────────────────────────┘
             │                   │
             │ SendAudio         │ Subscribe Events
             ▼                   ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │                      REALTIMEVOICESERVICE                                 │
  │  ┌─────────────────────────────────────────────────────────────────────┐ │
  │  │                     CONNECTION LAYER                                 │ │
  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │ │
  │  │  │ WebSocket   │  │ Session     │  │ Receive     │                  │ │
  │  │  │ Client      │  │ Config      │  │ Loop Task   │                  │ │
  │  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘                  │ │
  │  └─────────┼────────────────┼────────────────┼─────────────────────────┘ │
  │            │                │                │                            │
  │  ┌─────────┼────────────────┼────────────────┼─────────────────────────┐ │
  │  │         │        MESSAGE PROCESSING                                  │ │
  │  │         ▼                ▼                ▼                          │ │
  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │ │
  │  │  │ Audio       │  │ Transcript  │  │ Function    │                  │ │
  │  │  │ Streaming   │  │ Handling    │  │ Execution   │                  │ │
  │  │  └─────────────┘  └─────────────┘  └──────┬──────┘                  │ │
  │  └───────────────────────────────────────────┼─────────────────────────┘ │
  │                                              │                            │
  │  ┌───────────────────────────────────────────┼─────────────────────────┐ │
  │  │                     TOOL LAYER            │                          │ │
  │  │  ┌─────────────┐  ┌─────────────┐  ┌──────┴──────┐                  │ │
  │  │  │ get_        │  │ get_        │  │ switch_     │                  │ │
  │  │  │ education   │  │ advice      │  │ student     │                  │ │
  │  │  │ _data       │  │ (streaming) │  │             │                  │ │
  │  │  └──────┬──────┘  └──────┬──────┘  └─────────────┘                  │ │
  │  └─────────┼────────────────┼──────────────────────────────────────────┘ │
  └────────────┼────────────────┼──────────────────────────────────────────────┘
               │                │
               ▼                ▼
  ┌─────────────────────┐  ┌─────────────────────┐
  │  IStudentService    │  │  GPT-5-mini Client  │
  │  (Graph API)        │  │  (Advice Generation)│
  └─────────────────────┘  └─────────────────────┘
               │                │
               ▼                ▼
  ┌─────────────────────┐  ┌─────────────────────┐
  │ Microsoft Graph     │  │ Azure OpenAI        │
  │ Education API       │  │ Chat Completion API │
  └─────────────────────┘  └─────────────────────┘

EXTERNAL SERVICES:
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                    AZURE OPENAI REALTIME API                             │
  │  Endpoint: wss://{resource}.openai.azure.com/openai/realtime             │
  │  Model: gpt-4o-realtime-preview                                          │
  │  Protocol: WebSocket + JSON                                              │
  │  Audio: PCM16, 24kHz, Mono                                               │
  └─────────────────────────────────────────────────────────────────────────┘
```

### Class Dependency Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        RealtimeVoiceService                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│ Implements:                                                                  │
│   ├── IRealtimeVoiceService (interface contract)                            │
│   └── IDisposable (resource cleanup)                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│ Dependencies (Constructor Injection):                                        │
│   ├── IConfiguration ──────────────► Read AzureOpenAIRealtime settings      │
│   ├── ILogger<RealtimeVoiceService> ► Structured logging                    │
│   └── IStudentService ─────────────► Fetch education data from Graph API    │
├─────────────────────────────────────────────────────────────────────────────┤
│ Self-Created:                                                                │
│   └── ChatClient (Azure.AI.OpenAI) ► GPT-5-mini for advice streaming        │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. WebSocket Protocol Deep Dive

### 3.1 Connection Establishment

```
+==============================================================================+
|                     WEBSOCKET CONNECTION FLOW                                 |
+==============================================================================+

  CLIENT (Blazor)                              AZURE OPENAI REALTIME API
        │                                              │
        │ 1. Create ClientWebSocket                    │
        │    Set Header: "api-key" = {apiKey}          │
        │                                              │
        │ 2. ConnectAsync(URI)                         │
        │ ──────────────────────────────────────────►  │
        │    URI: wss://{endpoint}                     │
        │         ?api-version=2024-10-01-preview      │
        │         &deployment={deploymentName}         │
        │                                              │
        │ ◄──────────────────────────────────────────  │
        │    WebSocket Handshake Complete              │
        │    State: Open                               │
        │                                              │
        │ 3. SendSessionConfigAsync()                  │
        │ ──────────────────────────────────────────►  │
        │    { type: "session.update", session: {...}} │
        │                                              │
        │ ◄──────────────────────────────────────────  │
        │    { type: "session.updated", ... }          │
        │                                              │
        │ 4. Start ReceiveLoopAsync (background)       │
        │    ┌──────────────────────────────────────┐  │
        │    │ while (connected && !cancelled)      │  │
        │    │   await ReceiveAsync(buffer)         │  │
        │    │   ProcessMessageAsync(message)       │  │
        │    └──────────────────────────────────────┘  │
        │                                              │
```

### 3.2 WebSocket URI Construction

```csharp
// Code Location: Line 146
var connectionUrl = $"{_endpoint}?api-version={_apiVersion}&deployment={_deploymentName}";
```

| Parameter | Source | Example |
|-----------|--------|---------|
| `_endpoint` | appsettings.json `AzureOpenAIRealtime:Endpoint` | `wss://my-resource.openai.azure.com/openai/realtime` |
| `_apiVersion` | appsettings.json `AzureOpenAIRealtime:ApiVersion` | `2024-10-01-preview` |
| `_deploymentName` | appsettings.json `AzureOpenAIRealtime:DeploymentName` | `gpt-4o-realtime-preview` |

### 3.3 Message Protocol

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CLIENT → SERVER EVENTS (Outbound)                         │
├─────────────────────────────────────────────────────────────────────────────┤
│ Event Type                    │ Purpose                                      │
├───────────────────────────────┼──────────────────────────────────────────────┤
│ session.update                │ Configure voice, tools, VAD settings         │
│ input_audio_buffer.append     │ Send audio chunk (Base64 PCM16)              │
│ response.cancel               │ Cancel in-progress AI response               │
│ conversation.item.create      │ Add text/function output to conversation     │
│ response.create               │ Request AI to generate response              │
└───────────────────────────────┴──────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                    SERVER → CLIENT EVENTS (Inbound)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ Event Type                                   │ Handler Method               │
├──────────────────────────────────────────────┼──────────────────────────────┤
│ session.updated                              │ (logged only)                │
│ input_audio_buffer.speech_started            │ HandleInterruption           │
│ conversation.item.input_audio_transcription  │ HandleTranscriptReceived     │
│   .completed                                 │                              │
│ conversation.item.created (function_call)    │ SavePendingFunctionCall      │
│ response.function_call_arguments.delta       │ AccumulateFunctionArguments  │
│ response.function_call_arguments.done        │ ExecutePendingFunctionCall   │
│ response.audio.delta                         │ OnAudioReceived event        │
│ response.output_audio.delta                  │ OnAudioReceived event        │
│ response.*.transcript.delta                  │ Accumulate AI text           │
│ response.done                                │ OnTranscriptReceived event   │
└──────────────────────────────────────────────┴──────────────────────────────┘
```

---

## 4. Server VAD (Voice Activity Detection)

### 4.1 Concept Overview

```
+==============================================================================+
|                    SERVER VAD - VOICE ACTIVITY DETECTION                      |
+==============================================================================+
|                                                                              |
|  PURPOSE: Automatically detect when user starts/stops speaking               |
|  BENEFIT: No manual push-to-talk, natural conversation flow                  |
|                                                                              |
|  AUDIO TIMELINE:                                                             |
|                                                                              |
|  ──────────────────────────────────────────────────────────────────────────  |
|        │         │                           │              │               |
|        │◄──300ms─┼──── User Speaking ───────►│◄───800ms────►│               |
|        │ prefix  │                           │   silence    │               |
|        │ padding │                           │   duration   │               |
|  ──────────────────────────────────────────────────────────────────────────  |
|        ▲         ▲                           ▲              ▲               |
|   Start of    Speech                    Speech           Turn               |
|   capture     detected                  ended            complete           |
|   (buffered)  (threshold)               (< threshold)    (process)          |
|                                                                              |
+==============================================================================+
```

### 4.2 Configuration Parameters

```csharp
// Code Location: Lines 319-325
turn_detection = new
{
    type = "server_vad",
    threshold = 0.8,           // Sensitivity: 0.0 (very sensitive) to 1.0 (less sensitive)
    prefix_padding_ms = 300,   // Audio captured BEFORE speech detected
    silence_duration_ms = 800  // Silence required to consider turn complete
}
```

| Parameter | Value | Purpose | Trade-off |
|-----------|-------|---------|-----------|
| **threshold** | 0.8 | Higher = less sensitive to noise | May miss quiet speech |
| **prefix_padding_ms** | 300 | Capture beginning of utterance | Higher = more data sent |
| **silence_duration_ms** | 800 | Time to wait before processing | Lower = faster but may cut off |

### 4.3 VAD Events Flow

```
  USER MICROPHONE              AZURE REALTIME                 SERVICE
        │                            │                            │
        │ Start Speaking             │                            │
        │ ──────────────────────────►│                            │
        │                            │ input_audio_buffer         │
        │                            │ .speech_started            │
        │                            │ ──────────────────────────►│
        │                            │                            │ OnPlaybackInterrupted
        │                            │                            │ (cancel AI audio)
        │ Speaking...                │                            │
        │ ──────────────────────────►│                            │
        │                            │                            │
        │ Stop Speaking              │                            │
        │ (800ms silence)            │                            │
        │                            │                            │
        │                            │ input_audio_buffer         │
        │                            │ .speech_stopped            │
        │                            │                            │
        │                            │ (Whisper transcription)    │
        │                            │                            │
        │                            │ conversation.item          │
        │                            │ .input_audio_transcription │
        │                            │ .completed                 │
        │                            │ ──────────────────────────►│
        │                            │                            │ OnTranscriptReceived
        │                            │                            │ (text, isUser=true)
        │                            │                            │
```

---

## 5. Session Management Lifecycle

### 5.1 Complete Session Lifecycle

```
+==============================================================================+
|                    SESSION LIFECYCLE STATE DIAGRAM                            |
+==============================================================================+

                                    ┌─────────────┐
                                    │    IDLE     │
                                    │ (WebSocket  │
                                    │   = null)   │
                                    └──────┬──────┘
                                           │
                                           │ SetContextAsync()
                                           │ - Store parentId, parentDisplayName
                                           │ - Fetch students via IStudentService
                                           │ - Set default _currentStudentId
                                           ▼
                                    ┌─────────────┐
                                    │  CONTEXT    │
                                    │    SET      │
                                    └──────┬──────┘
                                           │
                                           │ ConnectAsync()
                                           ▼
                              ┌────────────────────────┐
                              │      CONNECTING        │
                              │ - Create ClientWebSocket│
                              │ - Set api-key header   │
                              │ - Connect to endpoint   │
                              └────────────┬───────────┘
                                           │
                                           │ WebSocket.State == Open
                                           ▼
                              ┌────────────────────────┐
                              │    CONFIGURING         │
                              │ - SendSessionConfigAsync│
                              │ - Build dynamic prompt │
                              │ - Define tools         │
                              └────────────┬───────────┘
                                           │
                                           │ session.updated received
                                           │ Start ReceiveLoopAsync
                                           ▼
          ┌─────────────────────────────────────────────────────────────┐
          │                        CONNECTED                            │
          │  ┌─────────────────────────────────────────────────────────┐│
          │  │                   ACTIVE SESSION                        ││
          │  │                                                         ││
          │  │  ┌─────────┐    ┌─────────┐    ┌─────────────────────┐  ││
          │  │  │LISTENING│◄──►│ PROCESS │◄──►│   AI RESPONDING     │  ││
          │  │  │(VAD     │    │ (Tools) │    │ (Audio streaming)   │  ││
          │  │  │ active) │    │         │    │                     │  ││
          │  │  └─────────┘    └─────────┘    └─────────────────────┘  ││
          │  │                                                         ││
          │  │  ┌─────────────────────────────────────────────────────┐││
          │  │  │ INTERRUPTION: speech_started → response.cancel      │││
          │  │  └─────────────────────────────────────────────────────┘││
          │  └─────────────────────────────────────────────────────────┘│
          └──────────────────────────────┬──────────────────────────────┘
                                         │
                                         │ DisconnectAsync()
                                         │ - _receiveCts.Cancel()
                                         │ - CloseAsync(NormalClosure)
                                         │ - Dispose WebSocket
                                         ▼
                                  ┌─────────────┐
                                  │ DISCONNECTED│
                                  │ (Cleanup)   │
                                  └─────────────┘
```

### 5.2 Key Methods by Phase

| Phase | Method | Purpose |
|-------|--------|---------|
| **Context** | `SetContextAsync()` | Load parent/student data, prepare context |
| **Connect** | `ConnectAsync()` | Establish WebSocket, start receive loop |
| **Configure** | `SendSessionConfigAsync()` | Send tools, VAD settings, system prompt |
| **Active** | `SendAudioAsync()` | Forward microphone audio to Azure |
| **Active** | `ReceiveLoopAsync()` | Process incoming messages continuously |
| **Disconnect** | `DisconnectAsync()` | Graceful shutdown, resource cleanup |

---

## 6. Message Processing Pipeline

### 6.1 Receive Loop Architecture

```csharp
// Code Location: Lines 397-431
private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
{
    var buffer = new byte[1024 * 4];           // 4KB buffer
    var messageBuilder = new List<byte>();      // Accumulate multi-frame messages
    var aiResponseBuilder = new StringBuilder(); // Accumulate AI transcript

    while (!cancellationToken.IsCancellationRequested && IsConnected)
    {
        // Read complete message (may span multiple frames)
        do {
            result = await _webSocket.ReceiveAsync(...);
            messageBuilder.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
        } while (!result.EndOfMessage);

        // Process JSON message
        await ProcessMessageAsync(message, aiResponseBuilder);
    }
}
```

### 6.2 Message Processing Flow

```
+==============================================================================+
|                    MESSAGE PROCESSING PIPELINE                                |
+==============================================================================+

  INCOMING JSON MESSAGE
         │
         ▼
  ┌──────────────────┐
  │ JsonDocument     │
  │ .Parse(message)  │
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────┐
  │ Extract "type"   │
  │ property         │
  └────────┬─────────┘
           │
           ▼
  ┌──────────────────────────────────────────────────────────────────────────┐
  │                         SWITCH BY MESSAGE TYPE                            │
  ├──────────────────────────────────────────────────────────────────────────┤
  │                                                                          │
  │  "input_audio_buffer.speech_started"                                     │
  │  ├── Clear aiResponseBuilder                                             │
  │  ├── SendJsonAsync({ type: "response.cancel" })                          │
  │  └── OnPlaybackInterrupted?.Invoke()                                     │
  │                                                                          │
  │  "conversation.item.input_audio_transcription.completed"                 │
  │  └── OnTranscriptReceived?.Invoke(text, isUser: true)                    │
  │                                                                          │
  │  "conversation.item.created" (item.type == "function_call")              │
  │  └── SavePendingFunctionCall(root)                                       │
  │                                                                          │
  │  "response.function_call_arguments.delta"                                │
  │  └── AccumulateFunctionArguments(root)                                   │
  │                                                                          │
  │  "response.function_call_arguments.done"                                 │
  │  └── ExecutePendingFunctionCallAsync(root)                               │
  │                                                                          │
  │  "response.audio.delta" OR "response.output_audio.delta"                 │
  │  ├── Base64 decode delta                                                 │
  │  └── OnAudioReceived?.Invoke(audioBytes)                                 │
  │                                                                          │
  │  "*transcript.delta"                                                     │
  │  └── aiResponseBuilder.Append(delta)                                     │
  │                                                                          │
  │  "response.done"                                                         │
  │  ├── OnTranscriptReceived?.Invoke(fullText, isUser: false)               │
  │  └── aiResponseBuilder.Clear()                                           │
  │                                                                          │
  └──────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Function Calling Workflow

### 7.1 Three-Phase Function Call Protocol

```
+==============================================================================+
|            FUNCTION CALLING - THREE PHASE PROTOCOL                            |
+==============================================================================+
|                                                                              |
|  Azure Realtime API streams function calls in 3 phases:                      |
|                                                                              |
|  PHASE 1: conversation.item.created                                          |
|           - Contains: call_id, name (function name)                          |
|           - Arguments: EMPTY at this point                                   |
|           → Action: Save to _pendingFunctionCalls dictionary                 |
|                                                                              |
|  PHASE 2: response.function_call_arguments.delta (multiple events)           |
|           - Contains: call_id, delta (argument chunk)                        |
|           → Action: Accumulate chunks into StringBuilder                     |
|                                                                              |
|  PHASE 3: response.function_call_arguments.done                              |
|           - Contains: call_id                                                |
|           → Action: Execute function with complete arguments                 |
|                                                                              |
+==============================================================================+
```

### 7.2 Function Call Sequence Diagram

```
  AZURE REALTIME                    SERVICE                       TOOLS
        │                              │                            │
        │ conversation.item.created    │                            │
        │ { call_id: "abc123",         │                            │
        │   name: "get_education_data",│                            │
        │   arguments: "" }            │                            │
        │ ───────────────────────────► │                            │
        │                              │ SavePendingFunctionCall    │
        │                              │ _pendingFunctionCalls      │
        │                              │   ["abc123"] = ("get_      │
        │                              │   education_data",         │
        │                              │   StringBuilder())         │
        │                              │                            │
        │ response.function_call       │                            │
        │ _arguments.delta             │                            │
        │ { call_id: "abc123",         │                            │
        │   delta: "{\"cla" }          │                            │
        │ ───────────────────────────► │                            │
        │                              │ AccumulateFunctionArguments│
        │                              │ StringBuilder += delta     │
        │                              │                            │
        │ response.function_call       │                            │
        │ _arguments.delta             │                            │
        │ { call_id: "abc123",         │                            │
        │   delta: "ss_id\":\"..." }   │                            │
        │ ───────────────────────────► │                            │
        │                              │ AccumulateFunctionArguments│
        │                              │ StringBuilder += delta     │
        │                              │                            │
        │ response.function_call       │                            │
        │ _arguments.done              │                            │
        │ { call_id: "abc123" }        │                            │
        │ ───────────────────────────► │                            │
        │                              │ ExecutePendingFunctionCall │
        │                              │ ──────────────────────────►│
        │                              │                            │ Execute
        │                              │ ◄──────────────────────────│ Tool
        │                              │          result            │
        │                              │                            │
        │ ◄─────────────────────────── │                            │
        │   conversation.item.create   │                            │
        │   { type: "function_call_    │                            │
        │     output", call_id, output}│                            │
        │                              │                            │
        │ ◄─────────────────────────── │                            │
        │   response.create            │                            │
        │                              │                            │
        │ (AI generates response based │                            │
        │  on function output)         │                            │
        │                              │                            │
```

### 7.3 Pending Function Calls Dictionary

```csharp
// Code Location: Lines 52-55
// Key = call_id, Value = (func_name, accumulated_arguments)
private readonly Dictionary<string, (string FuncName, StringBuilder Args)> _pendingFunctionCalls = new();
```

| Operation | Method | Line |
|-----------|--------|------|
| Save | `SavePendingFunctionCall()` | 536-549 |
| Accumulate | `AccumulateFunctionArguments()` | 554-562 |
| Execute | `ExecutePendingFunctionCallAsync()` | 567-617 |

---

## 8. Tool Implementations

### 8.1 Tool Registry Overview

```
+==============================================================================+
|                         VOICE TOOLS REGISTRY                                  |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│                          get_education_data                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│ TRIGGER: "homework", "assignments", "grades", "classes", "academic"          │
│ PARAMETERS: None (uses _currentStudentId from context)                       │
│ RETURNS: Formatted string with classes, assignments, deadlines, grades       │
│ CALLS: IStudentService.GetStudentClassesAsync()                              │
│        IStudentService.GetClassAssignmentsAsync()                            │
│        IStudentService.GetAssignmentGradeAsync()                             │
├─────────────────────────────────────────────────────────────────────────────┤
│ OUTPUT FORMAT:                                                               │
│   Education data for {StudentName}:                                          │
│   === {ClassName} ===                                                        │
│   - {AssignmentName}                                                         │
│       Deadline: {OVERDUE by N days | Due TODAY | N days left}                │
│       Grade: {X/Y points | Not submitted | Submitted, awaiting grade}        │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                             get_advice                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ TRIGGER: "advice", "recommend", "suggest", "should focus", "how to help"     │
│ PARAMETERS:                                                                  │
│   - education_data (required): Raw education data to analyze                 │
│   - advice_for (required): "student" | "parent" | "teacher"                  │
│   - specific_question (optional): Focus area from user                       │
│ RETURNS: "" (empty - streams directly to Realtime for TTS)                   │
│ CALLS: ChatClient.CompleteChatStreamingAsync() (GPT-5-mini)                  │
├─────────────────────────────────────────────────────────────────────────────┤
│ STREAMING BEHAVIOR:                                                          │
│   - Sentences detected by FindSentenceEnd()                                  │
│   - Each sentence sent via SendStreamingSentenceToRealtimeAsync()            │
│   - Realtime API converts to speech immediately (low latency)                │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                            switch_student                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│ TRIGGER: User requests viewing different child's data                        │
│ PARAMETERS:                                                                  │
│   - student_identifier (required): "A" | "B" | "C" | "{StudentName}"         │
│ RETURNS: Success message with confirmation                                   │
│ SIDE EFFECTS:                                                                │
│   - Update _currentStudentId, _currentStudentDisplayName                     │
│   - Clear _classNameToIdMap, _assignmentNameToIdMap                          │
│   - Pre-load classes for new student                                         │
│   - Fire OnStudentSwitched event → UI updates sidebar                        │
│   - Re-send session.update with new [ACTIVE] marker                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Tool Definition JSON Schema

```json
// Code Location: Lines 336-391
{
  "tools": [
    {
      "type": "function",
      "name": "get_education_data",
      "description": "Fetch education data for the current student...",
      "parameters": { "type": "object", "properties": {}, "required": [] }
    },
    {
      "type": "function",
      "name": "get_advice",
      "description": "Get detailed personalized educational advice...",
      "parameters": {
        "type": "object",
        "properties": {
          "education_data": { "type": "string", "description": "..." },
          "advice_for": { "type": "string", "description": "..." },
          "specific_question": { "type": "string", "description": "..." }
        },
        "required": ["education_data", "advice_for"]
      }
    },
    {
      "type": "function",
      "name": "switch_student",
      "description": "Switch to another student. Available: A) Sophia, B) Lucy...",
      "parameters": {
        "type": "object",
        "properties": {
          "student_identifier": { "type": "string", "description": "Option letter (A, B, C) or student name" }
        },
        "required": ["student_identifier"]
      }
    }
  ]
}
```

---

## 9. Streaming Response Architecture

### 9.1 GPT-5-mini Streaming for Advice

```
+==============================================================================+
|                 STREAMING ADVICE GENERATION ARCHITECTURE                      |
+==============================================================================+
|                                                                              |
|  PROBLEM: Non-streaming advice takes 5-10 seconds → user waits silently      |
|  SOLUTION: Stream sentence-by-sentence to Realtime API for immediate TTS     |
|                                                                              |
|  LATENCY COMPARISON:                                                         |
|  ┌────────────────────────────────────────────────────────────────────────┐  |
|  │ NON-STREAMING (old approach):                                          │  |
|  │ User asks ─────►[GPT-5-mini generates full response]─────► TTS ───► 🔊 │  |
|  │                 ◄──────── 5-10 seconds ────────────►                    │  |
|  │                                                                        │  |
|  │ STREAMING (current approach):                                          │  |
|  │ User asks ─► [Sentence 1] ─► TTS ─► 🔊                                  │  |
|  │             ◄─ 1-2 sec ──►         (user hears immediately)            │  |
|  │         ─► [Sentence 2] ─► TTS ─► 🔊                                    │  |
|  │         ─► [Sentence 3] ─► TTS ─► 🔊                                    │  |
|  │         ...                                                            │  |
|  └────────────────────────────────────────────────────────────────────────┘  |
|                                                                              |
+==============================================================================+
```

### 9.2 Sentence Boundary Detection

```csharp
// Code Location: Lines 913-957
private static int FindSentenceEnd(string text)
{
    // Detects sentence boundaries for streaming
    // Handles: . ! ? and paragraph breaks
    // Avoids: numbered lists (1. 2. 3.)
}
```

**Detection Rules:**

| Pattern | Action | Example |
|---------|--------|---------|
| `.` + space + uppercase | Split | "Good. The next..." → ["Good.", "The next..."] |
| `.` + newline | Split | "Done.\n" → ["Done."] |
| `!` or `?` | Split | "Great!" → ["Great!"] |
| `\n\n` | Split | Paragraph break |
| `1.` (digit + period) | NO split | "1. First item" stays together |

### 9.3 Streaming to Realtime API

```csharp
// Code Location: Lines 963-1005
private async Task SendStreamingSentenceToRealtimeAsync(string sentence, bool isFirst)
{
    // Step 1: Create text message in conversation
    var textItem = new {
        type = "conversation.item.create",
        item = new {
            type = "message",
            role = "assistant",
            content = new[] { new { type = "text", text = sentence } }
        }
    };
    await SendJsonAsync(textItem);

    // Step 2: Request Realtime to generate audio
    var responseCreate = new {
        type = "response.create",
        response = new {
            modalities = new[] { "audio" },
            instructions = "Read the latest message naturally..."
        }
    };
    await SendJsonAsync(responseCreate);
}
```

---

## 10. State Management

### 10.1 Service State Variables

```
+==============================================================================+
|                         STATE MANAGEMENT                                      |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│                        CONNECTION STATE                                      │
├───────────────────────────┬─────────────────────────────────────────────────┤
│ _webSocket                │ ClientWebSocket? - null when disconnected       │
│ _receiveCts               │ CancellationTokenSource? - controls receive loop│
│ _receiveTask              │ Task? - background receive loop                 │
│ IsConnected (property)    │ _webSocket?.State == WebSocketState.Open        │
└───────────────────────────┴─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         CONTEXT STATE                                        │
├───────────────────────────┬─────────────────────────────────────────────────┤
│ _currentParentId          │ Azure AD user ID of logged-in parent            │
│ _currentParentDisplayName │ Display name for system prompt                  │
│ _currentStudentId         │ Currently selected student (default: first)     │
│ _currentStudentDisplayName│ Student name for UI and prompts                 │
│ _cachedStudents           │ List<StudentProfile> - all students for parent  │
└───────────────────────────┴─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                          CACHE STATE                                         │
├───────────────────────────┬─────────────────────────────────────────────────┤
│ _classNameToIdMap         │ Dictionary<string, string> - "Math" → "class123"│
│ _assignmentNameToIdMap    │ Dictionary<string, (string, string)> - name→IDs │
│ _pendingFunctionCalls     │ Dictionary - tracks streaming function args     │
└───────────────────────────┴─────────────────────────────────────────────────┘
```

### 10.2 State Transitions

```
                      SetContextAsync()
  ┌─────────────┐    ┌─────────────────────────────────────────┐
  │   INITIAL   │───►│ _currentParentId = parentId              │
  │ (all null)  │    │ _currentParentDisplayName = displayName  │
  └─────────────┘    │ _cachedStudents = [fetched from Graph]   │
                     │ _currentStudentId = first student        │
                     └─────────────────────────────────────────┘
                                       │
                                       ▼
                            ┌─────────────────┐
                            │  CONTEXT READY  │
                            └────────┬────────┘
                                     │ ConnectAsync()
                                     ▼
                            ┌─────────────────┐
                            │   CONNECTED     │
                            │ _webSocket.Open │
                            └────────┬────────┘
                                     │
           ┌─────────────────────────┼─────────────────────────┐
           ▼                         ▼                         ▼
  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
  │ SendAudioAsync  │     │ ProcessMessage  │     │ ExecuteTool     │
  │ (audio → Azure) │     │ (Azure → events)│     │ (modify state)  │
  └─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                           │
                                                           ▼
                                            switch_student modifies:
                                            - _currentStudentId
                                            - _currentStudentDisplayName
                                            - Clears _classNameToIdMap
                                            - Fires OnStudentSwitched
```

---

## 11. Event-Driven Communication

### 11.1 Event Definitions

```csharp
// Code Location: Lines 63-75
public event Action<byte[]>? OnAudioReceived;           // AI audio for playback
public event Action<string, bool>? OnTranscriptReceived; // (text, isUser)
public event Action<string>? OnError;                    // Error messages
public event Action<string, string>? OnStudentSwitched;  // (studentId, studentName)
public event Action? OnPlaybackInterrupted;              // Cancel audio playback
```

### 11.2 Event Flow Diagram

```
+==============================================================================+
|                    EVENT-DRIVEN COMMUNICATION FLOW                            |
+==============================================================================+

  REALTIMEVOICESERVICE                              VOICEMODE.RAZOR.CS
         │                                                   │
         │                                                   │
         │══════════════════════════════════════════════════►│
         │            OnAudioReceived(byte[])                │
         │                                                   │
         │                                       ┌───────────┴───────────┐
         │                                       │ HandleAudioReceived   │
         │                                       │ JS.InvokeVoidAsync    │
         │                                       │ ("voiceRealtime      │
         │                                       │   .playAudio", base64)│
         │                                       └───────────────────────┘
         │                                                   │
         │══════════════════════════════════════════════════►│
         │         OnTranscriptReceived(text, isUser)        │
         │                                                   │
         │                                       ┌───────────┴───────────┐
         │                                       │HandleTranscriptReceived│
         │                                       │ if (isUser)            │
         │                                       │   _lastUserText = text │
         │                                       │ else                   │
         │                                       │   _lastAiText = text   │
         │                                       │ StateHasChanged()      │
         │                                       └───────────────────────┘
         │                                                   │
         │══════════════════════════════════════════════════►│
         │             OnStudentSwitched(id, name)           │
         │                                                   │
         │                                       ┌───────────┴───────────┐
         │                                       │ HandleStudentSwitched │
         │                                       │ OnStudentChanged      │
         │                                       │   .InvokeAsync(...)   │
         │                                       │ → Parent component    │
         │                                       │   updates sidebar     │
         │                                       └───────────────────────┘
         │                                                   │
         │══════════════════════════════════════════════════►│
         │             OnPlaybackInterrupted()               │
         │                                                   │
         │                                       ┌───────────┴───────────┐
         │                                       │HandlePlaybackInterrupt│
         │                                       │ JS.InvokeVoidAsync    │
         │                                       │ ("voiceRealtime      │
         │                                       │   .clearPlayback")   │
         │                                       └───────────────────────┘
         │                                                   │
         │══════════════════════════════════════════════════►│
         │               OnError(message)                    │
         │                                                   │
         │                                       ┌───────────┴───────────┐
         │                                       │ HandleError           │
         │                                       │ _statusText = message │
         │                                       │ StateHasChanged()     │
         │                                       └───────────────────────┘
```

---

## 12. Error Handling Patterns

### 12.1 Error Categories

```
+==============================================================================+
|                         ERROR HANDLING PATTERNS                               |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│                      CONNECTION ERRORS                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ Location: ConnectAsync() Lines 139-161                                       │
│ Pattern: try/catch → OnError?.Invoke() → throw                               │
│                                                                              │
│ Scenarios:                                                                   │
│  - Invalid endpoint URL                                                      │
│  - Invalid API key (401 Unauthorized)                                        │
│  - Network timeout                                                           │
│  - Azure service unavailable                                                 │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                      RECEIVE LOOP ERRORS                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ Location: ReceiveLoopAsync() Lines 421-430                                   │
│ Pattern: try/catch → OnError?.Invoke() → continue loop                       │
│                                                                              │
│ Scenarios:                                                                   │
│  - OperationCanceledException: Normal disconnect, break loop                 │
│  - WebSocketException: Connection dropped, notify and continue               │
│  - JsonException: Malformed message, log and continue                        │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                      MESSAGE PROCESSING ERRORS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│ Location: ProcessMessageAsync() Lines 443-531                                │
│ Pattern: try/catch → silent ignore (log in debug)                            │
│                                                                              │
│ Reason: Individual message failures shouldn't break session                  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                      TOOL EXECUTION ERRORS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ Location: ExecuteToolAsync() Lines 623-645                                   │
│ Pattern: try/catch → return error string → AI explains to user               │
│                                                                              │
│ Example: "Error executing get_education_data: Graph API timeout"             │
│ → AI: "I'm sorry, I couldn't fetch the data. Please try again."              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 12.2 Graceful Degradation

```csharp
// Pattern: Return error as tool output, let AI handle user communication
catch (Exception ex)
{
    _logger.LogError(ex, "Tool execution failed: {FuncName}", funcName);
    return $"Error executing {funcName}: {ex.Message}";
}
// AI receives error and responds naturally: "I'm having trouble accessing that information..."
```

---

## 13. Security Considerations

### 13.1 Security Analysis

```
+==============================================================================+
|                    SECURITY ANALYSIS                                          |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│ CRITICAL: HARDCODED API KEY                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│ Location: Line 95                                                            │
│                                                                              │
│ var adviceApiKey = "FbyVcIh4UvFa...AAAAACOG9KH8";                            │
│                                                                              │
│ RISK: API key exposed in source code                                         │
│ IMPACT: Anyone with code access can use your Azure OpenAI quota              │
│                                                                              │
│ REMEDIATION:                                                                 │
│  1. Move to Azure Key Vault: KeyVaultClient.GetSecret("advice-api-key")      │
│  2. Or environment variable: Environment.GetEnvironmentVariable("...")       │
│  3. Or appsettings.json (not committed): configuration["AzureOpenAIAdvice:   │
│     ApiKey"]                                                                 │
│  4. Rotate the exposed key immediately                                       │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ LOW: API KEY IN HEADER                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ Location: Line 143                                                           │
│                                                                              │
│ _webSocket.Options.SetRequestHeader("api-key", _apiKey);                     │
│                                                                              │
│ ANALYSIS: This is acceptable for server-side code                            │
│ - Key comes from appsettings.json (should be protected)                      │
│ - WebSocket connection is wss:// (encrypted)                                 │
│ - Server-to-server communication (not exposed to browser)                    │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ MEDIUM: INPUT VALIDATION                                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ CURRENT: Limited validation on tool parameters                               │
│                                                                              │
│ switch_student:                                                              │
│  - Validates: student_identifier exists                                      │
│  - Validates: matches cached students by letter or name                      │
│  ✓ No injection risk (uses dictionary lookup)                                │
│                                                                              │
│ get_advice:                                                                  │
│  - education_data: passed to GPT-5-mini as context                           │
│  - advice_for: validated against whitelist (student/parent/teacher)          │
│  - specific_question: user-provided, sent to AI                              │
│  ⚠ Potential prompt injection via specific_question (low risk)               │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 13.2 Security Recommendations

| Priority | Issue | Remediation |
|----------|-------|-------------|
| **P0** | Hardcoded API key (Line 95) | Move to Key Vault or env var |
| **P1** | No rate limiting | Add per-session call limits |
| **P2** | Prompt injection risk | Sanitize user inputs |
| **P3** | Logging sensitive data | Redact API keys in logs |

---

## 14. Performance Analysis

### 14.1 Latency Breakdown

```
+==============================================================================+
|                    LATENCY ANALYSIS                                           |
+==============================================================================+

  USER SPEAKS                                           USER HEARS RESPONSE
       │                                                        ▲
       │ 1. Audio Capture (JS)                                  │
       │    ~50ms (buffer fill)                                 │
       ▼                                                        │
  ┌─────────────────────────────────────────────────────────────┼───────────────┐
  │                                                             │               │
  │  2. WebSocket Send                                          │               │
  │     ~10-50ms (network)                                      │               │
  │                                                             │               │
  │  3. Azure Processing                               7. Audio Playback        │
  │     ~100-300ms (VAD, Whisper)                         ~50ms (JS)           │
  │                                                             │               │
  │  4. AI Generation                                  6. WebSocket Receive     │
  │     ~200-500ms (GPT response)                         ~10-50ms             │
  │                                                             │               │
  │  5. TTS Generation                                          │               │
  │     ~100-200ms                                              │               │
  │                                                             │               │
  └─────────────────────────────────────────────────────────────────────────────┘

  TOTAL TYPICAL LATENCY: 500-1200ms (0.5-1.2 seconds)

  WITH FUNCTION CALLING:
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  + Tool Execution                                                           │
  │    get_education_data: +200-500ms (Graph API calls)                         │
  │    get_advice: +2000-5000ms (GPT-5-mini, but STREAMED)                      │
  │    switch_student: +100-200ms (context switch)                              │
  └─────────────────────────────────────────────────────────────────────────────┘
```

### 14.2 Performance Optimizations in Code

| Optimization | Location | Impact |
|--------------|----------|--------|
| **Streaming advice** | Lines 751-905 | Reduces perceived latency by 80% |
| **Sentence boundary detection** | Lines 913-957 | Enables incremental TTS |
| **Cached students** | Line 50 | Avoids repeated Graph API calls |
| **Class name cache** | Line 46 | Fast lookup without API call |
| **Assignment name cache** | Line 48 | Fast lookup without API call |
| **Pre-load classes on switch** | Lines 1062-1073 | Ready for next query |

---

## 15. Best Practices Comparison

### 15.1 Azure OpenAI Realtime API Best Practices

| Best Practice | Implementation Status | Notes |
|---------------|----------------------|-------|
| Use `session.update` for configuration | ✅ Implemented | Lines 250-329 |
| Handle streaming function arguments | ✅ Implemented | 3-phase protocol |
| Implement interruption handling | ✅ Implemented | `response.cancel` + event |
| Use Server VAD for natural conversation | ✅ Implemented | threshold=0.8, 800ms silence |
| Close WebSocket gracefully | ✅ Implemented | `NormalClosure` status |
| Handle multi-frame messages | ✅ Implemented | `!result.EndOfMessage` loop |

### 15.2 Function Calling Best Practices

| Best Practice | Implementation Status | Notes |
|---------------|----------------------|-------|
| Use `tools` parameter (not deprecated `functions`) | ✅ Implemented | Modern API |
| Handle multiple function calls | ⚠️ Partial | Dictionary supports it, but AI typically calls one |
| Return errors as tool output | ✅ Implemented | AI handles user communication |
| Use JSON schema for parameters | ✅ Implemented | Lines 346-391 |
| Set tool_choice appropriately | ❌ Not used | Could add `tool_choice: "auto"` |

### 15.3 WebSocket Best Practices

| Best Practice | Implementation Status | Notes |
|---------------|----------------------|-------|
| Use CancellationToken for shutdown | ✅ Implemented | `_receiveCts` |
| Handle Close message type | ✅ Implemented | Line 414 |
| Use background task for receive | ✅ Implemented | `_receiveTask` |
| Implement IDisposable | ✅ Implemented | Lines 1096-1101 |
| Buffer management | ✅ Implemented | 4KB buffer, message accumulator |

---

## 16. Quick Reference

### 16.1 Method Reference Card

```
+==============================================================================+
|                    METHOD QUICK REFERENCE                                     |
+==============================================================================+

PUBLIC METHODS:
───────────────────────────────────────────────────────────────────────────────
│ Method                         │ Purpose                                     │
├────────────────────────────────┼─────────────────────────────────────────────┤
│ SetContextAsync(parentId, name)│ Initialize parent/student context           │
│ ConnectAsync()                 │ Establish WebSocket, start receive loop     │
│ SendAudioAsync(byte[])         │ Forward audio chunk to Azure                │
│ DisconnectAsync()              │ Graceful shutdown                           │
│ Dispose()                      │ Resource cleanup                            │
───────────────────────────────────────────────────────────────────────────────

PRIVATE METHODS - SESSION:
───────────────────────────────────────────────────────────────────────────────
│ BuildContextStringAsync()      │ Build parent/student info for system prompt │
│ SendSessionConfigAsync()       │ Send tools, VAD settings, instructions      │
│ GetEducationToolDefinitions()  │ Return tool JSON schemas                    │
───────────────────────────────────────────────────────────────────────────────

PRIVATE METHODS - MESSAGE PROCESSING:
───────────────────────────────────────────────────────────────────────────────
│ ReceiveLoopAsync(token)        │ Background loop for incoming messages       │
│ ProcessMessageAsync(msg, sb)   │ Route message by type to handlers           │
│ SendJsonAsync(object)          │ Serialize and send JSON over WebSocket      │
───────────────────────────────────────────────────────────────────────────────

PRIVATE METHODS - FUNCTION CALLING:
───────────────────────────────────────────────────────────────────────────────
│ SavePendingFunctionCall(root)  │ Phase 1: Save call_id and function name     │
│ AccumulateFunctionArguments()  │ Phase 2: Accumulate streaming arguments     │
│ ExecutePendingFunctionCallAsync│ Phase 3: Execute tool with complete args    │
│ ExecuteToolAsync(name, args)   │ Dispatch to specific tool implementation    │
───────────────────────────────────────────────────────────────────────────────

PRIVATE METHODS - TOOLS:
───────────────────────────────────────────────────────────────────────────────
│ ExecuteGetEducationDataAsync() │ Fetch classes, assignments, grades          │
│ ExecuteGetAdviceAsync(args)    │ Stream advice from GPT-5-mini               │
│ ExecuteSwitchStudentAsync(args)│ Change active student context               │
───────────────────────────────────────────────────────────────────────────────

PRIVATE METHODS - STREAMING:
───────────────────────────────────────────────────────────────────────────────
│ FindSentenceEnd(text)          │ Detect sentence boundaries for streaming    │
│ SendStreamingSentenceToRealtime│ Send sentence to Realtime for TTS           │
───────────────────────────────────────────────────────────────────────────────
```

### 16.2 Configuration Reference

```json
// appsettings.json structure
{
  "AzureOpenAIRealtime": {
    "Endpoint": "wss://{resource}.openai.azure.com/openai/realtime",
    "DeploymentName": "gpt-4o-realtime-preview",
    "ApiKey": "{api-key}",
    "ApiVersion": "2024-10-01-preview"
  },
  "AzureOpenAIAdvice": {
    "Endpoint": "https://{resource}.openai.azure.com/",
    "DeploymentName": "gpt-5-mini",
    "ApiKey": "{api-key}"  // ⚠️ Currently hardcoded - should be here
  }
}
```

### 16.3 Event Reference

```
EVENT                        │ TRIGGER                      │ HANDLER ACTION
─────────────────────────────┼──────────────────────────────┼────────────────────────
OnAudioReceived(byte[])      │ AI speaks (audio delta)      │ Play in browser via JS
OnTranscriptReceived(s,bool) │ User/AI speech transcribed   │ Update UI transcript
OnError(string)              │ Any error occurs             │ Display error message
OnStudentSwitched(id,name)   │ switch_student succeeds      │ Update sidebar
OnPlaybackInterrupted()      │ User interrupts AI           │ Clear audio queue
```

---

## Document References

### Internal Documents

| Document | Link | Description |
|----------|------|-------------|
| EXPLAIN-v1 | [EXPLAIN-v1-technology-deep-analysis.md](./EXPLAIN-v1-technology-deep-analysis.md) | Technology stack analysis |
| Over-v1 | [Over-v1-architecture-analysis.md](./Over-v1-architecture-analysis.md) | Architecture overview |

### External References

| Title | URL | Relevance |
|-------|-----|-----------|
| Azure OpenAI Realtime API WebSocket | [Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/realtime-audio-websockets) | Protocol documentation |
| OpenAI Realtime API Guide | [OpenAI Platform](https://platform.openai.com/docs/guides/realtime) | API reference |
| Voice Activity Detection | [OpenAI VAD Guide](https://platform.openai.com/docs/guides/realtime-vad) | VAD configuration |
| Azure OpenAI Function Calling | [Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/function-calling) | Function calling patterns |
| OpenAI Client Events | [OpenAI API Reference](https://platform.openai.com/docs/api-reference/realtime-client-events) | Event types reference |

---

*Document generated: 2026-01-12*
*Source: RealtimeVoiceService.cs (1102 lines)*
*Analysis depth: Deep dive with research*
