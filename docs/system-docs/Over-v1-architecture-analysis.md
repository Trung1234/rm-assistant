# EDU_MUL_AGENTS - Architecture Analysis Report

**Document ID**: Over-v1-architecture-analysis
**Created**: 2026-01-11
**Version**: v1
**Status**: Complete

---

## Table of Contents

1. [Overview Architecture](#1-overview-architecture)
2. [Component Relationship Diagram](#2-component-relationship-diagram)
3. [Service Registration Matrix](#3-service-registration-matrix-mece)
4. [Workflow Diagrams](#4-workflow-diagrams)
   - [Workflow 1: Authentication & Authorization Flow](#workflow-1-authentication--authorization-flow)
   - [Workflow 2: Voice Conversation Flow](#workflow-2-voice-conversation-flow)
   - [Workflow 3: Education Data Retrieval Flow](#workflow-3-education-data-retrieval-flow)
   - [Workflow 4: Advice Generation Flow (Streaming)](#workflow-4-advice-generation-flow-streaming)
   - [Workflow 5: Student Switching Flow](#workflow-5-student-switching-flow)
5. [Domain Model Relationships](#5-domain-model-relationships)
6. [Voice Tools Summary](#6-voice-tools-summary)
7. [Technology Decision Matrix](#7-technology-decision-matrix-mece)
8. [Quick Reference Card](#8-quick-reference-card)

---

## 1. Overview Architecture

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    EDU_MUL_AGENTS STACK ARCHITECTURE                              ║
+══════════════════════════════════════════════════════════════════════════════════+
║                                                                                   ║
║  USER LAYER (Browser)                                                             ║
║  ─────────────────────                                                            ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Browser (Chrome/Edge/Firefox)                                               │ ║
║  │  • voiceRealtime.js (Microphone capture, PCM16 encoding, Audio playback)    │ ║
║  │  • Blazor Server SignalR (Real-time UI updates)                             │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │ SignalR WebSocket                          ║
║                                     ▼                                             ║
║  WEB LAYER (edu_mul_agents.Web)                                                  ║
║  ─────────────────────────────────                                               ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  Blazor Server (.NET 8 LTS)                                                  │ ║
║  │  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐ ┌───────────────────┐│ ║
║  │  │ ParentPortal- │ │   VoiceMode   │ │  ChatSidebar  │ │ ParentAuthHandler ││ ║
║  │  │   Chat.razor  │ │     .razor    │ │    .razor     │ │   (Authorization) ││ ║
║  │  └───────┬───────┘ └───────┬───────┘ └───────┬───────┘ └─────────┬─────────┘│ ║
║  │          │                 │                 │                   │          │ ║
║  │  ┌───────┴─────────────────┴─────────────────┴───────────────────┴────────┐ │ ║
║  │  │                        Program.cs (DI Container)                        │ │ ║
║  │  │  Services: ICurrentUserService, IStudentService, IRealtimeVoiceService  │ │ ║
║  │  │  Auth: Microsoft Entra ID (OpenIDConnect) + "ParentOnly" Policy        │ │ ║
║  │  └────────────────────────────────────────────────────────────────────────┘ │ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │ Dependency Injection                       ║
║                                     ▼                                             ║
║  APPLICATION CORE LAYER (edu_mul_agents.ApplicationCore)                         ║
║  ────────────────────────────────────────────────────────                        ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  ★ NO EXTERNAL DEPENDENCIES (Clean Architecture Core)                       │ ║
║  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────────────┐│ ║
║  │  │   Interfaces/   │ │     Models/     │ │          MockData/              ││ ║
║  │  │ IStudentService │ │ StudentProfile  │ │  parents.json                   ││ ║
║  │  │ IRealtimeVoice  │ │ Assignment      │ │  parent-student-mapping.json    ││ ║
║  │  │ ICurrentUser    │ │ EducationClass  │ │  MockDataProvider.cs            ││ ║
║  │  │ IParentAuth     │ │ AssignmentGrade │ │                                 ││ ║
║  │  └─────────────────┘ └─────────────────┘ └─────────────────────────────────┘│ ║
║  └──────────────────────────────────┬──────────────────────────────────────────┘ ║
║                                     │ Implements Interfaces                      ║
║                                     ▼                                             ║
║  INFRASTRUCTURE LAYER (edu_mul_agents.Infrastructure)                            ║
║  ─────────────────────────────────────────────────────                           ║
║  ┌─────────────────────────────────────────────────────────────────────────────┐ ║
║  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────────────┐│ ║
║  │  │  StudentService │ │ RealtimeVoice   │ │    EducationGraphClient        ││ ║
║  │  │ (Graph API)     │ │ Service         │ │    Factory (Singleton)         ││ ║
║  │  │                 │ │ (WebSocket)     │ │    Client Credentials Flow     ││ ║
║  │  └────────┬────────┘ └────────┬────────┘ └─────────────┬───────────────────┘│ ║
║  └───────────┼───────────────────┼────────────────────────┼────────────────────┘ ║
║              │                   │                        │                      ║
║              ▼                   ▼                        ▼                      ║
║  EXTERNAL SERVICES                                                               ║
║  ─────────────────                                                               ║
║  ┌─────────────────┐ ┌─────────────────────────┐ ┌─────────────────────────────┐ ║
║  │ Microsoft Graph │ │ Azure OpenAI Realtime   │ │ Azure OpenAI (GPT-5-mini)   │ ║
║  │ Education API   │ │ (WebSocket, VAD)        │ │ (Advice Generation)         │ ║
║  │ /education/*    │ │ wss://...realtime       │ │ https://...chat             │ ║
║  └─────────────────┘ └─────────────────────────┘ └─────────────────────────────┘ ║
║                                                                                   ║
+══════════════════════════════════════════════════════════════════════════════════+

DATA FLOW SUMMARY:
  ┌────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
  │ Parent │───▶│ Entra ID │───▶│ Blazor   │───▶│ Graph    │───▶│ Student  │
  │ Login  │    │ Auth     │    │ Server   │    │ API      │    │ Data     │
  └────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘

  ┌────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
  │ Voice  │───▶│ Browser  │───▶│ WebSocket│───▶│ Azure    │───▶│ AI       │
  │ Input  │    │ JS Audio │    │ PCM16    │    │ Realtime │    │ Response │
  └────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘

KEY METRICS:
  Projects: 3          Services: 5          Voice Tools: 3
  Blazor Pages: 3      Models: 6            External APIs: 3
```

---

## 2. Component Relationship Diagram

```
+══════════════════════════════════════════════════════════════════════════════════+
║                      COMPONENT DEPENDENCY GRAPH (MECE)                            ║
+══════════════════════════════════════════════════════════════════════════════════+

                              ┌─────────────────────────┐
                              │    EXTERNAL SYSTEMS     │
                              ├─────────────────────────┤
                              │ • Microsoft Entra ID    │
                              │ • Microsoft Graph API   │
                              │ • Azure OpenAI Realtime │
                              │ • Azure OpenAI Chat     │
                              └────────────┬────────────┘
                                           │
        ┌──────────────────────────────────┼──────────────────────────────────┐
        │                                  │                                  │
        ▼                                  ▼                                  ▼
┌───────────────────┐           ┌───────────────────┐           ┌───────────────────┐
│EducationGraph-   │           │ RealtimeVoice-    │           │ CurrentUser-      │
│ ClientFactory    │           │ Service           │           │ Service           │
├───────────────────┤           ├───────────────────┤           ├───────────────────┤
│ ClientCredential │           │ WebSocket Mgmt    │           │ GraphServiceClient│
│ Token Cache      │           │ Tool Execution    │           │ User Profile      │
│ Graph Client     │           │ Audio Streaming   │           │ Claims Extraction │
└────────┬──────────┘           └────────┬──────────┘           └────────┬──────────┘
         │                               │                               │
         │  uses                         │ uses                          │
         ▼                               ▼                               │
┌───────────────────┐           ┌───────────────────┐                    │
│ StudentService    │◀──────────│ (Tool Handler)    │                    │
├───────────────────┤           │ get_education_data│                    │
│ GetClasses()      │           │ switch_student    │                    │
│ GetAssignments()  │           │ get_advice        │                    │
│ GetGrades()       │           └───────────────────┘                    │
│ GetTeachers()     │                                                    │
└────────┬──────────┘                                                    │
         │                                                               │
         │  reads                                                        │
         ▼                                                               │
┌───────────────────┐                                                    │
│ MockDataProvider  │◀───────────────────────────────────────────────────┤
├───────────────────┤                                                    │
│ parents.json      │           ┌───────────────────┐                    │
│ parent-student-   │◀──────────│ ParentAuth-       │                    │
│  mapping.json     │           │ orizationService  │                    │
└───────────────────┘           └────────┬──────────┘                    │
                                         │                               │
                                         │ used by                       │
                                         ▼                               │
                                ┌───────────────────┐                    │
                                │ ParentAuth-       │                    │
                                │ orizationHandler  │                    │
                                ├───────────────────┤                    │
                                │ Checks "oid" claim│                    │
                                │ Enforces policy   │                    │
                                └────────┬──────────┘                    │
                                         │                               │
                                         │ protects                      │
                                         ▼                               │
                    ┌────────────────────────────────────────────────────┘
                    │
                    ▼
┌════════════════════════════════════════════════════════════════════════════════┐
│                           BLAZOR COMPONENTS (UI LAYER)                          │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │  ParentPortalChat.razor (@page "/")                                      │  │
│  │  ┌─────────────────────┐  ┌─────────────────────────────────────────────┐│  │
│  │  │                     │  │                                             ││  │
│  │  │   ChatSidebar       │  │   [Text Mode]          [Voice Mode]         ││  │
│  │  │   ────────────      │  │   ┌───────────────┐   ┌───────────────────┐ ││  │
│  │  │   • Parent Profile  │  │   │ ChatHeader    │   │ VoiceMode.razor   │ ││  │
│  │  │   • Student List    │  │   │ ChatMessages  │   │ • Live Transcript │ ││  │
│  │  │   • StudentProfile  │  │   │ ChatInput     │   │ • Audio Stream    │ ││  │
│  │  │     Modal           │  │   │   [Voice Btn] │   │ • End Call Btn    │ ││  │
│  │  │                     │  │   └───────────────┘   └───────────────────┘ ││  │
│  │  └─────────────────────┘  └─────────────────────────────────────────────┘│  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                                │
│  ┌────────────────────────────┐  ┌────────────────────────────────────────┐   │
│  │  AccessDenied.razor        │  │  Error.razor                          │   │
│  │  (@page "/access-denied")  │  │  (@page "/Error")                     │   │
│  └────────────────────────────┘  └────────────────────────────────────────┘   │
│                                                                                │
└════════════════════════════════════════════════════════════════════════════════┘
```

---

## 3. Service Registration Matrix (MECE)

```
+══════════════════════════════════════════════════════════════════════════════════+
║                    SERVICE REGISTRATION (Program.cs) - MECE                       ║
+══════════════════════════════════════════════════════════════════════════════════+

┌─────────────────────────┬─────────────────────────┬───────────┬─────────────────┐
│ SERVICE INTERFACE       │ IMPLEMENTATION          │ LIFETIME  │ PURPOSE         │
├─────────────────────────┼─────────────────────────┼───────────┼─────────────────┤
│ ICurrentUserService     │ CurrentUserService      │ Scoped    │ Get parent      │
│                         │                         │           │ profile from    │
│                         │                         │           │ Graph /me       │
├─────────────────────────┼─────────────────────────┼───────────┼─────────────────┤
│ IParentAuthorizationSvc │ ParentAuthorizationSvc  │ Scoped    │ Check user in   │
│                         │                         │           │ parents.json    │
├─────────────────────────┼─────────────────────────┼───────────┼─────────────────┤
│ IStudentService         │ StudentService          │ Scoped    │ Fetch student   │
│                         │                         │           │ education data  │
├─────────────────────────┼─────────────────────────┼───────────┼─────────────────┤
│ IRealtimeVoiceService   │ RealtimeVoiceService    │ Scoped    │ WebSocket to    │
│                         │                         │           │ Azure Realtime  │
├─────────────────────────┼─────────────────────────┼───────────┼─────────────────┤
│ (Factory)               │ EducationGraphClient-   │ Singleton │ Graph client    │
│                         │ Factory                 │           │ w/ token cache  │
├─────────────────────────┼─────────────────────────┼───────────┼─────────────────┤
│ IAuthorizationHandler   │ ParentAuthorization-    │ Scoped    │ Enforce Parent- │
│                         │ Handler                 │           │ Only policy     │
└─────────────────────────┴─────────────────────────┴───────────┴─────────────────┘

AUTHORIZATION POLICY:
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │  "ParentOnly" Policy                                                        │
  │  ├── Requirement: ParentRequirement (marker class)                         │
  │  └── Handler: ParentAuthorizationHandler                                    │
  │       ├── Extract "oid" claim from Microsoft Entra ID                      │
  │       ├── Call IParentAuthorizationService.IsAuthorizedParentAsync()       │
  │       └── Succeed/Fail based on MockData parents.json                      │
  └─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Workflow Diagrams

### Workflow 1: Authentication & Authorization Flow

```
+══════════════════════════════════════════════════════════════════════════════════+
║              WORKFLOW 1: AUTHENTICATION & AUTHORIZATION FLOW                      ║
+══════════════════════════════════════════════════════════════════════════════════+

  Parent               Browser              Blazor Server         Entra ID        MockData
    │                     │                      │                    │              │
    │  1. Navigate to "/" │                      │                    │              │
    │ ────────────────────>                      │                    │              │
    │                     │  2. Check Auth State │                    │              │
    │                     │ ─────────────────────>                    │              │
    │                     │                      │  3. Redirect to    │              │
    │                     │                      │     Login          │              │
    │                     │ <════════════════════════════════════════>│              │
    │                     │                      │                    │              │
    │  4. Enter Credentials                      │                    │              │
    │ ────────────────────────────────────────────────────────────────>              │
    │                     │                      │                    │              │
    │  5. Receive ID Token + Access Token        │                    │              │
    │ <════════════════════════════════════════════════════════════════              │
    │                     │                      │                    │              │
    │                     │  6. Send Token       │                    │              │
    │                     │ ─────────────────────>                    │              │
    │                     │                      │                    │              │
    │                     │                      │  7. Extract "oid"  │              │
    │                     │                      │     claim          │              │
    │                     │                      │ ─────────────────────────────────>│
    │                     │                      │                    │              │
    │                     │                      │  8. Check parents.json             │
    │                     │                      │ <─────────────────────────────────│
    │                     │                      │                    │              │
    │                     │                      │                    │              │
    │                     │         ┌────────────┴────────────┐       │              │
    │                     │         │ Is User in parents.json?│       │              │
    │                     │         └────────────┬────────────┘       │              │
    │                     │                      │                    │              │
    │            ┌────────┴──────────────────────┼──────────────────────────────────┐│
    │            │                               │                                  ││
    │            ▼ YES                           ▼ NO                               ││
    │   ┌─────────────────────┐         ┌─────────────────────┐                     ││
    │   │  9a. Render         │         │  9b. Redirect to    │                     ││
    │   │  ParentPortalChat   │         │  /access-denied     │                     ││
    │   │  (Home Page)        │         │                     │                     ││
    │   └─────────────────────┘         └─────────────────────┘                     ││
    │            │                               │                                  ││
    └────────────┴───────────────────────────────┴──────────────────────────────────┘│

KEYWORDS:
  • OpenIDConnect           • "oid" claim (Object ID)
  • ParentRequirement       • ParentAuthorizationHandler
  • IParentAuthorizationService → MockDataProvider
  • AccessDeniedPath = "/access-denied"
```

---

### Workflow 2: Voice Conversation Flow

```
+══════════════════════════════════════════════════════════════════════════════════+
║                     WORKFLOW 2: VOICE CONVERSATION FLOW                           ║
+══════════════════════════════════════════════════════════════════════════════════+

  Parent        Browser JS       VoiceMode.razor.cs    RealtimeVoice-     Azure OpenAI
    │               │                   │              Service              Realtime
    │               │                   │                  │                   │
    │ 1. Click      │                   │                  │                   │
    │    Voice Mode │                   │                  │                   │
    │ ──────────────────────────────────>                  │                   │
    │               │                   │                  │                   │
    │               │ 2. OnAfterRender  │                  │                   │
    │               │    (firstRender)  │                  │                   │
    │               │ <─────────────────│                  │                   │
    │               │                   │                  │                   │
    │               │                   │ 3. SetContext-   │                   │
    │               │                   │    Async(parent) │                   │
    │               │                   │ ─────────────────>                   │
    │               │                   │                  │ [Cache students]  │
    │               │                   │                  │                   │
    │               │                   │ 4. ConnectAsync()│                   │
    │               │                   │ ─────────────────>                   │
    │               │                   │                  │ 5. WebSocket      │
    │               │                   │                  │    Connect        │
    │               │                   │                  │ ══════════════════>
    │               │                   │                  │                   │
    │               │                   │                  │ 6. session.update │
    │               │                   │                  │    (tools, VAD,   │
    │               │                   │                  │     instructions) │
    │               │                   │                  │ ══════════════════>
    │               │                   │                  │                   │
    │               │ 7. voiceRealtime  │                  │                   │
    │               │    .start()       │                  │                   │
    │               │ <─────────────────│                  │                   │
    │               │                   │                  │                   │
    │ 8. Speak      │ 9. Capture Audio  │                  │                   │
    │   "What are   │    (PCM16, 24kHz) │                  │                   │
    │    my child's │ ─────────────────>│                  │                   │
    │    grades?"   │                   │                  │                   │
    │               │                   │ 10. ReceiveAudio │                   │
    │               │                   │     Chunk        │                   │
    │               │                   │ ─────────────────>                   │
    │               │                   │                  │ 11. input_audio   │
    │               │                   │                  │     _buffer.append│
    │               │                   │                  │ ══════════════════>
    │               │                   │                  │                   │
    │               │                   │                  │ 12. VAD detects   │
    │               │                   │                  │     speech end    │
    │               │                   │                  │ <══════════════════
    │               │                   │                  │                   │
    │               │                   │                  │ 13. Function Call │
    │               │                   │                  │     get_education │
    │               │                   │                  │     _data         │
    │               │                   │                  │ <══════════════════
    │               │                   │                  │                   │
    │               │                   │                  │ 14. ExecuteTool   │
    │               │                   │                  │     → StudentSvc  │
    │               │                   │                  │ ─────────────────>│
    │               │                   │                  │                   │
    │               │                   │                  │ 15. Tool Result   │
    │               │                   │                  │     (grades data) │
    │               │                   │                  │ ══════════════════>
    │               │                   │                  │                   │
    │               │                   │                  │ 16. Audio Response│
    │               │                   │                  │     (AI speaking) │
    │               │                   │                  │ <══════════════════
    │               │                   │                  │                   │
    │               │                   │ 17. OnAudio-     │                   │
    │               │                   │     Received     │                   │
    │               │                   │ <─────────────────                   │
    │               │                   │                  │                   │
    │               │ 18. playAudio     │                  │                   │
    │ 19. Hear      │     (PCM16→speak) │                  │                   │
    │    Response   │ <─────────────────│                  │                   │
    │               │                   │                  │                   │

KEYWORDS:
  • Server VAD (Voice Activity Detection)    • PCM16, 24kHz, mono
  • input_audio_buffer.append                • response.audio.delta
  • Function Calling (get_education_data)    • conversation.item.create
  • DotNetObjectReference (JS Interop)       • voiceRealtime.js
```

---

### Workflow 3: Education Data Retrieval Flow

```
+══════════════════════════════════════════════════════════════════════════════════+
║                  WORKFLOW 3: EDUCATION DATA RETRIEVAL FLOW                        ║
+══════════════════════════════════════════════════════════════════════════════════+

  Voice Tool        RealtimeVoice-     StudentService    GraphClient      Microsoft
  Handler           Service                              Factory          Graph API
    │                   │                   │                │                │
    │ 1. get_education  │                   │                │                │
    │    _data trigger  │                   │                │                │
    │ ─────────────────>│                   │                │                │
    │                   │                   │                │                │
    │                   │ 2. ExecuteGet-    │                │                │
    │                   │    EducationData  │                │                │
    │                   │ ─────────────────>│                │                │
    │                   │                   │                │                │
    │                   │                   │ 3. GetClient() │                │
    │                   │                   │ ───────────────>                │
    │                   │                   │                │ [Token cached?]│
    │                   │                   │                │      │         │
    │                   │                   │    ┌───────────┴──────┴────────┐│
    │                   │                   │    │ NO: ClientCredential Flow ││
    │                   │                   │    │ POST /oauth2/v2.0/token   ││
    │                   │                   │    │ ─────────────────────────>││
    │                   │                   │    │ YES: Use cached token     ││
    │                   │                   │    └───────────┬───────────────┘│
    │                   │                   │                │                │
    │                   │                   │ 4. GET /education/users/{id}/  │
    │                   │                   │    classes                      │
    │                   │                   │ ────────────────────────────────>
    │                   │                   │                │                │
    │                   │                   │ 5. Classes List                 │
    │                   │                   │ <────────────────────────────────
    │                   │                   │                │                │
    │                   │                   │ [For each class...]             │
    │                   │                   │                │                │
    │                   │                   │ 6. GET /education/classes/{id}/│
    │                   │                   │    assignments?$orderby=due... │
    │                   │                   │ ────────────────────────────────>
    │                   │                   │                │                │
    │                   │                   │ 7. Assignments List             │
    │                   │                   │ <────────────────────────────────
    │                   │                   │                │                │
    │                   │                   │ [For each assignment...]        │
    │                   │                   │                │                │
    │                   │                   │ 8. GET /education/classes/{id}/│
    │                   │                   │    assignments/{id}/submissions│
    │                   │                   │    ?$filter=submittedBy...     │
    │                   │                   │    &$expand=outcomes            │
    │                   │                   │ ────────────────────────────────>
    │                   │                   │                │                │
    │                   │                   │ 9. Grade + Feedback             │
    │                   │                   │ <────────────────────────────────
    │                   │                   │                │                │
    │                   │ 10. Formatted     │                │                │
    │                   │     Education Data│                │                │
    │                   │ <─────────────────│                │                │
    │                   │                   │                │                │
    │ 11. Tool Result   │                   │                │                │
    │     (String)      │                   │                │                │
    │ <─────────────────│                   │                │                │
    │                   │                   │                │                │

KEYWORDS:
  • Application Permissions (not Delegated)   • Client Credentials Flow
  • EduRoster.Read, EduAssignments.Read      • $expand=outcomes
  • EducationPointsOutcome                    • EducationFeedbackOutcome
  • PublishedPoints (graded score)            • Graph SDK v4 syntax
```

---

### Workflow 4: Advice Generation Flow (Streaming)

```
+══════════════════════════════════════════════════════════════════════════════════+
║              WORKFLOW 4: ADVICE GENERATION FLOW (GPT-5-mini STREAMING)            ║
+══════════════════════════════════════════════════════════════════════════════════+

  Voice Tool      RealtimeVoice-      ChatClient         Azure OpenAI     Azure Realtime
  Handler         Service             (GPT-5-mini)       (gpt-5-mini)     (voice TTS)
    │                 │                    │                  │                │
    │ 1. get_advice   │                    │                  │                │
    │    triggered    │                    │                  │                │
    │    (user asked  │                    │                  │                │
    │     explicitly) │                    │                  │                │
    │ ───────────────>│                    │                  │                │
    │                 │                    │                  │                │
    │                 │ 2. Build prompts   │                  │                │
    │                 │    (advice_for:    │                  │                │
    │                 │     student/parent/│                  │                │
    │                 │     teacher)       │                  │                │
    │                 │                    │                  │                │
    │                 │ 3. CompleteChatStreamingAsync()       │                │
    │                 │ ──────────────────>│                  │                │
    │                 │                    │ 4. Stream Request│                │
    │                 │                    │ ─────────────────>                │
    │                 │                    │                  │                │
    │                 │                    │ 5. Token by Token│                │
    │                 │                    │ <═════════════════                │
    │                 │                    │ (async foreach)  │                │
    │                 │                    │                  │                │
    │                 │ 6. Accumulate      │                  │                │
    │                 │    sentence buffer │                  │                │
    │                 │ <──────────────────│                  │                │
    │                 │                    │                  │                │
    │                 │    ┌───────────────┴───────────────┐  │                │
    │                 │    │ Sentence boundary detected?   │  │                │
    │                 │    │ (. ! ? followed by space+cap) │  │                │
    │                 │    └───────────────┬───────────────┘  │                │
    │                 │                    │                  │                │
    │                 │    YES             │                  │                │
    │                 │    ▼               │                  │                │
    │                 │ 7. conversation.item.create           │                │
    │                 │    (type: message, role: assistant)   │                │
    │                 │ ────────────────────────────────────────────────────────>
    │                 │                    │                  │                │
    │                 │ 8. response.create │                  │                │
    │                 │    (modalities: ["audio"])            │                │
    │                 │ ────────────────────────────────────────────────────────>
    │                 │                    │                  │                │
    │                 │                    │                  │ 9. Audio TTS   │
    │                 │                    │                  │    response    │
    │                 │ <═══════════════════════════════════════════════════════
    │                 │                    │                  │                │
    │                 │ [Repeat 6-9 for each sentence...]     │                │
    │                 │                    │                  │                │
    │                 │                    │ 10. Stream End   │                │
    │                 │                    │ <─────────────────                │
    │                 │                    │                  │                │
    │ 11. Return ""   │                    │                  │                │
    │     (already    │                    │                  │                │
    │      streamed)  │                    │                  │                │
    │ <───────────────│                    │                  │                │

KEYWORDS:
  • GPT-5-mini (o4-mini reasoning)            • CompleteChatStreamingAsync
  • Sentence boundary detection               • FindSentenceEnd()
  • conversation.item.create (text)           • response.create (audio TTS)
  • Streaming reduces perceived latency       • MaxOutputTokenCount = 16000
  • advice_for: student | parent | teacher
```

---

### Workflow 5: Student Switching Flow

```
+══════════════════════════════════════════════════════════════════════════════════+
║                      WORKFLOW 5: STUDENT SWITCHING FLOW                           ║
+══════════════════════════════════════════════════════════════════════════════════+

  Parent Voice    RealtimeVoice-    VoiceMode.razor.cs   ParentPortal-    ChatSidebar
  Command         Service                                 Chat.razor       .razor
    │                 │                    │                  │                │
    │ "Switch to      │                    │                  │                │
    │  option B" or   │                    │                  │                │
    │ "Switch to Lucy"│                    │                  │                │
    │ ───────────────>│                    │                  │                │
    │                 │                    │                  │                │
    │                 │ 1. switch_student  │                  │                │
    │                 │    function call   │                  │                │
    │                 │    received from   │                  │                │
    │                 │    Azure           │                  │                │
    │                 │                    │                  │                │
    │                 │ 2. Parse identifier│                  │                │
    │                 │    ┌───────────────┴───────────────┐  │                │
    │                 │    │ Match by Letter? (A/B/C)      │  │                │
    │                 │    │ OR Match by Name? (contains)  │  │                │
    │                 │    └───────────────┬───────────────┘  │                │
    │                 │                    │                  │                │
    │                 │ 3. Update internal │                  │                │
    │                 │    context:        │                  │                │
    │                 │    _currentStudentId                  │                │
    │                 │    _currentStudent │                  │                │
    │                 │    DisplayName     │                  │                │
    │                 │                    │                  │                │
    │                 │ 4. Clear class/    │                  │                │
    │                 │    assignment cache│                  │                │
    │                 │                    │                  │                │
    │                 │ 5. Pre-load new    │                  │                │
    │                 │    student's classes                  │                │
    │                 │                    │                  │                │
    │                 │ 6. OnStudentSwitched.Invoke           │                │
    │                 │    (studentId, name)                  │                │
    │                 │ ───────────────────>                  │                │
    │                 │                    │                  │                │
    │                 │                    │ 7. HandleStudent │                │
    │                 │                    │    Switched()    │                │
    │                 │                    │ ─────────────────>                │
    │                 │                    │                  │                │
    │                 │                    │                  │ 8. HandleStudent│
    │                 │                    │                  │    ChangedFrom │
    │                 │                    │                  │    Voice()     │
    │                 │                    │                  │ ───────────────>│
    │                 │                    │                  │                │
    │                 │                    │                  │                │ 9. SelectStudent
    │                 │                    │                  │                │    ById()
    │                 │                    │                  │                │ ─────────────┐
    │                 │                    │                  │                │              │
    │                 │                    │                  │                │ 10. Update   │
    │                 │                    │                  │                │     "Selected"
    │                 │                    │                  │                │     badge    │
    │                 │                    │                  │                │ <────────────┘
    │                 │                    │                  │                │
    │                 │ 11. SendSession-   │                  │                │
    │                 │     ConfigAsync()  │                  │                │
    │                 │     (new [ACTIVE]) │                  │                │
    │                 │ ═══════════════════════════════════════════════════════>
    │                 │                    │                  │                │
    │ 12. Return      │                    │                  │                │
    │     success msg │                    │                  │                │
    │ <───────────────│                    │                  │                │
    │                 │                    │                  │                │
    │ 13. AI confirms │                    │                  │                │
    │     "Switched   │                    │                  │                │
    │      to Lucy"   │                    │                  │                │
    │ <═══════════════│                    │                  │                │

KEYWORDS:
  • switch_student tool                       • student_identifier parameter
  • Letter matching (A, B, C)                 • Name matching (contains)
  • OnStudentSwitched event                   • EventCallback cascade
  • chatSidebarRef?.SelectStudentById()       • Dynamic session.update
  • [ACTIVE] marker in instructions
```

---

## 5. Domain Model Relationships

```
+══════════════════════════════════════════════════════════════════════════════════+
║                       DOMAIN MODEL RELATIONSHIPS (ER-Style)                       ║
+══════════════════════════════════════════════════════════════════════════════════+

                    ┌─────────────────────────────────────────────────────────────┐
                    │                     MOCK DATA LAYER                          │
                    │  (JSON Files - To be replaced with Database)                │
                    │                                                             │
  ┌─────────────────┴─────────────────┐     ┌───────────────────────────────────┐│
  │        parents.json               │     │   parent-student-mapping.json     ││
  │  ┌─────────────────────────────┐  │     │  ┌─────────────────────────────┐  ││
  │  │ parentUserIds: string[]    │  │ 1:N │  │ mappings: [{               │  ││
  │  │   • UUID1                  │──┼─────┼──│   parentUserId: string,    │  ││
  │  │   • UUID2                  │  │     │  │   studentIds: string[]     │  ││
  │  │   • UUID3                  │  │     │  │ }]                         │  ││
  │  └─────────────────────────────┘  │     │  └─────────────────────────────┘  ││
  └───────────────────────────────────┘     └───────────────────────────────────┘│
                    │                                      │                      │
                    └──────────────────┬───────────────────┘                      │
                                       │                                          │
                    ┌──────────────────┴──────────────────────────────────────────┘
                    │                     DOMAIN MODELS (Graph API)
                    │
  ┌─────────────────┴─────────────────┐
  │           UserProfile             │◀───── GET /me (Delegated)
  ├───────────────────────────────────┤
  │ Id: string (oid)                  │       ┌───────────────────────────────┐
  │ DisplayName: string               │       │        StudentProfile         │
  │ Email: string                     │ 1:N   │  (Parent's Children)          │
  │ UserPrincipalName: string         │───────├───────────────────────────────┤
  │ JobTitle, Department, Office...   │       │ Id: string                    │
  └───────────────────────────────────┘       │ DisplayName: string           │
                                              │ Email: string                 │
                                              │ Grade: string (optional)      │
                                              │ SchoolName: string (optional) │
                                              └─────────────┬─────────────────┘
                                                            │
                    ┌───────────────────────────────────────┴───────────────────┐
                    │                                                           │
                    ▼                                                           ▼
  ┌─────────────────────────────────┐                     ┌─────────────────────────────────┐
  │       EducationClass            │◀── GET /education   │         Teacher                 │
  │  (Student's Enrolled Classes)   │    /users/{id}/    │  (Class Instructors)            │
  ├─────────────────────────────────┤    classes          ├─────────────────────────────────┤
  │ Id: string                      │                     │ Id: string                      │
  │ DisplayName: string             │ N:M                 │ DisplayName: string             │
  │ Description: string             │─────────────────────│ Email: string                   │
  │ ClassCode: string               │ GET /education/     │ MobilePhone: string             │
  │ MailNickname: string            │ classes/{id}/       └─────────────────────────────────┘
  └─────────────┬───────────────────┘ teachers
                │
                │ 1:N
                ▼
  ┌─────────────────────────────────┐
  │         Assignment              │◀── GET /education/classes/{id}/assignments
  ├─────────────────────────────────┤
  │ Id: string                      │
  │ DisplayName: string             │
  │ Instructions: string (HTML)     │
  │ DueDateTime: DateTime?          │
  │ Status: string                  │
  │ ClassName: string (joined)      │
  │ ClassId: string (joined)        │
  │ MaxPoints: int? (optional)      │
  └─────────────┬───────────────────┘
                │
                │ 1:1 (per student)
                ▼
  ┌─────────────────────────────────┐
  │       AssignmentGrade           │◀── GET /education/classes/{id}/assignments/{id}/
  │  (Student's Submission)         │    submissions?$filter=submittedBy...&$expand=outcomes
  ├─────────────────────────────────┤
  │ SubmissionId: string            │
  │ AssignmentId: string            │
  │ Status: string (working/        │
  │         submitted/returned)     │
  │ Points: decimal? (earned)       │◀── EducationPointsOutcome
  │ MaxPoints: int? (possible)      │
  │ Feedback: string?               │◀── EducationFeedbackOutcome
  │ SubmittedDateTime: DateTime?    │
  │ ReturnedDateTime: DateTime?     │
  └─────────────────────────────────┘

CARDINALITY LEGEND:
  1:N  = One to Many
  N:M  = Many to Many (via join)
  ◀──  = Data source indicator
```

---

## 6. Voice Tools Summary

```
+══════════════════════════════════════════════════════════════════════════════════+
║                         VOICE TOOLS SUMMARY (3 Tools)                             ║
+══════════════════════════════════════════════════════════════════════════════════+

┌────────────────────┬───────────────────────────────────────────────────────────────┐
│ TOOL NAME          │ DESCRIPTION & BEHAVIOR                                        │
├────────────────────┼───────────────────────────────────────────────────────────────┤
│                    │                                                               │
│ get_education_data │ AUTO-FETCH all education data for current student             │
│                    │                                                               │
│                    │ • No parameters required                                      │
│                    │ • Returns: Classes → Assignments → Grades in readable format │
│                    │ • Includes: Deadline status (OVERDUE/Due today/X days left)  │
│                    │ • Includes: Teacher feedback if available                    │
│                    │                                                               │
│                    │ TRIGGERS:                                                     │
│                    │   "What are my child's grades?"                              │
│                    │   "Show me homework status"                                   │
│                    │   "List assignments"                                          │
│                    │                                                               │
├────────────────────┼───────────────────────────────────────────────────────────────┤
│                    │                                                               │
│ get_advice         │ Get personalized educational advice from GPT-5-mini           │
│                    │                                                               │
│                    │ • Parameters:                                                 │
│                    │   - education_data (required): Output from get_education_data │
│                    │   - advice_for (required): "student" | "parent" | "teacher"  │
│                    │   - specific_question (optional): Focus area                  │
│                    │                                                               │
│                    │ • STREAMING: Sends sentences to Realtime for immediate TTS   │
│                    │ • Reduces perceived latency (user hears first sentence fast) │
│                    │                                                               │
│                    │ TRIGGERS (explicit only):                                     │
│                    │   "What advice do you have?"                                  │
│                    │   "How can I help my child improve?"                          │
│                    │   "What should Lucy focus on?"                                │
│                    │                                                               │
├────────────────────┼───────────────────────────────────────────────────────────────┤
│                    │                                                               │
│ switch_student     │ Switch to view another student's information                  │
│                    │                                                               │
│                    │ • Parameters:                                                 │
│                    │   - student_identifier: Letter (A/B/C) OR name (partial)     │
│                    │                                                               │
│                    │ • Actions after switch:                                       │
│                    │   1. Update _currentStudentId/_currentStudentDisplayName     │
│                    │   2. Clear class/assignment cache                            │
│                    │   3. Pre-load new student's classes                          │
│                    │   4. Fire OnStudentSwitched event → UI syncs                 │
│                    │   5. SendSessionConfigAsync() → AI gets new context          │
│                    │                                                               │
│                    │ TRIGGERS:                                                     │
│                    │   "Switch to option B"                                        │
│                    │   "Show me Lucy's grades"                                     │
│                    │   "Change to my other child"                                  │
│                    │                                                               │
└────────────────────┴───────────────────────────────────────────────────────────────┘
```

---

## 7. Technology Decision Matrix (MECE)

```
+══════════════════════════════════════════════════════════════════════════════════+
║                      TECHNOLOGY DECISION MATRIX                                   ║
+══════════════════════════════════════════════════════════════════════════════════+

┌─────────────────┬─────────────────────┬───────────────────────────────────────────┐
│ CONCERN         │ CHOICE              │ RATIONALE                                 │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Runtime         │ .NET 8 LTS          │ Long-term support, Blazor Server native   │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ UI Framework    │ Blazor Server       │ Real-time SignalR built-in, C# everywhere │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ CSS Framework   │ Bootstrap 5         │ Responsive, well-documented, no build step│
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Authentication  │ Microsoft Entra ID  │ Enterprise SSO, tenant-based, Graph scopes│
│                 │ (OpenIDConnect)     │                                           │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Authorization   │ Custom Policy +     │ MockData-based for v1, easy to swap to DB │
│                 │ Handler             │                                           │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Data Source     │ Microsoft Graph API │ Native to Microsoft 365 Education tenants │
│ (Education)     │ (Education Roster)  │                                           │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Graph Client    │ Application         │ Parent ≠ class member, can't use Delegated│
│ Auth            │ Permissions         │ permissions for /education/* endpoints    │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Voice AI        │ Azure OpenAI        │ Real-time voice-to-voice, Server VAD,     │
│                 │ Realtime API        │ function calling built-in                 │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Advice Engine   │ GPT-5-mini (o4-mini)│ Reasoning model for educational insights  │
│                 │                     │ STREAMING for low latency                 │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Audio Format    │ PCM16, 24kHz, mono  │ Required by Azure OpenAI Realtime API     │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ JS Interop      │ voiceRealtime.js    │ Browser audio capture/playback (Web Audio)│
│                 │ + DotNetObjectRef   │                                           │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Mock Data       │ JSON files +        │ v1 prototype, easy to replace with EF Core│
│                 │ Static provider     │                                           │
├─────────────────┼─────────────────────┼───────────────────────────────────────────┤
│ Hosting         │ Azure App Service   │ Managed .NET, easy deployment from VS     │
└─────────────────┴─────────────────────┴───────────────────────────────────────────┘
```

---

## 8. Quick Reference Card

```
+══════════════════════════════════════════════════════════════════════════════════+
║                              QUICK REFERENCE CARD                                 ║
+══════════════════════════════════════════════════════════════════════════════════+

PROJECT STRUCTURE:
─────────────────────────────────────────────────────────────────────────────────────
  src/
  ├── edu_mul_agents.Web/             ← UI Layer (Blazor, Auth, Program.cs)
  ├── edu_mul_agents.ApplicationCore/ ← Business Logic (NO dependencies)
  └── edu_mul_agents.Infrastructure/  ← External Services (Graph, Azure)

KEY FILES:
─────────────────────────────────────────────────────────────────────────────────────
  Program.cs                    ← Service registration, auth config
  IRealtimeVoiceService.cs      ← Voice service contract
  RealtimeVoiceService.cs       ← WebSocket + tool execution (1100 LOC)
  StudentService.cs             ← Graph API calls (390 LOC)
  VoiceMode.razor.cs            ← Browser ↔ .NET bridge

AUTHENTICATION:
─────────────────────────────────────────────────────────────────────────────────────
  Provider:     Microsoft Entra ID (OpenIDConnect)
  Claim:        "oid" (Object ID) → checked against parents.json
  Policy:       "ParentOnly" → enforced on all Razor components
  Access Token: For Graph API (/me, /users/{id})

VOICE TOOLS (3):
─────────────────────────────────────────────────────────────────────────────────────
  get_education_data    → Fetch all classes/assignments/grades
  get_advice            → GPT-5-mini streaming advice
  switch_student        → Change active student context

GRAPH API ENDPOINTS:
─────────────────────────────────────────────────────────────────────────────────────
  /me                                        → Current user profile (Delegated)
  /users/{id}                                → Student profile (App)
  /education/users/{id}/classes              → Student's classes (App)
  /education/classes/{id}/assignments        → Class assignments (App)
  /education/classes/{id}/assignments/
    {id}/submissions?$expand=outcomes        → Grades + feedback (App)
  /education/classes/{id}/teachers           → Class instructors (App)

RUN LOCALLY:
─────────────────────────────────────────────────────────────────────────────────────
  cd src/edu_mul_agents.Web
  dotnet run
  → https://localhost:5001

EVENTS FLOW (Voice Mode):
─────────────────────────────────────────────────────────────────────────────────────
  OnAudioReceived       ← Azure sends PCM16 → play in browser
  OnTranscriptReceived  ← User/AI text → update Live Transcript
  OnStudentSwitched     ← switch_student tool → sync UI sidebar
  OnPlaybackInterrupted ← User speaks → stop AI audio
  OnError               ← WebSocket/tool errors → show in UI
```

---

## Appendix A: File Statistics

| Project | File Count | Lines of Code (approx) | Primary Purpose |
|---------|-----------|------------------------|-----------------|
| **edu_mul_agents.Web** | 34 files | ~2,500 LOC | UI components, HTTP, authentication |
| **edu_mul_agents.ApplicationCore** | 14 files | ~800 LOC | Interfaces, models, mock data |
| **edu_mul_agents.Infrastructure** | 8 files | ~1,700 LOC | Service implementations |
| **Total** | 56 files | ~5,000 LOC | Complete 3-layer .NET 8 application |

---

## Appendix B: Configuration Files

### appsettings.json Structure

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "{tenant-id}",
    "ClientId": "{client-id}",
    "ClientSecret": "{client-secret}",
    "CallbackPath": "/signin-oidc"
  },
  "Graph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read User.Read.All EduRoster.Read EduAssignments.Read"
  },
  "AzureOpenAIRealtime": {
    "Endpoint": "wss://{resource}.openai.azure.com/openai/realtime",
    "DeploymentName": "gpt-realtime-mini",
    "ApiKey": "{api-key}",
    "ApiVersion": "2024-10-01-preview"
  },
  "AzureOpenAIAdvice": {
    "Endpoint": "https://{resource}.cognitiveservices.azure.com/",
    "DeploymentName": "gpt-5-mini",
    "ApiKey": "{api-key}"
  }
}
```

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1 | 2026-01-11 | Claude Code Analysis | Initial architecture analysis |

---

*Generated by Claude Code Architecture Analysis*
