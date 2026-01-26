# Backend API Test Data

This document defines test data aligned with the Backend API requirements and the **PadBot Navigation SDK v1.5.0** specifications.

---

## Table of Contents

1. [Layout & Coordinate System](#1-layout--coordinate-system)
2. [Robots](#2-robots)
3. [Tables](#3-tables)
4. [Checkpoints](#4-checkpoints)
5. [Guests](#5-guests)
6. [Tasks](#6-tasks)
7. [Zones](#7-zones)
8. [Reservations](#8-reservations)
9. [Alerts](#9-alerts)
10. [Simulation Scenarios](#10-simulation-scenarios)
11. [Robot SDK Mapping](#11-robot-sdk-mapping)

---

## 1. Layout & Coordinate System

### 1.1 Default Layout

```json
{
  "id": "layout_main_v1",
  "name": "Main Dining Hall",
  "coordinateMapping": {
    "scale": 0.05,
    "screenSize": { "width": 900, "height": 600 },
    "physicalSize": { "width": 45.0, "height": 30.0 },
    "gridResolution": 20
  },
  "floorId": "floor_1",
  "mapId": "map_restaurant_main"
}
```

### 1.2 Coordinate Conversion Reference

| Screen (px) | Physical (m) | Description |
|-------------|--------------|-------------|
| (0, 0) | (0.0, 0.0) | Top-left corner |
| (450, 300) | (22.5, 15.0) | Center |
| (900, 600) | (45.0, 30.0) | Bottom-right corner |
| (50, 50) | (2.5, 2.5) | Kitchen area |
| (850, 550) | (42.5, 27.5) | Charger area |

---

## 2. Robots

### 2.1 Robot Fleet (10 Robots)

```json
{
  "fleet": {
    "robotCount": 10,
    "maxRobots": 12,
    "robots": [
      {
        "id": "bot_1",
        "name": "Don Quixote",
        "serialNumber": "INBOT-DQ-001",
        "enabled": true,
        "state": "IDLE",
        "battery": 95,
        "position": {
          "screen": { "x": 50, "y": 550 },
          "physical": { "x": 2.5, "y": 27.5 }
        },
        "heading": 0,
        "homePosition": {
          "screen": { "x": 50, "y": 550 },
          "physical": { "x": 2.5, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_1"
      },
      {
        "id": "bot_2",
        "name": "Sancho Panza",
        "serialNumber": "INBOT-SP-002",
        "enabled": true,
        "state": "MOVING",
        "battery": 88,
        "position": {
          "screen": { "x": 300, "y": 250 },
          "physical": { "x": 15.0, "y": 12.5 }
        },
        "heading": 45,
        "homePosition": {
          "screen": { "x": 100, "y": 550 },
          "physical": { "x": 5.0, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_2",
        "currentTaskId": "task_101"
      },
      {
        "id": "bot_3",
        "name": "Dulcinea",
        "serialNumber": "INBOT-DL-003",
        "enabled": true,
        "state": "CHARGING",
        "battery": 45,
        "position": {
          "screen": { "x": 150, "y": 550 },
          "physical": { "x": 7.5, "y": 27.5 }
        },
        "heading": 0,
        "homePosition": {
          "screen": { "x": 150, "y": 550 },
          "physical": { "x": 7.5, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT"],
        "speedLimit": 0.8,
        "sdkPointId": "charge_point_3"
      },
      {
        "id": "bot_4",
        "name": "Rocinante",
        "serialNumber": "INBOT-RC-004",
        "enabled": true,
        "state": "IDLE",
        "battery": 92,
        "position": {
          "screen": { "x": 200, "y": 550 },
          "physical": { "x": 10.0, "y": 27.5 }
        },
        "heading": 0,
        "homePosition": {
          "screen": { "x": 200, "y": 550 },
          "physical": { "x": 10.0, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_4"
      },
      {
        "id": "bot_5",
        "name": "Clavileño",
        "serialNumber": "INBOT-CL-005",
        "enabled": true,
        "state": "MOVING",
        "battery": 78,
        "position": {
          "screen": { "x": 500, "y": 180 },
          "physical": { "x": 25.0, "y": 9.0 }
        },
        "heading": 90,
        "homePosition": {
          "screen": { "x": 250, "y": 550 },
          "physical": { "x": 12.5, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_5",
        "currentTaskId": "task_102"
      },
      {
        "id": "bot_6",
        "name": "Barataria",
        "serialNumber": "INBOT-BR-006",
        "enabled": true,
        "state": "IDLE",
        "battery": 85,
        "position": {
          "screen": { "x": 300, "y": 550 },
          "physical": { "x": 15.0, "y": 27.5 }
        },
        "heading": 0,
        "homePosition": {
          "screen": { "x": 300, "y": 550 },
          "physical": { "x": 15.0, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_6"
      },
      {
        "id": "bot_7",
        "name": "Mambrino",
        "serialNumber": "INBOT-MB-007",
        "enabled": true,
        "state": "BLOCKED",
        "battery": 72,
        "position": {
          "screen": { "x": 340, "y": 195 },
          "physical": { "x": 17.0, "y": 9.75 }
        },
        "heading": 135,
        "homePosition": {
          "screen": { "x": 350, "y": 550 },
          "physical": { "x": 17.5, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT"],
        "speedLimit": 0.9,
        "sdkPointId": "charge_point_7",
        "currentTaskId": "task_106",
        "blockedType": "TYPE_ENCOUNTER_OBSTACLES"
      },
      {
        "id": "bot_8",
        "name": "Montesinos",
        "serialNumber": "INBOT-MT-008",
        "enabled": true,
        "state": "MOVING",
        "battery": 65,
        "position": {
          "screen": { "x": 700, "y": 350 },
          "physical": { "x": 35.0, "y": 17.5 }
        },
        "heading": 270,
        "homePosition": {
          "screen": { "x": 400, "y": 550 },
          "physical": { "x": 20.0, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_8",
        "currentTaskId": "task_103"
      },
      {
        "id": "bot_9",
        "name": "Toboso",
        "serialNumber": "INBOT-TB-009",
        "enabled": true,
        "state": "CHARGING",
        "battery": 22,
        "position": {
          "screen": { "x": 450, "y": 550 },
          "physical": { "x": 22.5, "y": 27.5 }
        },
        "heading": 0,
        "homePosition": {
          "screen": { "x": 450, "y": 550 },
          "physical": { "x": 22.5, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT"],
        "speedLimit": 0.8,
        "sdkPointId": "charge_point_9"
      },
      {
        "id": "bot_10",
        "name": "La Mancha",
        "serialNumber": "INBOT-LM-010",
        "enabled": false,
        "state": "OFFLINE",
        "battery": 0,
        "position": {
          "screen": { "x": 500, "y": 550 },
          "physical": { "x": 25.0, "y": 27.5 }
        },
        "heading": 0,
        "homePosition": {
          "screen": { "x": 500, "y": 550 },
          "physical": { "x": 25.0, "y": 27.5 }
        },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_10",
        "offlineReason": "MAINTENANCE"
      }
    ]
  }
}
```

### 2.2 Robot State Test Cases

| Robot ID | State | Battery | Task | Test Purpose |
|----------|-------|---------|------|--------------|
| `bot_1` | `IDLE` | 95% | None | Normal available robot |
| `bot_2` | `MOVING` | 88% | `task_101` | Robot in transit |
| `bot_3` | `CHARGING` | 45% | None | Low battery charging |
| `bot_4` | `IDLE` | 92% | None | Available backup |
| `bot_5` | `MOVING` | 78% | `task_102` | Escort in progress |
| `bot_6` | `IDLE` | 85% | None | Ready for dispatch |
| `bot_7` | `BLOCKED` | 72% | `task_106` | Obstacle detection test |
| `bot_8` | `MOVING` | 65% | `task_103` | Bussing task |
| `bot_9` | `CHARGING` | 22% | None | Critical battery charging |
| `bot_10` | `OFFLINE` | -- | None | Maintenance mode |

### 2.3 Robot Position Updates (10Hz Simulation)

```json
[
  { "robotId": "bot_1", "timestamp": "2026-01-26T12:00:00.000Z", "position": { "screen": { "x": 120, "y": 300 } }, "heading": 45 },
  { "robotId": "bot_1", "timestamp": "2026-01-26T12:00:00.100Z", "position": { "screen": { "x": 125, "y": 298 } }, "heading": 47 },
  { "robotId": "bot_1", "timestamp": "2026-01-26T12:00:00.200Z", "position": { "screen": { "x": 130, "y": 295 } }, "heading": 50 },
  { "robotId": "bot_1", "timestamp": "2026-01-26T12:00:00.300Z", "position": { "screen": { "x": 136, "y": 292 } }, "heading": 52 },
  { "robotId": "bot_1", "timestamp": "2026-01-26T12:00:00.400Z", "position": { "screen": { "x": 142, "y": 288 } }, "heading": 55 }
]
```

---

## 3. Tables

### 3.1 Table Configuration (20 Tables)

```json
{
  "tables": [
    {
      "id": "table_1",
      "label": "T1",
      "type": "t2",
      "capacity": 2,
      "position": { "screen": { "x": 150, "y": 100 }, "physical": { "x": 7.5, "y": 5.0 } },
      "zone": "window",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["window_view"],
      "sdkPointId": "table_point_1"
    },
    {
      "id": "table_2",
      "label": "T2",
      "type": "t2",
      "capacity": 2,
      "position": { "screen": { "x": 250, "y": 100 }, "physical": { "x": 12.5, "y": 5.0 } },
      "zone": "window",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["window_view"],
      "sdkPointId": "table_point_2"
    },
    {
      "id": "table_3",
      "label": "T3",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 350, "y": 100 }, "physical": { "x": 17.5, "y": 5.0 } },
      "zone": "window",
      "state": "OCCUPIED_DINING",
      "enabled": true,
      "reservable": true,
      "features": ["window_view"],
      "sdkPointId": "table_point_3"
    },
    {
      "id": "table_4",
      "label": "T4",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 450, "y": 100 }, "physical": { "x": 22.5, "y": 5.0 } },
      "zone": "window",
      "state": "RESERVED",
      "enabled": true,
      "reservable": true,
      "features": ["window_view", "quiet"],
      "sdkPointId": "table_point_4"
    },
    {
      "id": "table_5",
      "label": "T5",
      "type": "t6",
      "capacity": 6,
      "position": { "screen": { "x": 600, "y": 100 }, "physical": { "x": 30.0, "y": 5.0 } },
      "zone": "window",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["window_view", "large_party"],
      "sdkPointId": "table_point_5"
    },
    {
      "id": "table_6",
      "label": "T6",
      "type": "t2",
      "capacity": 2,
      "position": { "screen": { "x": 150, "y": 200 }, "physical": { "x": 7.5, "y": 10.0 } },
      "zone": "main",
      "state": "DIRTY",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_6"
    },
    {
      "id": "table_7",
      "label": "T7",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 250, "y": 200 }, "physical": { "x": 12.5, "y": 10.0 } },
      "zone": "main",
      "state": "CLEANING",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_7"
    },
    {
      "id": "table_8",
      "label": "T8",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 350, "y": 200 }, "physical": { "x": 17.5, "y": 10.0 } },
      "zone": "main",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["booth"],
      "sdkPointId": "table_point_8"
    },
    {
      "id": "table_9",
      "label": "T9",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 450, "y": 200 }, "physical": { "x": 22.5, "y": 10.0 } },
      "zone": "main",
      "state": "OCCUPIED_SEATED",
      "enabled": true,
      "reservable": true,
      "features": ["booth"],
      "sdkPointId": "table_point_9"
    },
    {
      "id": "table_10",
      "label": "T10",
      "type": "t8",
      "capacity": 8,
      "position": { "screen": { "x": 600, "y": 200 }, "physical": { "x": 30.0, "y": 10.0 } },
      "zone": "main",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["large_party", "round_table"],
      "sdkPointId": "table_point_10"
    },
    {
      "id": "table_11",
      "label": "T11",
      "type": "t2",
      "capacity": 2,
      "position": { "screen": { "x": 150, "y": 300 }, "physical": { "x": 7.5, "y": 15.0 } },
      "zone": "center",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_11"
    },
    {
      "id": "table_12",
      "label": "T12",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 250, "y": 300 }, "physical": { "x": 12.5, "y": 15.0 } },
      "zone": "center",
      "state": "DO_NOT_DISTURB",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_12"
    },
    {
      "id": "table_13",
      "label": "T13",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 350, "y": 300 }, "physical": { "x": 17.5, "y": 15.0 } },
      "zone": "center",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_13"
    },
    {
      "id": "table_14",
      "label": "T14",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 450, "y": 300 }, "physical": { "x": 22.5, "y": 15.0 } },
      "zone": "center",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_14"
    },
    {
      "id": "table_15",
      "label": "T15",
      "type": "t6",
      "capacity": 6,
      "position": { "screen": { "x": 600, "y": 300 }, "physical": { "x": 30.0, "y": 15.0 } },
      "zone": "center",
      "state": "OUT_OF_SERVICE",
      "enabled": false,
      "reservable": false,
      "features": ["large_party"],
      "sdkPointId": "table_point_15"
    },
    {
      "id": "table_16",
      "label": "T16",
      "type": "t2",
      "capacity": 2,
      "position": { "screen": { "x": 150, "y": 400 }, "physical": { "x": 7.5, "y": 20.0 } },
      "zone": "bar",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["bar_view"],
      "sdkPointId": "table_point_16"
    },
    {
      "id": "table_17",
      "label": "T17",
      "type": "t2",
      "capacity": 2,
      "position": { "screen": { "x": 250, "y": 400 }, "physical": { "x": 12.5, "y": 20.0 } },
      "zone": "bar",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["bar_view"],
      "sdkPointId": "table_point_17"
    },
    {
      "id": "table_18",
      "label": "T18",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 350, "y": 400 }, "physical": { "x": 17.5, "y": 20.0 } },
      "zone": "bar",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_18"
    },
    {
      "id": "table_19",
      "label": "T19",
      "type": "t4",
      "capacity": 4,
      "position": { "screen": { "x": 450, "y": 400 }, "physical": { "x": 22.5, "y": 20.0 } },
      "zone": "bar",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": [],
      "sdkPointId": "table_point_19"
    },
    {
      "id": "table_20",
      "label": "VIP",
      "type": "t10",
      "capacity": 10,
      "position": { "screen": { "x": 750, "y": 300 }, "physical": { "x": 37.5, "y": 15.0 } },
      "zone": "vip",
      "state": "AVAILABLE",
      "enabled": true,
      "reservable": true,
      "features": ["vip", "private", "large_party"],
      "sdkPointId": "table_point_vip"
    }
  ]
}
```

### 3.2 Table State Distribution Summary

| State | Count | Table IDs |
|-------|-------|-----------|
| `AVAILABLE` | 12 | T1, T2, T5, T8, T10, T11, T13, T14, T16, T17, T18, T19, T20 |
| `OCCUPIED_DINING` | 1 | T3 |
| `OCCUPIED_SEATED` | 1 | T9 |
| `RESERVED` | 1 | T4 |
| `DIRTY` | 1 | T6 |
| `CLEANING` | 1 | T7 |
| `DO_NOT_DISTURB` | 1 | T12 |
| `OUT_OF_SERVICE` | 1 | T15 |

---

## 4. Checkpoints

### 4.1 Checkpoint Configuration

```json
{
  "checkpoints": [
    {
      "id": "kitchen",
      "name": "Kitchen",
      "type": "SERVICE_HUB",
      "position": { "screen": { "x": 50, "y": 50 }, "physical": { "x": 2.5, "y": 2.5 } },
      "functions": ["FOOD_PICKUP", "DISH_RETURN"],
      "sdkPointId": "kitchen_point",
      "sdkPointType": "TARGET_POINT_TYPE_SHIPMENT"
    },
    {
      "id": "reception",
      "name": "Reception",
      "type": "GUEST_ENTRY",
      "position": { "screen": { "x": 850, "y": 50 }, "physical": { "x": 42.5, "y": 2.5 } },
      "functions": ["GUEST_CHECKIN", "ESCORT_START"],
      "sdkPointId": "reception_point",
      "sdkPointType": "TARGET_POINT_TYPE_COMMON"
    },
    {
      "id": "cashier",
      "name": "Cashier",
      "type": "SERVICE_HUB",
      "position": { "screen": { "x": 850, "y": 550 }, "physical": { "x": 42.5, "y": 27.5 } },
      "functions": ["PAYMENT"],
      "sdkPointId": "cashier_point",
      "sdkPointType": "TARGET_POINT_TYPE_COMMON"
    },
    {
      "id": "charger_1",
      "name": "Charging Station 1",
      "type": "ROBOT_DOCK",
      "position": { "screen": { "x": 50, "y": 550 }, "physical": { "x": 2.5, "y": 27.5 } },
      "functions": ["CHARGING", "IDLE_PARKING"],
      "capacity": 1,
      "sdkPointId": "charge_point_1",
      "sdkPointType": "TARGET_POINT_TYPE_BASE_CHARGE"
    },
    {
      "id": "charger_2",
      "name": "Charging Station 2",
      "type": "ROBOT_DOCK",
      "position": { "screen": { "x": 100, "y": 550 }, "physical": { "x": 5.0, "y": 27.5 } },
      "functions": ["CHARGING", "IDLE_PARKING"],
      "capacity": 1,
      "sdkPointId": "charge_point_2",
      "sdkPointType": "TARGET_POINT_TYPE_BASE_CHARGE"
    },
    {
      "id": "charger_3",
      "name": "Charging Station 3",
      "type": "ROBOT_DOCK",
      "position": { "screen": { "x": 150, "y": 550 }, "physical": { "x": 7.5, "y": 27.5 } },
      "functions": ["CHARGING", "IDLE_PARKING"],
      "capacity": 1,
      "sdkPointId": "charge_point_3",
      "sdkPointType": "TARGET_POINT_TYPE_BASE_CHARGE"
    },
    {
      "id": "bar",
      "name": "Bar Counter",
      "type": "SERVICE_HUB",
      "position": { "screen": { "x": 100, "y": 400 }, "physical": { "x": 5.0, "y": 20.0 } },
      "functions": ["DRINK_PICKUP"],
      "sdkPointId": "bar_point",
      "sdkPointType": "TARGET_POINT_TYPE_COMMON"
    },
    {
      "id": "entrance",
      "name": "Main Entrance",
      "type": "ENTRANCE",
      "position": { "screen": { "x": 850, "y": 300 }, "physical": { "x": 42.5, "y": 15.0 } },
      "functions": ["ENTRANCE_CONTROL"],
      "sdkPointId": "entrance_point",
      "sdkPointType": "TARGET_POINT_TYPE_ENTRANCE"
    }
  ]
}
```

---

## 5. Guests

### 5.1 Guest Queue (100 Guests)

```json
{
  "guests": [
    { "id": "guest_101", "partySize": 2, "arrivalTime": "2026-01-26T12:00:00Z", "state": "QUEUED", "queuePosition": 1, "waitMinutes": 0, "preferences": ["window"], "notes": null },
    { "id": "guest_102", "partySize": 4, "arrivalTime": "2026-01-26T12:02:00Z", "state": "QUEUED", "queuePosition": 2, "waitMinutes": 2, "preferences": ["booth", "quiet"], "notes": "Birthday celebration" },
    { "id": "guest_103", "partySize": 6, "arrivalTime": "2026-01-26T12:05:00Z", "state": "ESCORTING", "queuePosition": null, "waitMinutes": 5, "preferences": ["large_party"], "reservationId": "res_201", "assignedTableId": "table_5", "escortRobotId": "bot_5" },
    { "id": "guest_104", "partySize": 4, "arrivalTime": "2026-01-26T11:30:00Z", "state": "EATING", "assignedTableId": "table_3", "seatedAt": "2026-01-26T11:35:00Z" },
    { "id": "guest_105", "partySize": 2, "arrivalTime": "2026-01-26T11:45:00Z", "state": "ORDERING", "assignedTableId": "table_9", "seatedAt": "2026-01-26T11:50:00Z" },
    { "id": "guest_106", "partySize": 8, "arrivalTime": "2026-01-26T11:42:00Z", "state": "QUEUED", "queuePosition": 3, "waitMinutes": 18, "preferences": ["large_party", "vip"], "notes": "Waiting for VIP table" },
    { "id": "guest_107", "partySize": 2, "arrivalTime": "2026-01-26T11:00:00Z", "state": "PAYING", "assignedTableId": "table_1", "seatedAt": "2026-01-26T11:05:00Z" },
    { "id": "guest_108", "partySize": 4, "arrivalTime": "2026-01-26T10:30:00Z", "state": "LINGERING", "assignedTableId": "table_12", "seatedAt": "2026-01-26T10:35:00Z", "lingeringMinutes": 20 },
    { "id": "guest_109", "partySize": 3, "arrivalTime": "2026-01-26T12:08:00Z", "state": "QUEUED", "queuePosition": 4, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_110", "partySize": 2, "arrivalTime": "2026-01-26T11:20:00Z", "state": "EATING", "assignedTableId": "table_2", "seatedAt": "2026-01-26T11:22:00Z" },
    { "id": "guest_111", "partySize": 4, "arrivalTime": "2026-01-26T11:15:00Z", "state": "EATING", "assignedTableId": "table_8", "seatedAt": "2026-01-26T11:18:00Z" },
    { "id": "guest_112", "partySize": 2, "arrivalTime": "2026-01-26T12:10:00Z", "state": "QUEUED", "queuePosition": 5, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_113", "partySize": 5, "arrivalTime": "2026-01-26T11:50:00Z", "state": "ORDERING", "assignedTableId": "table_10", "seatedAt": "2026-01-26T11:55:00Z" },
    { "id": "guest_114", "partySize": 2, "arrivalTime": "2026-01-26T11:40:00Z", "state": "EATING", "assignedTableId": "table_11", "seatedAt": "2026-01-26T11:42:00Z" },
    { "id": "guest_115", "partySize": 4, "arrivalTime": "2026-01-26T12:12:00Z", "state": "QUEUED", "queuePosition": 6, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_116", "partySize": 3, "arrivalTime": "2026-01-26T11:25:00Z", "state": "EATING", "assignedTableId": "table_13", "seatedAt": "2026-01-26T11:28:00Z" },
    { "id": "guest_117", "partySize": 2, "arrivalTime": "2026-01-26T11:55:00Z", "state": "ESCORTING", "assignedTableId": "table_16", "escortRobotId": "bot_2" },
    { "id": "guest_118", "partySize": 6, "arrivalTime": "2026-01-26T12:15:00Z", "state": "QUEUED", "queuePosition": 7, "waitMinutes": 0, "preferences": ["large_party", "window"] },
    { "id": "guest_119", "partySize": 2, "arrivalTime": "2026-01-26T11:10:00Z", "state": "PAYING", "assignedTableId": "table_17", "seatedAt": "2026-01-26T11:12:00Z" },
    { "id": "guest_120", "partySize": 4, "arrivalTime": "2026-01-26T11:35:00Z", "state": "EATING", "assignedTableId": "table_14", "seatedAt": "2026-01-26T11:38:00Z" },
    { "id": "guest_121", "partySize": 2, "arrivalTime": "2026-01-26T12:18:00Z", "state": "QUEUED", "queuePosition": 8, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_122", "partySize": 3, "arrivalTime": "2026-01-26T12:20:00Z", "state": "QUEUED", "queuePosition": 9, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_123", "partySize": 4, "arrivalTime": "2026-01-26T11:48:00Z", "state": "ORDERING", "assignedTableId": "table_18", "seatedAt": "2026-01-26T11:52:00Z" },
    { "id": "guest_124", "partySize": 2, "arrivalTime": "2026-01-26T12:22:00Z", "state": "QUEUED", "queuePosition": 10, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_125", "partySize": 5, "arrivalTime": "2026-01-26T12:25:00Z", "state": "QUEUED", "queuePosition": 11, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_126", "partySize": 2, "arrivalTime": "2026-01-26T11:05:00Z", "state": "EATING", "assignedTableId": "table_19", "seatedAt": "2026-01-26T11:08:00Z" },
    { "id": "guest_127", "partySize": 4, "arrivalTime": "2026-01-26T12:28:00Z", "state": "QUEUED", "queuePosition": 12, "waitMinutes": 0, "preferences": ["booth", "quiet"] },
    { "id": "guest_128", "partySize": 3, "arrivalTime": "2026-01-26T12:30:00Z", "state": "QUEUED", "queuePosition": 13, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_129", "partySize": 2, "arrivalTime": "2026-01-26T12:32:00Z", "state": "QUEUED", "queuePosition": 14, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_130", "partySize": 6, "arrivalTime": "2026-01-26T12:35:00Z", "state": "QUEUED", "queuePosition": 15, "waitMinutes": 0, "preferences": ["large_party"], "reservationId": "res_206" },
    { "id": "guest_131", "partySize": 2, "arrivalTime": "2026-01-26T12:38:00Z", "state": "QUEUED", "queuePosition": 16, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_132", "partySize": 4, "arrivalTime": "2026-01-26T12:40:00Z", "state": "QUEUED", "queuePosition": 17, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_133", "partySize": 3, "arrivalTime": "2026-01-26T12:42:00Z", "state": "QUEUED", "queuePosition": 18, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_134", "partySize": 2, "arrivalTime": "2026-01-26T12:45:00Z", "state": "QUEUED", "queuePosition": 19, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_135", "partySize": 4, "arrivalTime": "2026-01-26T12:48:00Z", "state": "QUEUED", "queuePosition": 20, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_136", "partySize": 2, "arrivalTime": "2026-01-26T12:50:00Z", "state": "QUEUED", "queuePosition": 21, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_137", "partySize": 5, "arrivalTime": "2026-01-26T12:52:00Z", "state": "QUEUED", "queuePosition": 22, "waitMinutes": 0, "preferences": ["large_party", "vip"] },
    { "id": "guest_138", "partySize": 2, "arrivalTime": "2026-01-26T12:55:00Z", "state": "QUEUED", "queuePosition": 23, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_139", "partySize": 3, "arrivalTime": "2026-01-26T12:58:00Z", "state": "QUEUED", "queuePosition": 24, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_140", "partySize": 4, "arrivalTime": "2026-01-26T13:00:00Z", "state": "QUEUED", "queuePosition": 25, "waitMinutes": 0, "preferences": ["quiet", "booth"] },
    { "id": "guest_141", "partySize": 2, "arrivalTime": "2026-01-26T13:02:00Z", "state": "QUEUED", "queuePosition": 26, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_142", "partySize": 6, "arrivalTime": "2026-01-26T13:05:00Z", "state": "QUEUED", "queuePosition": 27, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_143", "partySize": 2, "arrivalTime": "2026-01-26T13:08:00Z", "state": "QUEUED", "queuePosition": 28, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_144", "partySize": 4, "arrivalTime": "2026-01-26T13:10:00Z", "state": "QUEUED", "queuePosition": 29, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_145", "partySize": 3, "arrivalTime": "2026-01-26T13:12:00Z", "state": "QUEUED", "queuePosition": 30, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_146", "partySize": 2, "arrivalTime": "2026-01-26T13:15:00Z", "state": "QUEUED", "queuePosition": 31, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_147", "partySize": 4, "arrivalTime": "2026-01-26T13:18:00Z", "state": "QUEUED", "queuePosition": 32, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_148", "partySize": 2, "arrivalTime": "2026-01-26T13:20:00Z", "state": "QUEUED", "queuePosition": 33, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_149", "partySize": 5, "arrivalTime": "2026-01-26T13:22:00Z", "state": "QUEUED", "queuePosition": 34, "waitMinutes": 0, "preferences": ["large_party", "window"] },
    { "id": "guest_150", "partySize": 2, "arrivalTime": "2026-01-26T13:25:00Z", "state": "QUEUED", "queuePosition": 35, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_151", "partySize": 3, "arrivalTime": "2026-01-26T13:28:00Z", "state": "QUEUED", "queuePosition": 36, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_152", "partySize": 4, "arrivalTime": "2026-01-26T13:30:00Z", "state": "QUEUED", "queuePosition": 37, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_153", "partySize": 2, "arrivalTime": "2026-01-26T13:32:00Z", "state": "QUEUED", "queuePosition": 38, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_154", "partySize": 6, "arrivalTime": "2026-01-26T13:35:00Z", "state": "QUEUED", "queuePosition": 39, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_155", "partySize": 2, "arrivalTime": "2026-01-26T13:38:00Z", "state": "QUEUED", "queuePosition": 40, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_156", "partySize": 4, "arrivalTime": "2026-01-26T13:40:00Z", "state": "QUEUED", "queuePosition": 41, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_157", "partySize": 3, "arrivalTime": "2026-01-26T13:42:00Z", "state": "QUEUED", "queuePosition": 42, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_158", "partySize": 2, "arrivalTime": "2026-01-26T13:45:00Z", "state": "QUEUED", "queuePosition": 43, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_159", "partySize": 5, "arrivalTime": "2026-01-26T13:48:00Z", "state": "QUEUED", "queuePosition": 44, "waitMinutes": 0, "preferences": ["large_party", "vip"] },
    { "id": "guest_160", "partySize": 2, "arrivalTime": "2026-01-26T13:50:00Z", "state": "QUEUED", "queuePosition": 45, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_161", "partySize": 4, "arrivalTime": "2026-01-26T13:52:00Z", "state": "QUEUED", "queuePosition": 46, "waitMinutes": 0, "preferences": ["booth", "quiet"] },
    { "id": "guest_162", "partySize": 3, "arrivalTime": "2026-01-26T13:55:00Z", "state": "QUEUED", "queuePosition": 47, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_163", "partySize": 2, "arrivalTime": "2026-01-26T13:58:00Z", "state": "QUEUED", "queuePosition": 48, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_164", "partySize": 6, "arrivalTime": "2026-01-26T14:00:00Z", "state": "QUEUED", "queuePosition": 49, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_165", "partySize": 2, "arrivalTime": "2026-01-26T14:02:00Z", "state": "QUEUED", "queuePosition": 50, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_166", "partySize": 4, "arrivalTime": "2026-01-26T14:05:00Z", "state": "QUEUED", "queuePosition": 51, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_167", "partySize": 3, "arrivalTime": "2026-01-26T14:08:00Z", "state": "QUEUED", "queuePosition": 52, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_168", "partySize": 2, "arrivalTime": "2026-01-26T14:10:00Z", "state": "QUEUED", "queuePosition": 53, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_169", "partySize": 5, "arrivalTime": "2026-01-26T14:12:00Z", "state": "QUEUED", "queuePosition": 54, "waitMinutes": 0, "preferences": ["large_party", "window"] },
    { "id": "guest_170", "partySize": 2, "arrivalTime": "2026-01-26T14:15:00Z", "state": "QUEUED", "queuePosition": 55, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_171", "partySize": 4, "arrivalTime": "2026-01-26T14:18:00Z", "state": "QUEUED", "queuePosition": 56, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_172", "partySize": 3, "arrivalTime": "2026-01-26T14:20:00Z", "state": "QUEUED", "queuePosition": 57, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_173", "partySize": 2, "arrivalTime": "2026-01-26T14:22:00Z", "state": "QUEUED", "queuePosition": 58, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_174", "partySize": 6, "arrivalTime": "2026-01-26T14:25:00Z", "state": "QUEUED", "queuePosition": 59, "waitMinutes": 0, "preferences": ["large_party", "vip"] },
    { "id": "guest_175", "partySize": 2, "arrivalTime": "2026-01-26T14:28:00Z", "state": "QUEUED", "queuePosition": 60, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_176", "partySize": 4, "arrivalTime": "2026-01-26T14:30:00Z", "state": "QUEUED", "queuePosition": 61, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_177", "partySize": 3, "arrivalTime": "2026-01-26T14:32:00Z", "state": "QUEUED", "queuePosition": 62, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_178", "partySize": 2, "arrivalTime": "2026-01-26T14:35:00Z", "state": "QUEUED", "queuePosition": 63, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_179", "partySize": 5, "arrivalTime": "2026-01-26T14:38:00Z", "state": "QUEUED", "queuePosition": 64, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_180", "partySize": 2, "arrivalTime": "2026-01-26T14:40:00Z", "state": "QUEUED", "queuePosition": 65, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_181", "partySize": 4, "arrivalTime": "2026-01-26T14:42:00Z", "state": "QUEUED", "queuePosition": 66, "waitMinutes": 0, "preferences": ["booth", "quiet"] },
    { "id": "guest_182", "partySize": 3, "arrivalTime": "2026-01-26T14:45:00Z", "state": "QUEUED", "queuePosition": 67, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_183", "partySize": 2, "arrivalTime": "2026-01-26T14:48:00Z", "state": "QUEUED", "queuePosition": 68, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_184", "partySize": 6, "arrivalTime": "2026-01-26T14:50:00Z", "state": "QUEUED", "queuePosition": 69, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_185", "partySize": 2, "arrivalTime": "2026-01-26T14:52:00Z", "state": "QUEUED", "queuePosition": 70, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_186", "partySize": 4, "arrivalTime": "2026-01-26T14:55:00Z", "state": "QUEUED", "queuePosition": 71, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_187", "partySize": 3, "arrivalTime": "2026-01-26T14:58:00Z", "state": "QUEUED", "queuePosition": 72, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_188", "partySize": 2, "arrivalTime": "2026-01-26T15:00:00Z", "state": "QUEUED", "queuePosition": 73, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_189", "partySize": 5, "arrivalTime": "2026-01-26T15:02:00Z", "state": "QUEUED", "queuePosition": 74, "waitMinutes": 0, "preferences": ["large_party", "window"] },
    { "id": "guest_190", "partySize": 2, "arrivalTime": "2026-01-26T15:05:00Z", "state": "QUEUED", "queuePosition": 75, "waitMinutes": 0, "preferences": ["bar_view"] },
    { "id": "guest_191", "partySize": 4, "arrivalTime": "2026-01-26T15:08:00Z", "state": "QUEUED", "queuePosition": 76, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_192", "partySize": 3, "arrivalTime": "2026-01-26T15:10:00Z", "state": "QUEUED", "queuePosition": 77, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_193", "partySize": 2, "arrivalTime": "2026-01-26T15:12:00Z", "state": "QUEUED", "queuePosition": 78, "waitMinutes": 0, "preferences": ["window"] },
    { "id": "guest_194", "partySize": 6, "arrivalTime": "2026-01-26T15:15:00Z", "state": "QUEUED", "queuePosition": 79, "waitMinutes": 0, "preferences": ["large_party", "vip"] },
    { "id": "guest_195", "partySize": 2, "arrivalTime": "2026-01-26T15:18:00Z", "state": "QUEUED", "queuePosition": 80, "waitMinutes": 0, "preferences": ["booth"] },
    { "id": "guest_196", "partySize": 4, "arrivalTime": "2026-01-26T15:20:00Z", "state": "QUEUED", "queuePosition": 81, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_197", "partySize": 3, "arrivalTime": "2026-01-26T15:22:00Z", "state": "QUEUED", "queuePosition": 82, "waitMinutes": 0, "preferences": ["quiet"] },
    { "id": "guest_198", "partySize": 2, "arrivalTime": "2026-01-26T15:25:00Z", "state": "QUEUED", "queuePosition": 83, "waitMinutes": 0, "preferences": [] },
    { "id": "guest_199", "partySize": 5, "arrivalTime": "2026-01-26T15:28:00Z", "state": "QUEUED", "queuePosition": 84, "waitMinutes": 0, "preferences": ["large_party"] },
    { "id": "guest_200", "partySize": 2, "arrivalTime": "2026-01-26T15:30:00Z", "state": "QUEUED", "queuePosition": 85, "waitMinutes": 0, "preferences": ["window", "vip"] }
  ]
}
```

### 5.2 Guest State Summary (100 Guests)

| State | Count | Guest IDs (Sample) |
|-------|-------|-------------------|
| `QUEUED` | 85 | guest_101, guest_102, guest_109, ... guest_200 |
| `ESCORTING` | 2 | guest_103, guest_117 |
| `ORDERING` | 3 | guest_105, guest_113, guest_123 |
| `EATING` | 7 | guest_104, guest_110, guest_111, guest_114, guest_116, guest_120, guest_126 |
| `PAYING` | 2 | guest_107, guest_119 |
| `LINGERING` | 1 | guest_108 |

### 5.3 Guest State Test Cases

| Guest ID | Party Size | State | Table | Test Purpose |
|----------|------------|-------|-------|--------------|
| `guest_101` | 2 | `QUEUED` | -- | New walk-in |
| `guest_102` | 4 | `QUEUED` | -- | Walk-in with preferences |
| `guest_103` | 6 | `ESCORTING` | T5 | Being escorted (reservation) |
| `guest_104` | 4 | `EATING` | T3 | Active diner |
| `guest_105` | 2 | `ORDERING` | T9 | Just seated |
| `guest_106` | 8 | `QUEUED` | -- | Large party long wait (18 min) |
| `guest_107` | 2 | `PAYING` | T1 | About to leave |
| `guest_108` | 4 | `LINGERING` | T12 | Overstaying (20 min) |
| `guest_117` | 2 | `ESCORTING` | T16 | Being escorted (walk-in) |
| `guest_137` | 5 | `QUEUED` | -- | VIP preference in queue |
| `guest_174` | 6 | `QUEUED` | -- | Large party VIP |
| `guest_200` | 2 | `QUEUED` | -- | Last in queue (VIP) |

---

## 6. Tasks

### 6.1 Task Queue (Mixed States)

```json
{
  "tasks": [
    {
      "taskId": "task_101",
      "type": "DELIVERY",
      "priority": "HIGH",
      "status": "IN_PROGRESS",
      "targetId": "table_3",
      "target": { "screen": { "x": 350, "y": 100 }, "physical": { "x": 17.5, "y": 5.0 } },
      "assignedRobotId": "bot_1",
      "createdAt": "2026-01-26T12:00:00Z",
      "assignedAt": "2026-01-26T12:00:05Z",
      "estimatedCompletion": "2026-01-26T12:01:30Z",
      "payload": {
        "orderId": "order_501",
        "items": ["Grilled Salmon", "Caesar Salad", "Sparkling Water"]
      },
      "sdkTargetPointId": "table_point_3"
    },
    {
      "taskId": "task_102",
      "type": "ESCORT",
      "priority": "NORMAL",
      "status": "ASSIGNED",
      "targetId": "table_5",
      "target": { "screen": { "x": 600, "y": 100 }, "physical": { "x": 30.0, "y": 5.0 } },
      "assignedRobotId": "bot_2",
      "createdAt": "2026-01-26T12:02:00Z",
      "assignedAt": "2026-01-26T12:02:10Z",
      "estimatedCompletion": "2026-01-26T12:04:00Z",
      "payload": {
        "guestId": "guest_103",
        "partySize": 6
      },
      "sdkTargetPointId": "table_point_5"
    },
    {
      "taskId": "task_103",
      "type": "BUSSING",
      "priority": "LOW",
      "status": "PENDING",
      "targetId": "table_6",
      "target": { "screen": { "x": 150, "y": 200 }, "physical": { "x": 7.5, "y": 10.0 } },
      "assignedRobotId": null,
      "createdAt": "2026-01-26T12:03:00Z",
      "queuePosition": 1,
      "payload": null,
      "sdkTargetPointId": "table_point_6"
    },
    {
      "taskId": "task_104",
      "type": "DELIVERY",
      "priority": "CRITICAL",
      "status": "PENDING",
      "targetId": "table_20",
      "target": { "screen": { "x": 750, "y": 300 }, "physical": { "x": 37.5, "y": 15.0 } },
      "assignedRobotId": null,
      "createdAt": "2026-01-26T12:04:00Z",
      "queuePosition": 2,
      "payload": {
        "orderId": "order_502",
        "items": ["VIP Tasting Menu", "Dom Pérignon"],
        "notes": "VIP Guest - Top Priority"
      },
      "sdkTargetPointId": "table_point_vip"
    },
    {
      "taskId": "task_105",
      "type": "RETURN_TO_DOCK",
      "priority": "LOW",
      "status": "PENDING",
      "targetId": "charger_3",
      "target": { "screen": { "x": 150, "y": 550 }, "physical": { "x": 7.5, "y": 27.5 } },
      "assignedRobotId": "bot_3",
      "createdAt": "2026-01-26T12:05:00Z",
      "payload": {
        "reason": "LOW_BATTERY",
        "batteryLevel": 15
      },
      "sdkTargetPointId": "charge_point_3"
    }
  ]
}
```

### 6.2 Multi-Step Task Chain Example

```json
{
  "taskId": "task_chain_001",
  "type": "DELIVERY",
  "priority": "HIGH",
  "status": "IN_PROGRESS",
  "targetId": "table_8",
  "assignedRobotId": "bot_1",
  "chain": [
    {
      "step": 1,
      "type": "PICKUP",
      "location": "kitchen",
      "sdkPointId": "kitchen_point",
      "status": "COMPLETED",
      "completedAt": "2026-01-26T12:01:00Z"
    },
    {
      "step": 2,
      "type": "DELIVER",
      "location": "table_8",
      "sdkPointId": "table_point_8",
      "status": "IN_PROGRESS",
      "waitForAck": true
    },
    {
      "step": 3,
      "type": "RETURN",
      "location": "kitchen",
      "sdkPointId": "kitchen_point",
      "status": "PENDING",
      "condition": "IF_TRAY_EMPTY"
    }
  ]
}
```

### 6.3 Task State Test Matrix

| Task ID | Type | Status | Robot | Priority | Test Purpose |
|---------|------|--------|-------|----------|--------------|
| `task_101` | DELIVERY | `IN_PROGRESS` | bot_1 | HIGH | Active delivery |
| `task_102` | ESCORT | `ASSIGNED` | bot_2 | NORMAL | Guest escort |
| `task_103` | BUSSING | `PENDING` | -- | LOW | Queue waiting |
| `task_104` | DELIVERY | `PENDING` | -- | CRITICAL | VIP priority |
| `task_105` | RETURN_TO_DOCK | `PENDING` | bot_3 | LOW | Low battery return |
| `task_106` | DELIVERY | `FAILED` | bot_1 | HIGH | Failure recovery |
| `task_107` | DELIVERY | `REASSIGNED` | bot_2→bot_3 | HIGH | Failover test |

---

## 7. Zones

### 7.1 Semantic Zones (Permanent)

```json
{
  "semanticZones": [
    { "id": "window", "name": "Window Section", "tableIds": ["table_1", "table_2", "table_3", "table_4", "table_5"] },
    { "id": "main", "name": "Main Hall", "tableIds": ["table_6", "table_7", "table_8", "table_9", "table_10"] },
    { "id": "center", "name": "Center Section", "tableIds": ["table_11", "table_12", "table_13", "table_14", "table_15"] },
    { "id": "bar", "name": "Bar Area", "tableIds": ["table_16", "table_17", "table_18", "table_19"] },
    { "id": "vip", "name": "VIP Room", "tableIds": ["table_20"] }
  ]
}
```

### 7.2 Restricted Zones (Dynamic)

```json
{
  "restrictedZones": [
    {
      "id": "zone_001",
      "label": "Wet Floor - Bar Spill",
      "type": "WET_FLOOR",
      "active": true,
      "bounds": {
        "screen": { "x": 80, "y": 380, "width": 60, "height": 50 },
        "physical": { "x": 4.0, "y": 19.0, "width": 3.0, "height": 2.5 }
      },
      "createdAt": "2026-01-26T11:45:00Z",
      "expiresAt": "2026-01-26T12:45:00Z",
      "createdBy": "staff_john"
    },
    {
      "id": "zone_002",
      "label": "Maintenance - Floor Repair",
      "type": "MAINTENANCE",
      "active": true,
      "bounds": {
        "screen": { "x": 580, "y": 280, "width": 80, "height": 60 },
        "physical": { "x": 29.0, "y": 14.0, "width": 4.0, "height": 3.0 }
      },
      "createdAt": "2026-01-26T10:00:00Z",
      "expiresAt": "2026-01-26T14:00:00Z",
      "createdBy": "manager_sarah"
    },
    {
      "id": "zone_003",
      "label": "VIP Event - Private",
      "type": "RESERVED",
      "active": false,
      "bounds": {
        "screen": { "x": 700, "y": 250, "width": 150, "height": 120 },
        "physical": { "x": 35.0, "y": 12.5, "width": 7.5, "height": 6.0 }
      },
      "createdAt": "2026-01-26T09:00:00Z",
      "expiresAt": "2026-01-26T22:00:00Z",
      "scheduledActivation": "2026-01-26T18:00:00Z",
      "createdBy": "manager_sarah"
    }
  ]
}
```

---

## 8. Reservations

### 8.1 Today's Reservations

```json
{
  "reservations": [
    {
      "id": "res_201",
      "partySize": 6,
      "dateTime": "2026-01-26T12:00:00Z",
      "name": "Johnson",
      "phone": "+1-555-0101",
      "email": "johnson@email.com",
      "preferences": ["large_party", "window"],
      "notes": "Anniversary dinner",
      "status": "CHECKED_IN",
      "tableId": "table_5",
      "guestId": "guest_103",
      "createdAt": "2026-01-25T10:00:00Z"
    },
    {
      "id": "res_202",
      "partySize": 4,
      "dateTime": "2026-01-26T12:30:00Z",
      "name": "Smith",
      "phone": "+1-555-0102",
      "email": "smith@email.com",
      "preferences": ["quiet", "booth"],
      "notes": null,
      "status": "CONFIRMED",
      "tableId": "table_4",
      "guestId": null,
      "createdAt": "2026-01-24T14:00:00Z"
    },
    {
      "id": "res_203",
      "partySize": 2,
      "dateTime": "2026-01-26T13:00:00Z",
      "name": "Williams",
      "phone": "+1-555-0103",
      "email": null,
      "preferences": ["window"],
      "notes": "First visit",
      "status": "CONFIRMED",
      "tableId": null,
      "guestId": null,
      "createdAt": "2026-01-26T09:00:00Z"
    },
    {
      "id": "res_204",
      "partySize": 8,
      "dateTime": "2026-01-26T19:00:00Z",
      "name": "Chen",
      "phone": "+1-555-0104",
      "email": "chen@company.com",
      "preferences": ["vip", "private"],
      "notes": "Business dinner - CEO hosting",
      "status": "CONFIRMED",
      "tableId": "table_20",
      "guestId": null,
      "createdAt": "2026-01-20T16:00:00Z"
    },
    {
      "id": "res_205",
      "partySize": 4,
      "dateTime": "2026-01-26T11:00:00Z",
      "name": "Garcia",
      "phone": "+1-555-0105",
      "email": null,
      "preferences": [],
      "notes": null,
      "status": "NO_SHOW",
      "tableId": "table_8",
      "guestId": null,
      "createdAt": "2026-01-25T20:00:00Z",
      "noShowAt": "2026-01-26T11:15:00Z"
    }
  ]
}
```

---

## 9. Alerts

### 9.1 Active Alerts

```json
{
  "alerts": [
    {
      "id": "alert_501",
      "severity": "CRITICAL",
      "category": "ROBOT_STUCK",
      "title": "Robot bot_1 stuck for 3+ minutes",
      "message": "Bot_1 has been blocked near Table 8 for over 3 minutes. Manual assistance required.",
      "status": "ACTIVE",
      "createdAt": "2026-01-26T11:57:00Z",
      "context": {
        "robotId": "bot_1",
        "taskId": "task_106",
        "position": { "screen": { "x": 340, "y": 195 }, "physical": { "x": 17.0, "y": 9.75 } },
        "blockedDuration": 185,
        "blockedType": "TYPE_ENCOUNTER_OBSTACLES"
      },
      "suggestedActions": [
        "Clear obstacle near robot",
        "Manually push robot to safe zone",
        "Cancel current task and reassign"
      ]
    },
    {
      "id": "alert_502",
      "severity": "HIGH",
      "category": "GUEST_WAITING",
      "title": "Guest waiting 18+ minutes",
      "message": "Guest party of 8 has been waiting for 18 minutes. Consider priority seating.",
      "status": "ACKNOWLEDGED",
      "createdAt": "2026-01-26T11:42:00Z",
      "acknowledgedAt": "2026-01-26T11:55:00Z",
      "acknowledgedBy": "staff_jane",
      "context": {
        "guestId": "guest_106",
        "partySize": 8,
        "waitMinutes": 18,
        "queuePosition": 1
      },
      "suggestedActions": [
        "Offer complimentary drinks",
        "Prepare large table",
        "Consider splitting party"
      ]
    },
    {
      "id": "alert_503",
      "severity": "MEDIUM",
      "category": "BATTERY_CRITICAL",
      "title": "Robot bot_3 battery at 12%",
      "message": "Bot_3 battery is critically low. Robot is returning to dock.",
      "status": "ACTIVE",
      "createdAt": "2026-01-26T12:03:00Z",
      "context": {
        "robotId": "bot_3",
        "batteryLevel": 12,
        "estimatedRemainingMinutes": 8,
        "taskId": null
      },
      "suggestedActions": [
        "Ensure charging dock is available",
        "Redistribute pending tasks"
      ]
    },
    {
      "id": "alert_504",
      "severity": "LOW",
      "category": "GUEST_LINGERING",
      "title": "Table 12 guest lingering 20+ minutes",
      "message": "Guests at Table 12 have finished paying but remain seated for 20 minutes.",
      "status": "ACTIVE",
      "createdAt": "2026-01-26T11:50:00Z",
      "context": {
        "tableId": "table_12",
        "guestId": "guest_108",
        "lingeringMinutes": 20,
        "nextReservation": null
      },
      "suggestedActions": [
        "Politely check if guests need anything",
        "Offer dessert menu if not ordered"
      ]
    }
  ]
}
```

### 9.2 Alert Test Scenarios

| Alert ID | Severity | Category | Status | Test Purpose |
|----------|----------|----------|--------|--------------|
| `alert_501` | CRITICAL | ROBOT_STUCK | ACTIVE | Robot blocked alert |
| `alert_502` | HIGH | GUEST_WAITING | ACKNOWLEDGED | Guest wait time |
| `alert_503` | MEDIUM | BATTERY_CRITICAL | ACTIVE | Low battery warning |
| `alert_504` | LOW | GUEST_LINGERING | ACTIVE | Table turnover |
| `alert_505` | CRITICAL | COLLISION_DETECTED | RESOLVED | Emergency stop |
| `alert_506` | HIGH | LOW_FLEET_CAPACITY | ACTIVE | All robots busy |

---

## 10. Simulation Scenarios

### 10.1 Presets

```json
{
  "presets": {
    "SLOW": {
      "guestsPerHour": 20,
      "avgPartySize": 2,
      "avgDiningMinutes": 60
    },
    "NORMAL": {
      "guestsPerHour": 40,
      "avgPartySize": 3,
      "avgDiningMinutes": 45
    },
    "RUSH_HOUR": {
      "guestsPerHour": 80,
      "avgPartySize": 3,
      "avgDiningMinutes": 35,
      "peakMultiplier": 1.5
    },
    "STRESS_TEST": {
      "guestsPerHour": 120,
      "avgPartySize": 4,
      "avgDiningMinutes": 30,
      "peakMultiplier": 2.0
    }
  }
}
```

### 10.2 Fleet Optimization Test Parameters

```json
{
  "optimizationTest": {
    "layoutId": "layout_main_v1",
    "testRobotCounts": [4, 6, 8, 10, 12],
    "simulationParams": {
      "durationMinutes": 480,
      "timeScale": 50.0,
      "guestsPerHour": 100
    },
    "constraints": {
      "maxAcceptableWaitMinutes": 5,
      "minServiceLevel": 0.95,
      "budgetMaxRobots": 12
    },
    "costParams": {
      "robotLeaseMonthly": 500.00,
      "robotMaintenanceMonthly": 50.00,
      "laborCostHourly": 15.00,
      "laborHoursReplacedPerRobot": 4
    }
  }
}
```

### 10.3 Expected Optimization Results

| Robots | Avg Wait (min) | Service Level | Utilization | Monthly Cost | Monthly Savings | Verdict |
|--------|----------------|---------------|-------------|--------------|-----------------|---------|
| 4 | 8.5 | 0.82 | 0.92 | $2,200 | $1,400 | INSUFFICIENT |
| 6 | 4.8 | 0.93 | 0.85 | $3,300 | $2,100 | MARGINAL |
| 8 | 2.5 | 0.97 | 0.75 | $4,400 | $2,800 | GOOD |
| 10 | 1.5 | 0.98 | 0.65 | $5,500 | $3,500 | **OPTIMAL** |
| 12 | 1.0 | 0.99 | 0.52 | $6,600 | $4,200 | OVER_CAPACITY |

---

## 11. Robot SDK Mapping

### 11.1 PadBot Navigation SDK Reference

The backend integrates with **PadBot Navigation SDK v1.5.0**. Below are the key mappings:

#### SDK Client Methods → Backend Actions

| Backend Action | SDK Method | Description |
|----------------|------------|-------------|
| Navigate to point | `startNavigate(pointId)` | Navigate to target point |
| Auto charge | `startAutoCharge(pointId)` | Navigate to charge point |
| Pause navigation | `pauseNavigateOrAutoCharge()` | Pause current navigation |
| Resume navigation | `resumeNavigateOrAutoCharge()` | Resume navigation |
| Stop navigation | `stopNavigate()` | Cancel navigation |
| Get position | `getLocateInfo()` | Get current location |
| Get map info | `getMapInfo()` | Get floor/map data |

#### SDK Events → Backend Events

| SDK Event | Backend SignalR Event | Description |
|-----------|----------------------|-------------|
| `OnReceiveRobotPositionEvent` | `RobotMoved` | Position update (gridX, gridY, angle) |
| `OnRobotBasicStatusChangedEvent` | `RobotStateChanged` | Battery, charging status |
| `OnRobotBlockedEvent` | `RobotBlocked` | Obstacle detection |
| `OnNavigateInfoChangedEvent` | `TaskProgressed` | Navigation progress |
| `OnActionStatusChangedEvent` | `TaskCompleted/TaskFailed` | Action completion |
| `OnNavigationClientDisconnectedEvent` | `RobotOffline` | Connection lost |

#### SDK Target Point Types

| SDK Constant | Value | Backend Checkpoint Type |
|--------------|-------|------------------------|
| `TARGET_POINT_TYPE_COMMON` | 0 | Table, Reception, Cashier |
| `TARGET_POINT_TYPE_BASE_CHARGE` | 1 | Charging Station |
| `TARGET_POINT_TYPE_SHIPMENT` | 2 | Kitchen (Food Pickup) |
| `TARGET_POINT_TYPE_ENTRANCE` | 3 | Main Entrance |
| `TARGET_POINT_TYPE_ELEVATOR` | 4 | (Not used - single floor) |

#### SDK Blocked Types → Backend Alert Categories

| SDK RobotBlockedType | Backend Handling |
|---------------------|------------------|
| `TYPE_ENCOUNTER_OBSTACLES` | Create `ROBOT_STUCK` alert after timeout |
| `TYPE_GET_PATH_FAILED` | Create `TARGET_UNREACHABLE` error |
| `TYPE_ARRIVED_TARGET_POINT_TIMEOUT` | Trigger task failover |
| `TYPE_ARRIVED_TARGET_POINT_FAILED` | Mark task as FAILED |
| `TYPE_CRUISE_FAILED` | Log and retry |
| `TYPE_ROBOT_STATIC` | Check for stuck condition |
| `TYPE_SEARCH_CHARGING_BASE_TIMEOUT` | Create `BATTERY_CRITICAL` alert |

#### SDK Error Codes → Backend Error Handling

| SDK NavigateErrorCode | Backend Response |
|----------------------|------------------|
| `NO_TARGET_POINT` | 404 `TARGET_NOT_FOUND` |
| `NO_NAVIGATE_PATH_PLANNING` | 400 `TARGET_UNREACHABLE` |
| `RELOCATE_FAILED` | Robot → ERROR state |
| `RELOCATE_TIMEOUT` | Robot → ERROR state + alert |
| `SWITCH_MAP_FAILED` | (N/A - single floor) |

### 11.2 Coordinate System Mapping

```
SDK Grid Coordinates → Backend Screen Coordinates → Physical Coordinates

SDK gridX/gridY (integer)  →  screen.x/screen.y (pixels)  →  physical.x/physical.y (meters)

Conversion:
- screen.x = gridX * gridResolution (e.g., gridX * 20 = pixels)
- physical.x = screen.x * scale (e.g., pixels * 0.05 = meters)
```

### 11.3 SDK Connection Lifecycle

```
1. connect(Context)
   → Wait for OnNavigationClientConnectedEvent

2. initNavigation()
   → Wait for OnNavigationInitedEvent

3. refreshMapInfo()
   → Get IndoorMapVoListResult with target points

4. Subscribe to events via EventBus:
   - OnReceiveRobotPositionEvent (10Hz)
   - OnRobotBasicStatusChangedEvent
   - OnRobotBlockedEvent
   - OnNavigateInfoChangedEvent
   - OnActionStatusChangedEvent

5. Navigation operations:
   - startNavigate(pointId)
   - pauseNavigateOrAutoCharge()
   - resumeNavigateOrAutoCharge()
   - stopNavigate()
   - startAutoCharge(chargePointId)
```

---

## Appendix: Test Data Generation Scripts

### A.1 Generate Random Guests

```javascript
function generateRandomGuest(id) {
  const partySizes = [1, 2, 2, 2, 3, 3, 4, 4, 4, 5, 6, 8];
  const preferences = ["window", "booth", "quiet", "bar_view", "large_party", "vip"];
  
  return {
    id: `guest_${id}`,
    partySize: partySizes[Math.floor(Math.random() * partySizes.length)],
    arrivalTime: new Date().toISOString(),
    state: "QUEUED",
    preferences: preferences.filter(() => Math.random() > 0.7)
  };
}
```

### A.2 Generate Random Robot Movement

```javascript
function generateMovementPath(from, to, steps = 10) {
  const path = [];
  for (let i = 0; i <= steps; i++) {
    const t = i / steps;
    path.push({
      x: Math.round(from.x + (to.x - from.x) * t),
      y: Math.round(from.y + (to.y - from.y) * t),
      heading: Math.round(Math.atan2(to.y - from.y, to.x - from.x) * 180 / Math.PI)
    });
  }
  return path;
}
```
