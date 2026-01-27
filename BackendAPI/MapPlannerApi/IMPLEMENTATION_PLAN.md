# MapPlannerApi Implementation Plan

> Generated: January 27, 2026  
> Status: **Phase 1-6 Completed**  
> Target Framework: .NET 10.0

---

## 📋 Overview

This document outlines the remaining implementation work to complete the MapPlannerApi backend according to the specification in `backendapi.md`.

### Current Completion Status

| Component | Status | Completion |
|-----------|--------|------------|
| Entity Models & DB | ✅ Done | 100% |
| Basic CRUD APIs | ✅ Done | 100% |
| Robot Commands & Lifecycle | ✅ Done | 100% |
| Task Workflows | ✅ Done | 90% |
| Dispatch Engine | ✅ Done | 100% |
| SignalR Real-time Events | ✅ Done | 100% |
| Simulation APIs | ✅ Done | 100% |
| External Integrations | ✅ Done | 100% |
| State Machines | ⏸️ Optional | 0% |

---

## 🎯 Phase 1: Robot Commands & Lifecycle Enhancement

### Files to Modify
- `Endpoints/RobotEndpoints.cs`

### Tasks

- [ ] **1.1** Add `PATCH /api/robots/{id}/command` endpoint
  - Commands: `go_to`, `return_home`, `pause`, `resume`, `emergency_stop`, `clear_error`
  - Validate command parameters
  - Return command acknowledgment with estimated completion time

- [ ] **1.2** Add `POST /api/robots/{id}/recovery` endpoint
  - Recovery actions: `retry_task`, `return_home`, `manual_intervention`
  - Handle blocked/error state recovery

- [ ] **1.3** Add `POST /api/robots/{id}/heartbeat` endpoint
  - Accept heartbeat with battery, position, diagnostics
  - Update last_seen timestamp
  - Trigger alerts on missed heartbeats

- [ ] **1.4** Enhance `GET /api/robots` with query filters
  - Filter by: `status`, `zone`, `is_available`
  - Sort by: `name`, `battery_level`, `last_seen`

- [ ] **1.5** Add `GET /api/robots/{id}/history` endpoint
  - Return task history with timestamps
  - Support date range filtering

---

## 🎯 Phase 2: Task Workflow Enhancement

### Files to Modify
- `Endpoints/TaskEndpoints.cs`
- `Data/TaskRepository.cs`

### Tasks

- [ ] **2.1** Add `PATCH /api/tasks/{id}/reassign` endpoint
  - Change assigned robot mid-task
  - Validate robot availability and capability

- [ ] **2.2** Add task chain support
  - `POST /api/tasks/chain` - Create linked task sequence
  - Auto-trigger next task on completion

- [ ] **2.3** Add `PATCH /api/tasks/{id}/partial-complete` endpoint
  - Mark task partially complete with progress percentage
  - Support for multi-checkpoint delivery

- [ ] **2.4** Add `GET /api/tasks/stats` endpoint
  - Return completion rates, average times
  - Filter by date range, robot, task type

- [ ] **2.5** Add bulk operations
  - `POST /api/tasks/bulk-cancel` - Cancel multiple tasks
  - `POST /api/tasks/bulk-assign` - Assign multiple tasks to robot

---

## 🎯 Phase 3: Dispatch Engine Service

### Files to Create
- `Services/DispatchEngine.cs`
- `Services/IDispatchEngine.cs`
- `Models/DispatchModels.cs`

### Tasks

- [ ] **3.1** Create dispatch engine interface and implementation
  - Task assignment algorithm (nearest available robot)
  - Priority queue management
  - Load balancing across robots

- [ ] **3.2** Add automatic dispatch endpoints
  - `POST /api/dispatch/auto-assign` - Trigger auto-assignment
  - `GET /api/dispatch/queue` - View pending dispatch queue
  - `PATCH /api/dispatch/config` - Update dispatch rules

- [ ] **3.3** Implement failover logic
  - Detect blocked/failed robots
  - Auto-reassign tasks to available robots

- [ ] **3.4** Add preemption support
  - High-priority task preemption
  - Task interruption and queue management

---

## 🎯 Phase 4: SignalR Real-time Events

### Files to Create
- `Hubs/RestaurantHub.cs`
- `Services/EventBroadcaster.cs`

### Files to Modify
- `Program.cs` - Add SignalR configuration

### Tasks

- [ ] **4.1** Create SignalR hub
  - Client connection management
  - Group subscriptions (by zone, robot, table)

- [ ] **4.2** Implement event types
  - `RobotPositionUpdated` - Real-time position streaming
  - `RobotStatusChanged` - Status transitions
  - `TaskStatusChanged` - Task lifecycle events
  - `AlertCreated` - New alerts
  - `TableStatusChanged` - Occupancy changes
  - `GuestSeated` / `GuestLeft` - Guest events

- [ ] **4.3** Create event broadcaster service
  - Inject into endpoints to broadcast on changes
  - Support selective broadcasting (zone-based)

- [ ] **4.4** Add connection management endpoints
  - `GET /api/signalr/connections` - List active connections
  - `POST /api/signalr/broadcast` - Manual broadcast (admin)

---

## 🎯 Phase 5: Simulation APIs

### Files to Create
- `Endpoints/SimulationEndpoints.cs`
- `Services/SimulationService.cs`
- `Models/SimulationModels.cs`

### Tasks

- [ ] **5.1** Create simulation session management
  - `POST /api/simulation/sessions` - Start simulation
  - `DELETE /api/simulation/sessions/{id}` - Stop simulation
  - `GET /api/simulation/sessions` - List active sessions

- [ ] **5.2** Implement guest spawning
  - `POST /api/simulation/spawn-guest` - Create simulated guest
  - Configurable party size, wait tolerance, preferences

- [ ] **5.3** Add robot simulation controls
  - `PATCH /api/simulation/robots/{id}/position` - Move robot
  - `PATCH /api/simulation/robots/{id}/battery` - Set battery
  - `POST /api/simulation/robots/{id}/trigger-error` - Simulate failure

- [ ] **5.4** Implement time acceleration
  - `PATCH /api/simulation/time-scale` - Speed up simulation
  - Support 1x to 10x speed

- [ ] **5.5** Add optimization scenario endpoints
  - `POST /api/simulation/scenarios` - Load predefined scenario
  - `GET /api/simulation/metrics` - Performance analytics

---

## 🎯 Phase 6: External Integrations

### Files to Create
- `Endpoints/IntegrationEndpoints.cs`
- `Services/WebhookService.cs`
- `Models/IntegrationModels.cs`

### Tasks

- [ ] **6.1** Create webhook management
  - `POST /api/webhooks` - Register webhook URL
  - `DELETE /api/webhooks/{id}` - Remove webhook
  - `GET /api/webhooks` - List registered webhooks
  - `POST /api/webhooks/{id}/test` - Send test payload

- [ ] **6.2** Implement webhook event triggers
  - Fire webhooks on: task_completed, alert_created, robot_error
  - Include retry logic with exponential backoff

- [ ] **6.3** Add POS integration endpoints
  - `POST /api/integrations/pos/order` - Receive order from POS
  - `POST /api/integrations/pos/payment` - Payment confirmation

- [ ] **6.4** Add emergency system endpoints
  - `POST /api/emergency/stop-all` - Emergency stop all robots
  - `POST /api/emergency/evacuate` - Trigger evacuation mode
  - `POST /api/emergency/resume` - Resume normal operations

---

## 🎯 Phase 7: State Machines (Optional Enhancement)

### Files to Create
- `StateMachines/RobotStateMachine.cs`
- `StateMachines/TaskStateMachine.cs`
- `StateMachines/TableStateMachine.cs`

### Tasks

- [ ] **7.1** Install Stateless library
  ```bash
  dotnet add package Stateless
  ```

- [ ] **7.2** Implement robot state machine
  - States: Idle, MovingToPickup, AtPickup, MovingToDelivery, AtDelivery, Returning, Charging, Error, Blocked
  - Valid transitions with guards

- [ ] **7.3** Implement task state machine
  - States: Pending, Assigned, InProgress, Completed, Failed, Cancelled
  - Enforce valid transitions

- [ ] **7.4** Implement table state machine
  - States: Available, Reserved, Occupied, Cleaning
  - Time-based auto-transitions

---

## 📁 Final Project Structure

```
MapPlannerApi/
├── Data/
│   ├── RestaurantDbContext.cs
│   └── Repositories/
│       ├── RobotRepository.cs
│       ├── TaskRepository.cs
│       └── ... (existing)
├── Endpoints/
│   ├── RobotEndpoints.cs        # Enhanced
│   ├── TaskEndpoints.cs         # Enhanced
│   ├── SimulationEndpoints.cs   # NEW
│   ├── IntegrationEndpoints.cs  # NEW
│   └── ... (existing)
├── Entities/
│   └── ... (existing - complete)
├── Hubs/
│   └── RestaurantHub.cs         # NEW
├── Models/
│   ├── DispatchModels.cs        # NEW
│   ├── SimulationModels.cs      # NEW
│   ├── IntegrationModels.cs     # NEW
│   └── ... (existing)
├── Services/
│   ├── DispatchEngine.cs        # NEW
│   ├── EventBroadcaster.cs      # NEW
│   ├── SimulationService.cs     # NEW
│   ├── WebhookService.cs        # NEW
│   └── ... (existing)
├── StateMachines/               # NEW (Optional)
│   ├── RobotStateMachine.cs
│   ├── TaskStateMachine.cs
│   └── TableStateMachine.cs
└── Program.cs                   # Modified for SignalR
```

---

## 🚀 Implementation Order

1. **Phase 1** - Robot Commands (1-2 hours)
2. **Phase 2** - Task Workflows (1-2 hours)
3. **Phase 3** - Dispatch Engine (2-3 hours)
4. **Phase 4** - SignalR Events (2-3 hours)
5. **Phase 5** - Simulation APIs (2-3 hours)
6. **Phase 6** - External Integrations (1-2 hours)
7. **Phase 7** - State Machines (Optional, 1-2 hours)

**Total Estimated Time: 10-17 hours**

---

## ✅ Completion Checklist

- [x] Phase 1 Complete - Robot Commands & Lifecycle
- [x] Phase 2 Complete - Task Workflows (partial: chains pending)
- [x] Phase 3 Complete - Dispatch Engine
- [x] Phase 4 Complete - SignalR Events  
- [x] Phase 5 Complete - Simulation APIs
- [x] Phase 6 Complete - External Integrations
- [ ] Phase 7 Complete (Optional) - State Machines
- [x] All endpoints tested
- [x] SignalR hub configured
- [x] Documentation updated

---

## 📝 Implementation Summary

### New Files Created

| File | Description |
|------|-------------|
| `Hubs/RestaurantHub.cs` | SignalR hub for real-time events |
| `Services/EventBroadcaster.cs` | Service to broadcast events via SignalR |
| `Services/DispatchEngine.cs` | Auto task assignment engine |
| `Endpoints/DispatchEndpoints.cs` | Dispatch API endpoints |
| `Endpoints/SimulationEndpoints.cs` | Simulation API endpoints |
| `Endpoints/IntegrationEndpoints.cs` | Webhooks, POS, Emergency endpoints |

### Modified Files

| File | Changes |
|------|---------|
| `Endpoints/RobotEndpoints.cs` | Added command, recovery, heartbeat, history endpoints |
| `Dtos/ApiDtos.cs` | Added ~50 new DTOs for all features |
| `Program.cs` | Added SignalR, DI for new services, new endpoint mappings |

### New API Endpoints

#### Robot Commands
- `PATCH /api/robots/{id}/command` - Send command (go_to, return_home, pause, resume, emergency_stop, clear_error)
- `POST /api/robots/{id}/recovery` - Initiate recovery (retry_task, return_home, manual_intervention)
- `POST /api/robots/{id}/heartbeat` - Robot heartbeat with diagnostics
- `GET /api/robots/{id}/history` - Task history with date range filter

#### Dispatch Engine
- `GET /api/dispatch/queue` - View pending dispatch queue
- `POST /api/dispatch/auto-assign` - Trigger automatic assignment
- `GET /api/dispatch/config` - Get dispatch configuration
- `PATCH /api/dispatch/config` - Update dispatch rules
- `POST /api/dispatch/suggest/{taskId}` - Get suggested robot for task

#### Simulation
- `GET /api/simulation/sessions` - List simulation sessions
- `POST /api/simulation/sessions` - Create simulation session
- `DELETE /api/simulation/sessions/{id}` - Stop simulation
- `PATCH /api/simulation/sessions/{id}/pause` - Pause simulation
- `PATCH /api/simulation/sessions/{id}/resume` - Resume simulation
- `PATCH /api/simulation/time-scale` - Set time acceleration
- `POST /api/simulation/spawn-guest` - Spawn simulated guest
- `PATCH /api/simulation/robots/{id}/position` - Move robot position
- `PATCH /api/simulation/robots/{id}/battery` - Set robot battery
- `POST /api/simulation/robots/{id}/trigger-error` - Trigger robot error
- `GET /api/simulation/metrics` - Get simulation metrics
- `POST /api/simulation/scenarios/{name}` - Load predefined scenario

#### Integrations
- `GET /api/webhooks` - List webhooks
- `POST /api/webhooks` - Register webhook
- `DELETE /api/webhooks/{id}` - Remove webhook
- `POST /api/webhooks/{id}/test` - Test webhook
- `PATCH /api/webhooks/{id}/toggle` - Toggle webhook
- `POST /api/integrations/pos/order` - Receive POS order
- `POST /api/integrations/pos/payment` - Payment confirmation
- `POST /api/emergency/stop-all` - Emergency stop all robots
- `POST /api/emergency/evacuate` - Activate evacuation mode
- `POST /api/emergency/resume` - Resume normal operations

#### SignalR Hub
- Hub URL: `/hubs/restaurant`
- Events: RobotPositionUpdated, RobotStatusChanged, TaskStatusChanged, AlertCreated, TableStatusChanged, GuestEvent
- Client methods: SubscribeToRobot, SubscribeToZone, SubscribeToTable, SubscribeToAlerts, SubscribeToTasks
