# Restaurant Robot System - Supported Scenarios

> **Document Purpose**: Technical specification of all supported automation scenarios  
> **Audience**: Business managers with technical background  
> **Last Updated**: January 2026

---

## Executive Summary

The Restaurant Robot Management System automates task dispatch and robot coordination through an **event-driven architecture**. When events occur (guest arrival, food ready, table needs cleaning), the system automatically:

1. Creates appropriate tasks in the queue
2. Assigns tasks to the best available robot
3. Monitors task execution and handles failures
4. Provides real-time status updates via WebSocket

---

## Table of Contents

1. [Guest Scenarios](#1-guest-scenarios)
2. [Kitchen & Food Scenarios](#2-kitchen--food-scenarios)
3. [Table Management Scenarios](#3-table-management-scenarios)
4. [Robot Operations Scenarios](#4-robot-operations-scenarios)
5. [Task Queue & Dispatch](#5-task-queue--dispatch)
6. [Failover & Recovery](#6-failover--recovery)
7. [API Reference Summary](#7-api-reference-summary)
8. [Long-Run Simulation](#8-long-run-simulation)

---

## 1. Guest Scenarios

### 1.1 Guest Arrival

**Trigger**: Guest arrives at restaurant entrance

| Aspect | Details |
|--------|---------|
| Event Type | `GuestArrived` |
| Task Created | `Escort` (escort guest to table) |
| Priority | Normal (configurable) |
| Alert | `GuestWaiting` notification to host |
| API Endpoint | `POST /api/events/guest-arrived` |

**Flow**:
```
Guest arrives → System creates Escort task → Robot assigned → 
Robot guides guest to table → Task completed
```

**Request Example**:
```json
POST /api/events/guest-arrived
{
  "guestId": 42,
  "partySize": 4,
  "reservedTableId": 12,
  "notes": "Birthday celebration"
}
```

---

### 1.2 Guest Seated

**Trigger**: Guest has been seated at a table

| Aspect | Details |
|--------|---------|
| Event Type | `GuestSeated` |
| Task Created | `Deliver` (initial service - menus, water) |
| Priority | Normal |
| API Endpoint | `POST /api/events/trigger` with `eventType: "GuestSeated"` |

**Flow**:
```
Guest seated → Deliver task created → Robot brings menus/water → 
Task completed → Table status updated to "Occupied"
```

---

### 1.3 Guest Needs Help

**Trigger**: Guest presses help button or staff reports assistance needed

| Aspect | Details |
|--------|---------|
| Event Type | `GuestNeedsHelp` |
| Task Created | `Service` (guest assistance) |
| Priority | **High** or **Urgent** |
| Alert | `TableService` warning alert created |
| Table Status | Updated to `NeedsService` |
| API Endpoint | `POST /api/events/guest-help` |

**Flow**:
```
Help requested → High-priority Service task → Immediate dispatch → 
Robot/staff notified → Assistance provided → Task completed
```

**Request Example**:
```json
POST /api/events/guest-help
{
  "tableId": 8,
  "reason": "Spilled drink, needs cleanup",
  "isUrgent": true
}
```

---

### 1.4 Guest Requests Check

**Trigger**: Guest is ready to pay

| Aspect | Details |
|--------|---------|
| Event Type | `GuestRequestedCheck` |
| Task Created | `Deliver` (bring check to table) |
| Priority | Normal |
| API Endpoint | `POST /api/events/trigger` |

---

### 1.5 Guest Leaves

**Trigger**: Guest departs from table

| Aspect | Details |
|--------|---------|
| Event Type | `GuestLeft` |
| Task Created | `Cleaning` (bus and clean table) |
| Priority | Normal |
| Table Status | Updated to `Cleaning` |
| API Endpoint | `POST /api/events/trigger` |

**Flow**:
```
Guest leaves → Cleaning task created → Robot/staff clears table → 
Table cleaned → Status updated to "Available"
```

---

## 2. Kitchen & Food Scenarios

### 2.1 Food Ready for Delivery

**Trigger**: Kitchen marks food items as ready

| Aspect | Details |
|--------|---------|
| Event Type | `FoodReady` |
| Task Created | `Deliver` |
| Priority | **High** (hot food) |
| Auto-Dispatch | **Immediate** (triggers `AutoAssignPendingTasks`) |
| API Endpoint | `POST /api/kitchen/food-ready` |

**Flow**:
```
Food ready in kitchen → High-priority Deliver task → 
Immediate robot assignment → Robot delivers food → Task completed
```

**Request Example**:
```json
POST /api/kitchen/food-ready
{
  "tableId": 5,
  "orderId": "ORD-2026-0127-0042",
  "items": ["Steak medium-rare", "Caesar salad"]
}
```

---

### 2.2 Drinks Ready

**Trigger**: Bar/kitchen marks drinks as ready

| Aspect | Details |
|--------|---------|
| Event Type | `DrinkReady` |
| Task Created | `Deliver` |
| Priority | Normal |
| Auto-Dispatch | Immediate |
| API Endpoint | `POST /api/kitchen/drinks-ready` |

---

### 2.3 Complete Order Ready

**Trigger**: Full order is ready for delivery (multiple items)

| Aspect | Details |
|--------|---------|
| Event Type | `OrderReady` |
| Task Created | `Deliver` |
| Priority | **Urgent** (or High if not rush) |
| Auto-Dispatch | Immediate |
| API Endpoint | `POST /api/kitchen/order-ready` |

**Request Example**:
```json
POST /api/kitchen/order-ready
{
  "tableId": 12,
  "orderId": "ORD-2026-0127-0099",
  "notes": "VIP table - priority service",
  "isRush": true
}
```

**Business Impact**: Rush orders (`isRush: true`) receive **Urgent** priority and are assigned before all other pending tasks.

---

## 3. Table Management Scenarios

### 3.1 Table Needs Service

**Trigger**: Table requires staff attention (refills, questions, etc.)

| Aspect | Details |
|--------|---------|
| Event Type | `TableNeedsService` |
| Task Created | `Service` |
| Priority | Configurable (default: Normal) |
| Table Status | Updated to `NeedsService` |
| API Endpoint | `POST /api/events/table-status` |

---

### 3.2 Table Needs Cleaning

**Trigger**: Table is dirty and needs to be cleaned

| Aspect | Details |
|--------|---------|
| Event Type | `TableNeedsCleaning` |
| Task Created | `Cleaning` |
| Priority | Normal |
| Table Status | Updated to `Cleaning` |
| API Endpoint | `POST /api/events/table-status` |

---

### 3.3 Table Needs Bussing

**Trigger**: Dirty dishes need to be cleared

| Aspect | Details |
|--------|---------|
| Event Type | `TableNeedsBussing` |
| Task Created | `Return` (return dishes to kitchen) |
| Priority | Normal |
| API Endpoint | `POST /api/events/table-status` |

---

### 3.4 Table Needs Setup

**Trigger**: Table needs to be set for next guests

| Aspect | Details |
|--------|---------|
| Event Type | `TableNeedsSetup` |
| Task Created | `Deliver` (bring table settings) |
| Priority | Normal |
| API Endpoint | `POST /api/events/table-status` |

**Request Example**:
```json
POST /api/events/table-status
{
  "tableId": 7,
  "newStatus": "setup",
  "notes": "Large party arriving in 10 minutes"
}
```

---

## 4. Robot Operations Scenarios

### 4.1 Low Battery Detection

**Trigger**: Robot battery falls below threshold (default: 15%)

| Aspect | Details |
|--------|---------|
| Event Type | `RobotLowBattery` |
| Task Created | `Charge` |
| Priority | High |
| Alert | `LowBattery` warning |
| Detection | Automatic via `RobotMonitorService` |

**Flow**:
```
Battery drops below 15% → Monitor detects → Charge task created → 
Robot navigates to charging station → Charging begins
```

---

### 4.2 Robot Blocked

**Trigger**: Robot cannot complete navigation (obstacle, path blocked)

| Aspect | Details |
|--------|---------|
| Event Type | `RobotBlocked` |
| Alert | `NavigationBlocked` error alert |
| Action | Task unassigned and returned to queue |
| Failover | Task reassigned to another robot |
| Detection | Automatic (task running >2 minutes) |

**Flow**:
```
Robot blocked for >2min → Monitor detects → Alert created → 
Task returned to queue → Different robot assigned → 
Original robot marked for recovery
```

---

### 4.3 Robot Error

**Trigger**: Robot encounters hardware/software error

| Aspect | Details |
|--------|---------|
| Event Type | `RobotError` |
| Robot Status | Set to `Error` |
| Alert | `RobotError` alert created |
| Action | All active tasks reassigned |
| API Endpoint | `POST /api/events/trigger` or automatic detection |

---

### 4.4 Robot Heartbeat Timeout

**Trigger**: No heartbeat received for 30+ seconds

| Aspect | Details |
|--------|---------|
| Detection | Automatic via `RobotMonitorService` |
| Robot Status | Set to `Error` |
| Action | Active task returned to queue, retry count incremented |
| Alert | `RobotError` with "unresponsive" message |

---

## 5. Task Queue & Dispatch

### 5.1 Automatic Task Assignment

The system continuously monitors the task queue and assigns pending tasks to available robots.

**Dispatch Cycle** (default: every 10 seconds):
```
1. Check for pending tasks (sorted by Priority DESC, CreatedAt ASC)
2. For each task:
   a. Find eligible robots
   b. Apply assignment algorithm
   c. Assign task to best robot
   d. Broadcast status change via WebSocket
```

### 5.2 Assignment Algorithms

| Algorithm | Logic | Recommended Use |
|-----------|-------|-----------------|
| `nearest` | Shortest distance to target | Default - minimizes travel time |
| `round_robin` | Distribute tasks evenly | High-volume periods |
| `load_balanced` | Balance by workload + battery | Long shifts |
| `priority` | Best battery for urgent tasks | Premium service |

### 5.3 Robot Eligibility

A robot can receive tasks only if **ALL** conditions are met:

| Condition | Default | Configurable |
|-----------|---------|--------------|
| Enabled | `true` | Per-robot setting |
| Status | `Idle` | N/A |
| Battery | ≥20% | `dispatch.minBattery` |
| Current Tasks | <3 | `dispatch.maxTasksPerRobot` |

### 5.4 Task Priority Levels

| Priority | Value | Examples |
|----------|-------|----------|
| **Urgent** | 3 | Complete orders, rush requests |
| **High** | 2 | Food delivery, guest help |
| **Normal** | 1 | Drinks, table service |
| **Low** | 0 | Non-time-sensitive tasks |

### 5.5 Priority Escalation

Tasks waiting too long are automatically escalated:

| Wait Time | Action |
|-----------|--------|
| >3 minutes | Low → Normal |
| >3 minutes | Normal → High |
| >3 minutes | High → Urgent |
| >5 minutes | Alert created for high-priority tasks |

---

## 6. Failover & Recovery

### 6.1 Task Retry Logic

When a task fails or robot becomes unavailable:

```
Task fails → Increment RetryCount → Check against MaxRetryCount
  ├─ If RetryCount < MaxRetryCount → Return to queue, reassign
  └─ If RetryCount ≥ MaxRetryCount → Mark as Failed, create alert
```

**Default**: `MaxRetryCount = 3`

### 6.2 Robot Recovery

| Scenario | Detection | Recovery Action |
|----------|-----------|-----------------|
| Heartbeat timeout | 30 seconds | Mark Error, reassign tasks |
| Task blocked | 2 minutes | Trigger failover event |
| Low battery | ≤15% | Create charge task |
| Manual error report | API call | Mark Error, notify staff |

### 6.3 Task Failover Example

```
Robot-1 assigned Task-42 (Deliver food to Table 5)
    ↓
Robot-1 becomes blocked (obstacle in path)
    ↓
2 minutes pass, RobotMonitorService detects
    ↓
Task-42 unassigned from Robot-1
Task-42.RetryCount = 1
Task-42.Status = Pending
    ↓
Next dispatch cycle:
Robot-2 (nearest available) assigned Task-42
    ↓
Robot-2 completes delivery
Task-42.Status = Completed
```

---

## 7. API Reference Summary

### Event Trigger Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/events/trigger` | POST | Generic event trigger |
| `/api/events/guest-arrived` | POST | Guest arrival |
| `/api/events/guest-help` | POST | Guest assistance request |
| `/api/events/table-status` | POST | Table status change |
| `/api/events/types` | GET | List all event types |

### Kitchen Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/kitchen/order-ready` | POST | Complete order ready |
| `/api/kitchen/food-ready` | POST | Food items ready |
| `/api/kitchen/drinks-ready` | POST | Drinks ready |

### Task Management Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/tasks` | GET | List all tasks |
| `/api/tasks/pending` | GET | Get pending queue |
| `/api/tasks/{id}/assign` | POST | Manual assignment |
| `/api/tasks/{id}/unassign` | POST | Return task to queue |
| `/api/tasks/{id}/complete` | POST | Mark completed |
| `/api/tasks/{id}/fail` | POST | Mark failed |
| `/api/tasks/queue/summary` | GET | Queue statistics |

### Dispatch Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/dispatch/queue` | GET | View queue with suggestions |
| `/api/dispatch/auto-assign` | POST | Trigger immediate dispatch |
| `/api/dispatch/config` | GET/PUT | View/update dispatch settings |

### Real-Time Events (WebSocket)

Connect to: `wss://{host}/hubs/restaurant`

| Event | Payload | When Fired |
|-------|---------|------------|
| `RobotPositionUpdated` | Position, heading | Robot moves |
| `RobotStatusChanged` | Old/new status | Status transitions |
| `TaskStatusChanged` | Task details | Assignment, completion |
| `AlertCreated` | Alert details | New alerts |
| `TableStatusChanged` | Table details | Occupancy changes |

---

## Configuration Reference

| Setting | Key | Default | Impact |
|---------|-----|---------|--------|
| Dispatch enabled | `dispatch.autoEnabled` | `true` | Turn auto-dispatch on/off |
| Dispatch interval | `dispatch.intervalSeconds` | `10` | Seconds between dispatch cycles |
| Algorithm | `dispatch.algorithm` | `nearest` | Robot selection method |
| Max tasks/robot | `dispatch.maxTasksPerRobot` | `3` | Concurrent task limit |
| Min battery | `dispatch.minBattery` | `20` | Assignment threshold (%) |
| Task retry limit | `task.retryLimit` | `3` | Max retries before failure |
| Priority escalation | `task.priorityEscalationMinutes` | `3` | Minutes before escalation |
| Low battery threshold | `robot.lowBatteryThreshold` | `15` | Charge alert threshold (%) |

---

## Appendix: Task Type Reference

| Task Type | Description | Typical Priority |
|-----------|-------------|------------------|
| `Deliver` | Deliver items to table | Normal-Urgent |
| `Return` | Return items to kitchen | Normal |
| `Charge` | Navigate to charging station | High |
| `Patrol` | Routine patrol/monitoring | Low |
| `Escort` | Guide guest to table | Normal |
| `Greeting` | Welcome/greet guests | Normal |
| `Service` | General table service | Normal-High |
| `Cleaning` | Clean/bus table | Normal |
| `Custom` | Custom/miscellaneous | Varies |

---

## 8. Long-Run Simulation

### 8.1 Overview

The system supports **time-accelerated simulations** for capacity planning, stress testing, and forecasting. A simulation can compress weeks or months of restaurant operation into minutes or hours of real time.

> **Business Value**: Instead of waiting 30 days to see how your robot fleet performs during a busy month, run a simulation in ~1 hour and get a detailed performance report.

#### Why Use Long-Run Simulation?

| Business Question | How Simulation Answers It |
|-------------------|---------------------------|
| "Can 5 robots handle Saturday dinner rush?" | Run a weekend simulation, check peak utilization and failure rates |
| "How many robots do we need for the holidays?" | Simulate December with 1.5x traffic multiplier, identify bottlenecks |
| "Will robots run out of battery during busy periods?" | Review battery metrics and charging frequency in report |
| "What's our expected delivery success rate?" | Get overall success rate and daily breakdown |
| "When do we need extra human staff?" | Identify hours with high failure rates or task queues |

| Acceleration | Simulated Period | Real Time |
|--------------|------------------|-----------|
| 1x (Real-time) | 1 hour | 1 hour |
| 10x (Fast) | 1 hour | 6 minutes |
| 60x (Very Fast) | 1 hour | 1 minute |
| 720x (Monthly) | 1 month | ~1 hour |
| 8640x (Yearly) | 1 year | ~1 hour |

### 8.2 Quick Start: Running Your First Simulation

**Step 1**: Start a 1-week simulation (takes ~6 minutes at 1008x)
```bash
curl -X POST http://localhost:5199/api/simulation/long-run \
  -H "Content-Type: application/json" \
  -d '{
    "simulatedStartTime": "2026-02-01T07:00:00",
    "simulatedEndTime": "2026-02-08T23:00:00",
    "accelerationFactor": 1008,
    "robotCount": 5,
    "tableCount": 20
  }'
```

**Step 2**: Monitor progress
```bash
curl http://localhost:5199/api/simulation/long-run/progress
# Returns: progressPercent, eventsProcessed, estimatedTimeRemaining
```

**Step 3**: Get the report when complete
```bash
curl http://localhost:5199/api/simulation/long-run/report
```

**Key Metrics to Review**:
| Metric | Healthy Range | Action if Outside |
|--------|---------------|-------------------|
| Success Rate | >95% | Add robots or reduce tables |
| Robot Utilization | 60-85% | <60% = over-staffed, >85% = under-staffed |
| Peak Queue Size | <10 tasks | Indicates dispatch bottleneck |
| Avg Battery | >50% | Adjust charging schedules |

### 8.3 Starting a Long-Run Simulation

**Endpoint**: `POST /api/simulation/long-run`

**Request Example** (Simulate 1 month at 720x speed):
```json
{
  "simulatedStartTime": "2026-02-01T07:00:00",
  "simulatedEndTime": "2026-03-01T23:00:00",
  "accelerationFactor": 720,
  "robotCount": 5,
  "tableCount": 20,
  "randomSeed": 12345,
  "eventPatterns": {
    "averageMealDurationMinutes": 45,
    "guestNeedsHelpProbability": 0.15,
    "averagePartySize": 2.5
  }
}
```

**Response**:
```json
{
  "simulationId": "abc12345",
  "status": "Running",
  "simulatedStartTime": "2026-02-01T07:00:00",
  "simulatedEndTime": "2026-03-01T23:00:00",
  "accelerationFactor": 720,
  "estimatedEventsCount": 45230,
  "estimatedRealDuration": "1h 2m 30s"
}
```

### 8.3 Monitoring Progress

**Endpoint**: `GET /api/simulation/long-run/progress`

**Response**:
```json
{
  "simulationId": "abc12345",
  "state": "Running",
  "currentSimulatedTime": "2026-02-15T14:30:00",
  "progressPercent": 48.5,
  "realElapsedTime": "28m 15s",
  "estimatedTimeRemaining": "30m 5s",
  "eventsProcessed": 21890,
  "totalEventsScheduled": 45230,
  "guestsProcessed": 3420,
  "tasksCreated": 8560,
  "tasksCompleted": 8245,
  "currentSuccessRate": 96.3
}
```

### 8.4 Simulation Controls

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/simulation/long-run` | POST | Start new simulation |
| `/api/simulation/long-run/progress` | GET | Get current progress |
| `/api/simulation/long-run/pause` | POST | Pause simulation |
| `/api/simulation/long-run/resume` | POST | Resume paused simulation |
| `/api/simulation/long-run/stop` | POST | Stop and cancel simulation |
| `/api/simulation/long-run/report` | GET | Get final report |
| `/api/simulation/long-run/acceleration-presets` | GET | Get preset options |

### 8.5 Real-Time Progress via SignalR

The frontend can receive **live progress updates** via SignalR instead of polling.

**Connection**: `wss://{host}/hubs/restaurant`

**Subscribe to Group**: Join the `Simulation` group to receive updates.

**Event**: `SimulationProgressUpdated`

**Payload**:
```json
{
  "simulationId": "abc12345",
  "state": "Running",
  "currentSimulatedTime": "2026-02-15T14:30:00",
  "progressPercent": 48.5,
  "realElapsedTime": "28m 15s",
  "estimatedTimeRemaining": "30m 5s",
  "eventsProcessed": 21890,
  "totalEventsScheduled": 45230,
  "guestsProcessed": 3420,
  "tasksCreated": 8560,
  "tasksCompleted": 8245,
  "tasksFailed": 315,
  "currentSuccessRate": 96.3,
  "timestamp": "2026-01-27T10:30:45Z"
}
```

**Frontend Integration Example** (React):
```jsx
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

function SimulationDashboard() {
  const [progress, setProgress] = useState(null);
  
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/restaurant')
      .withAutomaticReconnect()
      .build();
    
    connection.on('SimulationProgressUpdated', (data) => {
      setProgress(data);
    });
    
    connection.start().then(() => {
      // Subscribe to simulation updates
      connection.invoke('SubscribeToSimulation');
    });
    
    return () => {
      connection.invoke('UnsubscribeFromSimulation');
      connection.stop();
    };
  }, []);

  if (!progress) return <div>No simulation running</div>;

  return (
    <div>
      <h2>Simulation: {progress.simulationId}</h2>
      <progress value={progress.progressPercent} max="100" />
      <p>{progress.progressPercent}% complete</p>
      <p>Simulated: {new Date(progress.currentSimulatedTime).toLocaleString()}</p>
      <p>ETA: {progress.estimatedTimeRemaining}</p>
      <p>Tasks: {progress.tasksCompleted} / {progress.tasksCreated}</p>
      <p>Success Rate: {progress.currentSuccessRate}%</p>
    </div>
  );
}
```

**Update Frequency**: Progress is broadcast every 5 seconds during simulation.

### 8.6 Simulation Report

After completion, retrieve detailed analytics:

**Endpoint**: `GET /api/simulation/long-run/report`

**Report Contents**:

| Section | Metrics Included |
|---------|------------------|
| **Summary** | Total guests, tasks, deliveries, failures, success rate |
| **Peak Analysis** | Peak guest time, peak task volume, peak hours |
| **Robot Performance** | Tasks completed per robot, utilization %, average battery |
| **Daily Breakdown** | Day-by-day metrics with hourly detail |
| **Alerts** | Total alerts, breakdown by type |

**Example Report Summary**:
```json
{
  "summary": {
    "simulationId": "abc12345",
    "simulatedDuration": "28d 16h",
    "realDuration": "1h 2m 30s",
    "totalGuests": 6840,
    "totalTasks": 17100,
    "totalDeliveries": 15200,
    "totalFailures": 342,
    "overallSuccessRate": 98.0,
    "averageTaskDurationSeconds": 32.5,
    "averageRobotUtilization": 72.3,
    "peakGuestCount": 45,
    "peakGuestTime": "2026-02-14T18:30:00"
  }
}
```

### 8.6 Event Generation Patterns

The simulation generates realistic restaurant traffic patterns:

**Hourly Arrival Rates** (guests/hour):
| Hour | Rate | Hour | Rate |
|------|------|------|------|
| 07:00 | 2 | 15:00 | 3 |
| 08:00 | 8 | 16:00 | 4 |
| 09:00 | 6 | 17:00 | 12 |
| 10:00 | 3 | 18:00 | 25 |
| 11:00 | 10 | 19:00 | 22 |
| 12:00 | 20 | 20:00 | 15 |
| 13:00 | 18 | 21:00 | 8 |
| 14:00 | 8 | 22:00 | 3 |

**Day-of-Week Multipliers**:
| Day | Multiplier |
|-----|------------|
| Sunday | 1.3x (Brunch) |
| Monday | 0.7x (Slowest) |
| Tuesday | 0.8x |
| Wednesday | 0.9x |
| Thursday | 1.0x |
| Friday | 1.4x |
| Saturday | 1.5x (Busiest) |

### 8.7 Use Cases

#### Use Case 1: Capacity Planning
**Scenario**: Restaurant manager wants to know if current 5-robot fleet can handle expected traffic.

```
1. Start simulation with 5 robots, 20 tables, 30-day period
2. Wait ~1 hour for simulation to complete
3. Review report:
   - If success rate > 95% and utilization < 85% → Fleet is adequate
   - If success rate < 90% or utilization > 90% → Consider adding robots
```

#### Use Case 2: Pre-Holiday Stress Test
**Scenario**: Testing if system can handle 50% more traffic during holiday season.

```
1. Customize event patterns with higher arrival rates
2. Run 1-week simulation
3. Identify:
   - Peak failure hours (need human backup)
   - Robot battery patterns (adjust charging schedules)
   - Table turnover bottlenecks
```

#### Use Case 3: New Restaurant Layout
**Scenario**: Planning to add 10 more tables, need to validate robot fleet size.

```
1. Run baseline simulation (20 tables, 5 robots) → Get baseline metrics
2. Run comparison simulation (30 tables, 5 robots) → See degradation
3. Run target simulation (30 tables, 7 robots) → Validate improvement
4. Compare success rates and utilization across scenarios
```

#### Use Case 4: ROI Calculation
**Scenario**: Justifying robot investment to stakeholders.

```
1. Run 1-year simulation (8640x speed, ~1 hour)
2. Extract from report:
   - Total deliveries completed: ~62,000/year
   - Average deliveries per robot per day: 34
   - Success rate: 97.5%
3. Calculate: Robot cost vs. labor cost for equivalent deliveries
```

---

*Document generated for RoboRunner Restaurant Robot Management System v1.0*
