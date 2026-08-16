# IMPL-v1: Python Implementation Plan for edu_mul_agents

**Created**: 2026-01-12
**Based On**: Over-v1, EXPLAIN-v1, EXPLAIN-v2
**Target Stack**: Python + Azure AI
**Status**: Implementation Plan

---

## 1. Executive Summary

This document provides a comprehensive plan to reimplement the `edu_mul_agents` Educational Multi-Agents Portal from .NET 8/Blazor to Python + Azure AI. The plan maintains Clean Architecture principles while leveraging Python's ecosystem for web development, real-time communication, and AI integration.

```
+==============================================================================+
|                    IMPLEMENTATION OVERVIEW                                    |
+==============================================================================+
|                                                                              |
|  CURRENT (.NET 8)              →    TARGET (Python)                         |
|  ─────────────────                  ─────────────────                        |
|  Blazor Server                 →    FastAPI + Jinja2 / React SPA            |
|  SignalR                       →    WebSocket (FastAPI) / Socket.IO         |
|  Microsoft.Graph.SDK           →    msgraph-sdk-python                      |
|  Microsoft.Identity.Web        →    msal-python + FastAPI-Users             |
|  Azure.AI.OpenAI               →    openai-python SDK                       |
|  ClientWebSocket               →    websockets / aiohttp                    |
|  .NET DI Container             →    dependency-injector / FastAPI Depends   |
|                                                                              |
+==============================================================================+
```

---

## 2. Technology Stack Decision Matrix

### 2.1 Core Framework Options

| Component | Option A (Recommended) | Option B | Option C |
|-----------|------------------------|----------|----------|
| **Web Framework** | FastAPI | Flask | Django |
| **Pros** | Async native, OpenAPI docs, type hints | Simple, mature | Full-featured ORM |
| **Cons** | Newer ecosystem | Sync-first | Heavy for this use case |
| **Decision** | **Selected** - Best fit for async WebSocket + API | - | - |

### 2.2 Selected Python Tech Stack

```
+==============================================================================+
|                    PYTHON TECH STACK                                          |
+==============================================================================+

┌─────────────────────────────────────────────────────────────────────────────┐
│  LAYER            │  TECHNOLOGY           │  PURPOSE                        │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Web Framework    │  FastAPI 0.109+       │  Async API + WebSocket support  │
│  ASGI Server      │  Uvicorn              │  Production ASGI server         │
│  Template Engine  │  Jinja2               │  Server-side rendering          │
│  Frontend Alt     │  React/Vue SPA        │  Client-side rendering option   │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Authentication   │  msal-python          │  Microsoft Entra ID OAuth       │
│  Session Mgmt     │  fastapi-session      │  Cookie-based sessions          │
│  Authorization    │  FastAPI Dependencies │  Custom policy handlers         │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Graph API        │  msgraph-sdk-python   │  Microsoft Graph SDK v1.0       │
│  HTTP Client      │  httpx / aiohttp      │  Async HTTP requests            │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Azure OpenAI     │  openai-python 1.x    │  Chat completions + streaming   │
│  WebSocket Client │  websockets           │  Azure Realtime API connection  │
│  Audio Processing │  numpy + struct       │  PCM16 audio handling           │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Real-time UI     │  FastAPI WebSocket    │  Browser ↔ Server bidirectional │
│  Alternative      │  python-socketio      │  Socket.IO protocol             │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Data Validation  │  Pydantic v2          │  Request/response models        │
│  Configuration    │  pydantic-settings    │  Environment-based config       │
│  Logging          │  structlog            │  Structured logging             │
├───────────────────┼───────────────────────┼─────────────────────────────────┤
│  Testing          │  pytest + pytest-asyncio │ Unit + integration tests     │
│  Type Checking    │  mypy                 │  Static type analysis           │
│  Linting          │  ruff                 │  Fast Python linter             │
└───────────────────┴───────────────────────┴─────────────────────────────────┘
```

### 2.3 Package Requirements (requirements.txt)

```
# Core Web Framework
fastapi>=0.109.0
uvicorn[standard]>=0.27.0
python-multipart>=0.0.6
jinja2>=3.1.2

# Authentication
msal>=1.26.0
python-jose[cryptography]>=3.3.0
fastapi-sessions>=0.0.6

# Microsoft Graph
msgraph-sdk>=1.0.0
azure-identity>=1.15.0

# Azure OpenAI
openai>=1.10.0
websockets>=12.0
aiohttp>=3.9.0

# Data & Validation
pydantic>=2.5.0
pydantic-settings>=2.1.0

# Async Support
httpx>=0.26.0
anyio>=4.2.0

# Utilities
structlog>=24.1.0
python-dotenv>=1.0.0

# Development
pytest>=8.0.0
pytest-asyncio>=0.23.0
mypy>=1.8.0
ruff>=0.1.0
```

---

## 3. Architecture Design

### 3.1 Clean Architecture Mapping

```
+==============================================================================+
|                    CLEAN ARCHITECTURE (Python)                                |
+==============================================================================+

  ┌─────────────────────────────────────────────────────────────────────────┐
  │                         API LAYER                                        │
  │                    (app/api/)                                            │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
  │  │   Routers       │  │    WebSocket    │  │   Templates     │          │
  │  │  (FastAPI)      │  │    Endpoints    │  │   (Jinja2)      │          │
  │  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘          │
  └───────────┼────────────────────┼────────────────────┼───────────────────┘
              │                    │                    │
              ▼                    ▼                    ▼
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                    DOMAIN LAYER                                          │
  │                    (app/domain/)                                         │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
  │  │   Interfaces    │  │     Models      │  │    MockData     │          │
  │  │  (Protocols)    │  │   (Pydantic)    │  │   (JSON Data)   │          │
  │  └────────┬────────┘  └─────────────────┘  └─────────────────┘          │
  └───────────┼─────────────────────────────────────────────────────────────┘
              │  ▲ Dependency Inversion
              ▼  │
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                    INFRASTRUCTURE LAYER                                  │
  │                    (app/infrastructure/)                                 │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
  │  │ StudentService  │  │  Graph Client   │  │ RealtimeVoice   │          │
  │  │                 │  │    Factory      │  │    Service      │          │
  │  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘          │
  └───────────┼────────────────────┼────────────────────┼───────────────────┘
              │                    │                    │
              ▼                    ▼                    ▼
  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
  │  MockData JSON  │  │ Microsoft Graph │  │ Azure OpenAI    │
  │   (v1 Data)     │  │      API        │  │  Realtime API   │
  └─────────────────┘  └─────────────────┘  └─────────────────┘
```

### 3.2 Project Structure

```
edu_mul_agents_python/
├── app/
│   ├── __init__.py
│   ├── main.py                    # FastAPI app factory
│   ├── config.py                  # Pydantic Settings
│   │
│   ├── api/                       # API Layer
│   │   ├── __init__.py
│   │   ├── deps.py                # Dependency injection
│   │   ├── routers/
│   │   │   ├── __init__.py
│   │   │   ├── auth.py            # OAuth endpoints
│   │   │   ├── students.py        # Student data endpoints
│   │   │   └── chat.py            # Chat/text endpoints
│   │   ├── websockets/
│   │   │   ├── __init__.py
│   │   │   └── voice.py           # Voice WebSocket handler
│   │   └── templates/
│   │       ├── base.html
│   │       ├── login.html
│   │       ├── portal.html
│   │       └── components/
│   │           ├── sidebar.html
│   │           ├── chat.html
│   │           └── voice.html
│   │
│   ├── domain/                    # Domain Layer (NO external deps)
│   │   ├── __init__.py
│   │   ├── models/
│   │   │   ├── __init__.py
│   │   │   ├── student.py         # StudentProfile, Assignment, etc.
│   │   │   ├── user.py            # UserProfile
│   │   │   ├── education.py       # EducationClass, Teacher
│   │   │   └── grade.py           # AssignmentGrade
│   │   ├── interfaces/
│   │   │   ├── __init__.py
│   │   │   ├── current_user.py    # ICurrentUserService protocol
│   │   │   ├── authorization.py   # IParentAuthorizationService
│   │   │   ├── student.py         # IStudentService protocol
│   │   │   └── voice.py           # IRealtimeVoiceService protocol
│   │   └── mockdata/
│   │       ├── __init__.py
│   │       ├── provider.py        # MockDataProvider
│   │       ├── parents.json
│   │       └── parent-student-mapping.json
│   │
│   └── infrastructure/            # Infrastructure Layer
│       ├── __init__.py
│       ├── services/
│       │   ├── __init__.py
│       │   ├── current_user.py    # CurrentUserService
│       │   ├── authorization.py   # ParentAuthorizationService
│       │   ├── student.py         # StudentService
│       │   └── voice/
│       │       ├── __init__.py
│       │       ├── realtime.py    # RealtimeVoiceService
│       │       ├── tools.py       # Voice tool implementations
│       │       └── audio.py       # Audio encoding/decoding
│       └── clients/
│           ├── __init__.py
│           ├── graph.py           # EducationGraphClientFactory
│           └── openai.py          # Azure OpenAI client wrapper
│
├── static/
│   ├── css/
│   │   └── styles.css
│   └── js/
│       ├── voice-realtime.js      # Audio capture/playback
│       └── chat.js                # Text chat handling
│
├── tests/
│   ├── __init__.py
│   ├── conftest.py                # Pytest fixtures
│   ├── unit/
│   │   ├── test_models.py
│   │   └── test_services.py
│   └── integration/
│       ├── test_graph_api.py
│       └── test_voice_service.py
│
├── requirements.txt
├── requirements-dev.txt
├── pyproject.toml
├── .env.example
└── README.md
```

---

## 4. Domain Models Implementation

### 4.1 Pydantic Models (app/domain/models/)

```python
# app/domain/models/student.py
from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional


class StudentProfile(BaseModel):
    """Student identity and education info"""
    id: str
    display_name: str = Field(alias="displayName")
    email: Optional[str] = None
    grade: Optional[str] = None
    school_name: Optional[str] = Field(None, alias="schoolName")
    mobile_phone: Optional[str] = Field(None, alias="mobilePhone")
    external_id: Optional[str] = Field(None, alias="externalId")

    class Config:
        populate_by_name = True


class Assignment(BaseModel):
    """Homework assignment"""
    id: str
    display_name: str = Field(alias="displayName")
    instructions: Optional[str] = None
    due_date_time: Optional[datetime] = Field(None, alias="dueDateTime")
    status: Optional[str] = None
    class_name: Optional[str] = Field(None, alias="className")
    class_id: Optional[str] = Field(None, alias="classId")
    max_points: Optional[float] = Field(None, alias="maxPoints")

    class Config:
        populate_by_name = True
```

```python
# app/domain/models/user.py
from pydantic import BaseModel, Field
from typing import Optional, List


class UserProfile(BaseModel):
    """Parent/user identity and work info"""
    id: str
    display_name: str = Field(alias="displayName")
    email: Optional[str] = None
    user_principal_name: Optional[str] = Field(None, alias="userPrincipalName")
    job_title: Optional[str] = Field(None, alias="jobTitle")
    department: Optional[str] = None
    office_location: Optional[str] = Field(None, alias="officeLocation")
    company_name: Optional[str] = Field(None, alias="companyName")
    mobile_phone: Optional[str] = Field(None, alias="mobilePhone")
    business_phones: List[str] = Field(default_factory=list, alias="businessPhones")
    city: Optional[str] = None
    country: Optional[str] = None
    postal_code: Optional[str] = Field(None, alias="postalCode")
    street_address: Optional[str] = Field(None, alias="streetAddress")
    state: Optional[str] = None
    employee_id: Optional[str] = Field(None, alias="employeeId")
    preferred_language: Optional[str] = Field(None, alias="preferredLanguage")

    class Config:
        populate_by_name = True
```

```python
# app/domain/models/education.py
from pydantic import BaseModel, Field
from typing import Optional


class EducationClass(BaseModel):
    """Course/class information"""
    id: str
    display_name: str = Field(alias="displayName")
    description: Optional[str] = None
    class_code: Optional[str] = Field(None, alias="classCode")
    mail_nickname: Optional[str] = Field(None, alias="mailNickname")

    class Config:
        populate_by_name = True


class Teacher(BaseModel):
    """Instructor/teacher"""
    id: str
    display_name: str = Field(alias="displayName")
    email: Optional[str] = None
    mobile_phone: Optional[str] = Field(None, alias="mobilePhone")

    class Config:
        populate_by_name = True
```

```python
# app/domain/models/grade.py
from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional


class AssignmentGrade(BaseModel):
    """Submission grade and feedback"""
    submission_id: str = Field(alias="submissionId")
    assignment_id: str = Field(alias="assignmentId")
    status: Optional[str] = None
    points: Optional[float] = None
    max_points: Optional[float] = Field(None, alias="maxPoints")
    feedback: Optional[str] = None
    submitted_date_time: Optional[datetime] = Field(None, alias="submittedDateTime")
    returned_date_time: Optional[datetime] = Field(None, alias="returnedDateTime")

    class Config:
        populate_by_name = True
```

### 4.2 Service Interfaces (Protocols)

```python
# app/domain/interfaces/student.py
from typing import Protocol, List, Optional
from app.domain.models.student import StudentProfile, Assignment
from app.domain.models.education import EducationClass, Teacher
from app.domain.models.grade import AssignmentGrade


class IStudentService(Protocol):
    """Service to fetch student data and assignments"""

    async def get_student_ids_for_parent(self, parent_user_id: str) -> List[str]:
        """Get student IDs mapped to parent from MockData"""
        ...

    async def get_student_profile(self, student_id: str) -> Optional[StudentProfile]:
        """Get student profile from Microsoft Graph"""
        ...

    async def get_student_assignments(self, student_id: str) -> List[Assignment]:
        """Get assignments for student from Education API"""
        ...

    async def get_students_for_parent(self, parent_user_id: str) -> List[StudentProfile]:
        """Get all student profiles for a parent"""
        ...

    # Voice Tool Functions
    async def get_student_classes(self, student_id: str) -> List[EducationClass]:
        """Get classes for student (voice tool: get_classes)"""
        ...

    async def get_class_details(self, class_id: str) -> Optional[EducationClass]:
        """Get class details (voice tool: get_class_info)"""
        ...

    async def get_class_assignments(
        self, class_id: str, limit: int = 10
    ) -> List[Assignment]:
        """Get assignments for class sorted by due date"""
        ...

    async def get_assignment_grade(
        self, class_id: str, assignment_id: str, student_id: str
    ) -> Optional[AssignmentGrade]:
        """Get grade for assignment submission"""
        ...

    async def get_class_teachers(self, class_id: str) -> List[Teacher]:
        """Get teachers for class"""
        ...
```

```python
# app/domain/interfaces/voice.py
from typing import Protocol, Callable, Awaitable, Optional


class IRealtimeVoiceService(Protocol):
    """Service for Azure OpenAI Realtime voice-to-voice communication"""

    @property
    def is_connected(self) -> bool:
        """Current WebSocket connection state"""
        ...

    async def connect(self) -> None:
        """Establish WebSocket connection to Azure Realtime API"""
        ...

    async def disconnect(self) -> None:
        """Close WebSocket connection gracefully"""
        ...

    async def send_audio(self, audio_data: bytes) -> None:
        """Send PCM16 audio chunk to Azure"""
        ...

    async def set_context(self, parent_id: str, parent_display_name: str) -> None:
        """Set parent context before connecting"""
        ...

    # Event callbacks (set by consumer)
    on_audio_received: Optional[Callable[[bytes], Awaitable[None]]]
    on_transcript_received: Optional[Callable[[str, bool], Awaitable[None]]]
    on_error: Optional[Callable[[str], Awaitable[None]]]
    on_student_switched: Optional[Callable[[str, str], Awaitable[None]]]
    on_playback_interrupted: Optional[Callable[[], Awaitable[None]]]
```

---

## 5. Infrastructure Implementation

### 5.1 Graph Client Factory

```python
# app/infrastructure/clients/graph.py
from msgraph import GraphServiceClient
from azure.identity import ClientSecretCredential
from app.config import Settings
import logging

logger = logging.getLogger(__name__)


class EducationGraphClientFactory:
    """Factory for creating Graph clients with application permissions"""

    def __init__(self, settings: Settings):
        self._settings = settings
        self._client: GraphServiceClient | None = None
        self._credential: ClientSecretCredential | None = None

    def get_client(self) -> GraphServiceClient:
        """Get or create cached Graph client"""
        if self._client is None:
            self._credential = ClientSecretCredential(
                tenant_id=self._settings.azure_ad_tenant_id,
                client_id=self._settings.azure_ad_client_id,
                client_secret=self._settings.azure_ad_client_secret,
            )
            self._client = GraphServiceClient(
                credentials=self._credential,
                scopes=["https://graph.microsoft.com/.default"],
            )
            logger.info("Created new GraphServiceClient with app credentials")
        return self._client
```

### 5.2 Student Service Implementation

```python
# app/infrastructure/services/student.py
from typing import List, Optional
from msgraph.generated.models import User, EducationClass as GraphClass
from msgraph.generated.models import EducationAssignment, EducationSubmission
from app.domain.interfaces.student import IStudentService
from app.domain.models.student import StudentProfile, Assignment
from app.domain.models.education import EducationClass, Teacher
from app.domain.models.grade import AssignmentGrade
from app.domain.mockdata.provider import MockDataProvider
from app.infrastructure.clients.graph import EducationGraphClientFactory
import logging

logger = logging.getLogger(__name__)


class StudentService(IStudentService):
    """Implementation of IStudentService using Microsoft Graph Education API"""

    def __init__(
        self,
        graph_factory: EducationGraphClientFactory,
        mock_provider: MockDataProvider,
    ):
        self._graph = graph_factory.get_client()
        self._mock = mock_provider

    async def get_student_ids_for_parent(self, parent_user_id: str) -> List[str]:
        """Get student IDs from MockData mapping"""
        return self._mock.get_student_ids_for_parent(parent_user_id)

    async def get_student_profile(self, student_id: str) -> Optional[StudentProfile]:
        """Fetch student profile from Graph API"""
        try:
            user = await self._graph.users.by_user_id(student_id).get()
            if user:
                return StudentProfile(
                    id=user.id,
                    displayName=user.display_name or "Unknown",
                    email=user.mail,
                    mobilePhone=user.mobile_phone,
                )
        except Exception as e:
            logger.warning(f"Failed to get student profile {student_id}: {e}")
        return None

    async def get_student_classes(self, student_id: str) -> List[EducationClass]:
        """Fetch classes for student from Education API"""
        try:
            result = await (
                self._graph.education.users
                .by_education_user_id(student_id)
                .classes
                .get()
            )
            if result and result.value:
                return [
                    EducationClass(
                        id=cls.id,
                        displayName=cls.display_name or "Unknown Class",
                        description=cls.description,
                        classCode=cls.class_code,
                        mailNickname=cls.mail_nickname,
                    )
                    for cls in result.value
                ]
        except Exception as e:
            logger.warning(f"Failed to get classes for student {student_id}: {e}")
        return []

    async def get_class_assignments(
        self, class_id: str, limit: int = 10
    ) -> List[Assignment]:
        """Fetch assignments for class sorted by due date"""
        try:
            from msgraph.generated.education.classes.item.assignments.assignments_request_builder import (
                AssignmentsRequestBuilder,
            )

            query = AssignmentsRequestBuilder.AssignmentsRequestBuilderGetQueryParameters(
                orderby=["dueDateTime desc"],
                top=limit,
            )
            config = AssignmentsRequestBuilder.AssignmentsRequestBuilderGetRequestConfiguration(
                query_parameters=query,
            )

            result = await (
                self._graph.education.classes
                .by_education_class_id(class_id)
                .assignments
                .get(request_configuration=config)
            )

            if result and result.value:
                return [
                    Assignment(
                        id=a.id,
                        displayName=a.display_name or "Untitled",
                        instructions=self._extract_instructions(a),
                        dueDateTime=a.due_date_time,
                        status=a.status.value if a.status else None,
                        classId=class_id,
                        maxPoints=(
                            a.grading.max_points
                            if a.grading and hasattr(a.grading, "max_points")
                            else None
                        ),
                    )
                    for a in result.value
                ]
        except Exception as e:
            logger.warning(f"Failed to get assignments for class {class_id}: {e}")
        return []

    async def get_assignment_grade(
        self, class_id: str, assignment_id: str, student_id: str
    ) -> Optional[AssignmentGrade]:
        """Fetch grade for student's assignment submission"""
        try:
            # Get submissions with outcomes expanded
            result = await (
                self._graph.education.classes
                .by_education_class_id(class_id)
                .assignments
                .by_education_assignment_id(assignment_id)
                .submissions
                .get()
            )

            if result and result.value:
                # Find submission for this student
                for sub in result.value:
                    if sub.recipient and hasattr(sub.recipient, "user_id"):
                        if sub.recipient.user_id == student_id:
                            return self._extract_grade(sub, assignment_id)

        except Exception as e:
            logger.warning(f"Failed to get grade for assignment {assignment_id}: {e}")
        return None

    def _extract_instructions(self, assignment) -> Optional[str]:
        """Extract plain text from assignment instructions"""
        if assignment.instructions and assignment.instructions.content:
            # Remove HTML tags for simple display
            import re
            return re.sub(r"<[^>]+>", "", assignment.instructions.content)
        return None

    def _extract_grade(
        self, submission: EducationSubmission, assignment_id: str
    ) -> AssignmentGrade:
        """Extract grade info from submission outcomes"""
        points = None
        feedback = None

        if submission.outcomes:
            for outcome in submission.outcomes:
                # Check for points outcome
                if hasattr(outcome, "points") and outcome.points:
                    points = outcome.points.points
                # Check for feedback outcome
                if hasattr(outcome, "feedback") and outcome.feedback:
                    if outcome.feedback.text:
                        feedback = outcome.feedback.text.content

        return AssignmentGrade(
            submissionId=submission.id,
            assignmentId=assignment_id,
            status=submission.status.value if submission.status else None,
            points=points,
            feedback=feedback,
            submittedDateTime=submission.submitted_date_time,
            returnedDateTime=submission.returned_date_time,
        )
```

### 5.3 Realtime Voice Service Implementation

```python
# app/infrastructure/services/voice/realtime.py
import asyncio
import json
import base64
import logging
from typing import Optional, Callable, Awaitable, Dict, Any, List
from websockets.client import connect, WebSocketClientProtocol
from app.config import Settings
from app.domain.interfaces.voice import IRealtimeVoiceService
from app.domain.interfaces.student import IStudentService
from app.domain.models.student import StudentProfile

logger = logging.getLogger(__name__)


class RealtimeVoiceService(IRealtimeVoiceService):
    """WebSocket service for Azure OpenAI Realtime API"""

    def __init__(
        self,
        settings: Settings,
        student_service: IStudentService,
    ):
        self._settings = settings
        self._student_service = student_service
        self._ws: Optional[WebSocketClientProtocol] = None
        self._receive_task: Optional[asyncio.Task] = None
        self._is_connected = False

        # Context state
        self._parent_id: Optional[str] = None
        self._parent_display_name: Optional[str] = None
        self._current_student_id: Optional[str] = None
        self._current_student_name: Optional[str] = None
        self._cached_students: List[StudentProfile] = []

        # Function call state
        self._pending_calls: Dict[str, Dict[str, Any]] = {}

        # Event callbacks
        self.on_audio_received: Optional[Callable[[bytes], Awaitable[None]]] = None
        self.on_transcript_received: Optional[Callable[[str, bool], Awaitable[None]]] = None
        self.on_error: Optional[Callable[[str], Awaitable[None]]] = None
        self.on_student_switched: Optional[Callable[[str, str], Awaitable[None]]] = None
        self.on_playback_interrupted: Optional[Callable[[], Awaitable[None]]] = None

    @property
    def is_connected(self) -> bool:
        return self._is_connected

    async def set_context(self, parent_id: str, parent_display_name: str) -> None:
        """Set parent context and prefetch students"""
        self._parent_id = parent_id
        self._parent_display_name = parent_display_name

        # Prefetch students
        student_ids = await self._student_service.get_student_ids_for_parent(parent_id)
        self._cached_students = []
        for sid in student_ids:
            profile = await self._student_service.get_student_profile(sid)
            if profile:
                self._cached_students.append(profile)

        # Set default student
        if self._cached_students:
            self._current_student_id = self._cached_students[0].id
            self._current_student_name = self._cached_students[0].display_name

        logger.info(
            f"Context set: parent={parent_display_name}, "
            f"students={len(self._cached_students)}"
        )

    async def connect(self) -> None:
        """Connect to Azure OpenAI Realtime API"""
        if self._is_connected:
            return

        ws_url = (
            f"{self._settings.azure_openai_realtime_endpoint}"
            f"?api-version={self._settings.azure_openai_api_version}"
            f"&deployment={self._settings.azure_openai_realtime_deployment}"
        )

        headers = {
            "api-key": self._settings.azure_openai_realtime_api_key,
        }

        try:
            self._ws = await connect(ws_url, extra_headers=headers)
            self._is_connected = True
            logger.info("Connected to Azure Realtime API")

            # Send session configuration
            await self._send_session_config()

            # Start receive loop
            self._receive_task = asyncio.create_task(self._receive_loop())

        except Exception as e:
            logger.error(f"Failed to connect: {e}")
            await self._fire_error(f"Connection failed: {e}")

    async def disconnect(self) -> None:
        """Close WebSocket connection"""
        self._is_connected = False
        if self._receive_task:
            self._receive_task.cancel()
            try:
                await self._receive_task
            except asyncio.CancelledError:
                pass
        if self._ws:
            await self._ws.close()
            self._ws = None
        logger.info("Disconnected from Azure Realtime API")

    async def send_audio(self, audio_data: bytes) -> None:
        """Send PCM16 audio to Azure"""
        if not self._ws or not self._is_connected:
            return

        audio_b64 = base64.b64encode(audio_data).decode("utf-8")
        message = {
            "type": "input_audio_buffer.append",
            "audio": audio_b64,
        }
        await self._ws.send(json.dumps(message))

    async def _send_session_config(self) -> None:
        """Send session.update with tools and VAD config"""
        system_prompt = self._build_system_prompt()
        tools = self._build_tools()

        config = {
            "type": "session.update",
            "session": {
                "modalities": ["text", "audio"],
                "instructions": system_prompt,
                "voice": "marin",
                "input_audio_format": "pcm16",
                "output_audio_format": "pcm16",
                "input_audio_transcription": {"model": "whisper-1"},
                "turn_detection": {
                    "type": "server_vad",
                    "threshold": 0.8,
                    "prefix_padding_ms": 300,
                    "silence_duration_ms": 800,
                },
                "tools": tools,
            },
        }
        await self._ws.send(json.dumps(config))
        logger.debug("Sent session configuration")

    def _build_system_prompt(self) -> str:
        """Build dynamic system prompt with context"""
        student_list = ""
        for i, s in enumerate(self._cached_students):
            marker = "[ACTIVE]" if s.id == self._current_student_id else ""
            student_list += f"\n  {chr(65+i)}. {s.display_name} {marker}"

        return f"""You are Alice - a gentle, cheerful parent assistant for education portal.

Current Context:
- Parent: {self._parent_display_name}
- Students:{student_list}

Behaviors:
1. Use get_education_data tool when asked about assignments, grades, classes
2. Use get_advice tool to provide detailed educational advice
3. Use switch_student tool when parent wants to see different child's info
4. Speak naturally, concisely in Vietnamese or English based on parent's language
5. Always confirm which student you're discussing
"""

    def _build_tools(self) -> List[Dict[str, Any]]:
        """Build tool definitions for session"""
        return [
            {
                "type": "function",
                "name": "get_education_data",
                "description": "Get student's classes, assignments, and grades",
                "parameters": {
                    "type": "object",
                    "properties": {},
                    "required": [],
                },
            },
            {
                "type": "function",
                "name": "get_advice",
                "description": "Get detailed educational advice based on data",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "education_data": {
                            "type": "string",
                            "description": "The education data to analyze",
                        },
                        "advice_for": {
                            "type": "string",
                            "enum": ["student", "parent", "teacher"],
                            "description": "Who the advice is for",
                        },
                        "specific_question": {
                            "type": "string",
                            "description": "Specific question to address",
                        },
                    },
                    "required": ["education_data", "advice_for"],
                },
            },
            {
                "type": "function",
                "name": "switch_student",
                "description": "Switch to view different student's information",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "student_identifier": {
                            "type": "string",
                            "description": "Option letter (A/B/C) or student name",
                        },
                    },
                    "required": ["student_identifier"],
                },
            },
        ]

    async def _receive_loop(self) -> None:
        """Background task to receive and process messages"""
        try:
            async for message in self._ws:
                await self._process_message(json.loads(message))
        except asyncio.CancelledError:
            pass
        except Exception as e:
            logger.error(f"Receive loop error: {e}")
            await self._fire_error(str(e))

    async def _process_message(self, msg: Dict[str, Any]) -> None:
        """Process incoming WebSocket message"""
        msg_type = msg.get("type", "")

        if msg_type == "input_audio_buffer.speech_started":
            # User started speaking - interrupt playback
            await self._fire_playback_interrupted()

        elif msg_type == "conversation.item.input_audio_transcription.completed":
            # User speech transcription
            transcript = msg.get("transcript", "")
            if transcript:
                await self._fire_transcript(transcript, is_user=True)

        elif msg_type == "conversation.item.created":
            # New item created - might be function call
            item = msg.get("item", {})
            if item.get("type") == "function_call":
                call_id = item.get("call_id")
                name = item.get("name")
                self._pending_calls[call_id] = {
                    "name": name,
                    "arguments": "",
                }

        elif msg_type == "response.function_call_arguments.delta":
            # Accumulate function arguments
            call_id = msg.get("call_id")
            delta = msg.get("delta", "")
            if call_id in self._pending_calls:
                self._pending_calls[call_id]["arguments"] += delta

        elif msg_type == "response.function_call_arguments.done":
            # Execute function call
            call_id = msg.get("call_id")
            if call_id in self._pending_calls:
                pending = self._pending_calls.pop(call_id)
                await self._execute_function(
                    call_id=call_id,
                    name=pending["name"],
                    arguments=pending["arguments"],
                )

        elif msg_type == "response.audio.delta":
            # Audio chunk from AI
            audio_b64 = msg.get("delta", "")
            if audio_b64:
                audio_bytes = base64.b64decode(audio_b64)
                await self._fire_audio(audio_bytes)

        elif msg_type == "response.audio_transcript.delta":
            # AI speech transcription
            transcript = msg.get("delta", "")
            if transcript:
                await self._fire_transcript(transcript, is_user=False)

    async def _execute_function(
        self, call_id: str, name: str, arguments: str
    ) -> None:
        """Execute voice tool and return result"""
        logger.info(f"Executing function: {name}")

        try:
            args = json.loads(arguments) if arguments else {}
        except json.JSONDecodeError:
            args = {}

        result = ""

        if name == "get_education_data":
            result = await self._execute_get_education_data()
        elif name == "get_advice":
            result = await self._execute_get_advice(args)
        elif name == "switch_student":
            result = await self._execute_switch_student(args)
        else:
            result = f"Unknown function: {name}"

        # Send function result back
        await self._send_function_result(call_id, result)

    async def _execute_get_education_data(self) -> str:
        """Execute get_education_data tool"""
        if not self._current_student_id:
            return "No student selected"

        classes = await self._student_service.get_student_classes(
            self._current_student_id
        )

        lines = [f"Education Data for {self._current_student_name}:\n"]

        for cls in classes:
            lines.append(f"\n**{cls.display_name}**")
            assignments = await self._student_service.get_class_assignments(
                cls.id, limit=5
            )
            for a in assignments:
                grade = await self._student_service.get_assignment_grade(
                    cls.id, a.id, self._current_student_id
                )
                grade_text = f" - Grade: {grade.points}/{grade.max_points}" if grade and grade.points else ""
                due_text = self._format_due_date(a.due_date_time)
                lines.append(f"  - {a.display_name}: {due_text}{grade_text}")

        return "\n".join(lines)

    async def _execute_switch_student(self, args: Dict[str, Any]) -> str:
        """Execute switch_student tool"""
        identifier = args.get("student_identifier", "").strip()
        if not identifier:
            return "Please specify student name or option letter"

        # Try option letter (A, B, C)
        if len(identifier) == 1 and identifier.upper() in "ABCDEFGH":
            idx = ord(identifier.upper()) - ord("A")
            if 0 <= idx < len(self._cached_students):
                student = self._cached_students[idx]
                self._current_student_id = student.id
                self._current_student_name = student.display_name
                await self._fire_student_switched(student.id, student.display_name)
                await self._send_session_config()
                return f"Switched to {student.display_name}"

        # Try name matching
        identifier_lower = identifier.lower()
        for student in self._cached_students:
            if identifier_lower in student.display_name.lower():
                self._current_student_id = student.id
                self._current_student_name = student.display_name
                await self._fire_student_switched(student.id, student.display_name)
                await self._send_session_config()
                return f"Switched to {student.display_name}"

        return f"Could not find student: {identifier}"

    def _format_due_date(self, due_date) -> str:
        """Format due date relative to now"""
        if not due_date:
            return "No due date"
        from datetime import datetime, timezone
        now = datetime.now(timezone.utc)
        diff = (due_date - now).days
        if diff < 0:
            return f"Overdue by {abs(diff)} days"
        elif diff == 0:
            return "Due today"
        else:
            return f"Due in {diff} days"

    async def _send_function_result(self, call_id: str, result: str) -> None:
        """Send function call result back to Azure"""
        # Create conversation item with result
        item_msg = {
            "type": "conversation.item.create",
            "item": {
                "type": "function_call_output",
                "call_id": call_id,
                "output": result,
            },
        }
        await self._ws.send(json.dumps(item_msg))

        # Request response generation
        response_msg = {"type": "response.create"}
        await self._ws.send(json.dumps(response_msg))

    # Event firing methods
    async def _fire_audio(self, audio: bytes) -> None:
        if self.on_audio_received:
            await self.on_audio_received(audio)

    async def _fire_transcript(self, text: str, is_user: bool) -> None:
        if self.on_transcript_received:
            await self.on_transcript_received(text, is_user)

    async def _fire_error(self, message: str) -> None:
        if self.on_error:
            await self.on_error(message)

    async def _fire_student_switched(self, student_id: str, name: str) -> None:
        if self.on_student_switched:
            await self.on_student_switched(student_id, name)

    async def _fire_playback_interrupted(self) -> None:
        if self.on_playback_interrupted:
            await self.on_playback_interrupted()
```

---

## 6. API Layer Implementation

### 6.1 FastAPI Application Factory

```python
# app/main.py
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from contextlib import asynccontextmanager
from app.config import Settings
from app.api.routers import auth, students, chat
from app.api.websockets import voice


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifecycle events"""
    # Startup
    yield
    # Shutdown


def create_app() -> FastAPI:
    settings = Settings()

    app = FastAPI(
        title="edu_mul_agents",
        description="Educational Multi-Agents Portal - Python Edition",
        version="1.0.0",
        lifespan=lifespan,
    )

    # Mount static files
    app.mount("/static", StaticFiles(directory="static"), name="static")

    # Include routers
    app.include_router(auth.router, prefix="/auth", tags=["Authentication"])
    app.include_router(students.router, prefix="/api/students", tags=["Students"])
    app.include_router(chat.router, prefix="/api/chat", tags=["Chat"])

    # WebSocket endpoint
    app.include_router(voice.router, prefix="/ws", tags=["Voice"])

    return app


app = create_app()
```

### 6.2 Configuration

```python
# app/config.py
from pydantic_settings import BaseSettings
from functools import lru_cache


class Settings(BaseSettings):
    """Application settings from environment variables"""

    # Azure AD
    azure_ad_tenant_id: str
    azure_ad_client_id: str
    azure_ad_client_secret: str

    # Azure OpenAI Realtime
    azure_openai_realtime_endpoint: str
    azure_openai_realtime_deployment: str
    azure_openai_realtime_api_key: str
    azure_openai_api_version: str = "2024-10-01-preview"

    # Azure OpenAI Advice (GPT-5-mini)
    azure_openai_advice_endpoint: str
    azure_openai_advice_deployment: str
    azure_openai_advice_api_key: str

    # Microsoft Graph
    graph_base_url: str = "https://graph.microsoft.com/v1.0"

    # Application
    secret_key: str
    debug: bool = False

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"


@lru_cache()
def get_settings() -> Settings:
    return Settings()
```

### 6.3 Dependency Injection

```python
# app/api/deps.py
from fastapi import Depends, HTTPException, Request
from typing import Optional
from app.config import Settings, get_settings
from app.domain.interfaces.student import IStudentService
from app.domain.interfaces.voice import IRealtimeVoiceService
from app.domain.mockdata.provider import MockDataProvider
from app.infrastructure.clients.graph import EducationGraphClientFactory
from app.infrastructure.services.student import StudentService
from app.infrastructure.services.voice.realtime import RealtimeVoiceService


# Singleton instances
_graph_factory: Optional[EducationGraphClientFactory] = None
_mock_provider: Optional[MockDataProvider] = None


def get_mock_provider() -> MockDataProvider:
    global _mock_provider
    if _mock_provider is None:
        _mock_provider = MockDataProvider()
    return _mock_provider


def get_graph_factory(
    settings: Settings = Depends(get_settings),
) -> EducationGraphClientFactory:
    global _graph_factory
    if _graph_factory is None:
        _graph_factory = EducationGraphClientFactory(settings)
    return _graph_factory


def get_student_service(
    graph_factory: EducationGraphClientFactory = Depends(get_graph_factory),
    mock_provider: MockDataProvider = Depends(get_mock_provider),
) -> IStudentService:
    return StudentService(graph_factory, mock_provider)


def get_voice_service(
    settings: Settings = Depends(get_settings),
    student_service: IStudentService = Depends(get_student_service),
) -> IRealtimeVoiceService:
    return RealtimeVoiceService(settings, student_service)


async def get_current_user(request: Request) -> dict:
    """Get current authenticated user from session"""
    user = request.session.get("user")
    if not user:
        raise HTTPException(status_code=401, detail="Not authenticated")
    return user


async def require_parent_authorization(
    current_user: dict = Depends(get_current_user),
    mock_provider: MockDataProvider = Depends(get_mock_provider),
) -> dict:
    """Verify user is authorized parent"""
    user_id = current_user.get("oid")
    if not user_id:
        raise HTTPException(status_code=401, detail="Invalid user token")

    parent_ids = mock_provider.get_parent_user_ids()
    if user_id.lower() not in [p.lower() for p in parent_ids]:
        raise HTTPException(status_code=403, detail="Not authorized as parent")

    return current_user
```

### 6.4 Voice WebSocket Endpoint

```python
# app/api/websockets/voice.py
from fastapi import APIRouter, WebSocket, WebSocketDisconnect, Depends
from app.api.deps import get_voice_service, get_current_user
from app.domain.interfaces.voice import IRealtimeVoiceService
import json
import base64
import logging

logger = logging.getLogger(__name__)
router = APIRouter()


@router.websocket("/voice")
async def voice_websocket(
    websocket: WebSocket,
    voice_service: IRealtimeVoiceService = Depends(get_voice_service),
):
    """WebSocket endpoint for voice communication"""
    await websocket.accept()

    # Set up event handlers to forward to browser
    async def on_audio(audio: bytes):
        audio_b64 = base64.b64encode(audio).decode("utf-8")
        await websocket.send_json({"type": "audio", "data": audio_b64})

    async def on_transcript(text: str, is_user: bool):
        await websocket.send_json({
            "type": "transcript",
            "text": text,
            "isUser": is_user,
        })

    async def on_error(message: str):
        await websocket.send_json({"type": "error", "message": message})

    async def on_student_switched(student_id: str, name: str):
        await websocket.send_json({
            "type": "studentSwitched",
            "studentId": student_id,
            "studentName": name,
        })

    async def on_playback_interrupted():
        await websocket.send_json({"type": "playbackInterrupted"})

    # Attach handlers
    voice_service.on_audio_received = on_audio
    voice_service.on_transcript_received = on_transcript
    voice_service.on_error = on_error
    voice_service.on_student_switched = on_student_switched
    voice_service.on_playback_interrupted = on_playback_interrupted

    try:
        # Receive messages from browser
        while True:
            data = await websocket.receive_json()
            msg_type = data.get("type")

            if msg_type == "connect":
                # Set context and connect
                parent_id = data.get("parentId")
                parent_name = data.get("parentName")
                await voice_service.set_context(parent_id, parent_name)
                await voice_service.connect()
                await websocket.send_json({"type": "connected"})

            elif msg_type == "audio":
                # Forward audio to Azure
                audio_b64 = data.get("data", "")
                audio_bytes = base64.b64decode(audio_b64)
                await voice_service.send_audio(audio_bytes)

            elif msg_type == "disconnect":
                await voice_service.disconnect()
                break

    except WebSocketDisconnect:
        logger.info("WebSocket disconnected")
    except Exception as e:
        logger.error(f"WebSocket error: {e}")
    finally:
        await voice_service.disconnect()
```

---

## 7. Frontend Implementation

### 7.1 Voice JavaScript Module

```javascript
// static/js/voice-realtime.js
class VoiceRealtimeClient {
    constructor() {
        this.ws = null;
        this.audioContext = null;
        this.mediaStream = null;
        this.processor = null;
        this.playbackQueue = [];
        this.isPlaying = false;
    }

    async start(parentId, parentName) {
        // Connect WebSocket
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.ws = new WebSocket(`${protocol}//${window.location.host}/ws/voice`);

        this.ws.onopen = () => {
            this.ws.send(JSON.stringify({
                type: 'connect',
                parentId: parentId,
                parentName: parentName,
            }));
        };

        this.ws.onmessage = (event) => this.handleMessage(JSON.parse(event.data));
        this.ws.onerror = (error) => console.error('WebSocket error:', error);

        // Start audio capture
        await this.startAudioCapture();
    }

    async startAudioCapture() {
        this.audioContext = new AudioContext({ sampleRate: 24000 });
        this.mediaStream = await navigator.mediaDevices.getUserMedia({
            audio: {
                channelCount: 1,
                sampleRate: 24000,
                echoCancellation: true,
                noiseSuppression: true,
            }
        });

        const source = this.audioContext.createMediaStreamSource(this.mediaStream);
        await this.audioContext.audioWorklet.addModule('/static/js/audio-processor.js');
        this.processor = new AudioWorkletNode(this.audioContext, 'audio-processor');

        this.processor.port.onmessage = (event) => {
            if (this.ws && this.ws.readyState === WebSocket.OPEN) {
                const pcm16 = this.floatToPCM16(event.data);
                const base64 = btoa(String.fromCharCode(...new Uint8Array(pcm16.buffer)));
                this.ws.send(JSON.stringify({ type: 'audio', data: base64 }));
            }
        };

        source.connect(this.processor);
        this.processor.connect(this.audioContext.destination);
    }

    floatToPCM16(floatArray) {
        const pcm16 = new Int16Array(floatArray.length);
        for (let i = 0; i < floatArray.length; i++) {
            const s = Math.max(-1, Math.min(1, floatArray[i]));
            pcm16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
        }
        return pcm16;
    }

    handleMessage(msg) {
        switch (msg.type) {
            case 'connected':
                console.log('Voice connected');
                break;
            case 'audio':
                this.queueAudio(msg.data);
                break;
            case 'transcript':
                this.onTranscript?.(msg.text, msg.isUser);
                break;
            case 'studentSwitched':
                this.onStudentSwitched?.(msg.studentId, msg.studentName);
                break;
            case 'playbackInterrupted':
                this.clearPlayback();
                break;
            case 'error':
                this.onError?.(msg.message);
                break;
        }
    }

    queueAudio(base64Data) {
        const binary = atob(base64Data);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        this.playbackQueue.push(bytes);
        if (!this.isPlaying) {
            this.playNextChunk();
        }
    }

    async playNextChunk() {
        if (this.playbackQueue.length === 0) {
            this.isPlaying = false;
            return;
        }

        this.isPlaying = true;
        const chunk = this.playbackQueue.shift();

        // Convert PCM16 to AudioBuffer
        const pcm16 = new Int16Array(chunk.buffer);
        const floatData = new Float32Array(pcm16.length);
        for (let i = 0; i < pcm16.length; i++) {
            floatData[i] = pcm16[i] / 32768.0;
        }

        const audioBuffer = this.audioContext.createBuffer(1, floatData.length, 24000);
        audioBuffer.copyToChannel(floatData, 0);

        const source = this.audioContext.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(this.audioContext.destination);
        source.onended = () => this.playNextChunk();
        source.start();
    }

    clearPlayback() {
        this.playbackQueue = [];
        this.isPlaying = false;
    }

    stop() {
        if (this.ws) {
            this.ws.send(JSON.stringify({ type: 'disconnect' }));
            this.ws.close();
        }
        if (this.mediaStream) {
            this.mediaStream.getTracks().forEach(track => track.stop());
        }
        if (this.audioContext) {
            this.audioContext.close();
        }
    }

    // Event handlers (to be set by consumer)
    onTranscript = null;
    onStudentSwitched = null;
    onError = null;
}

window.VoiceRealtimeClient = VoiceRealtimeClient;
```

### 7.2 Audio Worklet Processor

```javascript
// static/js/audio-processor.js
class AudioProcessor extends AudioWorkletProcessor {
    constructor() {
        super();
        this.buffer = [];
        this.bufferSize = 4800; // 200ms at 24kHz
    }

    process(inputs, outputs, parameters) {
        const input = inputs[0];
        if (input.length > 0) {
            const channel = input[0];
            this.buffer.push(...channel);

            if (this.buffer.length >= this.bufferSize) {
                const chunk = new Float32Array(this.buffer.splice(0, this.bufferSize));
                this.port.postMessage(chunk);
            }
        }
        return true;
    }
}

registerProcessor('audio-processor', AudioProcessor);
```

---

## 8. Authentication Implementation

### 8.1 OAuth Flow with MSAL

```python
# app/api/routers/auth.py
from fastapi import APIRouter, Request, Depends
from fastapi.responses import RedirectResponse
from msal import ConfidentialClientApplication
from app.config import Settings, get_settings
import uuid

router = APIRouter()


def get_msal_app(settings: Settings) -> ConfidentialClientApplication:
    return ConfidentialClientApplication(
        client_id=settings.azure_ad_client_id,
        client_credential=settings.azure_ad_client_secret,
        authority=f"https://login.microsoftonline.com/{settings.azure_ad_tenant_id}",
    )


@router.get("/login")
async def login(
    request: Request,
    settings: Settings = Depends(get_settings),
):
    """Initiate OAuth login flow"""
    msal_app = get_msal_app(settings)

    # Store state for CSRF protection
    state = str(uuid.uuid4())
    request.session["oauth_state"] = state

    redirect_uri = str(request.url_for("auth_callback"))

    auth_url = msal_app.get_authorization_request_url(
        scopes=["User.Read", "User.Read.All", "EduRoster.Read", "EduAssignments.Read"],
        state=state,
        redirect_uri=redirect_uri,
    )

    return RedirectResponse(auth_url)


@router.get("/callback")
async def auth_callback(
    request: Request,
    code: str = None,
    state: str = None,
    settings: Settings = Depends(get_settings),
):
    """Handle OAuth callback"""
    # Verify state
    stored_state = request.session.get("oauth_state")
    if state != stored_state:
        return RedirectResponse("/error?message=Invalid+state")

    msal_app = get_msal_app(settings)
    redirect_uri = str(request.url_for("auth_callback"))

    result = msal_app.acquire_token_by_authorization_code(
        code=code,
        scopes=["User.Read", "User.Read.All", "EduRoster.Read", "EduAssignments.Read"],
        redirect_uri=redirect_uri,
    )

    if "error" in result:
        return RedirectResponse(f"/error?message={result['error_description']}")

    # Store user info in session
    id_token_claims = result.get("id_token_claims", {})
    request.session["user"] = {
        "oid": id_token_claims.get("oid"),
        "name": id_token_claims.get("name"),
        "email": id_token_claims.get("preferred_username"),
    }
    request.session["access_token"] = result.get("access_token")

    return RedirectResponse("/")


@router.get("/logout")
async def logout(request: Request):
    """Clear session and logout"""
    request.session.clear()
    return RedirectResponse("/login")
```

---

## 9. Implementation Phases

### Phase 1: Foundation (Week 1-2)

```
+==============================================================================+
|  PHASE 1: FOUNDATION                                                          |
+==============================================================================+

Tasks:
┌────────────────────────────────────────────────────────────────────────────┐
│  1.1 Project Setup                                                          │
│      □ Initialize Python project with pyproject.toml                        │
│      □ Set up FastAPI application factory                                   │
│      □ Configure pydantic-settings for environment                          │
│      □ Set up logging with structlog                                        │
│                                                                             │
│  1.2 Domain Layer                                                           │
│      □ Implement all Pydantic models                                        │
│      □ Define Protocol interfaces                                           │
│      □ Port MockDataProvider                                                │
│      □ Copy JSON mock data files                                            │
│                                                                             │
│  1.3 Authentication                                                         │
│      □ Implement MSAL OAuth flow                                            │
│      □ Set up session management                                            │
│      □ Create ParentAuthorization dependency                                │
│      □ Test with Azure AD tenant                                            │
└────────────────────────────────────────────────────────────────────────────┘

Deliverables:
  - Working FastAPI app with OAuth login
  - Domain models validated
  - Mock data accessible
```

### Phase 2: Graph Integration (Week 2-3)

```
+==============================================================================+
|  PHASE 2: GRAPH INTEGRATION                                                   |
+==============================================================================+

Tasks:
┌────────────────────────────────────────────────────────────────────────────┐
│  2.1 Graph Client                                                           │
│      □ Implement EducationGraphClientFactory                                │
│      □ Set up client credentials auth                                       │
│      □ Configure token caching                                              │
│                                                                             │
│  2.2 Student Service                                                        │
│      □ Implement all IStudentService methods                                │
│      □ Map Graph responses to domain models                                 │
│      □ Handle API errors gracefully                                         │
│      □ Write integration tests                                              │
│                                                                             │
│  2.3 API Endpoints                                                          │
│      □ Create /api/students endpoints                                       │
│      □ Add proper authorization                                             │
│      □ Document with OpenAPI                                                │
└────────────────────────────────────────────────────────────────────────────┘

Deliverables:
  - Working student data API
  - Graph API integration tested
  - API documentation generated
```

### Phase 3: Voice Integration (Week 3-4)

```
+==============================================================================+
|  PHASE 3: VOICE INTEGRATION                                                   |
+==============================================================================+

Tasks:
┌────────────────────────────────────────────────────────────────────────────┐
│  3.1 WebSocket Infrastructure                                               │
│      □ Set up FastAPI WebSocket endpoint                                    │
│      □ Implement browser ↔ server protocol                                  │
│      □ Create audio encoding utilities                                      │
│                                                                             │
│  3.2 Realtime Voice Service                                                 │
│      □ Port RealtimeVoiceService to Python                                  │
│      □ Implement Azure WebSocket connection                                 │
│      □ Handle session configuration                                         │
│      □ Process all message types                                            │
│                                                                             │
│  3.3 Voice Tools                                                            │
│      □ Implement get_education_data tool                                    │
│      □ Implement switch_student tool                                        │
│      □ Implement get_advice tool with streaming                             │
│      □ Test function calling flow                                           │
│                                                                             │
│  3.4 Frontend Audio                                                         │
│      □ Create audio capture module                                          │
│      □ Create audio playback module                                         │
│      □ Handle interruption                                                  │
└────────────────────────────────────────────────────────────────────────────┘

Deliverables:
  - Working voice-to-voice conversation
  - All 3 voice tools functional
  - Audio capture/playback in browser
```

### Phase 4: UI & Polish (Week 4-5)

```
+==============================================================================+
|  PHASE 4: UI & POLISH                                                         |
+==============================================================================+

Tasks:
┌────────────────────────────────────────────────────────────────────────────┐
│  4.1 Templates                                                              │
│      □ Create base Jinja2 template                                          │
│      □ Build portal page with sidebar                                       │
│      □ Build voice mode component                                           │
│      □ Add responsive styling                                               │
│                                                                             │
│  4.2 Text Chat (Optional)                                                   │
│      □ Create chat UI component                                             │
│      □ Implement chat API endpoint                                          │
│      □ Add typing indicators                                                │
│                                                                             │
│  4.3 Testing & Documentation                                                │
│      □ Write unit tests (80% coverage)                                      │
│      □ Write integration tests                                              │
│      □ Create deployment documentation                                       │
│      □ Document API with examples                                           │
│                                                                             │
│  4.4 Security Hardening                                                     │
│      □ Move secrets to environment variables                                │
│      □ Add rate limiting                                                    │
│      □ Implement CORS properly                                              │
│      □ Add security headers                                                 │
└────────────────────────────────────────────────────────────────────────────┘

Deliverables:
  - Complete UI matching .NET version
  - Test suite passing
  - Production-ready security
```

---

## 10. Technology Mapping Reference

### 10.1 .NET to Python Mapping

| .NET Component | Python Equivalent | Notes |
|----------------|-------------------|-------|
| `Task<T>` | `async def -> T` | Native async/await |
| `IServiceCollection` | FastAPI `Depends` | Dependency injection |
| `ILogger<T>` | `logging` / `structlog` | Standard logging |
| `JsonSerializer` | Pydantic models | Auto serialization |
| `ClientWebSocket` | `websockets` | Async WebSocket |
| `HttpClient` | `httpx` / `aiohttp` | Async HTTP |
| `CancellationToken` | `asyncio.CancelledError` | Task cancellation |
| `StringBuilder` | `str.join()` / `io.StringIO` | String building |
| `Dictionary<K,V>` | `dict[K, V]` | Type hints |
| `List<T>` | `list[T]` | Type hints |
| `Nullable<T>` | `Optional[T]` | Type hints |
| `Action<T>` | `Callable[[T], None]` | Callbacks |
| `Func<T, R>` | `Callable[[T], R]` | Functions |

### 10.2 Library Mapping

| .NET Package | Python Package | Purpose |
|--------------|----------------|---------|
| Microsoft.Graph | msgraph-sdk-python | Graph API |
| Microsoft.Identity.Web | msal | Azure AD auth |
| Azure.AI.OpenAI | openai | OpenAI SDK |
| System.Net.WebSockets | websockets | WebSocket client |
| Newtonsoft.Json | pydantic | JSON handling |
| Microsoft.AspNetCore | FastAPI | Web framework |
| SignalR | WebSocket / Socket.IO | Real-time |

---

## 11. Risk Assessment & Mitigations

### 11.1 Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Graph SDK differences | Medium | Test all endpoints early, use raw HTTP as fallback |
| WebSocket protocol | High | Use exact same message format as .NET |
| Audio encoding | Medium | Test PCM16 conversion thoroughly |
| Async complexity | Medium | Use asyncio properly, avoid blocking |
| MSAL token refresh | Low | Implement proper token caching |

### 11.2 Security Considerations

```
+==============================================================================+
|                    SECURITY CHECKLIST                                         |
+==============================================================================+

□ Store all secrets in environment variables (not code)
□ Use Azure Key Vault for production secrets
□ Implement CSRF protection for OAuth flow
□ Validate all WebSocket messages
□ Rate limit API and WebSocket endpoints
□ Use HTTPS only in production
□ Implement proper CORS configuration
□ Sanitize all user inputs
□ Log security events
□ Regular dependency updates
```

---

## 12. Deployment Architecture

### 12.1 Azure App Service (Recommended)

```
+==============================================================================+
|                    DEPLOYMENT ARCHITECTURE                                    |
+==============================================================================+

                         ┌─────────────────────────────────────┐
                         │         Azure App Service           │
                         │         (Python 3.11+)              │
                         │                                     │
                         │  ┌─────────────────────────────┐   │
                         │  │     Gunicorn + Uvicorn      │   │
Internet ────────────────┤  │     (ASGI Workers)          │   │
                         │  └──────────────┬──────────────┘   │
                         │                 │                   │
                         │  ┌──────────────▼──────────────┐   │
                         │  │       FastAPI App           │   │
                         │  │  - HTTP Routes              │   │
                         │  │  - WebSocket /ws/voice      │   │
                         │  └─────────────────────────────┘   │
                         └─────────────────────────────────────┘
                                          │
                    ┌─────────────────────┼─────────────────────┐
                    │                     │                     │
                    ▼                     ▼                     ▼
           ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
           │  Azure AD    │     │ Graph API    │     │ Azure OpenAI │
           │  (Auth)      │     │ (Data)       │     │ (Voice)      │
           └──────────────┘     └──────────────┘     └──────────────┘
```

### 12.2 Docker Configuration

```dockerfile
# Dockerfile
FROM python:3.11-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY . .

# Run with Gunicorn + Uvicorn workers
CMD ["gunicorn", "app.main:app", "-w", "4", "-k", "uvicorn.workers.UvicornWorker", "-b", "0.0.0.0:8000"]
```

---

## 13. Summary

This implementation plan provides a comprehensive roadmap for migrating the edu_mul_agents portal from .NET 8/Blazor to Python + Azure AI. Key highlights:

1. **Clean Architecture Preserved**: Same 3-layer separation (API, Domain, Infrastructure)
2. **Technology Parity**: Every .NET component has a Python equivalent
3. **Feature Complete**: All voice tools, Graph integration, authentication
4. **Modern Python**: Async/await, type hints, Pydantic v2
5. **Production Ready**: Security, logging, deployment considerations

### Quick Start Commands

```bash
# Create project
mkdir edu_mul_agents_python && cd edu_mul_agents_python
python -m venv venv
source venv/bin/activate  # Linux/Mac
pip install -r requirements.txt

# Create .env
cp .env.example .env
# Edit .env with your Azure credentials

# Run development server
uvicorn app.main:app --reload --port 8000

# Run tests
pytest tests/ -v

# Type check
mypy app/

# Lint
ruff check app/
```

---

**Document Info**:
- Created: 2026-01-12
- Version: IMPL-v1
- Based on: Over-v1, EXPLAIN-v1, EXPLAIN-v2
- Target: Python 3.11+ with FastAPI
