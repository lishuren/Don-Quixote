# Backend API Requirements

## Table of Contents

1. [Overview & Architecture](#1-overview--architecture)
   - [1.1 Design Principles](#11-design-principles)
   - [1.2 Base URLs](#12-base-urls)
   - [1.3 Coordinate System](#13-coordinate-system)
2. [Configuration APIs](#2-configuration-apis)
   - [2.1 System Configuration](#21-system-configuration)
   - [2.2 Layout & Map](#22-layout--map)
   - [2.3 Fleet Configuration](#23-fleet-configuration)
   - [2.4 Table Configuration](#24-table-configuration)
   - [2.5 Checkpoint Configuration](#25-checkpoint-configuration)
   - [2.6 Zone Configuration](#26-zone-configuration)
   - [2.7 Dispatch Engine Configuration](#27-dispatch-engine-configuration)
   - [2.8 Alert Rules Configuration](#28-alert-rules-configuration)
   - [2.9 Queue & Limits Configuration](#29-queue--limits-configuration)
   - [2.10 Reservation Configuration](#210-reservation-configuration)
3. [Runtime APIs](#3-runtime-apis)
   - [3.1 Robots (Status & Commands)](#31-robots-status--commands)
   - [3.2 Tasks (Queue & Dispatch)](#32-tasks-queue--dispatch)
   - [3.3 Tables (Status & Actions)](#33-tables-status--actions)
   - [3.4 Guests (Queue & Seating)](#34-guests-queue--seating)
   - [3.5 Zones (Active Restrictions)](#35-zones-active-restrictions)
   - [3.6 Reservations](#36-reservations)
   - [3.7 Alerts (Human Escalation)](#37-alerts-human-escalation)
4. [Simulation APIs](#4-simulation-apis)
   - [4.1 Guest Spawn Settings](#41-guest-spawn-settings)
   - [4.2 Dining Duration Settings](#42-dining-duration-settings)
   - [4.3 Table Turnover Settings](#43-table-turnover-settings)
   - [4.4 Session Control](#44-session-control)
   - [4.5 Fleet Optimization & ROI](#45-fleet-optimization--roi)
5. [Real-Time (SignalR)](#5-real-time-signalr)
   - [5.1 Connection](#51-connection)
   - [5.2 Server → Client Events](#52-server--client-events)
   - [5.3 Client → Server Methods](#53-client--server-methods)
   - [5.4 React Integration](#54-react-integration)
6. [External Integrations](#6-external-integrations)
   - [6.1 Webhook Configuration](#61-webhook-configuration)
   - [6.2 Inbound Webhooks](#62-inbound-webhooks)
   - [6.3 Outbound Events](#63-outbound-events)
   - [6.4 Staff Mobile App](#64-staff-mobile-app)
7. [State Machines](#7-state-machines)
8. [Error Handling](#8-error-handling)
9. [Validation & Edge Cases](#9-validation--edge-cases)
   - [9.1 Robot Validation](#91-robot-validation)
   - [9.2 Table Validation](#92-table-validation)
   - [9.3 Guest Validation](#93-guest-validation)
   - [9.4 Task Validation](#94-task-validation)
   - [9.5 Zone Validation](#95-zone-validation)
   - [9.6 Dispatch Validation](#96-dispatch-validation)
   - [9.7 Configuration Validation](#97-configuration-validation)
   - [9.8 Simulation Validation](#98-simulation-validation)
10. [Reference](#10-reference)
    - [10.1 Alert Categories](#101-alert-categories)
    - [10.2 Alert Severity Levels](#102-alert-severity-levels)
    - [10.3 C# Libraries](#103-c-libraries)
    - [10.4 Frontend Control Panel Mapping](#104-frontend-control-panel-mapping)

---

## 1. Overview & Architecture

The backend serves as the central hub for the **Restaurant Direction Assistant System**, bridging the **Frontend Dashboard** (React/SVG) and the **Physical Robot Fleet**.

### 1.1 Design Principles

| Principle | Description |
|-----------|-------------|
| **Dual Coordinates** | Every positional entity returns both `screen` (pixels) and `physical` (meters) |
| **REST + SignalR** | REST for CRUD operations; SignalR for real-time state push |
| **Stateless REST** | All state lives in the backend; frontend is a "dumb" renderer |
| **Smart Dispatch** | Centralized task queue with failover and load balancing |

### 1.2 Base URLs

| Protocol | URL |
|----------|-----|
| REST API | `https://api.don-quixote.internal/v1` |
| SignalR Hub | `wss://api.don-quixote.internal/hubs/restaurant` |

### 1.3 Coordinate System

All positional responses include both coordinate systems:

```json
{
  "screen": { "x": 450, "y": 300 },      // Pixels (for SVG/Canvas rendering)
  "physical": { "x": 22.5, "y": 15.0 }   // Meters (for info panel display)
}
```

**Layout Metadata** (returned once on init):
```json
{
  "coordinateMapping": {
    "scale": 0.05,
    "screenSize": { "width": 900, "height": 600 },
    "physicalSize": { "width": 45.0, "height": 30.0 }
  }
}
```

---

## 2. Configuration APIs

> **Setup-time endpoints** for configuring the restaurant layout, fleet, tables, and system behavior.

### 2.1 System Configuration

Get or update the entire system configuration in one call.

#### Get System Configuration
*   **GET** `/config`
*   **Response:**
    ```json
    {
      "fleet": {
        "robotCount": 3,
        "robots": [
          {
            "id": "bot_1",
            "name": "Don Quixote",
            "enabled": true,
            "homePosition": {
              "screen": { "x": 50, "y": 550 },
              "physical": { "x": 2.5, "y": 27.5 }
            }
          }
        ]
      },
      "tables": {
        "count": 20,
        "list": [
          {
            "id": "table_1",
            "label": "Table 1",
            "type": "t2",
            "capacity": 2,
            "position": {
              "screen": { "x": 150, "y": 100 },
              "physical": { "x": 7.5, "y": 5.0 }
            },
            "zone": "window",
            "enabled": true
          }
        ]
      },
      "checkpoints": {
        "kitchen": {
          "screen": { "x": 50, "y": 50 },
          "physical": { "x": 2.5, "y": 2.5 }
        },
        "reception": {
          "screen": { "x": 850, "y": 50 },
          "physical": { "x": 42.5, "y": 2.5 }
        },
        "cashier": {
          "screen": { "x": 850, "y": 550 },
          "physical": { "x": 42.5, "y": 27.5 }
        },
        "charger": {
          "screen": { "x": 50, "y": 550 },
          "physical": { "x": 2.5, "y": 27.5 }
        }
      },
      "zones": [
        { "id": "window", "name": "Window Section", "tableIds": ["table_1", "table_5"] },
        { "id": "main", "name": "Main Hall", "tableIds": ["table_2", "table_3", "table_4"] }
      ]
    }
    ```

#### Update System Configuration
*   **PUT** `/config`
*   **Request:** (partial update supported)
    ```json
    { "fleet": { "robotCount": 4 } }
    ```

---

### 2.2 Layout & Map

#### Get Current Layout
*   **GET** `/layouts/current`
*   **Response:**
    ```json
    {
      "id": "layout_main_v2",
      "name": "Main Dining Hall",
      "coordinateMapping": {
        "scale": 0.05,
        "screenSize": { "width": 900, "height": 600 },
        "physicalSize": { "width": 45.0, "height": 30.0 }
      },
      "checkpoints": {
        "kitchen": { "screen": { "x": 50, "y": 50 }, "physical": { "x": 2.5, "y": 2.5 } },
        "reception": { "screen": { "x": 850, "y": 50 }, "physical": { "x": 42.5, "y": 2.5 } },
        "cashier": { "screen": { "x": 850, "y": 550 }, "physical": { "x": 42.5, "y": 27.5 } }
      }
    }
    ```

#### Update Layout Settings
*   **PUT** `/layouts/current`

---

### 2.3 Fleet Configuration

#### Get Fleet Settings
*   **GET** `/config/fleet`
*   **Response:**
    ```json
    {
      "robotCount": 3,
      "maxRobots": 10,
      "robots": [
        {
          "id": "bot_1",
          "name": "Don Quixote",
          "enabled": true,
          "homePosition": { "screen": { "x": 50, "y": 550 }, "physical": { "x": 2.5, "y": 27.5 } },
          "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
          "speedLimit": 1.0
        }
      ]
    }
    ```

#### Set Robot Count
*   **PUT** `/config/fleet/robot-count`
*   **Request:** `{ "count": 4 }`
*   **Validation:** Cannot reduce below active task count
*   **Error:** `INSUFFICIENT_ROBOTS` if constraint violated

#### Add Robot
*   **POST** `/config/fleet/robots`
*   **Request:**
    ```json
    {
      "name": "Dulcinea",
      "homePosition": { "screen": { "x": 140, "y": 550 } },
      "capabilities": ["DELIVERY", "ESCORT"]
    }
    ```

#### Update Robot Settings
*   **PUT** `/config/fleet/robots/{robotId}`

#### Remove Robot
*   **DELETE** `/config/fleet/robots/{robotId}`
*   **Validation:** Cannot delete if robot has active task
*   **Force:** `?force=true&reassignTo=bot_2`

---

### 2.4 Table Configuration

#### Get All Table Settings
*   **GET** `/config/tables`
*   **Response:**
    ```json
    {
      "count": 20,
      "tables": [
        {
          "id": "table_1",
          "label": "Table 1",
          "type": "t2",
          "capacity": 2,
          "position": { "screen": { "x": 150, "y": 100 }, "physical": { "x": 7.5, "y": 5.0 } },
          "zone": "window",
          "enabled": true,
          "reservable": true,
          "features": ["window_view", "quiet"]
        }
      ]
    }
    ```

#### Set Table Count
*   **PUT** `/config/tables/count`
*   **Request:** `{ "count": 25 }`

#### Add Table
*   **POST** `/config/tables`
*   **Validation:** Position not overlapping existing table, within map bounds

#### Update Table Settings
*   **PUT** `/config/tables/{tableId}`

#### Remove Table
*   **DELETE** `/config/tables/{tableId}`
*   **Validation:** Cannot delete if not `AVAILABLE` or has future reservations

#### Bulk Update Tables
*   **POST** `/config/tables/bulk`
*   **Request:**
    ```json
    {
      "mode": "REPLACE",
      "tables": [
        { "id": "table_1", "label": "T1", "type": "t2", "position": { "screen": { "x": 100, "y": 100 } } }
      ]
    }
    ```
*   **Modes:** `REPLACE`, `MERGE`, `APPEND`

---

### 2.5 Checkpoint Configuration

#### Get All Checkpoints
*   **GET** `/config/checkpoints`
*   **Response:**
    ```json
    {
      "checkpoints": [
        {
          "id": "kitchen",
          "name": "Kitchen",
          "type": "SERVICE_HUB",
          "position": { "screen": { "x": 50, "y": 50 }, "physical": { "x": 2.5, "y": 2.5 } },
          "functions": ["FOOD_PICKUP", "DISH_RETURN"]
        },
        {
          "id": "charger",
          "name": "Charging Station",
          "type": "ROBOT_DOCK",
          "position": { "screen": { "x": 50, "y": 550 }, "physical": { "x": 2.5, "y": 27.5 } },
          "functions": ["CHARGING", "IDLE_PARKING"],
          "capacity": 4
        }
      ]
    }
    ```

#### Add/Update/Remove Checkpoint
*   **POST** `/config/checkpoints`
*   **PUT** `/config/checkpoints/{checkpointId}`
*   **DELETE** `/config/checkpoints/{checkpointId}`
*   **Validation:** Cannot delete if referenced by active tasks; recalculates paths if position changed

---

### 2.6 Zone Configuration

Configure how restricted zones behave (auto-merge, expiration).

#### Get Zone Config
*   **GET** `/map/zones/config`
*   **Response:**
    ```json
    {
      "autoMergeEnabled": true,
      "autoMergeThresholdPercent": 30,
      "mergeStrategy": "BOUNDING_BOX",
      "expirationStrategy": "USE_LATEST",
      "typeConflictStrategy": "USE_HIGHEST_SEVERITY"
    }
    ```

#### Update Zone Config
*   **PUT** `/map/zones/config`

**Configuration Options:**

| Setting | Values | Description |
|---------|--------|-------------|
| `autoMergeEnabled` | `true`/`false` | Auto-merge overlapping zones |
| `autoMergeThresholdPercent` | `0-100` | Merge if overlap > threshold |
| `mergeStrategy` | `BOUNDING_BOX`, `UNION_POLYGON`, `CONVEX_HULL` | How to calculate merged bounds |
| `expirationStrategy` | `USE_EARLIEST`, `USE_LATEST`, `USE_MAX` | Which expiration to inherit |
| `typeConflictStrategy` | `USE_HIGHEST_SEVERITY`, `USE_FIRST`, `REQUIRE_SAME` | Type conflict resolution |

**Type Severity Order:** `EMERGENCY` > `WET_FLOOR` > `MAINTENANCE` > `CONSTRUCTION` > `RESERVED`

---

### 2.7 Dispatch Engine Configuration

Control how the system assigns tasks to robots.

#### Get Dispatch Settings
*   **GET** `/fleet/dispatch/config`
*   **Response:**
    ```json
    {
      "algorithm": "NEAREST_AVAILABLE",
      "failoverEnabled": true,
      "failoverTriggers": {
        "blockedTimeoutSeconds": 30,
        "batteryThreshold": 15,
        "errorRetryLimit": 2
      },
      "priorityWeights": {
        "CRITICAL": 100, "HIGH": 50, "NORMAL": 10, "LOW": 1
      },
      "loadBalancing": {
        "enabled": true,
        "maxTasksPerRobot": 3,
        "considerBatteryLevel": true,
        "considerDistanceToTarget": true
      },
      "reservationBuffer": { "enabled": true, "keepIdleRobots": 1 }
    }
    ```

#### Update Dispatch Settings
*   **PUT** `/fleet/dispatch/config`
*   **Algorithms:** `NEAREST_AVAILABLE`, `LEAST_BUSY`, `ROUND_ROBIN`, `BATTERY_OPTIMIZED`, `ZONE_AFFINITY`

#### Get/Update Failover Rules
*   **GET** `/fleet/dispatch/failover-rules`
*   **POST** `/fleet/dispatch/failover-rules`
*   **Response:**
    ```json
    {
      "rules": [
        { "id": "rule_blocked", "trigger": "ROBOT_BLOCKED", "condition": { "durationSeconds": 30 }, "action": "REASSIGN_TO_NEAREST", "enabled": true },
        { "id": "rule_low_battery", "trigger": "LOW_BATTERY", "condition": { "threshold": 15 }, "action": "REASSIGN_AND_DOCK", "enabled": true },
        { "id": "rule_error", "trigger": "ROBOT_ERROR", "condition": { "errorCodes": ["NAV_FAILED", "SENSOR_ERROR"] }, "action": "REASSIGN_IMMEDIATE", "enabled": true }
      ]
    }
    ```

#### Get/Update Charging Policy
*   **GET** `/fleet/dispatch/charging-policy`
*   **PUT** `/fleet/dispatch/charging-policy`
*   **Response:**
    ```json
    {
      "maxSimultaneousCharging": 2,
      "minAvailableRobots": 1,
      "staggerChargingMinutes": 15,
      "lowBatteryThreshold": 20,
      "criticalBatteryThreshold": 10
    }
    ```

#### Get/Update Preemption Rules
*   **GET** `/fleet/dispatch/preemption-rules`
*   **Response:**
    ```json
    {
      "enabled": true,
      "rules": [
        {
          "id": "rule_critical_preempt",
          "condition": { "incomingPriority": "CRITICAL", "currentTaskPriority": ["LOW", "NORMAL"] },
          "action": "PREEMPT_AND_REQUEUE",
          "notifyStaff": true
        }
      ]
    }
    ```

---

### 2.8 Alert Rules Configuration

Configure when alerts are automatically triggered.

#### Get Alert Rules
*   **GET** `/alerts/rules`
*   **Response:**
    ```json
    {
      "rules": [
        {
          "id": "rule_robot_stuck",
          "category": "ROBOT_STUCK",
          "enabled": true,
          "condition": { "blockedDurationSeconds": 120, "failoverAttempts": 2 },
          "severity": "CRITICAL",
          "autoCreateZone": true,
          "notifyChannels": ["DASHBOARD", "MOBILE", "SPEAKER"]
        },
        {
          "id": "rule_guest_wait",
          "category": "GUEST_WAITING",
          "enabled": true,
          "condition": { "waitMinutes": 15, "partySize": { "min": 1 } },
          "severity": "HIGH",
          "escalateAfterMinutes": 25
        },
        {
          "id": "rule_low_fleet",
          "category": "LOW_FLEET_CAPACITY",
          "enabled": true,
          "condition": { "availableRobots": { "lessThan": 1 }, "pendingTasks": { "greaterThan": 3 } },
          "severity": "HIGH"
        },
        {
          "id": "rule_battery_critical",
          "category": "BATTERY_CRITICAL",
          "enabled": true,
          "condition": { "batteryPercent": { "lessThan": 10 }, "notCharging": true },
          "severity": "CRITICAL"
        },
        {
          "id": "rule_collision_risk",
          "category": "COLLISION_DETECTED",
          "enabled": true,
          "condition": { "emergencyStopTriggered": true },
          "severity": "CRITICAL",
          "autoHaltNearbyRobots": true
        }
      ]
    }
    ```

#### Update Alert Rule
*   **PUT** `/alerts/rules/{ruleId}`

---

### 2.9 Queue & Limits Configuration

#### Get Queue Limits
*   **GET** `/config/queue`
*   **Response:**
    ```json
    {
      "maxGuestQueueSize": 50,
      "maxTaskQueueSize": 100,
      "queueFullAction": "REJECT",
      "queueFullAlertEnabled": true
    }
    ```

#### Update Queue Limits
*   **PUT** `/config/queue`

---

### 2.10 Reservation Configuration

#### Get Reservation Config
*   **GET** `/reservations/config`
*   **Response:**
    ```json
    {
      "noShowGracePeriodMinutes": 15,
      "autoReleaseEnabled": true,
      "penaltyTracking": true
    }
    ```

#### Update Reservation Config
*   **PUT** `/reservations/config`

---

## 3. Runtime APIs

> **Operational endpoints** for managing live restaurant operations.

### 3.1 Robots (Status & Commands)

#### Get All Robots
*   **GET** `/fleet/robots`
*   **Response:**
    ```json
    [
      {
        "id": "bot_1",
        "name": "Don Quixote",
        "state": "MOVING",
        "battery": 87,
        "position": { "screen": { "x": 120, "y": 300 }, "physical": { "x": 6.0, "y": 15.0 } },
        "heading": 90,
        "currentTask": { "taskId": "task_101", "type": "DELIVERY", "targetId": "table_5" }
      }
    ]
    ```

#### Get Single Robot
*   **GET** `/fleet/robots/{robotId}`

#### Command Robot
*   **PATCH** `/fleet/robots/{robotId}`
*   **Request:** `{ "command": "RETURN_TO_DOCK" }`
*   **Commands:** `RETURN_TO_DOCK`, `PAUSE`, `RESUME`, `CANCEL_TASK`

#### Get Robot's Task Queue
*   **GET** `/fleet/robots/{robotId}/tasks`

#### Robot Recovery
*   **POST** `/fleet/robots/{robotId}/recover`
*   **Request:** `{ "action": "RESET_AND_RESUME", "clearErrors": true }`
*   **Actions:** `RESET_AND_RESUME`, `RETURN_TO_DOCK`, `FORCE_IDLE`, `REBOOT`

#### Heartbeat Check
*   **GET** `/fleet/robots/{robotId}/heartbeat`
*   **POST** `/fleet/robots/{robotId}/ping`

#### Get Heartbeat Config
*   **GET** `/fleet/heartbeat/config`
*   **Response:**
    ```json
    {
      "intervalSeconds": 5,
      "timeoutSeconds": 10,
      "offlineGracePeriodSeconds": 30,
      "maxMissedBeforeOffline": 3
    }
    ```

---

### 3.2 Tasks (Queue & Dispatch)

#### Get Task Queue
*   **GET** `/fleet/tasks`
*   **Query Params:** `?status=PENDING,ASSIGNED&priority=HIGH`
*   **Response:**
    ```json
    {
      "queue": [
        {
          "taskId": "task_301",
          "type": "DELIVERY",
          "priority": "HIGH",
          "status": "ASSIGNED",
          "targetId": "table_12",
          "target": { "screen": { "x": 400, "y": 300 }, "physical": { "x": 20.0, "y": 15.0 } },
          "assignedRobotId": "bot_1",
          "createdAt": "2026-01-26T19:30:00Z",
          "estimatedCompletion": "2026-01-26T19:31:30Z"
        }
      ],
      "stats": { "pending": 3, "assigned": 2, "inProgress": 1, "avgWaitSeconds": 45 }
    }
    ```

#### Create Task
*   **POST** `/fleet/tasks`
*   **Request Header:** `Idempotency-Key: uuid-12345` (prevents duplicates)
*   **Request:**
    ```json
    {
      "type": "DELIVERY",
      "targetId": "table_12",
      "priority": "HIGH",
      "payload": { "orderId": "order_555", "items": ["Pasta", "Salad"] },
      "constraints": { "maxWaitSeconds": 120, "preferRobotId": "bot_2", "avoidZones": ["zone_99"] }
    }
    ```
*   **Task Types:** `DELIVERY`, `BUSSING`, `ESCORT`, `PATROL`, `RETURN_TO_DOCK`, `CHARGE`
*   **Priority:** `CRITICAL`, `HIGH`, `NORMAL`, `LOW`

#### Create Multi-Step Task Chain
*   **POST** `/fleet/tasks`
*   **Request:**
    ```json
    {
      "type": "DELIVERY",
      "targetId": "table_12",
      "chain": [
        { "step": 1, "type": "PICKUP", "location": "kitchen", "waitForAck": true },
        { "step": 2, "type": "DELIVER", "location": "table_12", "waitForAck": true },
        { "step": 3, "type": "RETURN", "location": "kitchen", "condition": "IF_TRAY_EMPTY" }
      ]
    }
    ```

#### Cancel Task
*   **DELETE** `/fleet/tasks/{taskId}?reason=USER_CANCELLED`

#### Reassign Task
*   **POST** `/fleet/tasks/{taskId}/reassign`
*   **Request:** `{ "robotId": "bot_3", "reason": "MANUAL_OVERRIDE" }`

#### Override Priority
*   **PATCH** `/fleet/tasks/{taskId}/priority`
*   **Request:** `{ "priority": "CRITICAL", "reason": "VIP_GUEST" }`

#### Partial Complete
*   **POST** `/fleet/tasks/{taskId}/partial-complete`
*   **Request:**
    ```json
    {
      "completedItems": ["Pasta"],
      "failedItems": ["Salad"],
      "reason": "ITEM_DROPPED"
    }
    ```

#### Get Dispatch Analytics
*   **GET** `/fleet/dispatch/analytics?period=today`
*   **Response:**
    ```json
    {
      "period": "2026-01-26",
      "tasksTotal": 156,
      "tasksCompleted": 142,
      "tasksFailed": 8,
      "tasksReassigned": 6,
      "avgCompletionSeconds": 87,
      "avgQueueWaitSeconds": 23,
      "failoverBreakdown": { "ROBOT_BLOCKED": 4, "LOW_BATTERY": 1, "ROBOT_ERROR": 1 },
      "robotEfficiency": [
        { "robotId": "bot_1", "completed": 52, "failed": 2, "avgTime": 82 }
      ],
      "peakHour": "19:00",
      "bottleneck": "BLOCKED_ROBOTS"
    }
    ```

---

### 3.3 Tables (Status & Actions)

#### Get All Tables
*   **GET** `/tables`
*   **Response:**
    ```json
    [
      {
        "id": "table_5",
        "label": "Table 5",
        "type": "t4",
        "capacity": 4,
        "state": "OCCUPIED_DINING",
        "position": { "screen": { "x": 200, "y": 150 }, "physical": { "x": 10.0, "y": 7.5 } },
        "currentGuest": { "guestId": "guest_101", "phase": "EATING", "seatedAt": "2026-01-26T19:15:00Z" }
      }
    ]
    ```

#### Get Single Table
*   **GET** `/tables/{tableId}`

#### Update Table State
*   **PUT** `/tables/{tableId}/state`
*   **Request:** `{ "state": "DIRTY" }`
*   **States:** `AVAILABLE`, `RESERVED`, `OCCUPIED_SEATED`, `OCCUPIED_DINING`, `DO_NOT_DISTURB`, `DIRTY`, `CLEANING`, `OUT_OF_SERVICE`

#### Trigger Table Action
*   **POST** `/tables/{tableId}/trigger`
*   **Request:** `{ "action": "MARK_DIRTY" }`
*   **Actions:** `MARK_DIRTY`, `START_CLEANING`, `MARK_READY`, `FORCE_AVAILABLE`

#### Swap Guest Tables
*   **POST** `/tables/swap`
*   **Request:**
    ```json
    {
      "guestId": "guest_101",
      "fromTableId": "table_5",
      "toTableId": "table_8",
      "reason": "GUEST_REQUEST",
      "skipRobotEscort": true
    }
    ```

#### Force Clear Table
*   **POST** `/tables/{tableId}/force-clear`
*   **Request:** `{ "reason": "EMERGENCY", "relocateGuestTo": "table_12" }`

#### Get Table Analytics
*   **GET** `/tables/analytics?period=today`

---

### 3.4 Guests (Queue & Seating)

#### Get Guest Queue
*   **GET** `/guests?status=WAITING`
*   **Response:**
    ```json
    [
      {
        "id": "guest_101",
        "partySize": 4,
        "arrivalTime": "2026-01-26T19:30:00Z",
        "waitMinutes": 12,
        "queuePosition": 3,
        "preferences": ["window", "quiet"]
      }
    ]
    ```

#### Add Walk-in Guest
*   **POST** `/guests`
*   **Request:**
    ```json
    {
      "partySize": 4,
      "preferences": ["booth"],
      "notes": "Birthday celebration"
    }
    ```
*   **Validation:** Queue not at max capacity, party size ≤ max table capacity

#### Assign Guest to Table
*   **POST** `/guests/{guestId}/assign`
*   **Request:** `{ "tableId": "table_8" }`
*   **Effect:** Creates ESCORT task, updates table state

#### Update Guest
*   **PUT** `/guests/{guestId}`
*   **Request:** `{ "partySize": 6, "reason": "ADDITIONAL_GUESTS_ARRIVED" }`
*   **Validation:** If new size > current table capacity, suggests table swap

#### Remove Guest
*   **DELETE** `/guests/{guestId}?reason=LEFT_VOLUNTARILY`
*   **Reasons:** `LEFT_VOLUNTARILY`, `CANCELLED_DURING_ESCORT`, `NO_SHOW`

#### Get Guest Details
*   **GET** `/guests/{guestId}`

---

### 3.5 Zones (Active Restrictions)

#### Get Active Zones
*   **GET** `/map/zones?active=true`
*   **Response:**
    ```json
    [
      {
        "id": "zone_99",
        "label": "Spill near Bar",
        "type": "WET_FLOOR",
        "active": true,
        "bounds": {
          "screen": { "x": 100, "y": 200, "width": 60, "height": 40 },
          "physical": { "x": 5.0, "y": 10.0, "width": 3.0, "height": 2.0 }
        },
        "createdAt": "2026-01-26T19:00:00Z",
        "expiresAt": "2026-01-26T20:00:00Z"
      }
    ]
    ```

#### Create Restricted Zone
*   **POST** `/map/zones`
*   **Request:**
    ```json
    {
      "label": "Private Event",
      "type": "RESERVED",
      "bounds": { "screen": { "x": 300, "y": 100, "width": 200, "height": 150 } },
      "expiresAt": "2026-01-26T23:00:00Z"
    }
    ```
*   **Types:** `WET_FLOOR`, `MAINTENANCE`, `RESERVED`, `CONSTRUCTION`, `EMERGENCY`
*   **Validation:** Does not block ALL paths to any table (unless `force: true`)

#### Update Zone
*   **PATCH** `/map/zones/{zoneId}`
*   **Request:** `{ "active": false, "reason": "Cleaned up" }`

#### Delete Zone
*   **DELETE** `/map/zones/{zoneId}`

#### Merge Zones (Manual)
*   **POST** `/map/zones/merge`
*   **Request:** `{ "zoneIds": ["zone_99", "zone_100"], "newLabel": "Expanded Spill Area" }`

#### Get Aisles (Traffic Control)
*   **GET** `/map/aisles`
*   **GET** `/map/aisles/{aisleId}/reservations`

---

### 3.6 Reservations

#### Get Reservations
*   **GET** `/reservations?date=2026-01-26`

#### Create Reservation
*   **POST** `/reservations`
*   **Request:**
    ```json
    {
      "partySize": 6,
      "dateTime": "2026-01-26T19:00:00Z",
      "name": "Smith",
      "phone": "+1234567890",
      "preferences": ["quiet", "round_table"],
      "notes": "Anniversary dinner"
    }
    ```
*   **Validation:** Table not already reserved, party size ≤ table capacity

#### Check-in Reservation
*   **POST** `/reservations/{reservationId}/check-in`
*   **Effect:** Creates Guest entry with priority seating

#### Cancel Reservation
*   **DELETE** `/reservations/{reservationId}`

---

### 3.7 Alerts (Human Escalation)

The system automatically detects situations beyond robot capabilities.

#### Get Active Alerts
*   **GET** `/alerts?status=ACTIVE`
*   **Query Params:** `?severity=CRITICAL,HIGH&category=ROBOT`
*   **Response:**
    ```json
    {
      "alerts": [
        {
          "id": "alert_501",
          "severity": "CRITICAL",
          "category": "ROBOT_STUCK",
          "title": "Robot Bot_1 stuck for 2+ minutes",
          "message": "Bot_1 is blocked near Table 8. Manual assistance required.",
          "status": "ACTIVE",
          "createdAt": "2026-01-26T19:45:00Z",
          "context": {
            "robotId": "bot_1",
            "taskId": "task_301",
            "position": { "screen": { "x": 320, "y": 180 }, "physical": { "x": 16.0, "y": 9.0 } },
            "blockedDuration": 125
          },
          "suggestedActions": ["Clear obstacle near robot", "Manually push robot to safe zone", "Cancel current task"]
        }
      ],
      "summary": { "critical": 1, "high": 2, "medium": 3, "low": 1 }
    }
    ```

#### Get Alert History
*   **GET** `/alerts/history?period=today`

#### Acknowledge Alert
*   **POST** `/alerts/{alertId}/acknowledge`
*   **Request:** `{ "staffId": "staff_john", "notes": "On my way to help" }`

#### Resolve Alert
*   **POST** `/alerts/{alertId}/resolve`
*   **Request:** `{ "resolution": "MANUAL_INTERVENTION", "notes": "Moved chair blocking robot", "staffId": "staff_john" }`
*   **Resolutions:** `MANUAL_INTERVENTION`, `AUTO_RESOLVED`, `FALSE_ALARM`, `ESCALATED_MANAGER`

#### Dismiss Alert
*   **DELETE** `/alerts/{alertId}?reason=FALSE_ALARM`

#### Create Manual Alert
*   **POST** `/alerts`
*   **Request:**
    ```json
    {
      "severity": "HIGH",
      "category": "SAFETY",
      "title": "Broken glass near Table 3",
      "message": "Customer dropped wine glass, needs cleanup",
      "location": { "screen": { "x": 150, "y": 200 } }
    }
    ```

---

## 4. Simulation APIs

> **Endpoints for testing and optimizing restaurant operations.**

### 4.1 Guest Spawn Settings

#### Get Spawn Settings
*   **GET** `/simulation/guest-spawn`
*   **Response:**
    ```json
    {
      "enabled": true,
      "guestsPerHour": 60,
      "avgPartySize": 3,
      "partySizeDistribution": { "1": 0.10, "2": 0.30, "3": 0.25, "4": 0.25, "5+": 0.10 },
      "peakHours": [
        { "start": "12:00", "end": "14:00", "multiplier": 1.5 },
        { "start": "18:00", "end": "21:00", "multiplier": 2.0 }
      ]
    }
    ```

#### Update Spawn Settings
*   **PUT** `/simulation/guest-spawn`

#### Apply Preset
*   **POST** `/simulation/guest-spawn/preset`
*   **Request:** `{ "preset": "RUSH_HOUR" }`
*   **Presets:** `SLOW`, `NORMAL`, `RUSH_HOUR`, `STRESS_TEST`

#### Toggle Auto-Spawn
*   **PATCH** `/simulation/guest-spawn`
*   **Request:** `{ "enabled": false }`

---

### 4.2 Dining Duration Settings

#### Get Duration Settings
*   **GET** `/simulation/dining-duration`
*   **Response:**
    ```json
    {
      "avgDurationMinutes": 45,
      "minDurationMinutes": 20,
      "maxDurationMinutes": 90,
      "durationByPartySize": { "1-2": 30, "3-4": 45, "5+": 60 },
      "phases": {
        "ordering": 5,
        "waitingForFood": 15,
        "eating": 20,
        "dessertCoffee": 10,
        "payingLeaving": 5,
        "maxLingeringMinutes": 15
      },
      "lingeringAlert": { "enabled": true, "thresholdMinutes": 15, "severity": "LOW" }
    }
    ```

#### Update Duration Settings
*   **PUT** `/simulation/dining-duration`

#### Accelerate Current Diners
*   **POST** `/simulation/dining-duration/accelerate`
*   **Request:** `{ "multiplier": 2.0 }`

---

### 4.3 Table Turnover Settings

#### Get Turnover Settings
*   **GET** `/simulation/table-turnover`
*   **Response:**
    ```json
    {
      "cleaningDurationSeconds": 180,
      "autoAssignEnabled": true,
      "bussingMode": "ROBOT",
      "priorityRules": { "preferLargerTables": false, "balanceZones": true, "reservationBuffer": 15 }
    }
    ```

#### Update Turnover Settings
*   **PUT** `/simulation/table-turnover`
*   **Bussing Modes:** `ROBOT`, `STAFF`, `HYBRID`

---

### 4.4 Session Control

#### Start Simulation
*   **POST** `/simulation/sessions`
*   **Request:** `{ "robotCount": 3, "timeScale": 10.0, "durationMinutes": 240 }`

#### Control Simulation
*   **PUT** `/simulation/sessions/{sessionId}/state`
*   **Request:** `{ "action": "PAUSE" }`
*   **Actions:** `START`, `PAUSE`, `RESUME`, `STOP`, `STEP`

#### Change Time Scale Mid-Simulation
*   **PATCH** `/simulation/sessions/{sessionId}`
*   **Request:** `{ "timeScale": 20.0 }`

#### Get Simulation Status
*   **GET** `/simulation/sessions/{sessionId}`

#### Get Session Summary
*   **GET** `/simulation/sessions/{sessionId}/summary`
*   **Response:**
    ```json
    {
      "sessionId": "sim_12345",
      "duration": { "simulatedMinutes": 480, "realSeconds": 58 },
      "fleetPerformance": {
        "robotCount": 3,
        "avgUtilization": 0.72,
        "tasksCompleted": 178,
        "tasksFailed": 2,
        "failoverEvents": 3
      },
      "serviceMetrics": {
        "guestsServed": 180,
        "guestsAbandoned": 0,
        "avgWaitMinutes": 3.1,
        "maxWaitMinutes": 8,
        "serviceLevel": 0.97
      },
      "bottlenecks": [
        { "type": "PEAK_HOUR_CONGESTION", "time": "19:00-20:00", "impact": "Wait time increased 40%" }
      ],
      "recommendation": { "currentFleetAdequate": true, "suggestedChanges": [] }
    }
    ```

#### Compare Simulations
*   **POST** `/simulation/compare`
*   **Request:** `{ "sessionIds": ["sim_12345", "sim_12346", "sim_12347"] }`

---

### 4.5 Fleet Optimization & ROI

#### Run Fleet Optimization
*   **POST** `/simulation/fleet-optimization`
*   **Request:**
    ```json
    {
      "layoutId": "layout_main_v2",
      "testRobotCounts": [1, 2, 3, 4, 5, 6],
      "simulationParams": { "durationMinutes": 480, "timeScale": 50.0, "guestsPerHour": 60 },
      "constraints": { "maxAcceptableWaitMinutes": 5, "minServiceLevel": 0.95, "budgetMaxRobots": 6 },
      "costParams": {
        "robotLeaseMonthly": 500.00,
        "robotMaintenanceMonthly": 50.00,
        "laborCostHourly": 15.00,
        "laborHoursReplacedPerRobot": 4
      }
    }
    ```

#### Get Optimization Results
*   **GET** `/simulation/fleet-optimization/{analysisId}`
*   **Response:**
    ```json
    {
      "analysisId": "analysis_789",
      "status": "COMPLETED",
      "recommendation": {
        "optimalRobotCount": 3,
        "confidence": 0.92,
        "reasoning": "3 robots provide optimal balance of service level (97%) and cost efficiency."
      },
      "scenarios": [
        {
          "robotCount": 3,
          "metrics": { "avgWaitMinutes": 3.1, "serviceLevel": 0.97, "robotUtilization": 0.72 },
          "cost": { "monthlyRobotCost": 1650.00, "monthlyLaborSaved": 2700.00, "netMonthlySavings": 1050.00 },
          "verdict": "OPTIMAL"
        }
      ],
      "charts": {
        "waitTimeVsRobots": [{ "robots": 1, "avgWait": 12.5 }, { "robots": 3, "avgWait": 3.1 }],
        "roiVsRobots": [{ "robots": 1, "netSavings": 350 }, { "robots": 3, "netSavings": 1050 }]
      },
      "breakEvenAnalysis": { "monthsToBreakEven": 4.2, "annualSavings": 12600.00, "fiveYearROI": 2.8 }
    }
    ```

#### Resume Interrupted Optimization
*   **POST** `/simulation/fleet-optimization/{analysisId}/resume`

---

## 5. Real-Time (SignalR)

### 5.1 Connection

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("wss://api.don-quixote.internal/hubs/restaurant")
    .withAutomaticReconnect()
    .build();

await connection.start();
```

### 5.2 Server → Client Events

#### Robot Events
| Event | Payload | Description |
|-------|---------|-------------|
| `RobotMoved` | `{ robotId, position, heading }` | Position update (10Hz) |
| `RobotStateChanged` | `{ robotId, state, battery, currentTask }` | State transition |
| `RobotBlocked` | `{ robotId, position, obstacleType }` | Obstacle encountered |
| `RobotBlockedTimeout` | `{ robotId, taskId, blockedSeconds }` | Failover triggered |
| `RobotOffline` | `{ robotId, lastPosition, lastSeen, activeTaskId }` | Lost connectivity |

#### Task Events
| Event | Payload | Description |
|-------|---------|-------------|
| `TaskQueued` | `{ taskId, type, priority, queuePosition }` | New task added |
| `TaskAssigned` | `{ taskId, robotId, estimatedSeconds }` | Robot picked up task |
| `TaskReassigned` | `{ taskId, oldRobotId, newRobotId, reason }` | Failover triggered |
| `TaskProgressed` | `{ taskId, robotId, progress, eta }` | Progress update |
| `TaskCompleted` | `{ taskId, robotId, durationSeconds }` | Successfully done |
| `TaskFailed` | `{ taskId, robotId, error, willRetry }` | Task failed |
| `TaskPartialComplete` | `{ taskId, completedItems, failedItems }` | Partial delivery |
| `TaskPreempted` | `{ preemptedTaskId, preemptedBy, robotId }` | Lower priority displaced |
| `TaskChainStepCompleted` | `{ taskId, chainId, step, totalSteps }` | Multi-step progress |

#### Table Events
| Event | Payload | Description |
|-------|---------|-------------|
| `TableStateChanged` | `{ tableId, oldState, newState }` | State transition |
| `TableTurnoverComplete` | `{ tableId, cycleTimeMinutes }` | Full cycle completed |

#### Guest Events
| Event | Payload | Description |
|-------|---------|-------------|
| `GuestQueued` | `{ guestId, partySize, queuePosition }` | New guest |
| `GuestSeated` | `{ guestId, tableId, robotId }` | Escorted to table |
| `GuestPhaseChanged` | `{ guestId, phase, remainingMinutes }` | Dining phase changed |
| `GuestLingering` | `{ guestId, tableId, lingeringMinutes }` | Overstaying |
| `GuestDeparted` | `{ guestId, tableId, totalDurationMinutes }` | Left |
| `ReservationNoShow` | `{ reservationId, tableId, tableReleasedAt }` | No-show detected |

#### Zone Events
| Event | Payload | Description |
|-------|---------|-------------|
| `ZoneCreated` | `{ zoneId, label, bounds, type }` | New zone |
| `ZoneUpdated` | `{ zoneId, active, reason }` | Status changed |
| `ZoneRemoved` | `{ zoneId }` | Deleted |
| `ZonesAutoMerged` | `{ mergedZoneId, originalZoneIds, newBounds }` | Auto-merged |

#### Alert Events
| Event | Payload | Description |
|-------|---------|-------------|
| `AlertCreated` | `{ alertId, severity, category, title, context }` | New alert |
| `AlertAcknowledged` | `{ alertId, staffId }` | Staff saw it |
| `AlertResolved` | `{ alertId, resolution, staffId }` | Handled |
| `AlertEscalated` | `{ alertId, fromSeverity, toSeverity }` | Auto-escalation |

#### Simulation Events
| Event | Payload | Description |
|-------|---------|-------------|
| `SimulationTick` | `{ simulatedTime, guestsServed, avgWait }` | Periodic stats |
| `SimulationCompleted` | `{ sessionId, summary }` | Finished |
| `SimulationTimeScaleChanged` | `{ sessionId, oldScale, newScale }` | Speed changed |
| `OptimizationProgress` | `{ analysisId, progress }` | Optimization progress |
| `OptimizationCompleted` | `{ analysisId, recommendation }` | Optimization done |

### 5.3 Client → Server Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToRobot` | `robotId` | High-frequency updates |
| `UnsubscribeFromRobot` | `robotId` | Stop updates |
| `SubscribeToFloor` | `floorId` | All floor events |
| `SubscribeToTable` | `tableId` | Table updates |

### 5.4 React Integration

```javascript
useEffect(() => {
  connection.on("RobotMoved", (data) => {
    setRobots(prev => prev.map(r => 
      r.id === data.robotId ? { ...r, position: data.position, heading: data.heading } : r
    ));
  });

  connection.on("AlertCreated", (alert) => {
    setAlerts(prev => [...prev, alert]);
    if (alert.severity === "CRITICAL") {
      playAlarmSound();
      showFullScreenNotification(alert);
    }
  });

  return () => {
    connection.off("RobotMoved");
    connection.off("AlertCreated");
  };
}, []);
```

---

## 6. External Integrations

### 6.1 Webhook Configuration

#### Get Webhooks
*   **GET** `/webhooks`

#### Create Webhook
*   **POST** `/webhooks`
*   **Request:**
    ```json
    {
      "name": "POS Integration",
      "url": "https://pos.restaurant.com/api/robot-events",
      "events": ["ORDER_READY", "DELIVERY_COMPLETE", "ALERT_CREATED"],
      "secret": "my-webhook-secret"
    }
    ```

### 6.2 Inbound Webhooks

#### POS: Order Ready
*   **POST** `/integrations/pos/order-ready`
*   **Request:** `{ "orderId": "order_555", "tableId": "table_12", "items": ["Pasta"], "priority": "NORMAL" }`
*   **Effect:** Auto-creates DELIVERY task

#### Payment: Complete
*   **POST** `/integrations/payment/complete`
*   **Request:** `{ "tableId": "table_5", "transactionId": "txn_12345" }`
*   **Effect:** Guest state → `PAYING`, then auto-create BUSSING task

#### Emergency: Fire Alarm
*   **POST** `/integrations/emergency/fire-alarm`
*   **Request:** `{ "alarmType": "FIRE", "zone": "ALL" }`
*   **Effect:** All robots → `EMERGENCY_STOP`, all tasks → `SUSPENDED`, CRITICAL alert

### 6.3 Outbound Events

| Event | Payload | Trigger |
|-------|---------|---------|
| `ORDER_READY_PICKUP` | `{ orderId, robotId, eta }` | Robot assigned |
| `DELIVERY_COMPLETE` | `{ orderId, tableId, robotId }` | Food delivered |
| `ALERT_CREATED` | `{ alertId, severity, category }` | Human needed |
| `ALERT_RESOLVED` | `{ alertId, resolution }` | Handled |
| `GUEST_SEATED` | `{ guestId, tableId, partySize }` | At table |
| `GUEST_DEPARTED` | `{ guestId, tableId, duration }` | Left |
| `ROBOT_OFFLINE` | `{ robotId, lastPosition }` | Disconnected |
| `ROBOT_ERROR` | `{ robotId, errorCode }` | Hardware/software error |

### 6.4 Staff Mobile App

#### Notify Staff
*   **POST** `/integrations/staff-app/notify`
*   **Request:**
    ```json
    {
      "staffIds": ["staff_john", "staff_jane"],
      "alertId": "alert_501",
      "priority": "HIGH",
      "channels": ["PUSH", "SMS"]
    }
    ```

---

## 7. State Machines

### Robot States
```
IDLE → MOVING → ARRIVED → RETURNING → IDLE
  ↓       ↓         ↓
CHARGING  BLOCKED → WAITING_AISLE → MOVING
  ↓         ↓
OFFLINE   ERROR → RECOVERY → IDLE
  ↓
EMERGENCY_STOP → IDLE (after clearance)
```

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| `IDLE` | No task, at dock or waiting | → `MOVING`, `CHARGING`, `EMERGENCY_STOP` |
| `MOVING` | En route to target | → `ARRIVED`, `BLOCKED`, `ERROR` |
| `ARRIVED` | At destination, performing action | → `RETURNING`, `IDLE` |
| `RETURNING` | Going back to dock | → `IDLE`, `BLOCKED` |
| `BLOCKED` | Obstacle detected, waiting | → `MOVING`, `WAITING_AISLE`, `ERROR` |
| `WAITING_AISLE` | Yielding in aisle | → `MOVING` |
| `CHARGING` | At charger | → `IDLE` |
| `ERROR` | Hardware/software fault | → `RECOVERY`, `OFFLINE` |
| `RECOVERY` | Attempting self-recovery | → `IDLE`, `ERROR` |
| `OFFLINE` | Lost connectivity | → `IDLE` (when reconnected) |
| `EMERGENCY_STOP` | Emergency halt | → `IDLE` (after manual clearance) |

### Task States
```
PENDING → ASSIGNED → IN_PROGRESS → COMPLETED
    ↓         ↓           ↓            ↓
 CANCELLED  REASSIGNED  PARTIAL    VERIFIED
                ↓         ↓
              FAILED → RETRY → ASSIGNED
                ↓
            ESCALATED → MANUAL_COMPLETE
                ↓
            PREEMPTED → REQUEUED → PENDING
```

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| `PENDING` | In queue, no robot assigned | → `ASSIGNED`, `CANCELLED` |
| `ASSIGNED` | Robot picked up task | → `IN_PROGRESS`, `REASSIGNED`, `PREEMPTED` |
| `IN_PROGRESS` | Robot executing | → `COMPLETED`, `PARTIAL`, `FAILED` |
| `COMPLETED` | Successfully finished | → `VERIFIED` (if verification enabled) |
| `PARTIAL` | Partially completed | → `VERIFIED`, `MANUAL_COMPLETE` |
| `FAILED` | Task failed | → `RETRY`, `ESCALATED` |
| `RETRY` | Retrying with same/different robot | → `ASSIGNED` |
| `REASSIGNED` | Moved to different robot (failover) | → `ASSIGNED` |
| `PREEMPTED` | Displaced by higher priority | → `REQUEUED` → `PENDING` |
| `ESCALATED` | Needs human intervention | → `MANUAL_COMPLETE` |
| `CANCELLED` | User/system cancelled | Terminal |

### Guest States
```
QUEUED → ESCORTING → SEATED → ORDERING → WAITING_FOOD → EATING → PAYING → LINGERING → DEPARTED
    ↓         ↓                                                      ↓
 LEFT_QUEUE  ESCORT_CANCELLED                                   DEPARTED
                                                                     ↓
                                                                NO_SHOW (reservation)
```

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| `QUEUED` | Waiting for table | → `ESCORTING`, `LEFT_QUEUE` |
| `ESCORTING` | Robot guiding to table | → `SEATED`, `ESCORT_CANCELLED` |
| `SEATED` | At table, not ordered | → `ORDERING` |
| `ORDERING` | Placing order | → `WAITING_FOOD` |
| `WAITING_FOOD` | Order placed, waiting | → `EATING` |
| `EATING` | Food arrived, dining | → `PAYING` |
| `PAYING` | Bill requested/paying | → `LINGERING`, `DEPARTED` |
| `LINGERING` | Paid but still at table | → `DEPARTED` |
| `DEPARTED` | Left restaurant | Terminal |
| `NO_SHOW` | Reservation not checked in | Terminal |

### Table States
```
AVAILABLE → RESERVED → OCCUPIED_SEATED → OCCUPIED_DINING → DO_NOT_DISTURB → DIRTY → CLEANING → AVAILABLE
                                                                              ↓
                                                                        OUT_OF_SERVICE
```

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| `AVAILABLE` | Ready for seating | → `RESERVED`, `OCCUPIED_SEATED` |
| `RESERVED` | Reservation holds | → `OCCUPIED_SEATED`, `AVAILABLE` |
| `OCCUPIED_SEATED` | Guest just sat | → `OCCUPIED_DINING` |
| `OCCUPIED_DINING` | Guest eating | → `DO_NOT_DISTURB`, `DIRTY` |
| `DO_NOT_DISTURB` | Guest requested privacy | → `DIRTY` |
| `DIRTY` | Needs cleaning | → `CLEANING` |
| `CLEANING` | Robot/staff cleaning | → `AVAILABLE` |
| `OUT_OF_SERVICE` | Maintenance/disabled | → `AVAILABLE` |

### Alert States
```
TRIGGERED → ACTIVE → ACKNOWLEDGED → RESOLVED
               ↓          ↓
           ESCALATED   ESCALATED → ACKNOWLEDGED → RESOLVED
               ↓
           AUTO_RESOLVED
```

| State | Description | Valid Transitions |
|-------|-------------|-------------------|
| `TRIGGERED` | Just created | → `ACTIVE` |
| `ACTIVE` | Visible, unhandled | → `ACKNOWLEDGED`, `ESCALATED`, `AUTO_RESOLVED` |
| `ACKNOWLEDGED` | Staff saw it | → `RESOLVED`, `ESCALATED` |
| `ESCALATED` | Upgraded severity | → `ACKNOWLEDGED`, `RESOLVED` |
| `RESOLVED` | Handled | Terminal |
| `AUTO_RESOLVED` | System resolved | Terminal |

### Failover Flow Example
```
1. Task "DELIVERY to Table 12" assigned to Bot_1
2. Bot_1 starts moving (TaskAssigned event)
3. Bot_1 encounters obstacle, enters BLOCKED state
4. 30 seconds pass, failover triggers (RobotBlockedTimeout)
5. Dispatch engine finds Bot_2 available
6. Task reassigned to Bot_2 (TaskReassigned event)
7. Bot_1 marked as BLOCKED, staff notified
8. Bot_2 completes delivery (TaskCompleted)
```

---

## 8. Error Handling

### Standard Error Response
```json
{
  "error": {
    "code": "TABLE_NOT_AVAILABLE",
    "message": "Table 5 is currently occupied",
    "details": { "tableId": "table_5", "currentState": "OCCUPIED_DINING" },
    "suggestions": ["Wait for table to become available", "Choose a different table"]
  }
}
```

### Error Codes

| Code | HTTP | Description |
|------|------|-------------|
| `NOT_FOUND` | 404 | Resource does not exist |
| `INVALID_STATE` | 409 | Invalid state transition |
| `TABLE_NOT_AVAILABLE` | 409 | Table cannot be assigned |
| `ROBOT_BUSY` | 409 | Robot has active task |
| `ROBOT_HAS_ACTIVE_TASK` | 409 | Cannot delete robot with task |
| `ZONE_OVERLAP` | 400 | Zone overlaps existing |
| `ZONE_BLOCKS_ACCESS` | 400 | Zone blocks all paths to table |
| `CAPACITY_EXCEEDED` | 400 | Party size > table capacity |
| `PARTY_TOO_LARGE` | 400 | No table can fit party |
| `QUEUE_FULL` | 400 | Guest queue at max |
| `TABLE_POSITION_CONFLICT` | 400 | Tables overlap |
| `POSITION_OUT_OF_BOUNDS` | 400 | Outside map |
| `TARGET_NOT_FOUND` | 404 | Task target doesn't exist |
| `TARGET_UNREACHABLE` | 400 | No path to target |
| `TIME_SLOT_UNAVAILABLE` | 409 | Reservation conflict |
| `CHECKPOINT_IN_USE` | 409 | Cannot delete active checkpoint |
| `INSUFFICIENT_ROBOTS` | 400 | Cannot reduce below active tasks |
| `SIMULATION_NOT_RUNNING` | 400 | Control when stopped |
| `NO_ROBOTS_AVAILABLE` | 503 | All robots busy/offline |
| `REASSIGN_FAILED` | 500 | No alternate robot found |
| `ALERT_ALREADY_RESOLVED` | 409 | Already handled |
| `IDEMPOTENCY_CONFLICT` | 409 | Duplicate request with same key |
| `MERGE_TYPE_CONFLICT` | 400 | Zone types incompatible |

---

## 9. Validation & Edge Cases

### 9.1 Robot Validation

| Scenario | Handling |
|----------|----------|
| Robot loses connectivity | Mark `OFFLINE` after timeout; reassign tasks; create alert |
| Robot battery critical | Force return to dock; reassign active task |
| Robot blocked > threshold | Trigger failover; reassign task; create alert |
| Multiple robots need charging | Stagger charging; keep min available |
| Robot in error state | Attempt auto-recovery; escalate if fails |
| Delete robot with active task | Reject or `?force=true&reassignTo=bot_x` |
| All robots offline | Queue tasks; create CRITICAL alert |

### 9.2 Table Validation

| Scenario | Handling |
|----------|----------|
| Delete occupied table | Reject with `TABLE_NOT_AVAILABLE` |
| Delete table with reservation | Reject or auto-reassign reservation |
| Table position overlaps | Reject with `TABLE_POSITION_CONFLICT` |
| Table outside map bounds | Reject with `POSITION_OUT_OF_BOUNDS` |
| Change capacity with seated guest | Allow but warn if party > new capacity |
| Table completely blocked by zone | Create alert; suggest zone modification |

### 9.3 Guest Validation

| Scenario | Handling |
|----------|----------|
| Party size > largest table | Reject with `PARTY_TOO_LARGE` |
| Guest queue at max | Reject with `QUEUE_FULL` |
| Delete guest mid-escort | Cancel escort task; robot returns |
| Party size increases after seated | Warn if > table capacity; suggest swap |
| Reservation no-show | Wait grace period; release table; mark `NO_SHOW` |
| Guest leaves during escort | Cancel task; mark `ESCORT_CANCELLED` |

### 9.4 Task Validation

| Scenario | Handling |
|----------|----------|
| Target table doesn't exist | Reject with `TARGET_NOT_FOUND` |
| Target unreachable (all paths blocked) | Reject with `TARGET_UNREACHABLE` |
| No robots available | Queue with warning; create alert if prolonged |
| Task timeout exceeded | Mark `FAILED`; reassign or escalate |
| Duplicate task (same idempotency key) | Return existing task ID |
| Cancel completed task | Reject with `INVALID_STATE` |
| Reassign to offline robot | Reject; choose available robot |

### 9.5 Zone Validation

| Scenario | Handling |
|----------|----------|
| Zone blocks all paths to table | Reject unless `force: true`; create alert |
| Zone covers entire map | Reject |
| Overlapping zones created | Auto-merge if enabled; else reject |
| Delete zone with robot inside | Allow; robot may need rerouting |
| Expired zone | Auto-deactivate; clean up |
| Conflicting zone types on merge | Use `typeConflictStrategy` |

### 9.6 Dispatch Validation

| Scenario | Handling |
|----------|----------|
| Reduce robot count below active | Reject with `INSUFFICIENT_ROBOTS` |
| All robots charging | Keep min available; reject new charging |
| High priority task with all busy | Preempt lower priority if enabled |
| Failover exhausts all robots | Escalate to human; suspend task |
| Task queue overflow | Reject new tasks or based on policy |

### 9.7 Configuration Validation

| Scenario | Handling |
|----------|----------|
| Invalid coordinates (negative) | Reject with `INVALID_COORDINATES` |
| Missing required checkpoints | Reject; kitchen required at minimum |
| Checkpoint overlaps table | Warn; allow with confirmation |
| Fleet count = 0 | Allow for setup; warn during operations |
| Invalid dispatch algorithm | Reject; list valid options |
| Zone config merge threshold > 100 | Reject; clamp to valid range |

### 9.8 Simulation Validation

| Scenario | Handling |
|----------|----------|
| Pause already paused | No-op; return current state |
| Resume when not paused | No-op; return current state |
| Time scale ≤ 0 | Reject; minimum 0.1 |
| Guests/hour negative | Reject; set to 0 for none |
| Stop non-existent session | Return `NOT_FOUND` |
| Optimization with 0 robots tested | Reject; need at least 1 |

---

## 10. Reference

### 10.1 Alert Categories

| Category | Description | Typical Trigger |
|----------|-------------|-----------------|
| `ROBOT_STUCK` | Robot cannot move | Blocked > 2 min after failover |
| `ROBOT_ERROR` | Hardware/software failure | Sensor error, nav failure |
| `BATTERY_CRITICAL` | Robot about to die | Battery < 10% not charging |
| `COLLISION_DETECTED` | Emergency stop | Obstacle contact sensor |
| `TASK_TIMEOUT` | Task too long | > 10 min for simple task |
| `GUEST_WAITING` | Long queue wait | Wait > 15 min |
| `GUEST_COMPLAINT` | Manual report | Staff input |
| `TABLE_BLOCKED` | Cannot access | Robot can't reach target |
| `LOW_FLEET_CAPACITY` | Not enough robots | All busy, tasks queued |
| `SPILL_DETECTED` | Liquid on floor | Sensor or manual |
| `SAFETY` | General safety | Manual or sensor |
| `MAINTENANCE` | Robot needs service | Error count threshold |

### 10.2 Alert Severity Levels

| Severity | Response Time | Notification |
|----------|---------------|--------------|
| `CRITICAL` | Immediate | Alarm, all dashboards, mobile push |
| `HIGH` | < 5 min | Dashboard popup, mobile push |
| `MEDIUM` | < 15 min | Dashboard notification |
| `LOW` | When convenient | Log only, daily summary |

### 10.3 C# Libraries

| Purpose | Library | Notes |
|---------|---------|-------|
| Web API | **ASP.NET Core** | Native, high-performance |
| Real-time | **SignalR** | Built into ASP.NET Core |
| Geometry | **NetTopologySuite** | Polygon intersection, point-in-zone |
| Pathfinding | **Roy-T.AStar** | Fast 2D grid A* |
| State Machine | **Stateless** | Robot/Table/Task/Alert transitions |
| Robot Comms | **MQTTnet** | Physical robot telemetry |
| Scheduling | **Quartz.NET** | Simulation event scheduling |
| Validation | **FluentValidation** | Request validation |
| Mapping | **AutoMapper** | DTO ↔ Entity |
| Queue | **MediatR** | In-process task dispatch |
| Notifications | **FirebaseAdmin** | Mobile push |

### 10.4 Frontend Control Panel Mapping

| UI Control | API Endpoint |
|------------|--------------|
| "Add Walk-in" button | `POST /guests` |
| "Guests/Hour" slider | `PUT /simulation/guest-spawn` |
| "Party Size" dropdown | `POST /guests` body |
| "Avg Dining Time" slider | `PUT /simulation/dining-duration` |
| "Cleaning Speed" slider | `PUT /simulation/table-turnover` |
| "Rush Hour" preset | `POST /simulation/guest-spawn/preset` |
| "Speed Up All" button | `POST /simulation/dining-duration/accelerate` |
| Table click → context menu | `POST /tables/{id}/trigger` |
| Draw restricted zone | `POST /map/zones` |
| Clear zone | `DELETE /map/zones/{id}` |
| Robot click → "Return to Dock" | `PATCH /fleet/robots/{id}` |
| Task queue list | `GET /fleet/tasks` |
| Cancel task button | `DELETE /fleet/tasks/{id}` |
| Reassign task dropdown | `POST /fleet/tasks/{id}/reassign` |
| Dispatch algorithm selector | `PUT /fleet/dispatch/config` |
| Failover timeout slider | `PUT /fleet/dispatch/config` |
| Robot task view | `GET /fleet/robots/{id}/tasks` |
| Dispatch analytics panel | `GET /fleet/dispatch/analytics` |
| Alert panel | `GET /alerts?status=ACTIVE` |
| Alert acknowledge | `POST /alerts/{id}/acknowledge` |
| Alert resolve | `POST /alerts/{id}/resolve` |
| Create manual alert | `POST /alerts` |
| Alert rules config | `GET/PUT /alerts/rules` |
| Fleet Optimization | `POST /simulation/fleet-optimization` |
| ROI Charts | `GET /simulation/fleet-optimization/{id}` |
