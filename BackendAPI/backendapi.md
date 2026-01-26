# Backend API Requirements

## Overview
The backend serves as the central hub for the **Restaurant Direction Assistant System**, bridging the **Frontend Dashboard** (React/SVG) and the **Physical Robot Fleet**.

### Key Design Principles
1. **Dual Coordinates:** Every positional entity returns both `screen` (pixels for rendering) and `physical` (meters for display/debugging).
2. **REST + SignalR:** REST for CRUD operations; SignalR for real-time state push.
3. **Stateless REST:** All state lives in the backend; frontend is a "dumb" renderer.
4. **Smart Dispatch:** Centralized task queue with failover and load balancing.

## Base URL
- **REST API:** `https://api.don-quixote.internal/v1`
- **SignalR Hub:** `wss://api.don-quixote.internal/hubs/restaurant`

---

## 1. Coordinate System

All positional responses include both coordinate systems:

```json
{
  "screen": { "x": 450, "y": 300 },      // Pixels (for SVG/Canvas rendering)
  "physical": { "x": 22.5, "y": 15.0 }   // Meters (for info panel display)
}
```

### Layout Metadata (returned once on init)
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

## 2. REST API Endpoints

### 2.1 Layout & Map

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
        }
      }
    }
    ```

#### Update Layout Settings
*   **PUT** `/layouts/current`
*   **Request:**
    ```json
    {
      "name": "Main Dining Hall - Event Mode",
      "coordinateMapping": {
        "scale": 0.05,
        "screenSize": { "width": 900, "height": 600 },
        "physicalSize": { "width": 45.0, "height": 30.0 }
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
      }
    }
    ```

---

### 2.2 System Configuration (Fixed Entities)

Configure the restaurant's fixed entities: robot fleet, tables, and checkpoints.

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
          },
          {
            "id": "bot_2",
            "name": "Sancho Panza",
            "enabled": true,
            "homePosition": {
              "screen": { "x": 80, "y": 550 },
              "physical": { "x": 4.0, "y": 27.5 }
            }
          },
          {
            "id": "bot_3",
            "name": "Rocinante",
            "enabled": true,
            "homePosition": {
              "screen": { "x": 110, "y": 550 },
              "physical": { "x": 5.5, "y": 27.5 }
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
          },
          {
            "id": "table_2",
            "label": "Table 2",
            "type": "t4",
            "capacity": 4,
            "position": {
              "screen": { "x": 250, "y": 100 },
              "physical": { "x": 12.5, "y": 5.0 }
            },
            "zone": "main",
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
        },
        "dishwash": {
          "screen": { "x": 50, "y": 100 },
          "physical": { "x": 2.5, "y": 5.0 }
        }
      },
      "zones": [
        { "id": "window", "name": "Window Section", "tableIds": ["table_1", "table_5"] },
        { "id": "main", "name": "Main Hall", "tableIds": ["table_2", "table_3", "table_4"] },
        { "id": "private", "name": "Private Room", "tableIds": ["table_20"] }
      ]
    }
    ```

#### Update System Configuration
*   **PUT** `/config`
*   **Request:** (partial update supported)
    ```json
    {
      "fleet": {
        "robotCount": 4
      }
    }
    ```

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
          "homePosition": {
            "screen": { "x": 50, "y": 550 },
            "physical": { "x": 2.5, "y": 27.5 }
          },
          "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
          "speedLimit": 1.0
        }
      ]
    }
    ```

#### Set Robot Count
*   **PUT** `/config/fleet/robot-count`
*   **Request:**
    ```json
    { "count": 4 }
    ```
*   **Response:**
    ```json
    {
      "previousCount": 3,
      "newCount": 4,
      "addedRobots": [
        {
          "id": "bot_4",
          "name": "Dulcinea",
          "enabled": true,
          "homePosition": {
            "screen": { "x": 140, "y": 550 },
            "physical": { "x": 7.0, "y": 27.5 }
          }
        }
      ]
    }
    ```

#### Add Robot
*   **POST** `/config/fleet/robots`
*   **Request:**
    ```json
    {
      "name": "Dulcinea",
      "homePosition": {
        "screen": { "x": 140, "y": 550 }
      },
      "capabilities": ["DELIVERY", "ESCORT"]
    }
    ```

#### Update Robot Settings
*   **PUT** `/config/fleet/robots/{robotId}`
*   **Request:**
    ```json
    {
      "name": "Don Quixote II",
      "enabled": false,
      "speedLimit": 0.8
    }
    ```

#### Remove Robot
*   **DELETE** `/config/fleet/robots/{robotId}`

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
          "position": {
            "screen": { "x": 150, "y": 100 },
            "physical": { "x": 7.5, "y": 5.0 }
          },
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
*   **Request:**
    ```json
    { "count": 25 }
    ```

#### Add Table
*   **POST** `/config/tables`
*   **Request:**
    ```json
    {
      "label": "Table 21",
      "type": "t6",
      "capacity": 6,
      "position": {
        "screen": { "x": 400, "y": 300 }
      },
      "zone": "main",
      "features": ["large_party"]
    }
    ```

#### Update Table Settings
*   **PUT** `/config/tables/{tableId}`
*   **Request:**
    ```json
    {
      "label": "VIP Table 1",
      "enabled": true,
      "reservable": true,
      "features": ["vip", "window_view"]
    }
    ```

#### Remove Table
*   **DELETE** `/config/tables/{tableId}`

#### Bulk Update Tables (Layout Import)
*   **POST** `/config/tables/bulk`
*   **Request:**
    ```json
    {
      "mode": "REPLACE",
      "tables": [
        { "id": "table_1", "label": "T1", "type": "t2", "position": { "screen": { "x": 100, "y": 100 } } },
        { "id": "table_2", "label": "T2", "type": "t4", "position": { "screen": { "x": 200, "y": 100 } } }
      ]
    }
    ```
*   **Modes:** `REPLACE` (delete all, add new), `MERGE` (update existing, add new), `APPEND` (add only)

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
          "position": {
            "screen": { "x": 50, "y": 50 },
            "physical": { "x": 2.5, "y": 2.5 }
          },
          "functions": ["FOOD_PICKUP", "DISH_RETURN"]
        },
        {
          "id": "reception",
          "name": "Reception",
          "type": "ENTRY_POINT",
          "position": {
            "screen": { "x": 850, "y": 50 },
            "physical": { "x": 42.5, "y": 2.5 }
          },
          "functions": ["GUEST_CHECKIN", "ESCORT_START"]
        },
        {
          "id": "charger",
          "name": "Charging Station",
          "type": "ROBOT_DOCK",
          "position": {
            "screen": { "x": 50, "y": 550 },
            "physical": { "x": 2.5, "y": 27.5 }
          },
          "functions": ["CHARGING", "IDLE_PARKING"],
          "capacity": 4
        }
      ]
    }
    ```

#### Add Checkpoint
*   **POST** `/config/checkpoints`
*   **Request:**
    ```json
    {
      "id": "restroom",
      "name": "Restrooms",
      "type": "TARGET_POINT",
      "position": {
        "screen": { "x": 700, "y": 500 }
      },
      "functions": ["ESCORT_DESTINATION"]
    }
    ```

#### Update Checkpoint
*   **PUT** `/config/checkpoints/{checkpointId}`
*   **Request:**
    ```json
    {
      "name": "Main Kitchen",
      "position": {
        "screen": { "x": 60, "y": 60 }
      }
    }
    ```

#### Remove Checkpoint
*   **DELETE** `/config/checkpoints/{checkpointId}`

---

### 2.6 Robots (Fleet Runtime Status)

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
        "position": {
          "screen": { "x": 120, "y": 300 },
          "physical": { "x": 6.0, "y": 15.0 }
        },
        "heading": 90,
        "currentTask": {
          "taskId": "task_101",
          "type": "DELIVERY",
          "targetId": "table_5"
        }
      }
    ]
    ```

#### Get Single Robot
*   **GET** `/fleet/robots/{robotId}`

#### Command Robot
*   **PATCH** `/fleet/robots/{robotId}`
*   **Request:**
    ```json
    { "command": "RETURN_TO_DOCK" }
    ```
*   **Commands:** `RETURN_TO_DOCK`, `PAUSE`, `RESUME`, `CANCEL_TASK`

#### Get Robot's Task Queue
*   **GET** `/fleet/robots/{robotId}/tasks`
*   **Response:**
    ```json
    {
      "robotId": "bot_1",
      "currentTask": {
        "taskId": "task_301",
        "type": "DELIVERY",
        "status": "IN_PROGRESS",
        "progress": 0.65,
        "eta": "2026-01-26T19:31:30Z"
      },
      "queuedTasks": [
        {
          "taskId": "task_305",
          "type": "BUSSING",
          "queuePosition": 1
        }
      ],
      "completedToday": 23,
      "failedToday": 1
    }
    ```

---

### 2.7 Task Queue & Dispatch Engine

The backend maintains a centralized task queue with intelligent assignment and failover logic.

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
          "target": {
            "screen": { "x": 400, "y": 300 },
            "physical": { "x": 20.0, "y": 15.0 }
          },
          "assignedRobotId": "bot_1",
          "createdAt": "2026-01-26T19:30:00Z",
          "estimatedCompletion": "2026-01-26T19:31:30Z",
          "retryCount": 0
        },
        {
          "taskId": "task_302",
          "type": "BUSSING",
          "priority": "NORMAL",
          "status": "PENDING",
          "targetId": "table_5",
          "assignedRobotId": null,
          "queuePosition": 1
        }
      ],
      "stats": {
        "pending": 3,
        "assigned": 2,
        "inProgress": 1,
        "avgWaitSeconds": 45
      }
    }
    ```

#### Create Task
*   **POST** `/fleet/tasks`
*   **Request:**
    ```json
    {
      "type": "DELIVERY",
      "targetId": "table_12",
      "priority": "HIGH",
      "payload": {
        "orderId": "order_555",
        "items": ["Pasta", "Salad"]
      },
      "constraints": {
        "maxWaitSeconds": 120,
        "preferRobotId": "bot_2",
        "avoidZones": ["zone_99"]
      }
    }
    ```
*   **Task Types:** `DELIVERY`, `BUSSING`, `ESCORT`, `PATROL`, `RETURN_TO_DOCK`, `CHARGE`
*   **Priority:** `CRITICAL`, `HIGH`, `NORMAL`, `LOW`
*   **Response:**
    ```json
    {
      "taskId": "task_202",
      "assignedRobotId": "bot_2",
      "estimatedArrivalSeconds": 45
    }
    ```

#### Cancel Task
*   **DELETE** `/fleet/tasks/{taskId}?reason=USER_CANCELLED`

#### Reassign Task Manually
*   **POST** `/fleet/tasks/{taskId}/reassign`
*   **Request:**
    ```json
    { "robotId": "bot_3", "reason": "MANUAL_OVERRIDE" }
    ```

---

### 2.8 Dispatch Engine Configuration

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
        "CRITICAL": 100,
        "HIGH": 50,
        "NORMAL": 10,
        "LOW": 1
      },
      "loadBalancing": {
        "enabled": true,
        "maxTasksPerRobot": 3,
        "considerBatteryLevel": true,
        "considerDistanceToTarget": true
      },
      "reservationBuffer": {
        "enabled": true,
        "keepIdleRobots": 1
      }
    }
    ```

#### Update Dispatch Settings
*   **PUT** `/fleet/dispatch/config`
*   **Request:**
    ```json
    {
      "algorithm": "LEAST_BUSY",
      "failoverEnabled": true,
      "failoverTriggers": {
        "blockedTimeoutSeconds": 20
      }
    }
    ```
*   **Algorithms:**
    *   `NEAREST_AVAILABLE` - Closest idle robot
    *   `LEAST_BUSY` - Robot with fewest queued tasks
    *   `ROUND_ROBIN` - Fair distribution
    *   `BATTERY_OPTIMIZED` - Prefer robots with higher charge
    *   `ZONE_AFFINITY` - Prefer robots already in target zone

#### Get Failover Rules
*   **GET** `/fleet/dispatch/failover-rules`
*   **Response:**
    ```json
    {
      "rules": [
        {
          "id": "rule_blocked",
          "trigger": "ROBOT_BLOCKED",
          "condition": { "durationSeconds": 30 },
          "action": "REASSIGN_TO_NEAREST",
          "enabled": true
        },
        {
          "id": "rule_low_battery",
          "trigger": "LOW_BATTERY",
          "condition": { "threshold": 15 },
          "action": "REASSIGN_AND_DOCK",
          "enabled": true
        },
        {
          "id": "rule_error",
          "trigger": "ROBOT_ERROR",
          "condition": { "errorCodes": ["NAV_FAILED", "SENSOR_ERROR"] },
          "action": "REASSIGN_IMMEDIATE",
          "enabled": true
        },
        {
          "id": "rule_timeout",
          "trigger": "TASK_TIMEOUT",
          "condition": { "maxDurationSeconds": 300 },
          "action": "ESCALATE_AND_REASSIGN",
          "enabled": true
        }
      ]
    }
    ```

#### Create/Update Failover Rule
*   **POST** `/fleet/dispatch/failover-rules`
*   **Request:**
    ```json
    {
      "trigger": "ROBOT_BLOCKED",
      "condition": { "durationSeconds": 15 },
      "action": "REASSIGN_TO_NEAREST",
      "notifyStaff": true
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
      "failoverBreakdown": {
        "ROBOT_BLOCKED": 4,
        "LOW_BATTERY": 1,
        "ROBOT_ERROR": 1
      },
      "robotEfficiency": [
        { "robotId": "bot_1", "completed": 52, "failed": 2, "avgTime": 82 },
        { "robotId": "bot_2", "completed": 48, "failed": 3, "avgTime": 91 },
        { "robotId": "bot_3", "completed": 42, "failed": 3, "avgTime": 88 }
      ],
      "peakHour": "19:00",
      "bottleneck": "BLOCKED_ROBOTS"
    }
    ```

---

### 2.9 Tables (Runtime Status)

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
        "position": {
          "screen": { "x": 200, "y": 150 },
          "physical": { "x": 10.0, "y": 7.5 }
        },
        "currentGuest": {
          "guestId": "guest_101",
          "phase": "EATING",
          "seatedAt": "2026-01-26T19:15:00Z"
        },
        "assignedRobotId": null
      }
    ]
    ```

#### Get Single Table
*   **GET** `/tables/{tableId}`

#### Update Table State
*   **PUT** `/tables/{tableId}/state`
*   **Request:**
    ```json
    { "state": "DIRTY" }
    ```
*   **States:** `AVAILABLE`, `RESERVED`, `OCCUPIED_SEATED`, `OCCUPIED_DINING`, `DO_NOT_DISTURB`, `DIRTY`, `CLEANING`

#### Trigger Table Action (Manual Override)
*   **POST** `/tables/{tableId}/trigger`
*   **Request:**
    ```json
    { "action": "MARK_DIRTY" }
    ```
*   **Actions:** `MARK_DIRTY`, `START_CLEANING`, `MARK_READY`, `FORCE_AVAILABLE`

#### Get Table Analytics
*   **GET** `/tables/analytics?period=today`
*   **Response:**
    ```json
    {
      "period": "2026-01-26",
      "tables": [
        {
          "tableId": "table_5",
          "turnovers": 8,
          "avgOccupancyMinutes": 42,
          "avgCleaningSeconds": 165,
          "utilization": 0.78
        }
      ],
      "summary": {
        "totalTurnovers": 64,
        "avgTurnoverMinutes": 52,
        "peakHour": "19:00",
        "bottleneck": "CLEANING"
      }
    }
    ```

---

### 2.10 Guests

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

#### Add Guest Directly (Walk-in)
*   **POST** `/guests`
*   **Request:**
    ```json
    {
      "partySize": 4,
      "preferences": ["booth"],
      "notes": "Birthday celebration"
    }
    ```
*   **Response:**
    ```json
    {
      "id": "guest_102",
      "partySize": 4,
      "arrivalTime": "2026-01-26T19:45:00Z",
      "queuePosition": 3
    }
    ```

#### Assign Guest to Table (Escort)
*   **POST** `/guests/{guestId}/assign`
*   **Request:**
    ```json
    { "tableId": "table_8" }
    ```
*   **Effect:** Creates an ESCORT task, updates table state to `OCCUPIED_SEATED`.

#### Remove Guest from Queue
*   **DELETE** `/guests/{guestId}?reason=LEFT_VOLUNTARILY`

#### Get Guest Details (Seated)
*   **GET** `/guests/{guestId}`
*   **Response:**
    ```json
    {
      "id": "guest_101",
      "partySize": 4,
      "state": "EATING",
      "tableId": "table_5",
      "seatedAt": "2026-01-26T19:35:00Z",
      "currentPhase": "EATING",
      "estimatedDepartureAt": "2026-01-26T20:20:00Z"
    }
    ```

---

### 2.11 Restricted Zones

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
      "bounds": {
        "screen": { "x": 300, "y": 100, "width": 200, "height": 150 }
      },
      "expiresAt": "2026-01-26T23:00:00Z"
    }
    ```
*   **Types:** `WET_FLOOR`, `MAINTENANCE`, `RESERVED`, `CONSTRUCTION`, `EMERGENCY`

#### Update Zone
*   **PATCH** `/map/zones/{zoneId}`
*   **Request:**
    ```json
    { "active": false, "reason": "Cleaned up" }
    ```

#### Delete Zone
*   **DELETE** `/map/zones/{zoneId}`

---

### 2.12 Reservations

#### Get Reservations
*   **GET** `/reservations?date=2026-01-26`
*   **Response:**
    ```json
    [
      {
        "id": "res_301",
        "name": "Smith",
        "partySize": 6,
        "dateTime": "2026-01-26T19:00:00Z",
        "status": "CONFIRMED",
        "suggestedTableId": "table_12",
        "notes": "Anniversary dinner"
      }
    ]
    ```

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

#### Check-in Reservation
*   **POST** `/reservations/{reservationId}/check-in`
*   **Effect:** Creates a Guest entry with priority seating.

#### Cancel Reservation
*   **DELETE** `/reservations/{reservationId}`

---

### 2.13 Alerts & Human Escalation

The system automatically detects situations beyond robot capabilities and alerts staff for intervention.

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
          "message": "Bot_1 is blocked near Table 8 and cannot proceed. Manual assistance required.",
          "status": "ACTIVE",
          "createdAt": "2026-01-26T19:45:00Z",
          "context": {
            "robotId": "bot_1",
            "taskId": "task_301",
            "position": {
              "screen": { "x": 320, "y": 180 },
              "physical": { "x": 16.0, "y": 9.0 }
            },
            "blockedDuration": 125
          },
          "suggestedActions": [
            "Clear obstacle near robot",
            "Manually push robot to safe zone",
            "Cancel current task"
          ]
        },
        {
          "id": "alert_502",
          "severity": "HIGH",
          "category": "GUEST_WAITING",
          "title": "Guest waiting 20+ minutes",
          "message": "Party of 6 (guest_105) has been waiting over 20 minutes with no available tables.",
          "status": "ACTIVE",
          "createdAt": "2026-01-26T19:50:00Z",
          "context": {
            "guestId": "guest_105",
            "partySize": 6,
            "waitMinutes": 22
          },
          "suggestedActions": [
            "Offer complimentary drinks",
            "Combine smaller tables",
            "Expedite current diners"
          ]
        }
      ],
      "summary": {
        "critical": 1,
        "high": 2,
        "medium": 3,
        "low": 1
      }
    }
    ```

#### Get Alert History
*   **GET** `/alerts/history?period=today`

#### Acknowledge Alert
Staff has seen the alert and is working on it.
*   **POST** `/alerts/{alertId}/acknowledge`
*   **Request:**
    ```json
    {
      "staffId": "staff_john",
      "notes": "On my way to help"
    }
    ```

#### Resolve Alert
Mark the issue as handled.
*   **POST** `/alerts/{alertId}/resolve`
*   **Request:**
    ```json
    {
      "resolution": "MANUAL_INTERVENTION",
      "notes": "Moved chair blocking robot path",
      "staffId": "staff_john"
    }
    ```
*   **Resolutions:** `MANUAL_INTERVENTION`, `AUTO_RESOLVED`, `FALSE_ALARM`, `ESCALATED_MANAGER`

#### Dismiss Alert
*   **DELETE** `/alerts/{alertId}?reason=FALSE_ALARM`

#### Create Manual Alert
Staff can raise alerts manually.
*   **POST** `/alerts`
*   **Request:**
    ```json
    {
      "severity": "HIGH",
      "category": "SAFETY",
      "title": "Broken glass near Table 3",
      "message": "Customer dropped wine glass, needs cleanup before robot can pass",
      "location": {
        "screen": { "x": 150, "y": 200 }
      }
    }
    ```

---

### 2.14 Alert Rules & Thresholds

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
          "condition": {
            "blockedDurationSeconds": 120,
            "failoverAttempts": 2
          },
          "severity": "CRITICAL",
          "autoCreateZone": true,
          "notifyChannels": ["DASHBOARD", "MOBILE", "SPEAKER"]
        },
        {
          "id": "rule_guest_wait",
          "category": "GUEST_WAITING",
          "enabled": true,
          "condition": {
            "waitMinutes": 15,
            "partySize": { "min": 1 }
          },
          "severity": "HIGH",
          "escalateAfterMinutes": 25
        },
        {
          "id": "rule_low_fleet",
          "category": "LOW_FLEET_CAPACITY",
          "enabled": true,
          "condition": {
            "availableRobots": { "lessThan": 1 },
            "pendingTasks": { "greaterThan": 3 }
          },
          "severity": "HIGH"
        },
        {
          "id": "rule_battery_critical",
          "category": "BATTERY_CRITICAL",
          "enabled": true,
          "condition": {
            "batteryPercent": { "lessThan": 10 },
            "notCharging": true
          },
          "severity": "CRITICAL"
        },
        {
          "id": "rule_collision_risk",
          "category": "COLLISION_DETECTED",
          "enabled": true,
          "condition": {
            "emergencyStopTriggered": true
          },
          "severity": "CRITICAL",
          "autoHaltNearbyRobots": true
        },
        {
          "id": "rule_task_timeout",
          "category": "TASK_TIMEOUT",
          "enabled": true,
          "condition": {
            "taskDurationMinutes": { "greaterThan": 10 }
          },
          "severity": "MEDIUM"
        },
        {
          "id": "rule_spill_detected",
          "category": "SPILL_DETECTED",
          "enabled": true,
          "condition": {
            "sensorType": "LIQUID_SENSOR"
          },
          "severity": "HIGH",
          "autoCreateZone": true
        }
      ]
    }
    ```

#### Update Alert Rule
*   **PUT** `/alerts/rules/{ruleId}`
*   **Request:**
    ```json
    {
      "enabled": true,
      "condition": {
        "waitMinutes": 10
      },
      "severity": "CRITICAL"
    }
    ```

---

### Alert Categories Reference

| Category | Description | Typical Trigger |
|----------|-------------|-----------------|
| `ROBOT_STUCK` | Robot cannot move | Blocked > 2 min after failover |
| `ROBOT_ERROR` | Hardware/software failure | Sensor error, nav failure |
| `BATTERY_CRITICAL` | Robot about to die | Battery < 10% not charging |
| `COLLISION_DETECTED` | Emergency stop triggered | Obstacle contact sensor |
| `TASK_TIMEOUT` | Task taking too long | > 10 min for simple task |
| `GUEST_WAITING` | Long queue wait | Wait > 15 min |
| `GUEST_COMPLAINT` | Manual report | Staff input |
| `TABLE_BLOCKED` | Cannot access table | Robot cannot reach target |
| `LOW_FLEET_CAPACITY` | Not enough robots | All robots busy, tasks queued |
| `SPILL_DETECTED` | Liquid on floor | Sensor or manual report |
| `SAFETY` | General safety issue | Manual or sensor |
| `MAINTENANCE` | Robot needs service | Error count threshold |

### Alert Severity Levels

| Severity | Response Time | Notification |
|----------|---------------|--------------|
| `CRITICAL` | Immediate | Sound alarm, all dashboards, mobile push |
| `HIGH` | < 5 min | Dashboard popup, mobile push |
| `MEDIUM` | < 15 min | Dashboard notification |
| `LOW` | When convenient | Log only, daily summary |

---

## 3. Simulation Control API

### 3.1 Guest Spawn Settings

#### Get Spawn Settings
*   **GET** `/simulation/guest-spawn`
*   **Response:**
    ```json
    {
      "enabled": true,
      "guestsPerHour": 60,
      "avgPartySize": 3,
      "partySizeDistribution": {
        "1": 0.10,
        "2": 0.30,
        "3": 0.25,
        "4": 0.25,
        "5+": 0.10
      },
      "peakHours": [
        { "start": "12:00", "end": "14:00", "multiplier": 1.5 },
        { "start": "18:00", "end": "21:00", "multiplier": 2.0 }
      ]
    }
    ```

#### Update Spawn Settings
*   **PUT** `/simulation/guest-spawn`
*   **Request:**
    ```json
    {
      "enabled": true,
      "guestsPerHour": 80,
      "avgPartySize": 2.5,
      "peakHours": [
        { "start": "19:00", "end": "21:00", "multiplier": 2.5 }
      ]
    }
    ```

#### Apply Preset
*   **POST** `/simulation/guest-spawn/preset`
*   **Request:**
    ```json
    { "preset": "RUSH_HOUR" }
    ```
*   **Presets:** `SLOW`, `NORMAL`, `RUSH_HOUR`, `STRESS_TEST`

#### Toggle Auto-Spawn
*   **PATCH** `/simulation/guest-spawn`
*   **Request:**
    ```json
    { "enabled": false }
    ```

---

### 3.2 Dining Duration Settings

#### Get Duration Settings
*   **GET** `/simulation/dining-duration`
*   **Response:**
    ```json
    {
      "avgDurationMinutes": 45,
      "minDurationMinutes": 20,
      "maxDurationMinutes": 90,
      "durationByPartySize": {
        "1-2": 30,
        "3-4": 45,
        "5+": 60
      },
      "phases": {
        "ordering": 5,
        "waitingForFood": 15,
        "eating": 20,
        "dessertCoffee": 10,
        "payingLeaving": 5
      }
    }
    ```

#### Update Duration Settings
*   **PUT** `/simulation/dining-duration`
*   **Request:**
    ```json
    {
      "avgDurationMinutes": 35,
      "durationByPartySize": {
        "1-2": 25,
        "3-4": 35,
        "5+": 50
      }
    }
    ```

#### Accelerate Current Diners
*   **POST** `/simulation/dining-duration/accelerate`
*   **Request:**
    ```json
    { "multiplier": 2.0 }
    ```

---

### 3.3 Table Turnover Settings

#### Get Turnover Settings
*   **GET** `/simulation/table-turnover`
*   **Response:**
    ```json
    {
      "cleaningDurationSeconds": 180,
      "autoAssignEnabled": true,
      "bussingMode": "ROBOT",
      "priorityRules": {
        "preferLargerTables": false,
        "balanceZones": true,
        "reservationBuffer": 15
      }
    }
    ```

#### Update Turnover Settings
*   **PUT** `/simulation/table-turnover`
*   **Request:**
    ```json
    {
      "cleaningDurationSeconds": 120,
      "bussingMode": "HYBRID",
      "autoAssignEnabled": true
    }
    ```
*   **Bussing Modes:** `ROBOT`, `STAFF`, `HYBRID`

---

### 3.4 Simulation Session Control

#### Start Simulation
*   **POST** `/simulation/sessions`
*   **Request:**
    ```json
    {
      "robotCount": 3,
      "timeScale": 10.0,
      "durationMinutes": 240
    }
    ```
*   **Response:**
    ```json
    {
      "sessionId": "sim_12345",
      "status": "RUNNING",
      "startedAt": "2026-01-26T10:00:00Z"
    }
    ```

#### Control Simulation
*   **PUT** `/simulation/sessions/{sessionId}/state`
*   **Request:**
    ```json
    { "action": "PAUSE" }
    ```
*   **Actions:** `START`, `PAUSE`, `RESUME`, `STOP`, `STEP`

#### Get Simulation Status
*   **GET** `/simulation/sessions/{sessionId}`

---

### 3.5 Fleet Size Optimization & ROI Analysis

After running simulations, the system analyzes results and recommends the optimal robot count.

#### Run Fleet Optimization Analysis
Run multiple simulations with varying robot counts to find the optimal fleet size.
*   **POST** `/simulation/fleet-optimization`
*   **Request:**
    ```json
    {
      "layoutId": "layout_main_v2",
      "testRobotCounts": [1, 2, 3, 4, 5, 6],
      "simulationParams": {
        "durationMinutes": 480,
        "timeScale": 50.0,
        "guestsPerHour": 60,
        "peakMultiplier": 2.0
      },
      "constraints": {
        "maxAcceptableWaitMinutes": 5,
        "minServiceLevel": 0.95,
        "budgetMaxRobots": 6
      },
      "costParams": {
        "robotLeaseMonthly": 500.00,
        "robotMaintenanceMonthly": 50.00,
        "laborCostHourly": 15.00,
        "laborHoursReplacedPerRobot": 4
      }
    }
    ```
*   **Response:**
    ```json
    {
      "analysisId": "analysis_789",
      "status": "RUNNING",
      "progress": 0,
      "estimatedCompletionMinutes": 5
    }
    ```

#### Get Optimization Results
*   **GET** `/simulation/fleet-optimization/{analysisId}`
*   **Response:**
    ```json
    {
      "analysisId": "analysis_789",
      "status": "COMPLETED",
      "completedAt": "2026-01-26T11:05:00Z",
      "recommendation": {
        "optimalRobotCount": 3,
        "confidence": 0.92,
        "reasoning": "3 robots provide optimal balance of service level (97%) and cost efficiency. Adding a 4th robot improves wait time by only 8% but increases costs by 33%."
      },
      "scenarios": [
        {
          "robotCount": 1,
          "metrics": {
            "avgWaitMinutes": 12.5,
            "maxWaitMinutes": 28,
            "serviceLevel": 0.72,
            "tasksCompleted": 145,
            "tasksFailed": 23,
            "robotUtilization": 0.98,
            "guestsServed": 168,
            "guestsAbandoned": 12
          },
          "cost": {
            "monthlyRobotCost": 550.00,
            "monthlyLaborSaved": 900.00,
            "netMonthlySavings": 350.00
          },
          "verdict": "INSUFFICIENT",
          "issues": ["High wait times", "Robot overloaded", "Guest abandonment"]
        },
        {
          "robotCount": 2,
          "metrics": {
            "avgWaitMinutes": 6.2,
            "maxWaitMinutes": 15,
            "serviceLevel": 0.89,
            "tasksCompleted": 162,
            "tasksFailed": 8,
            "robotUtilization": 0.85,
            "guestsServed": 175,
            "guestsAbandoned": 5
          },
          "cost": {
            "monthlyRobotCost": 1100.00,
            "monthlyLaborSaved": 1800.00,
            "netMonthlySavings": 700.00
          },
          "verdict": "ACCEPTABLE",
          "issues": ["Peak hour stress"]
        },
        {
          "robotCount": 3,
          "metrics": {
            "avgWaitMinutes": 3.1,
            "maxWaitMinutes": 8,
            "serviceLevel": 0.97,
            "tasksCompleted": 178,
            "tasksFailed": 2,
            "robotUtilization": 0.72,
            "guestsServed": 180,
            "guestsAbandoned": 0
          },
          "cost": {
            "monthlyRobotCost": 1650.00,
            "monthlyLaborSaved": 2700.00,
            "netMonthlySavings": 1050.00
          },
          "verdict": "OPTIMAL",
          "issues": []
        },
        {
          "robotCount": 4,
          "metrics": {
            "avgWaitMinutes": 2.8,
            "maxWaitMinutes": 6,
            "serviceLevel": 0.98,
            "tasksCompleted": 180,
            "tasksFailed": 1,
            "robotUtilization": 0.58,
            "guestsServed": 180,
            "guestsAbandoned": 0
          },
          "cost": {
            "monthlyRobotCost": 2200.00,
            "monthlyLaborSaved": 3600.00,
            "netMonthlySavings": 1400.00
          },
          "verdict": "OVER_PROVISIONED",
          "issues": ["Low utilization", "Diminishing returns"]
        }
      ],
      "charts": {
        "waitTimeVsRobots": [
          { "robots": 1, "avgWait": 12.5 },
          { "robots": 2, "avgWait": 6.2 },
          { "robots": 3, "avgWait": 3.1 },
          { "robots": 4, "avgWait": 2.8 }
        ],
        "roiVsRobots": [
          { "robots": 1, "netSavings": 350 },
          { "robots": 2, "netSavings": 700 },
          { "robots": 3, "netSavings": 1050 },
          { "robots": 4, "netSavings": 1400 }
        ],
        "utilizationVsRobots": [
          { "robots": 1, "utilization": 0.98 },
          { "robots": 2, "utilization": 0.85 },
          { "robots": 3, "utilization": 0.72 },
          { "robots": 4, "utilization": 0.58 }
        ]
      },
      "breakEvenAnalysis": {
        "monthsToBreakEven": 4.2,
        "annualSavings": 12600.00,
        "fiveYearROI": 2.8
      }
    }
    ```

#### Get Simulation Session Summary
Get detailed results after a single simulation completes.
*   **GET** `/simulation/sessions/{sessionId}/summary`
*   **Response:**
    ```json
    {
      "sessionId": "sim_12345",
      "duration": {
        "simulatedMinutes": 480,
        "realSeconds": 58
      },
      "fleetPerformance": {
        "robotCount": 3,
        "avgUtilization": 0.72,
        "totalDistanceTraveled": 4520.5,
        "tasksCompleted": 178,
        "tasksFailed": 2,
        "avgTaskDuration": 87,
        "failoverEvents": 3,
        "blockedIncidents": 5,
        "chargeEvents": 4
      },
      "serviceMetrics": {
        "guestsServed": 180,
        "guestsAbandoned": 0,
        "avgWaitMinutes": 3.1,
        "maxWaitMinutes": 8,
        "serviceLevel": 0.97,
        "tableTurnovers": 64
      },
      "bottlenecks": [
        {
          "type": "PEAK_HOUR_CONGESTION",
          "time": "19:00-20:00",
          "impact": "Wait time increased 40%",
          "suggestion": "Consider 4th robot during dinner rush"
        }
      ],
      "recommendation": {
        "currentFleetAdequate": true,
        "suggestedChanges": [],
        "alternativeScenarios": [
          "Add 1 robot for 15% improvement during peak",
          "Reduce guest spawn rate by 10% to maintain current fleet"
        ]
      }
    }
    ```

#### Compare Multiple Simulation Runs
*   **POST** `/simulation/compare`
*   **Request:**
    ```json
    {
      "sessionIds": ["sim_12345", "sim_12346", "sim_12347"]
    }
    ```
*   **Response:**
    ```json
    {
      "comparison": [
        {
          "sessionId": "sim_12345",
          "robotCount": 2,
          "avgWait": 6.2,
          "serviceLevel": 0.89,
          "utilization": 0.85
        },
        {
          "sessionId": "sim_12346",
          "robotCount": 3,
          "avgWait": 3.1,
          "serviceLevel": 0.97,
          "utilization": 0.72
        },
        {
          "sessionId": "sim_12347",
          "robotCount": 4,
          "avgWait": 2.8,
          "serviceLevel": 0.98,
          "utilization": 0.58
        }
      ],
      "winner": "sim_12346",
      "winnerReason": "Best balance of service level and robot utilization"
    }
    ```

---

## 4. SignalR Real-Time Hub

### Connection
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("wss://api.don-quixote.internal/hubs/restaurant")
    .withAutomaticReconnect()
    .build();

await connection.start();
```

### Server → Client Events

#### Robot Events
| Event | Payload | Description |
|-------|---------|-------------|
| `RobotMoved` | `{ robotId, position: { screen, physical }, heading }` | Position update (10Hz) |
| `RobotStateChanged` | `{ robotId, state, battery, currentTask }` | State transition |
| `RobotBlocked` | `{ robotId, position, obstacleType }` | Robot encountered obstacle |
| `RobotBlockedTimeout` | `{ robotId, taskId, blockedSeconds }` | Blocked too long, failover triggered |

#### Task Events
| Event | Payload | Description |
|-------|---------|-------------|
| `TaskQueued` | `{ taskId, type, priority, queuePosition }` | New task added |
| `TaskAssigned` | `{ taskId, robotId, estimatedSeconds }` | Robot picked up task |
| `TaskReassigned` | `{ taskId, oldRobotId, newRobotId, reason }` | Failover triggered |
| `TaskProgressed` | `{ taskId, robotId, progress, eta }` | Progress update |
| `TaskCompleted` | `{ taskId, robotId, durationSeconds }` | Successfully done |
| `TaskFailed` | `{ taskId, robotId, error, willRetry }` | Task failed |
| `TaskEscalated` | `{ taskId, reason, requiresHuman }` | Needs staff help |
| `DispatchDecision` | `{ taskId, candidates, selected, reason }` | Debug: why this robot |

#### Table Events
| Event | Payload | Description |
|-------|---------|-------------|
| `TableStateChanged` | `{ tableId, oldState, newState, assignedTo }` | State transition |
| `TableTurnoverComplete` | `{ tableId, cycleTimeMinutes }` | Full cycle completed |

#### Guest Events
| Event | Payload | Description |
|-------|---------|-------------|
| `GuestQueued` | `{ guestId, partySize, queuePosition }` | New guest in queue |
| `GuestSeated` | `{ guestId, tableId, robotId }` | Guest escorted to table |
| `GuestPhaseChanged` | `{ guestId, phase, remainingMinutes }` | Dining phase changed |
| `GuestDeparted` | `{ guestId, tableId, totalDurationMinutes }` | Guest left |

#### Zone Events
| Event | Payload | Description |
|-------|---------|-------------|
| `ZoneCreated` | `{ zoneId, label, bounds, type }` | New restricted zone |
| `ZoneUpdated` | `{ zoneId, active, reason }` | Zone status changed |
| `ZoneRemoved` | `{ zoneId }` | Zone deleted |

#### Simulation Events
| Event | Payload | Description |
|-------|---------|-------------|
| `SpawnSettingsChanged` | `{ guestsPerHour, enabled }` | Spawn rate updated |
| `SimulationTick` | `{ simulatedTime, guestsServed, avgWait }` | Periodic stats |
| `SimulationCompleted` | `{ sessionId, summary }` | Simulation finished |
| `OptimizationProgress` | `{ analysisId, progress, currentRobotCount }` | Fleet optimization progress |
| `OptimizationCompleted` | `{ analysisId, recommendation, scenarios }` | Optimization finished |

#### Alert Events (Human Escalation)
| Event | Payload | Description |
|-------|---------|-------------|
| `AlertCreated` | `{ alertId, severity, category, title, context }` | New alert triggered |
| `AlertAcknowledged` | `{ alertId, staffId, acknowledgedAt }` | Staff saw alert |
| `AlertResolved` | `{ alertId, resolution, staffId, resolvedAt }` | Issue handled |
| `AlertEscalated` | `{ alertId, fromSeverity, toSeverity, reason }` | Auto-escalation |
| `AlertExpired` | `{ alertId, autoResolved }` | Alert timed out |

### Client → Server Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToRobot` | `robotId` | High-frequency updates for one robot |
| `UnsubscribeFromRobot` | `robotId` | Stop robot updates |
| `SubscribeToFloor` | `floorId` | All events on a floor |
| `SubscribeToTable` | `tableId` | Updates for specific table |

### React Integration Example
```javascript
// In RobotMap.jsx
useEffect(() => {
  connection.on("RobotMoved", (data) => {
    setRobots(prev => prev.map(r => 
      r.id === data.robotId 
        ? { ...r, position: data.position, heading: data.heading }
        : r
    ));
  });

  connection.on("TableStateChanged", ({ tableId, newState }) => {
    setTables(prev => prev.map(t =>
      t.id === tableId ? { ...t, state: newState } : t
    ));
  });

  connection.on("TaskReassigned", ({ taskId, oldRobotId, newRobotId, reason }) => {
    console.log(`Task ${taskId} moved from ${oldRobotId} to ${newRobotId}: ${reason}`);
    // Update UI to show reassignment
  });

  connection.on("ZoneCreated", (zone) => {
    setRestrictedZones(prev => [...prev, zone]);
  });

  connection.on("GuestQueued", (guest) => {
    setGuestQueue(prev => [...prev, guest]);
  });

  // Alert handling for human escalation
  connection.on("AlertCreated", (alert) => {
    setAlerts(prev => [...prev, alert]);
    if (alert.severity === "CRITICAL") {
      playAlarmSound();
      showFullScreenNotification(alert);
    }
  });

  connection.on("AlertResolved", ({ alertId }) => {
    setAlerts(prev => prev.filter(a => a.id !== alertId));
  });

  return () => {
    connection.off("RobotMoved");
    connection.off("TableStateChanged");
    connection.off("TaskReassigned");
    connection.off("ZoneCreated");
    connection.off("GuestQueued");
    connection.off("AlertCreated");
    connection.off("AlertResolved");
  };
}, []);
```

---

## 5. State Machines

### Alert State Machine
```
TRIGGERED → ACTIVE → ACKNOWLEDGED → RESOLVED
               ↓          ↓
           ESCALATED   ESCALATED → ACKNOWLEDGED → RESOLVED
               ↓
           AUTO_RESOLVED (timeout/condition cleared)
```

### Task Queue State Machine
```
PENDING → ASSIGNED → IN_PROGRESS → COMPLETED
    ↓         ↓           ↓
 CANCELLED  REASSIGNED  FAILED → RETRY → ASSIGNED
                           ↓
                       ESCALATED (needs human)
```

### Guest Lifecycle
```
QUEUED → ESCORTING → SEATED → ORDERING → WAITING_FOOD → EATING → PAYING → DEPARTED
                                              ↓
                                      DESSERT (optional)
```

### Table Lifecycle
```
AVAILABLE → RESERVED → OCCUPIED_SEATED → OCCUPIED_DINING → DO_NOT_DISTURB → DIRTY → CLEANING → AVAILABLE
```

### Robot States
```
IDLE → MOVING → ARRIVED → RETURNING
  ↓       ↓
CHARGING  BLOCKED → ERROR
```

### Failover Flow Example
```
1. Task "DELIVERY to Table 12" assigned to Bot_1
2. Bot_1 starts moving (TaskAssigned event)
3. Bot_1 encounters obstacle, enters BLOCKED state (RobotStateChanged)
4. 30 seconds pass, failover triggers (RobotBlockedTimeout)
5. Dispatch engine finds Bot_2 is available and closer
6. Task reassigned to Bot_2 (TaskReassigned event)
7. Bot_1 marked as BLOCKED, staff notified
8. Bot_2 completes delivery (TaskCompleted)
```

---

## 6. Error Handling

### Standard Error Response
```json
{
  "error": {
    "code": "TABLE_NOT_AVAILABLE",
    "message": "Table 5 is currently occupied",
    "details": {
      "tableId": "table_5",
      "currentState": "OCCUPIED_DINING"
    }
  }
}
```

### Common Error Codes
| Code | HTTP Status | Description |
|------|-------------|-------------|
| `NOT_FOUND` | 404 | Resource does not exist |
| `INVALID_STATE` | 409 | Invalid state transition |
| `TABLE_NOT_AVAILABLE` | 409 | Table cannot be assigned |
| `ROBOT_BUSY` | 409 | Robot already has a task |
| `ZONE_OVERLAP` | 400 | New zone overlaps existing |
| `CAPACITY_EXCEEDED` | 400 | Party size > table capacity |
| `SIMULATION_NOT_RUNNING` | 400 | Simulation control when stopped |
| `NO_ROBOTS_AVAILABLE` | 503 | All robots busy or offline |
| `TASK_NOT_FOUND` | 404 | Task ID does not exist |
| `REASSIGN_FAILED` | 500 | Could not find alternate robot |
| `ALERT_NOT_FOUND` | 404 | Alert ID does not exist |
| `ALERT_ALREADY_RESOLVED` | 409 | Alert was already handled |

---

## 7. Recommended C# Libraries

| Purpose | Library | Notes |
|---------|---------|-------|
| Web API | **ASP.NET Core** | Native, high-performance |
| Real-time | **SignalR** | Built into ASP.NET Core |
| Geometry | **NetTopologySuite** | Polygon intersection, point-in-zone |
| Pathfinding | **Roy-T.AStar** | Fast 2D grid A* |
| State Machine | **Stateless** | Robot/Table/Task/Alert state transitions |
| Robot Comms | **MQTTnet** | Physical robot telemetry |
| Scheduling | **Quartz.NET** | Simulation event scheduling |
| Validation | **FluentValidation** | Request validation |
| Mapping | **AutoMapper** | DTO ↔ Entity mapping |
| Queue | **MediatR** | In-process task dispatch |
| Notifications | **FirebaseAdmin** | Mobile push notifications |

---

## 8. Frontend Control Panel Mapping

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
| **Alert panel (active alerts)** | `GET /alerts?status=ACTIVE` |
| **Alert acknowledge button** | `POST /alerts/{id}/acknowledge` |
| **Alert resolve button** | `POST /alerts/{id}/resolve` |
| **Create manual alert** | `POST /alerts` |
| **Alert rules config** | `GET/PUT /alerts/rules` |
| **Alert history** | `GET /alerts/history` |
| **Run Fleet Optimization** | `POST /simulation/fleet-optimization` |
| **View Optimization Results** | `GET /simulation/fleet-optimization/{id}` |
| **Simulation Summary** | `GET /simulation/sessions/{id}/summary` |
| **Compare Simulations** | `POST /simulation/compare` |
| **ROI Charts (Wait vs Robots)** | From optimization response `charts.waitTimeVsRobots` |
| **ROI Charts (Savings vs Robots)** | From optimization response `charts.roiVsRobots` |
| **Optimal Robot Recommendation** | From optimization response `recommendation` |

---

## 9. Edge Cases & Validation Rules

### 9.1 Robot Edge Cases

#### Robot Goes Offline Mid-Task
*   **Trigger:** Network drop, power loss, hardware failure
*   **Detection:** Heartbeat timeout (default: 10 seconds)
*   **SignalR Event:**
    ```json
    {
      "event": "RobotOffline",
      "payload": {
        "robotId": "bot_1",
        "lastPosition": {
          "screen": { "x": 320, "y": 180 },
          "physical": { "x": 16.0, "y": 9.0 }
        },
        "lastSeen": "2026-01-26T19:45:00Z",
        "activeTaskId": "task_301"
      }
    }
    ```
*   **Auto-Action:** Task reassigned after `offlineGracePeriodSeconds` (default: 30)
*   **API:**
    *   `GET /fleet/robots/{robotId}/heartbeat` - Check last heartbeat
    *   `POST /fleet/robots/{robotId}/ping` - Force connectivity check

#### Robot Heartbeat Configuration
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

#### Multiple Robots in Narrow Aisle (Traffic Control)
*   **Trigger:** Two robots attempt same aisle segment simultaneously
*   **Detection:** Path reservation system
*   **API:**
    *   `GET /map/aisles` - Get aisle definitions with capacity
    *   `GET /map/aisles/{aisleId}/reservations` - Current robot reservations
*   **Response:**
    ```json
    {
      "aisles": [
        {
          "id": "aisle_main_1",
          "capacity": 1,
          "bounds": {
            "screen": { "x": 200, "y": 100, "width": 40, "height": 300 },
            "physical": { "x": 10.0, "y": 5.0, "width": 2.0, "height": 15.0 }
          },
          "currentOccupants": ["bot_1"],
          "queuedRobots": ["bot_2"]
        }
      ]
    }
    ```
*   **Dispatch Rule:**
    ```json
    {
      "id": "rule_aisle_traffic",
      "trigger": "AISLE_CONFLICT",
      "condition": { "aisleCapacity": "FULL" },
      "action": "QUEUE_AND_WAIT",
      "maxWaitSeconds": 60
    }
    ```

#### Partial Task Completion (Spill/Drop)
*   **Trigger:** Robot detects tray imbalance, items fell, delivery incomplete
*   **API:**
    *   `POST /fleet/tasks/{taskId}/partial-complete`
    *   **Request:**
        ```json
        {
          "completedItems": ["Pasta"],
          "failedItems": ["Salad"],
          "reason": "ITEM_DROPPED",
          "notes": "Salad fell at coordinates (15.2, 8.5)"
        }
        ```
*   **Response:**
    ```json
    {
      "taskId": "task_301",
      "status": "PARTIAL_COMPLETE",
      "followUpTaskId": "task_302",
      "followUpType": "CLEANUP",
      "alertId": "alert_550"
    }
    ```
*   **SignalR Event:** `TaskPartialComplete`

#### Robot Recovery After Error
*   **POST** `/fleet/robots/{robotId}/recover`
*   **Request:**
    ```json
    {
      "action": "RESET_AND_RESUME",
      "clearErrors": true
    }
    ```
*   **Actions:** `RESET_AND_RESUME`, `RETURN_TO_DOCK`, `FORCE_IDLE`, `REBOOT`

---

### 9.2 Table Edge Cases

#### Guest Moves Tables Themselves
*   **Trigger:** Guest relocates without robot escort
*   **API:**
    *   `POST /tables/swap`
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
*   **Effect:** Updates guest assignment, no ESCORT task created

#### Table Out of Service
*   **API:**
    *   `PUT /tables/{tableId}/state`
    *   **Request:**
        ```json
        {
          "state": "OUT_OF_SERVICE",
          "reason": "Broken chair leg",
          "estimatedRepairAt": "2026-01-27T10:00:00Z"
        }
        ```
*   **States Extended:** `AVAILABLE`, `RESERVED`, `OCCUPIED_SEATED`, `OCCUPIED_DINING`, `DO_NOT_DISTURB`, `DIRTY`, `CLEANING`, `OUT_OF_SERVICE`
*   **Validation:** Cannot assign guests to `OUT_OF_SERVICE` tables

#### Force Clear Table (Emergency)
*   **POST** `/tables/{tableId}/force-clear`
*   **Request:**
    ```json
    {
      "reason": "EMERGENCY",
      "relocateGuestTo": "table_12",
      "createBussingTask": false
    }
    ```

---

### 9.3 Guest Edge Cases

#### Reservation No-Show
*   **Configuration:**
    ```json
    {
      "noShowGracePeriodMinutes": 15,
      "autoReleaseEnabled": true,
      "penaltyTracking": true
    }
    ```
*   **GET** `/reservations/config`
*   **PUT** `/reservations/config`
*   **Auto-Action:** After grace period, reservation status → `NO_SHOW`, table released
*   **SignalR Event:**
    ```json
    {
      "event": "ReservationNoShow",
      "payload": {
        "reservationId": "res_301",
        "tableId": "table_12",
        "tableReleasedAt": "2026-01-26T19:20:00Z"
      }
    }
    ```

#### Guest Party Size Changes After Seating
*   **PUT** `/guests/{guestId}`
*   **Request:**
    ```json
    {
      "partySize": 6,
      "reason": "ADDITIONAL_GUESTS_ARRIVED"
    }
    ```
*   **Response:**
    ```json
    {
      "guestId": "guest_101",
      "originalPartySize": 4,
      "newPartySize": 6,
      "currentTableId": "table_5",
      "currentTableCapacity": 4,
      "action": "TABLE_SWAP_REQUIRED",
      "suggestedTableId": "table_12",
      "swapTaskId": "task_350"
    }
    ```
*   **Validation:** If new size > capacity, auto-suggest or require table swap

#### Guest Lingering After Payment
*   **Configuration:**
    *   `GET /simulation/dining-duration`
    *   Add `lingeringThresholdMinutes` and `lingeringAlertEnabled`
    ```json
    {
      "phases": {
        "payingLeaving": 5,
        "maxLingeringMinutes": 15
      },
      "lingeringAlert": {
        "enabled": true,
        "thresholdMinutes": 15,
        "severity": "LOW"
      }
    }
    ```
*   **SignalR Event:** `GuestLingering`
*   **States Extended:** `QUEUED`, `ESCORTING`, `SEATED`, `ORDERING`, `WAITING_FOOD`, `EATING`, `PAYING`, `LINGERING`, `DEPARTED`

#### Guest Cancels Mid-Escort
*   **DELETE** `/guests/{guestId}?reason=CANCELLED_DURING_ESCORT`
*   **Effect:** 
    *   Cancel active ESCORT task
    *   Robot returns to dock or picks up next task
    *   Table state reverts to `AVAILABLE`

---

### 9.4 Task Edge Cases

#### Duplicate Task Prevention (Idempotency)
*   **POST** `/fleet/tasks`
*   **Request Header:** `Idempotency-Key: uuid-12345`
*   **Behavior:** 
    *   Same key within 5 minutes returns existing task
    *   Prevents double-click duplicates
*   **Response (duplicate detected):**
    ```json
    {
      "taskId": "task_301",
      "status": "ALREADY_EXISTS",
      "originalCreatedAt": "2026-01-26T19:30:00Z"
    }
    ```

#### Task Dependencies (Multi-Step Chains)
*   **POST** `/fleet/tasks`
*   **Request:**
    ```json
    {
      "type": "DELIVERY",
      "targetId": "table_12",
      "chain": [
        {
          "step": 1,
          "type": "PICKUP",
          "location": "kitchen",
          "waitForAck": true
        },
        {
          "step": 2,
          "type": "DELIVER",
          "location": "table_12",
          "waitForAck": true
        },
        {
          "step": 3,
          "type": "RETURN",
          "location": "kitchen",
          "condition": "IF_TRAY_EMPTY"
        }
      ]
    }
    ```
*   **Response:**
    ```json
    {
      "taskId": "task_301",
      "chainId": "chain_101",
      "currentStep": 1,
      "totalSteps": 3
    }
    ```
*   **SignalR Event:** `TaskChainStepCompleted`

#### Priority Override Mid-Queue
*   **PATCH** `/fleet/tasks/{taskId}/priority`
*   **Request:**
    ```json
    {
      "priority": "CRITICAL",
      "reason": "VIP_GUEST"
    }
    ```
*   **Effect:** Task reordered in queue, may preempt lower priority

#### Task Preemption Rules
*   **GET** `/fleet/dispatch/preemption-rules`
*   **Response:**
    ```json
    {
      "enabled": true,
      "rules": [
        {
          "id": "rule_critical_preempt",
          "condition": {
            "incomingPriority": "CRITICAL",
            "currentTaskPriority": ["LOW", "NORMAL"]
          },
          "action": "PREEMPT_AND_REQUEUE",
          "notifyStaff": true
        }
      ]
    }
    ```

---

### 9.5 Zone Edge Cases

#### Overlapping Zone Validation
*   **POST** `/map/zones` - Returns error if overlap detected
*   **Error Response:**
    ```json
    {
      "error": {
        "code": "ZONE_OVERLAP",
        "message": "New zone overlaps with existing zone 'zone_99'",
        "details": {
          "existingZoneId": "zone_99",
          "overlapArea": {
            "screen": { "x": 120, "y": 210, "width": 20, "height": 30 }
          }
        },
        "suggestions": [
          "Merge zones",
          "Adjust bounds to avoid overlap",
          "Delete existing zone first"
        ]
      }
    }
    ```
*   **Override:** Add `"allowOverlap": true` to force creation

#### Zone Merge
*   **POST** `/map/zones/merge`
*   **Request:**
    ```json
    {
      "zoneIds": ["zone_99", "zone_100"],
      "newLabel": "Expanded Spill Area"
    }
    ```

#### Zone Blocks All Paths (Validation)
*   **Pre-creation validation:** System checks if zone would block access to any table
*   **Error Response:**
    ```json
    {
      "error": {
        "code": "ZONE_BLOCKS_ACCESS",
        "message": "Zone would block all paths to tables: table_5, table_6",
        "details": {
          "blockedTableIds": ["table_5", "table_6"],
          "affectedTasks": ["task_301", "task_302"]
        },
        "suggestions": [
          "Reduce zone size",
          "Move affected guests first",
          "Cancel pending tasks to blocked tables"
        ]
      }
    }
    ```
*   **Override:** Add `"force": true` with `reason`

---

### 9.6 Dispatch Edge Cases

#### All Robots Charging Simultaneously
*   **Alert Rule:**
    ```json
    {
      "id": "rule_no_available_robots",
      "category": "LOW_FLEET_CAPACITY",
      "condition": {
        "availableRobots": 0,
        "chargingRobots": { "equals": "totalRobots" }
      },
      "severity": "CRITICAL"
    }
    ```
*   **Prevention Config:**
    ```json
    {
      "chargingPolicy": {
        "maxSimultaneousCharging": 2,
        "minAvailableRobots": 1,
        "staggerChargingMinutes": 15,
        "lowBatteryThreshold": 20,
        "criticalBatteryThreshold": 10
      }
    }
    ```
*   **GET/PUT** `/fleet/dispatch/charging-policy`

#### High-Priority Task but All Robots Busy
*   **Preemption Config:**
    ```json
    {
      "preemptionEnabled": true,
      "preemptablePriorities": ["LOW"],
      "preemptForPriorities": ["CRITICAL"],
      "requeue PreemptedTask": true
    }
    ```
*   **SignalR Event:** `TaskPreempted`
    ```json
    {
      "event": "TaskPreempted",
      "payload": {
        "preemptedTaskId": "task_300",
        "preemptedBy": "task_350",
        "robotId": "bot_1",
        "preemptedTaskRequeued": true,
        "newQueuePosition": 3
      }
    }
    ```

---

### 9.7 Configuration Validation Rules

#### Delete Robot Validation
*   **DELETE** `/config/fleet/robots/{robotId}`
*   **Validation Rules:**
    *   Cannot delete if robot has active task
    *   Cannot delete if robot is last available
*   **Error Response:**
    ```json
    {
      "error": {
        "code": "ROBOT_HAS_ACTIVE_TASK",
        "message": "Cannot delete robot with active task",
        "details": {
          "robotId": "bot_1",
          "activeTaskId": "task_301"
        },
        "suggestions": [
          "Cancel or reassign task first",
          "Use force=true to auto-reassign"
        ]
      }
    }
    ```
*   **Force Delete:** `DELETE /config/fleet/robots/{robotId}?force=true&reassignTo=bot_2`

#### Delete Table Validation
*   **DELETE** `/config/tables/{tableId}`
*   **Validation Rules:**
    *   Cannot delete if state is not `AVAILABLE`
    *   Cannot delete if has future reservations
*   **Error Response:**
    ```json
    {
      "error": {
        "code": "TABLE_NOT_AVAILABLE",
        "message": "Cannot delete occupied table",
        "details": {
          "tableId": "table_5",
          "currentState": "OCCUPIED_DINING",
          "guestId": "guest_101"
        }
      }
    }
    ```

#### Delete Checkpoint Validation
*   **DELETE** `/config/checkpoints/{checkpointId}`
*   **Validation Rules:**
    *   Cannot delete `kitchen` if tasks reference it
    *   Cannot delete `charger` if robots need charging
*   **Error Response:**
    ```json
    {
      "error": {
        "code": "CHECKPOINT_IN_USE",
        "message": "Cannot delete checkpoint referenced by active tasks",
        "details": {
          "checkpointId": "kitchen",
          "referencingTaskIds": ["task_301", "task_302"]
        }
      }
    }
    ```

#### Change Checkpoint Position Validation
*   **PUT** `/config/checkpoints/{checkpointId}`
*   **Validation:** If robots en route to checkpoint, recalculate paths
*   **Response:**
    ```json
    {
      "checkpointId": "kitchen",
      "positionUpdated": true,
      "affectedRobots": ["bot_1", "bot_2"],
      "pathsRecalculated": 2
    }
    ```

#### Set Robot Count Validation
*   **PUT** `/config/fleet/robot-count`
*   **Validation:** Cannot reduce below active task count
*   **Error Response:**
    ```json
    {
      "error": {
        "code": "INSUFFICIENT_ROBOTS",
        "message": "Cannot reduce to 1 robot with 3 active tasks",
        "details": {
          "requestedCount": 1,
          "activeTaskCount": 3,
          "minimumRequired": 2
        }
      }
    }
    ```

---

### 9.8 API Request Validation

| Endpoint | Validation Rule | Error Code |
|----------|-----------------|------------|
| `POST /guests` | Queue not at max capacity | `QUEUE_FULL` |
| `POST /guests` | Party size ≤ max table capacity | `PARTY_TOO_LARGE` |
| `POST /config/tables` | Position not overlapping existing table | `TABLE_POSITION_CONFLICT` |
| `POST /config/tables` | Position within map bounds | `POSITION_OUT_OF_BOUNDS` |
| `POST /map/zones` | Does not block ALL paths to any table | `ZONE_BLOCKS_ACCESS` |
| `POST /fleet/tasks` | Target ID exists | `TARGET_NOT_FOUND` |
| `POST /fleet/tasks` | Target is reachable (pathfinding) | `TARGET_UNREACHABLE` |
| `POST /reservations` | Table not already reserved for time slot | `TIME_SLOT_UNAVAILABLE` |
| `POST /reservations` | Party size ≤ table capacity | `CAPACITY_EXCEEDED` |
| `PUT /simulation/*` | Simulation is running | `SIMULATION_NOT_RUNNING` |

#### Max Queue Size Configuration
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

---

### 9.9 Simulation Edge Cases

#### Time Scale Change Mid-Simulation
*   **PATCH** `/simulation/sessions/{sessionId}`
*   **Request:**
    ```json
    { "timeScale": 20.0 }
    ```
*   **Effect:** 
    *   In-flight task ETAs recalculated
    *   Guest dining durations adjusted proportionally
*   **SignalR Event:** `SimulationTimeScaleChanged`

#### Network Disconnect During Optimization
*   **GET** `/simulation/fleet-optimization/{analysisId}`
*   **Response (partial):**
    ```json
    {
      "analysisId": "analysis_789",
      "status": "INTERRUPTED",
      "progress": 0.6,
      "completedScenarios": [1, 2, 3],
      "pendingScenarios": [4, 5, 6],
      "partialResults": { ... },
      "canResume": true
    }
    ```
*   **Resume:** `POST /simulation/fleet-optimization/{analysisId}/resume`

---

## 10. External Integrations (Webhooks)

### 10.1 Webhook Configuration
*   **GET** `/webhooks`
*   **Response:**
    ```json
    {
      "webhooks": [
        {
          "id": "webhook_pos",
          "name": "POS Integration",
          "url": "https://pos.restaurant.com/api/robot-events",
          "events": ["ORDER_READY", "DELIVERY_COMPLETE"],
          "enabled": true,
          "secret": "********"
        }
      ]
    }
    ```

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

### 10.2 Inbound Webhooks (External → System)

#### POS: Order Ready
*   **POST** `/integrations/pos/order-ready`
*   **Request:**
    ```json
    {
      "orderId": "order_555",
      "tableId": "table_12",
      "items": ["Pasta Carbonara", "Caesar Salad"],
      "priority": "NORMAL",
      "pickupLocation": "kitchen"
    }
    ```
*   **Effect:** Auto-creates DELIVERY task

#### Payment Terminal: Payment Complete
*   **POST** `/integrations/payment/complete`
*   **Request:**
    ```json
    {
      "tableId": "table_5",
      "transactionId": "txn_12345",
      "timestamp": "2026-01-26T20:15:00Z"
    }
    ```
*   **Effect:** 
    *   Guest state → `PAYING`
    *   After departure timeout → auto-create BUSSING task

#### Fire Alarm: Emergency Stop
*   **POST** `/integrations/emergency/fire-alarm`
*   **Request:**
    ```json
    {
      "alarmType": "FIRE",
      "zone": "ALL",
      "timestamp": "2026-01-26T20:00:00Z"
    }
    ```
*   **Effect:**
    *   All robots → EMERGENCY_STOP
    *   All tasks → SUSPENDED
    *   Clear paths to exits
    *   CRITICAL alert created

### 10.3 Outbound Webhook Events

| Event | Payload | Trigger |
|-------|---------|---------|
| `ORDER_READY_PICKUP` | `{ orderId, robotId, eta }` | Robot assigned to delivery |
| `DELIVERY_COMPLETE` | `{ orderId, tableId, robotId, timestamp }` | Food delivered |
| `ALERT_CREATED` | `{ alertId, severity, category, message }` | Human intervention needed |
| `ALERT_RESOLVED` | `{ alertId, resolution, staffId }` | Alert handled |
| `GUEST_SEATED` | `{ guestId, tableId, partySize }` | Guest at table |
| `GUEST_DEPARTED` | `{ guestId, tableId, duration }` | Guest left |
| `ROBOT_OFFLINE` | `{ robotId, lastPosition, activeTask }` | Robot disconnected |
| `ROBOT_ERROR` | `{ robotId, errorCode, errorMessage }` | Robot hardware/software error |

### 10.4 Staff Mobile App Callbacks
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

## 11. Extended State Machines

### Robot States (Extended)
```
IDLE → MOVING → ARRIVED → RETURNING → IDLE
  ↓       ↓         ↓
CHARGING  BLOCKED → WAITING_AISLE → MOVING
  ↓         ↓
OFFLINE   ERROR → RECOVERY → IDLE
  ↓
EMERGENCY_STOP → IDLE (after clearance)
```

### Task States (Extended)
```
PENDING → ASSIGNED → IN_PROGRESS → COMPLETED
    ↓         ↓           ↓            ↓
 CANCELLED  REASSIGNED  PARTIAL   VERIFIED
                ↓         ↓
              FAILED → RETRY → ASSIGNED
                ↓
            ESCALATED → MANUAL_COMPLETE
                ↓
            PREEMPTED → REQUEUED → PENDING
```

### Guest States (Extended)
```
QUEUED → ESCORTING → SEATED → ORDERING → WAITING_FOOD → EATING → PAYING → LINGERING → DEPARTED
    ↓         ↓                                                      ↓
 LEFT_QUEUE  ESCORT_CANCELLED                                   DEPARTED (normal)
                                                                     ↓
                                                                NO_SHOW (reservation only)
```
