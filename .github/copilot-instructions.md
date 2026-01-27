# GitHub Copilot Instructions for Don-Quixote Project

> **Project**: Don-Quixote (RoboRunner)  
> **Type**: Full-Stack Monorepo  
> **Description**: Restaurant Robot Management & Simulation System

## Monorepo Overview

This is a full-stack application for managing autonomous delivery robots in a restaurant environment. The system includes robot navigation, task dispatch, real-time tracking, and simulation capabilities.

## Project Structure

```
Don-Quixote/
├── BackendAPI/
│   └── MapPlannerApi/          # .NET 10 REST API
│       └── .github/copilot-instructions.md
├── Frontend/                    # React 19 SPA
│   └── .github/copilot-instructions.md
├── Robot/
│   └── RobotNavigationSDK-1.5.0/  # Android Robot SDK
├── Restaurant/
│   └── restaurant.md           # Restaurant domain docs
└── README.md
```

## Project Components

| Component | Technology | Port | Description |
|-----------|------------|------|-------------|
| **Backend API** | .NET 10 / ASP.NET Core | 5199 | REST API + SignalR hub |
| **Frontend** | React 19 / Konva.js | 3000 | Visual robot simulation |
| **Robot SDK** | Android / Kotlin | - | Physical robot integration |

## Quick Start

### Backend API
```bash
cd BackendAPI/MapPlannerApi
dotnet run --urls "http://localhost:5199"
```

### Frontend
```bash
cd Frontend
npm install
npm start
```

## Architecture Overview

```
┌─────────────────┐     HTTP/REST      ┌─────────────────┐
│                 │ ◄───────────────── │                 │
│   Frontend      │                    │   Backend API   │
│   (React)       │ ◄───────────────── │   (.NET 10)     │
│                 │     SignalR        │                 │
└─────────────────┘                    └─────────────────┘
                                              │
                                              │ SQLite
                                              ▼
                                       ┌─────────────────┐
                                       │   Database      │
                                       │   (restaurant.db)│
                                       └─────────────────┘
                                              │
                                              │ SDK Integration
                                              ▼
                                       ┌─────────────────┐
                                       │   Physical      │
                                       │   Robots        │
                                       └─────────────────┘
```

## Key Concepts

### Entities
- **Robot**: Autonomous delivery bot with position, battery, status
- **Task**: Work item (Deliver, Return, Charge, Patrol)
- **Table**: Restaurant table with capacity and status
- **Guest**: Customer/party with waitlist management
- **Zone**: Restaurant area (Dining, Kitchen, Charging)

### Robot States
`Idle → Navigating → Delivering → Returning → Idle`

### Task States
`Pending → Assigned → InProgress → Completed/Failed`

## API Endpoints Summary

| Category | Base Path | Description |
|----------|-----------|-------------|
| Health | `/api/health` | Service health check |
| Robots | `/api/robots` | Robot CRUD + commands |
| Tasks | `/api/tasks` | Task management |
| Tables | `/api/tables` | Table management |
| Guests | `/api/guests` | Guest/waitlist |
| Dispatch | `/api/dispatch` | Auto task assignment |
| Simulation | `/api/simulation` | Test scenarios |
| WebSocket | `/hubs/restaurant` | Real-time events |

## Cross-Project Considerations

### Coordinate System
Both frontend and backend use dual coordinates:
- **Screen**: Pixels for UI rendering
- **Physical**: Meters for real-world navigation

### Real-time Communication
- Backend broadcasts events via SignalR
- Frontend subscribes to robot position updates
- Events: `RobotPositionUpdated`, `TaskStatusChanged`, `AlertCreated`

### API Contract
Frontend expects JSON responses with:
- camelCase property names
- String enums (not numeric)
- ISO 8601 date format

## Development Guidelines

### When Working on Backend
- See: `BackendAPI/MapPlannerApi/.github/copilot-instructions.md`
- Use repository pattern
- Return DTOs, not entities
- Broadcast events for state changes

### When Working on Frontend
- See: `Frontend/.github/copilot-instructions.md`
- Use Vega design system components
- Use React-Konva for map rendering
- Handle loading/error states

### When Integrating
- Match coordinate systems (pixels ↔ meters)
- Handle SignalR reconnection
- Use consistent enum values between projects
