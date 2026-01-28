# Frontend Implementation Guide

> **Project**: Don-Quixote (RoboRunner) Frontend  
> **Backend URL**: `http://localhost:5199`  
> **SignalR Hub**: `ws://localhost:5199/hubs/restaurant`  
> **Last Updated**: January 2026

---

## Table of Contents

1. [Overview](#1-overview)
2. [TypeScript Definitions](#2-typescript-definitions)
3. [API Client Setup](#3-api-client-setup)
4. [Entity APIs](#4-entity-apis)
5. [SignalR Real-Time Integration](#5-signalr-real-time-integration)
6. [Business Scenarios](#6-business-scenarios)
7. [UI Component Patterns](#7-ui-component-patterns)
8. [Error Handling](#8-error-handling)
9. [Long-Run Simulation](#9-long-run-simulation)

---

## 1. Overview

### Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (React 19)                       │
├─────────────────────────────────────────────────────────────┤
│  Components    │  State (Redux)  │  Services                │
│  - RobotMap    │  - robots       │  - apiClient             │
│  - ControlPanel│  - tables       │  - signalRService        │
│  - Dashboard   │  - tasks        │  - simulationService     │
│  - Alerts      │  - alerts       │                          |
└────────┬────────────────┬────────────────────┬──────────────┘
         │                │                    │
         │  HTTP/REST     │  SignalR WebSocket │
         ▼                ▼                    ▼
┌─────────────────────────────────────────────────────────────┐
│                Backend API (.NET 10)                         │
│                http://localhost:5199                         │
└─────────────────────────────────────────────────────────────┘
```

### Key Concepts
- **Dual Coordinates**: Screen (pixels) for UI, Physical (meters) for navigation
- **Real-time Updates**: SignalR for live robot positions, task status, alerts
- **String Enums**: All enums returned as strings (e.g., `"Idle"` not `0`)
- **ISO 8601 Dates**: All timestamps in ISO format (e.g., `"2026-01-27T10:30:00Z"`)

---

## 2. TypeScript Definitions

### Position (Used Everywhere)
```typescript
interface PositionDto {
  screenX: number;     // Screen coordinates (pixels)
  screenY: number;
  physicalX: number;   // Physical coordinates (meters)
  physicalY: number;
}
```

### Enums
```typescript
// Robot
type RobotStatus = "Idle" | "Navigating" | "Delivering" | "Returning" | "Charging" | "Error" | "Offline";

// Task
type TaskType = "Deliver" | "Return" | "Charge" | "Patrol" | "Escort" | "Greeting" | "Service" | "Cleaning" | "Custom";
type TaskStatus = "Pending" | "Assigned" | "InProgress" | "Completed" | "Failed" | "Cancelled";
type TaskPriority = "Low" | "Normal" | "High" | "Urgent";

// Table
type TableStatus = "Available" | "Occupied" | "Reserved" | "NeedsService" | "Cleaning";
type TableShape = "Rectangle" | "Circle" | "Square";

// Guest
type GuestStatus = "Waiting" | "Seated" | "Departed";

// Alert
type AlertType = "LowBattery" | "NavigationBlocked" | "RobotError" | "TableService" | "GuestWaiting" | "Custom";
type AlertSeverity = "Info" | "Warning" | "Error" | "Critical";

// Zone
type ZoneType = "Dining" | "Kitchen" | "Charging" | "Entrance" | "Restroom";

```

### Robot DTOs
```typescript
interface RobotDto {
  id: number;
  name: string;
  model: string | null;
  status: RobotStatus;
  position: PositionDto;
  heading: number;       // 0-360 degrees
  batteryLevel: number;  // 0-100
  velocity: number;      // m/s
  currentTaskId: number | null;
  isEnabled: boolean;
  lastUpdated: string;
}

interface CreateRobotRequest {
  name: string;
  model?: string;
  position?: PositionDto;
  isEnabled?: boolean;
}

interface UpdateRobotRequest {
  name?: string;
  model?: string;
  status?: RobotStatus;
  isEnabled?: boolean;
}

interface RobotPositionUpdate {
  screenX: number;
  screenY: number;
  physicalX?: number;
  physicalY?: number;
  heading: number;
  velocity: number;
}

interface RobotCommandRequest {
  command: "go_to" | "return_home" | "pause" | "resume" | "emergency_stop" | "clear_error";
  targetPosition?: PositionDto;
  targetTableId?: number;
  speed?: number;
  reason?: string;
}

interface CommandAcknowledgment {
  robotId: number;
  command: string;
  accepted: boolean;
  message: string | null;
  estimatedCompletionSeconds: number | null;
  timestamp: string;
}

interface RobotHistoryEntry {
  taskId: number;
  taskType: string;
  status: string;
  startedAt: string | null;
  completedAt: string | null;
  durationSeconds: number | null;
  targetTableLabel: string | null;
}

interface RobotHistoryResponse {
  robotId: number;
  robotName: string;
  history: RobotHistoryEntry[];
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  averageTaskDurationSeconds: number;
}
```

### Task DTOs
```typescript
interface TaskDto {
  id: number;
  type: TaskType;
  status: TaskStatus;
  priority: TaskPriority;
  robotId: number | null;
  robotName: string | null;
  targetTableId: number | null;
  targetTableLabel: string | null;
  startPosition: PositionDto;
  targetPosition: PositionDto;
  estimatedDurationSeconds: number | null;
  actualDurationSeconds: number | null;
  errorMessage: string | null;
  retryCount: number;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

interface CreateTaskRequest {
  type: TaskType;
  priority?: TaskPriority;
  robotId?: number;
  targetTableId?: number;
  startPosition?: PositionDto;
  targetPosition?: PositionDto;
}

interface TaskQueueSummary {
  pendingCount: number;
  assignedCount: number;
  inProgressCount: number;
  completedToday: number;
  failedToday: number;
}
```

### Table DTOs
```typescript
interface TableDto {
  id: number;
  label: string | null;
  shape: TableShape;
  status: TableStatus;
  center: PositionDto;
  width: number;
  height: number;
  rotation: number;
  capacity: number;
  zoneId: number | null;
  zoneName: string | null;
  currentGuestCount: number;
}

interface CreateTableRequest {
  label?: string;
  shape?: TableShape;
  center?: PositionDto;
  width?: number;
  height?: number;
  rotation?: number;
  capacity?: number;
  zoneId?: number;
}

interface UpdateTableRequest {
  label?: string;
  shape?: TableShape;
  status?: TableStatus;
  center?: PositionDto;
  width?: number;
  height?: number;
  rotation?: number;
  capacity?: number;
  zoneId?: number;
}

interface TableStatusSummary {
  total: number;
  available: number;
  occupied: number;
  reserved: number;
  needsService: number;
  cleaning: number;
  occupancyPercent: number;
}
```

### Guest DTOs
```typescript
interface GuestDto {
  id: number;
  name: string | null;
  partySize: number;
  status: GuestStatus;
  tableId: number | null;
  tableLabel: string | null;
  queuePosition: number | null;
  arrivalTime: string;
  seatedTime: string | null;
  departedTime: string | null;
  estimatedWaitMinutes: number | null;
  notes: string | null;
  phoneNumber: string | null;
}

interface CreateGuestRequest {
  name?: string;
  partySize?: number;
  notes?: string;
  phoneNumber?: string;
}

interface WaitlistSummary {
  totalWaiting: number;
  averageWaitMinutes: number;
  guests: GuestDto[];
}
```

### Alert DTOs
```typescript
interface AlertDto {
  id: number;
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  message: string | null;
  robotId: number | null;
  robotName: string | null;
  tableId: number | null;
  taskId: number | null;
  isAcknowledged: boolean;
  acknowledgedBy: string | null;
  acknowledgedAt: string | null;
  isResolved: boolean;
  resolvedAt: string | null;
  createdAt: string;
}

interface CreateAlertRequest {
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  message?: string;
  robotId?: number;
  tableId?: number;
  taskId?: number;
}

interface AlertSummary {
  totalActive: number;
  critical: number;
  errors: number;
  warnings: number;
  unacknowledged: number;
}
```

### Dashboard DTOs
```typescript
interface DashboardSummary {
  robots: RobotSummary;
  tables: TableStatusSummary;
  tasks: TaskQueueSummary;
  alerts: AlertSummary;
  activeGuests: number;
  waitlistCount: number;
}

interface RobotSummary {
  total: number;
  idle: number;
  navigating: number;
  delivering: number;
  charging: number;
  error: number;
  offline: number;
  averageBattery: number;
}
```

### Dispatch DTOs
```typescript
interface DispatchConfig {
  algorithm: "nearest" | "round_robin" | "load_balanced" | "priority";
  maxTasksPerRobot: number;
  minBatteryForAssignment: number;
  autoAssignEnabled: boolean;
  assignmentIntervalSeconds: number;
}

interface DispatchQueueItem {
  taskId: number;
  taskType: string;
  priority: string;
  createdAt: string;
  suggestedRobotId: number | null;
  suggestedRobotName: string | null;
  estimatedDistance: number | null;
}

interface AutoAssignResult {
  tasksAssigned: number;
  tasksSkipped: number;
  assignments: TaskAssignment[];
}

interface TaskAssignment {
  taskId: number;
  robotId: number;
  robotName: string;
  reason: string;
}
```

### Simulation DTOs
```typescript
interface StartSimulationRequest {
  simulatedStartTime?: string;
  simulatedEndTime?: string;
  accelerationFactor?: number;  // default: 720
  robotCount?: number;          // default: 5
  tableCount?: number;          // default: 20
  randomSeed?: number;
  eventPatterns?: SimulationEventPatternDto;
}

interface SimulationEventPatternDto {
  hourlyArrivalRates?: number[];
  dayOfWeekMultipliers?: number[];
  averageMealDurationMinutes?: number;
  guestNeedsHelpProbability?: number;
  averagePartySize?: number;
}

interface StartSimulationResponse {
  simulationId: string;
  status: string;
  simulatedStartTime: string;
  simulatedEndTime: string;
  accelerationFactor: number;
  estimatedEventsCount: number;
  estimatedRealDuration: string;
}

interface SimulationProgressDto {
  simulationId: string;
  state: "Running" | "Paused" | "Completed" | "Cancelled";
  simulatedStartTime: string;
  simulatedEndTime: string;
  currentSimulatedTime: string;
  progressPercent: number;
  realElapsedTime: string;
  estimatedTimeRemaining: string;
  accelerationFactor: number;
  eventsProcessed: number;
  totalEventsScheduled: number;
  guestsProcessed: number;
  tasksCreated: number;
  tasksCompleted: number;
  currentSuccessRate: number;
}

interface SimulationReportDto {
  summary: SimulationReportSummaryDto;
  dailyBreakdown: SimulationDailyMetricsDto[];
  robotMetrics: SimulationRobotMetricsDto[];
}

interface SimulationReportSummaryDto {
  simulationId: string;
  simulatedDays: number;
  totalGuests: number;
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  overallSuccessRate: number;
  averageTaskDurationSeconds: number;
  peakConcurrentGuests: number;
  averageRobotUtilization: number;
}
```

### kitchenTask
``` typescript
interface kitchenTaskDto {
  id: string;
  tableId: number;
  orderId: string;
  items: string[];
  status: "Pending" | "InProgress" | "Completed" | "Cancelled";
}
```
---

## 3. API Client Setup

### Base Client
```typescript
// src/services/apiClient.ts
const API_BASE = process.env.REACT_APP_API_BASE || 'http://localhost:5199';

interface ApiError {
  error: string;
}

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    path: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    if (!response.ok) {
      if (response.status === 204) {
        return undefined as T;
      }
      const error: ApiError = await response.json();
      throw new Error(error.error || `HTTP ${response.status}`);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return response.json();
  }

  get<T>(path: string): Promise<T> {
    return this.request<T>(path, { method: 'GET' });
  }

  post<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  put<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  patch<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'PATCH',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  delete(path: string): Promise<void> {
    return this.request<void>(path, { method: 'DELETE' });
  }
}

export const apiClient = new ApiClient();
```

---

## 4. Entity APIs

### Robots API
```typescript
// src/services/robotsApi.ts
import { apiClient } from './apiClient';

export const robotsApi = {
  // List robots with optional filters
  list: (params?: { status?: string; isEnabled?: boolean }) => {
    const query = new URLSearchParams(params as Record<string, string>).toString();
    return apiClient.get<RobotDto[]>(`/api/robots${query ? `?${query}` : ''}`);
  },

  // Get single robot
  get: (id: number) => apiClient.get<RobotDto>(`/api/robots/${id}`),

  // Create robot
  create: (data: CreateRobotRequest) => 
    apiClient.post<RobotDto>('/api/robots', data),

  // Update robot
  update: (id: number, data: UpdateRobotRequest) =>
    apiClient.put<RobotDto>(`/api/robots/${id}`, data),

  // Delete robot
  delete: (id: number) => apiClient.delete(`/api/robots/${id}`),

  // Get robot by status
  getByStatus: (status: RobotStatus) =>
    apiClient.get<RobotDto[]>(`/api/robots/status/${status}`),

  // Get idle robots
  getIdle: () => apiClient.get<RobotDto[]>('/api/robots/idle'),

  // Update position
  updatePosition: (id: number, position: RobotPositionUpdate) =>
    apiClient.put<RobotDto>(`/api/robots/${id}/position`, position),

  // Update battery
  updateBattery: (id: number, level: number) =>
    apiClient.put<RobotDto>(`/api/robots/${id}/battery`, { batteryLevel: level }),

  // Send command
  sendCommand: (id: number, command: RobotCommandRequest) =>
    apiClient.patch<CommandAcknowledgment>(`/api/robots/${id}/command`, command),

  // Get task history
  getHistory: (id: number, limit?: number) =>
    apiClient.get<RobotHistoryResponse>(
      `/api/robots/${id}/history${limit ? `?limit=${limit}` : ''}`
    ),
};
```

### Tasks API
```typescript
// src/services/tasksApi.ts
export const tasksApi = {
  // List tasks with filters
  list: (params?: { status?: string; type?: string; robotId?: number }) => {
    const query = new URLSearchParams(params as Record<string, string>).toString();
    return apiClient.get<TaskDto[]>(`/api/tasks${query ? `?${query}` : ''}`);
  },

  get: (id: number) => apiClient.get<TaskDto>(`/api/tasks/${id}`),

  create: (data: CreateTaskRequest) => 
    apiClient.post<TaskDto>('/api/tasks', data),

  delete: (id: number) => apiClient.delete(`/api/tasks/${id}`),

  // Task queue
  getPending: () => apiClient.get<TaskDto[]>('/api/tasks/status/Pending'),
  
  getByRobot: (robotId: number) =>
    apiClient.get<TaskDto[]>(`/api/tasks/robot/${robotId}`),

  // Task lifecycle
  assign: (taskId: number, robotId: number) =>
    apiClient.post<TaskDto>(`/api/tasks/${taskId}/assign`, { robotId }),

  start: (id: number) => apiClient.post<TaskDto>(`/api/tasks/${id}/start`),

  complete: (id: number) => apiClient.post<TaskDto>(`/api/tasks/${id}/complete`),

  fail: (id: number, reason?: string) =>
    apiClient.post<TaskDto>(`/api/tasks/${id}/fail`, { reason }),

  unassign: (id: number) => apiClient.post<TaskDto>(`/api/tasks/${id}/unassign`),

  // Summary
  getSummary: () => apiClient.get<TaskQueueSummary>('/api/tasks/summary'),
};
```

### Tables API
```typescript
// src/services/tablesApi.ts
export const tablesApi = {
  list: () => apiClient.get<TableDto[]>('/api/tables'),

  get: (id: number) => apiClient.get<TableDto>(`/api/tables/${id}`),

  create: (data: CreateTableRequest) =>
    apiClient.post<TableDto>('/api/tables', data),

  update: (id: number, data: UpdateTableRequest) =>
    apiClient.put<TableDto>(`/api/tables/${id}`, data),

  delete: (id: number) => apiClient.delete(`/api/tables/${id}`),

  getByStatus: (status: TableStatus) =>
    apiClient.get<TableDto[]>(`/api/tables/status/${status}`),

  getAvailable: (minCapacity?: number) =>
    apiClient.get<TableDto[]>(
      `/api/tables/available${minCapacity ? `?minCapacity=${minCapacity}` : ''}`
    ),

  updateStatus: (id: number, status: TableStatus) =>
    apiClient.put<TableDto>(`/api/tables/${id}/status`, { status }),

  getSummary: () => apiClient.get<TableStatusSummary>('/api/tables/summary'),

  getByZone: (zoneId: number) =>
    apiClient.get<TableDto[]>(`/api/tables/zone/${zoneId}`),
};
```

### Guests API
```typescript
// src/services/guestsApi.ts
export const guestsApi = {
  list: (params?: { status?: string }) => {
    const query = new URLSearchParams(params as Record<string, string>).toString();
    return apiClient.get<GuestDto[]>(`/api/guests${query ? `?${query}` : ''}`);
  },

  get: (id: number) => apiClient.get<GuestDto>(`/api/guests/${id}`),

  // Add to waitlist
  create: (data: CreateGuestRequest) =>
    apiClient.post<GuestDto>('/api/guests', data),

  delete: (id: number) => apiClient.delete(`/api/guests/${id}`),

  // WaitList
  getWaitList: () => apiClient.get<WaitlistSummary>('/api/guests/waitlist'),

  // Seat guest at table
  seat: (guestId: number, tableId: number) =>
    apiClient.post<GuestDto>(`/api/guests/${guestId}/seat`, { tableId }),

  // Check out
  checkout: (id: number) =>
    apiClient.post<GuestDto>(`/api/guests/${id}/checkout`),

  getByTable: (tableId: number) =>
    apiClient.get<GuestDto[]>(`/api/guests/table/${tableId}`),
};
```

### Alerts API
```typescript
// src/services/alertsApi.ts
export const alertsApi = {
  list: (params?: { severity?: string; acknowledged?: boolean }) => {
    const query = new URLSearchParams(params as Record<string, string>).toString();
    return apiClient.get<AlertDto[]>(`/api/alerts${query ? `?${query}` : ''}`);
  },

  get: (id: number) => apiClient.get<AlertDto>(`/api/alerts/${id}`),

  create: (data: CreateAlertRequest) =>
    apiClient.post<AlertDto>('/api/alerts', data),

  delete: (id: number) => apiClient.delete(`/api/alerts/${id}`),

  getUnacknowledged: () =>
    apiClient.get<AlertDto[]>('/api/alerts/unacknowledged'),

  acknowledge: (id: number, acknowledgedBy?: string) =>
    apiClient.post<AlertDto>(`/api/alerts/${id}/acknowledge`, { acknowledgedBy }),

  resolve: (id: number) =>
    apiClient.post<AlertDto>(`/api/alerts/${id}/resolve`),

  getByRobot: (robotId: number) =>
    apiClient.get<AlertDto[]>(`/api/alerts/robot/${robotId}`),

  getSummary: () => apiClient.get<AlertSummary>('/api/alerts/summary'),
};
```

### Dashboard API
```typescript
// src/services/dashboardApi.ts
export const dashboardApi = {
  getSummary: () => apiClient.get<DashboardSummary>('/api/dashboard'),

  getRobotSummary: () => apiClient.get<RobotSummary>('/api/dashboard/robots'),

  getTableSummary: () => apiClient.get<TableStatusSummary>('/api/dashboard/tables'),
};
```

### Dispatch API
```typescript
// src/services/dispatchApi.ts
export const dispatchApi = {
  getQueue: () => apiClient.get<DispatchQueueItem[]>('/api/dispatch/queue'),

  triggerAutoAssign: () =>
    apiClient.post<AutoAssignResult>('/api/dispatch/auto-assign'),

  getConfig: () => apiClient.get<DispatchConfig>('/api/dispatch/config'),

  updateConfig: (config: Partial<DispatchConfig>) =>
    apiClient.patch<DispatchConfig>('/api/dispatch/config', config),

  suggestRobot: (taskId: number) =>
    apiClient.post<{ robotId: number; robotName: string; reason: string }>(
      `/api/dispatch/suggest/${taskId}`
    ),
};
```

### Events API (Trigger Actions)
```typescript
// src/services/eventsApi.ts
export const eventsApi = {
  // Guest arrived at restaurant
  guestArrived: (partySize: number, guestName?: string) =>
    apiClient.post('/api/events/guest-arrived', { partySize, guestName }),

  // Guest needs help at table
  guestNeedsHelp: (tableId: number, reason?: string) =>
    apiClient.post('/api/events/guest-help', { tableId, reason }),

  // Change table status
  tableStatusChange: (tableId: number, newStatus: TableStatus) =>
    apiClient.post('/api/events/table-status', { tableId, newStatus }),
};
```

### Kitchen API
```typescript
// src/services/kitchenApi.ts
export const kitchenApi = {
  // Food ready for delivery
  foodReady: (tableId: number, orderId?: string, items?: string[]) =>
    apiClient.post('/api/kitchen/food-ready', { tableId, orderId, items }),

  // Drinks ready
  drinksReady: (tableId: number, orderId?: string, items?: string[]) =>
    apiClient.post('/api/kitchen/drinks-ready', { tableId, orderId, items }),

  // Generic order ready
  orderReady: (tableId: number, orderId?: string, note?: string, isRush?: boolean) =>
    apiClient.post('/api/kitchen/order-ready', { tableId, orderId, note, isRush }),
};
```

### Emergency API
```typescript
// src/services/emergencyApi.ts
export const emergencyApi = {
  // Emergency stop all robots
  stopAll: () => apiClient.post('/api/emergency/stop-all'),

  // Evacuation mode
  evacuate: () => apiClient.post('/api/emergency/evacuate'),

  // Resume normal operations
  resume: () => apiClient.post('/api/emergency/resume'),
};
```

### Simulation API
```typescript
// src/services/simulationApi.ts
interface AccelerationPreset {
  name: string;
  factor: number;
  description: string;
}

export const simulationApi = {
  // Get available speed presets
  getPresets: () => 
    apiClient.get<AccelerationPreset[]>('/api/simulation/long-run/acceleration-presets'),

  // Start long-run simulation
  start: (request: StartSimulationRequest) =>
    apiClient.post<StartSimulationResponse>('/api/simulation/long-run', request),

  // Get current progress
  getProgress: () =>
    apiClient.get<SimulationProgressDto>('/api/simulation/long-run/progress'),

  // Control simulation
  pause: () => apiClient.post('/api/simulation/long-run/pause'),
  resume: () => apiClient.post('/api/simulation/long-run/resume'),
  stop: () => apiClient.post('/api/simulation/long-run/stop'),

  // Get final report
  getReport: () =>
    apiClient.get<SimulationReportDto>('/api/simulation/long-run/report'),
};
```

---

## 5. SignalR Real-Time Integration

### Setup SignalR Connection
```typescript
// src/services/signalRService.ts
import * as signalR from '@microsoft/signalr';

const HUB_URL = process.env.REACT_APP_SIGNALR_HUB || 'http://localhost:5199/hubs/restaurant';

class SignalRService {
  private connection: signalR.HubConnection | null = null;

  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 2, 4, 8, 16, 32 seconds
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupConnectionHandlers();
    
    try {
      await this.connection.start();
      console.log('SignalR connected');
    } catch (err) {
      console.error('SignalR connection error:', err);
      throw err;
    }
  }

  private setupConnectionHandlers(): void {
    if (!this.connection) return;

    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.resubscribeAll();
    });

    this.connection.onclose((error) => {
      console.error('SignalR disconnected:', error);
    });
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  // Subscription methods
  async subscribeToRobot(robotId: number): Promise<void> {
    await this.connection?.invoke('SubscribeToRobot', robotId);
  }

  async unsubscribeFromRobot(robotId: number): Promise<void> {
    await this.connection?.invoke('UnsubscribeFromRobot', robotId);
  }

  async subscribeToZone(zoneId: number): Promise<void> {
    await this.connection?.invoke('SubscribeToZone', zoneId);
  }

  async subscribeToTable(tableId: number): Promise<void> {
    await this.connection?.invoke('SubscribeToTable', tableId);
  }

  async subscribeToAlerts(): Promise<void> {
    await this.connection?.invoke('SubscribeToAlerts');
  }

  async subscribeToTasks(): Promise<void> {
    await this.connection?.invoke('SubscribeToTasks');
  }

  async subscribeToSimulation(): Promise<void> {
    await this.connection?.invoke('SubscribeToSimulation');
  }

  async unsubscribeFromSimulation(): Promise<void> {
    await this.connection?.invoke('UnsubscribeFromSimulation');
  }

  async ping(): Promise<string> {
    return await this.connection?.invoke('Ping') || '';
  }

  // Event handlers
  onRobotPositionUpdated(callback: (event: RobotPositionEvent) => void): void {
    this.connection?.on('RobotPositionUpdated', callback);
  }

  onRobotStatusChanged(callback: (event: RobotStatusEvent) => void): void {
    this.connection?.on('RobotStatusChanged', callback);
  }

  onTaskStatusChanged(callback: (event: TaskStatusEvent) => void): void {
    this.connection?.on('TaskStatusChanged', callback);
  }

  onAlertCreated(callback: (event: AlertEvent) => void): void {
    this.connection?.on('AlertCreated', callback);
  }

  onTableStatusChanged(callback: (event: TableStatusEvent) => void): void {
    this.connection?.on('TableStatusChanged', callback);
  }

  onGuestEvent(callback: (event: GuestEvent) => void): void {
    this.connection?.on('GuestEvent', callback);
  }

  onSimulationProgressUpdated(callback: (event: SimulationProgressEvent) => void): void {
    this.connection?.on('SimulationProgressUpdated', callback);
  }

  off(eventName: string): void {
    this.connection?.off(eventName);
  }

  private async resubscribeAll(): Promise<void> {
    // Re-subscribe to previously active subscriptions after reconnect
  }
}

export const signalRService = new SignalRService();
```

### SignalR Event Types
```typescript
// src/types/signalREvents.ts

interface RobotPositionEvent {
  robotId: number;
  robotName: string;
  position: PositionDto;
  heading: number;
  velocity: number;
  timestamp: string;
}

interface RobotStatusEvent {
  robotId: number;
  robotName: string;
  oldStatus: RobotStatus;
  newStatus: RobotStatus;
  reason: string | null;
  timestamp: string;
}

interface TaskStatusEvent {
  taskId: number;
  taskType: TaskType;
  oldStatus: TaskStatus;
  newStatus: TaskStatus;
  robotId: number | null;
  robotName: string | null;
  timestamp: string;
}

interface AlertEvent {
  alertId: number;
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  message: string | null;
  robotId: number | null;
  timestamp: string;
}

interface TableStatusEvent {
  tableId: number;
  label: string | null;
  oldStatus: TableStatus;
  newStatus: TableStatus;
  guestId: number | null;
  timestamp: string;
}

interface GuestEvent {
  guestId: number;
  guestName: string | null;
  eventType: "Arrived" | "Seated" | "Left";
  tableId: number | null;
  tableLabel: string | null;
  timestamp: string;
}

interface SimulationProgressEvent {
  simulationId: string;
  state: "Running" | "Paused" | "Completed" | "Cancelled";
  currentSimulatedTime: string;
  progressPercent: number;
  realElapsedTime: string;
  estimatedTimeRemaining: string;
  eventsProcessed: number;
  totalEventsScheduled: number;
  guestsProcessed: number;
  tasksCreated: number;
  tasksCompleted: number;
  tasksFailed: number;
  currentSuccessRate: number;
  timestamp: string;
}
```

### React Hook for SignalR
```typescript
// src/hooks/useSignalR.ts
import { useEffect, useRef, useCallback } from 'react';
import { signalRService } from '../services/signalRService';

export function useSignalR() {
  const isConnected = useRef(false);

  useEffect(() => {
    const connect = async () => {
      if (!isConnected.current) {
        await signalRService.connect();
        isConnected.current = true;
      }
    };

    connect();

    return () => {
      // Don't disconnect on unmount - connection is shared
    };
  }, []);

  return {
    subscribeToRobot: (robotId: number) => signalRService.subscribeToRobot(robotId),
    unsubscribeFromRobot: (robotId: number) => signalRService.unsubscribeFromRobot(robotId),
    subscribeToAlerts: () => signalRService.subscribeToAlerts(),
    subscribeToTasks: () => signalRService.subscribeToTasks(),
    subscribeToSimulation: () => signalRService.subscribeToSimulation(),
    unsubscribeFromSimulation: () => signalRService.unsubscribeFromSimulation(),
    onRobotPositionUpdated: signalRService.onRobotPositionUpdated.bind(signalRService),
    onRobotStatusChanged: signalRService.onRobotStatusChanged.bind(signalRService),
    onTaskStatusChanged: signalRService.onTaskStatusChanged.bind(signalRService),
    onAlertCreated: signalRService.onAlertCreated.bind(signalRService),
    onTableStatusChanged: signalRService.onTableStatusChanged.bind(signalRService),
    onGuestEvent: signalRService.onGuestEvent.bind(signalRService),
    onSimulationProgressUpdated: signalRService.onSimulationProgressUpdated.bind(signalRService),
    off: signalRService.off.bind(signalRService),
  };
}
```

---

## 6. kitchenQueue implementation

```typescript

export type TaskStatus = 'pending' | 'complete';
export type TableId = number;

export interface BaseTask<TItem extends string = string> {
  id: string;
  orderId: string;     // ORD-YYYY-MMDD-####
  tableId: TableId;
  items: TItem[];      //food or drink items
  status: TaskStatus;
  createdAt: number;
}


export interface QueueOptions<TTask extends BaseTask = BaseTask> {
  // 1min default
  intervalMs?: number;
}

export type TableStatus = 'available';

const genId = () =>
  (crypto as any)?.randomUUID?.() ?? `t_${Date.now()}_${Math.random().toString(16).slice(2)}`;

const seqByDay = new Map<string, number>();

function nextDailyIndex(date = new Date()): string {
  const y = date.getFullYear();
  const mm = `${date.getMonth() + 1}`.padStart(2, '0');
  const dd = `${date.getDate()}`.padStart(2, '0');
  const key = `${y}${mm}${dd}`;
  const next = (seqByDay.get(key) ?? 0) + 1;
  seqByDay.set(key, next);
  return `${next}`.padStart(4, '0');
}

/**  OrderId：ORD-YYYY-MMDD-####（{md} = MMDD） */
export function generateOrderId(date = new Date()): string {
  const year = date.getFullYear();
  const mm = `${date.getMonth() + 1}`.padStart(2, '0');
  const dd = `${date.getDate()}`.padStart(2, '0');
  const md = `${mm}${dd}`;
  const index = nextDailyIndex(date);
  return `ORD-${year}-${md}-${index}`;
}

export enum FoodItem {
  SteakMediumRare = 'Steak medium-rare',
  CaesarSalad = 'Caesar salad',
  MargheritaPizza = 'Margherita pizza',
  GrilledSalmon = 'Grilled salmon',
  MushroomRisotto = 'Mushroom risotto',
  SpaghettiBolognese = 'Spaghetti bolognese',
}

export enum DrinkItem {
  Latte = 'Latte',
  Cappuccino = 'Cappuccino',
  IcedTea = 'Iced tea',
  OrangeJuice = 'Orange juice',
  SparklingWater = 'Sparkling water',
  Cola = 'Cola',
}


export interface FoodTask extends BaseTask<FoodItem | string> {}
export interface DrinkTask extends BaseTask<DrinkItem | string> {}
export interface CleanTask extends BaseTask<string> {} 


class BaseQueue<TTask extends BaseTask> {
  protected q: TTask[] = [];
  private timer: number | null = null;
  private running = false;

  constructor(
    private readonly opts: QueueOptions<TTask>,
    private readonly onReadyApi: (task: TTask) => Promise<any>
  ) {}

  enqueue(input: Omit<TTask, 'id' | 'status' | 'createdAt' | 'orderId'> & { orderId?: string }): TTask {
    const task = {
      id: genId(),
      orderId: input.orderId ?? generateOrderId(),
      status: 'pending' as const,
      createdAt: Date.now(),
      ...input,
      items: Array.isArray((input as any).items) ? (input as any).items : [],
    } as TTask;

    this.q.push(task);
    return task;
  }

  protected async processOne() {
    if (!this.q.length) return;
    const idx = this.q.findIndex(t => t.status === 'pending');
    if (idx === -1) return;

    const task = this.q[idx];

    await this.onReadyApi(task);
    task.status = 'complete';
    this.q.splice(idx, 1);
  }

  start() {
    if (this.running) return;
    this.running = true;

    const interval = this.opts.intervalMs ?? 60_000;
    this.timer = window.setInterval(() => {
      void this.processOne();
    }, interval);
  }

  stop() {
    this.running = false;
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  clear() {
    this.q = [];
  }

  size() {
    return this.q.length;
  }

  snapshot(): ReadonlyArray<TTask> {
    return this.q.slice();
  }
}

export class FoodQueue extends BaseQueue<FoodTask> {
  constructor(api: apiClient, opts: QueueOptions<FoodTask> = {}) {
    super(
      { intervalMs: opts.intervalMs ?? 60_000 },
      (task) => api.foodReady(task.tableId, task.orderId, task.items)
    );
  }
}

export class DrinkQueue extends BaseQueue<DrinkTask> {
  constructor(api: apiClient, opts: QueueOptions<DrinkTask> = {}) {
    super(
      { intervalMs: opts.intervalMs ?? 60_000 },
      (task) => api.drinksReady(task.tableId, task.orderId, task.items)
    );
  }
}

export class CleanQueue extends BaseQueue<CleanTask> {
  constructor(api: apiClient, opts: QueueOptions<CleanTask> = {}) {
    super(
      { intervalMs: opts.intervalMs ?? 30_000 }, // 30s
      (task) => api.tableStatusChange(task.tableId, 'available')
    );
  }
}

export class TableOrderRegistry {
  private map = new Map<TableId, string>();

  getOrCreate(tableId: TableId): string {
    const existing = this.map.get(tableId);
    if (existing) return existing;
    const fresh = generateOrderId();
    this.map.set(tableId, fresh);
    return fresh;
  }

  startNew(tableId: TableId): string {
    const fresh = generateOrderId();
    this.map.set(tableId, fresh);
    return fresh;
  }

  end(tableId: TableId) {
    this.map.delete(tableId);
  }
}

export class KitchenManager {
  constructor(
    private readonly api: KitchenApi,
    public readonly foodQ  = new FoodQueue(api),
    public readonly drinkQ = new DrinkQueue(api),
    public readonly cleanQ = new CleanQueue(api),
    private readonly orders = new TableOrderRegistry()
  ) {}

  startAll() { this.foodQ.start(); this.drinkQ.start(); this.cleanQ.start(); }
  stopAll()  { this.foodQ.stop();  this.drinkQ.stop();  this.cleanQ.stop();  }

  placeOrder(tableId: TableId, foodItems: (FoodItem | string)[], drinkItems: (DrinkItem | string)[]) {
    const orderId = this.orders.getOrCreate(tableId); 
    const foodTask  = this.foodQ.enqueue({ orderId, tableId, items: foodItems });
    const drinkTask = this.drinkQ.enqueue({ orderId, tableId, items: drinkItems });
    return { orderId, foodTask, drinkTask };
  }

  enqueueFood(tableId: TableId, items: (FoodItem | string)[], orderId?: string) {
    const oid = orderId ?? this.orders.getOrCreate(tableId);
    return this.foodQ.enqueue({ orderId: oid, tableId, items });
  }

  enqueueDrink(tableId: TableId, items: (DrinkItem | string)[], orderId?: string) {
    const oid = orderId ?? this.orders.getOrCreate(tableId);
    return this.drinkQ.enqueue({ orderId: oid, tableId, items });
  }
  startNewOrder(tableId: TableId): string {
    return this.orders.startNew(tableId);
  }

  scheduleClean(tableId: TableId) {
    return this.cleanQ.enqueue({ tableId, items: [] });
  }
}

/* ===================== use case =====================

const km = new KitchenManager(api);
// A：Food and Drink share orderId
km.placeOrder(1, [FoodItem.SteakMediumRare, FoodItem.CaesarSalad], [DrinkItem.Latte, DrinkItem.Cola]);

// B： Separate Food and Drink orders
km.enqueueFood(2, [FoodItem.MargheritaPizza]);
km.enqueueDrink(2, [DrinkItem.SparklingWater]);

km.startNewOrder(2);

km.scheduleClean(3);

*/


```


## 7. Business Scenarios

### Scenario 1: Guest Arrival → Food Delivery Flow
```typescript
// Complete flow from guest arrival to food delivery

async function handleGuestArrival(partySize: number, guestName?: string) {
  // 1. Register guest arrival (creates Escort task automatically)
  await eventsApi.guestArrived(partySize, guestName);
  
  // 2. Auto-dispatch assigns robot (handled by backend)
  // SignalR will notify: TaskStatusChanged (Pending → Assigned)
  // SignalR will notify: RobotStatusChanged (Idle → Navigating)
}
  // 3. stay a few seconds to simulate order preparation time (about 5s), also can be speed by acceleration setting
  // 4. add the order to a global queue and call kitchenApi.foodReady when done

async function handleFoodReady(tableId: number) {
  // 5. Kitchen signals food is ready (creates Deliver task)
  await kitchenApi.foodReady(tableId);
  
  // 6. Auto-dispatch assigns available robot
  // Robot delivers food
  // SignalR notifies completion
}
```

### Scenario 2: Manual Task Assignment
```typescript
async function manuallyAssignTask(taskId: number, robotId: number) {
  try {
    // Assign task to specific robot
    const task = await tasksApi.assign(taskId, robotId);
    console.log(`Task ${task.id} assigned to robot ${task.robotName}`);
    
    // Optionally start immediately
    await tasksApi.start(taskId);
  } catch (error) {
    console.error('Assignment failed:', error);
  }
}
```

### Scenario 3: Emergency Stop
```tsx

function EmergencyButton() {
  const [isStopped, setIsStopped] = useState(false);

  const handleEmergencyStop = async () => {
    await emergencyApi.stopAll();
    setIsStopped(true);
  };

  const handleResume = async () => {
    await emergencyApi.resume();
    setIsStopped(false);
  };

  return (
    <VegaButton 
      icon={isPaused ? "fa-solid fa-play" : "fa-solid fa-pause"} 
      label={isPaused ? "Resume Operations" : "EMERGENCY STOP"}
      variant="secondary"
      onVegaClick={onPause}
    />   
  );
}
```

### Scenario 4: Robot Commands
```typescript
async function sendRobotToTable(robotId: number, tableId: number) {
  const result = await robotsApi.sendCommand(robotId, {
    command: 'go_to',
    targetTableId: tableId,
  });

  if (result.accepted) {
    console.log(`Robot heading to table, ETA: ${result.estimatedCompletionSeconds}s`);
  } else {
    console.error(`Command rejected: ${result.message}`);
  }
}

async function sendRobotHome(robotId: number) {
  await robotsApi.sendCommand(robotId, { command: 'return_home' });
}

async function pauseRobot(robotId: number, reason: string) {
  await robotsApi.sendCommand(robotId, { 
    command: 'pause',
    reason 
  });
}
```

### Scenario 5: Waitlist Management
```typescript
async function addToWaitlist(partySize: number, name?: string, phone?: string) {
  const guest = await guestsApi.create({
    partySize,
    name,
    phoneNumber: phone,
  });
  return guest;
}

async function seatNextGuest(tableId: number) {
  const waitlist = await guestsApi.getWaitlist();
  if (waitlist.guests.length > 0) {
    const nextGuest = waitlist.guests[0];
    await guestsApi.seat(nextGuest.id, tableId);
  }
}

async function checkoutGuest(guestId: number) {
  await guestsApi.checkout(guestId);
  // Table status automatically changes to "Cleaning"
  // add the cleaning task to a cleaning queue, every cleaning task takes about 2 minutes by default, and can be seeped up by acceleration setting
}
```

---

## 7. UI Component Patterns

### Dashboard Component
```tsx
// src/components/Dashboard.tsx
import { useState, useEffect } from 'react';
import { dashboardApi } from '../services/dashboardApi';

function Dashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        const loader = VegaLoader.load({
          containerSelector: '#dashboard-container' // dashboard-container is the id of the div where the loader will be shown
        })
        const data = await dashboardApi.getSummary();
        setSummary(data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load dashboard');
      } finally {
        VegaLoader.close(loader);
      }
    };

    fetchDashboard();
    const interval = setInterval(fetchDashboard, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, []);

  if (error) return <div className="error">{error}</div>;
  if (!summary) return null;

  return (
    <div className="dashboard-grid" id="dashboard-container">
      {/* Robots Card */}
      <div className="card">
        <h3>Robots</h3>
        <div className="stat-grid">
          <div className="stat green">Idle: {summary.robots.idle}</div>
          <div className="stat blue">Active: {summary.robots.navigating + summary.robots.delivering}</div>
          <div className="stat yellow">Charging: {summary.robots.charging}</div>
          <div className="stat red">Error: {summary.robots.error}</div>
        </div>
        <p>Avg Battery: {summary.robots.averageBattery}%</p>
      </div>

      {/* Tables Card */}
      <div className="card">
        <h3>Tables</h3>
        <div className="stat-grid">
          <div className="stat green">Available: {summary.tables.available}</div>
          <div className="stat orange">Occupied: {summary.tables.occupied}</div>
          <div className="stat red">Needs Service: {summary.tables.needsService}</div>
        </div>
        <p>Occupancy: {summary.tables.occupancyPercent}%</p>
      </div>

      {/* Tasks Card */}
      <div className="card">
        <h3>Tasks</h3>
        <div className="stat-grid">
          <div className="stat">Pending: {summary.tasks.pendingCount}</div>
          <div className="stat">In Progress: {summary.tasks.inProgressCount}</div>
          <div className="stat green">Completed: {summary.tasks.completedToday}</div>
          <div className="stat red">Failed: {summary.tasks.failedToday}</div>
        </div>
      </div>

      {/* Alerts Card */}
      <div className="card">
        <h3>Alerts</h3>
        <div className="stat-grid">
          <div className="stat red">Critical: {summary.alerts.critical}</div>
          <div className="stat yellow">Warnings: {summary.alerts.warnings}</div>
          <div className="stat">Unacknowledged: {summary.alerts.unacknowledged}</div>
        </div>
      </div>
    </div>
  );
}
```

### Robot Map with Live Updates
```tsx
// src/components/RobotMapLive.tsx
import React, { useEffect, useState } from 'react';
import { Stage, Layer, Circle, Arrow, Text } from 'react-konva';
import { useSignalR } from '../hooks/useSignalR';
import { robotsApi } from '../services/robotsApi';

function RobotMapLive() {
  const [robots, setRobots] = useState<RobotDto[]>([]);
  const { onRobotPositionUpdated, onRobotStatusChanged } = useSignalR();

  // Initial load
  useEffect(() => {
    robotsApi.list().then(setRobots);
  }, []);

  // Real-time position updates
  useEffect(() => {
    onRobotPositionUpdated((event) => {
      setRobots(prev => prev.map(robot => 
        robot.id === event.robotId
          ? { 
              ...robot, 
              position: event.position,
              heading: event.heading,
              velocity: event.velocity 
            }
          : robot
      ));
    });

    onRobotStatusChanged((event) => {
      setRobots(prev => prev.map(robot =>
        robot.id === event.robotId
          ? { ...robot, status: event.newStatus as RobotStatus }
          : robot
      ));
    });
  }, [onRobotPositionUpdated, onRobotStatusChanged]);

  const getStatusColor = (status: RobotStatus): string => {
    const colors: Record<RobotStatus, string> = {
      Idle: '#4CAF50',
      Navigating: '#2196F3',
      Delivering: '#FF9800',
      Returning: '#9C27B0',
      Charging: '#FFC107',
      Error: '#F44336',
      Offline: '#9E9E9E',
    };
    return colors[status] || '#9E9E9E';
  };

  return (
    <Stage width={800} height={600}>
      <Layer>
        {robots.map(robot => (
          <React.Fragment key={robot.id}>
            {/* Robot body */}
            <Circle
              x={robot.position.screenX}
              y={robot.position.screenY}
              radius={15}
              fill={getStatusColor(robot.status as RobotStatus)}
              stroke="#333"
              strokeWidth={2}
            />
            {/* Heading arrow */}
            <Arrow
              x={robot.position.screenX}
              y={robot.position.screenY}
              points={[0, 0, 20, 0]}
              rotation={robot.heading}
              fill="#333"
              stroke="#333"
            />
            {/* Robot name */}
            <Text
              x={robot.position.screenX - 20}
              y={robot.position.screenY + 20}
              text={robot.name}
              fontSize={12}
              fill="#333"
            />
          </React.Fragment>
        ))}
      </Layer>
    </Stage>
  );
}
```

### Alert Panel with Real-Time Updates
```tsx
// src/components/AlertPanel.tsx
import { useState, useEffect } from 'react';
import { useSignalR } from '../hooks/useSignalR';
import { alertsApi } from '../services/alertsApi';

function AlertPanel() {
  const [alerts, setAlerts] = useState<AlertDto[]>([]);
  const { onAlertCreated, subscribeToAlerts } = useSignalR();

  useEffect(() => {
    // Initial load
    alertsApi.getUnacknowledged().then(setAlerts);
    
    // Subscribe to new alerts
    subscribeToAlerts();
    onAlertCreated((event) => {
      const newAlert: AlertDto = {
        id: event.alertId,
        type: event.type as AlertType,
        severity: event.severity as AlertSeverity,
        title: event.title,
        message: event.message,
        robotId: event.robotId,
        robotName: null,
        tableId: null,
        taskId: null,
        isAcknowledged: false,
        acknowledgedBy: null,
        acknowledgedAt: null,
        isResolved: false,
        resolvedAt: null,
        createdAt: event.timestamp,
      };
      setAlerts(prev => [newAlert, ...prev]);
    });
  }, []);

  const handleAcknowledge = async (alertId: number) => {
    await alertsApi.acknowledge(alertId, 'CurrentUser');
    setAlerts(prev => prev.filter(a => a.id !== alertId));
  };

  const getSeverityClass = (severity: AlertSeverity) => {
    return `alert-${severity.toLowerCase()}`;
  };

  return (
    <div className="alert-panel">
      <h3>Active Alerts ({alerts.length})</h3>
      {alerts.length === 0 ? (
        <p className="no-alerts">No active alerts</p>
      ) : (
        alerts.map(alert => (
          <div key={alert.id} className={`alert ${getSeverityClass(alert.severity as AlertSeverity)}`}>
            <div className="alert-header">
              <span className="severity-badge">{alert.severity}</span>
              <span className="alert-type">{alert.type}</span>
            </div>
            <strong>{alert.title}</strong>
            {alert.message && <p>{alert.message}</p>}
            <div className="alert-actions">
              <button onClick={() => handleAcknowledge(alert.id)}>
                Acknowledge
              </button>
            </div>
          </div>
        ))
      )}
    </div>
  );
}
```

### Task Queue Component
```tsx
// src/components/TaskQueue.tsx
import { useState, useEffect } from 'react';
import { useSignalR } from '../hooks/useSignalR';
import { tasksApi } from '../services/tasksApi';
import { robotsApi } from '../services/robotsApi';
import { dispatchApi } from '../services/dispatchApi';

function TaskQueue() {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [idleRobots, setIdleRobots] = useState<RobotDto[]>([]);
  const { onTaskStatusChanged } = useSignalR();

  useEffect(() => {
    // Load pending tasks
    tasksApi.getPending().then(setTasks);
    robotsApi.getIdle().then(setIdleRobots);
    
    // Real-time updates
    onTaskStatusChanged((event) => {
      if (event.newStatus === 'Pending') {
        tasksApi.get(event.taskId).then(task => {
          setTasks(prev => [...prev, task]);
        });
      } else {
        setTasks(prev => prev.filter(t => t.id !== event.taskId));
      }
    });
  }, []);

  const handleAutoAssign = async () => {
    const result = await dispatchApi.triggerAutoAssign();
    alert(`Assigned ${result.tasksAssigned} tasks`);
    // Refresh lists
    tasksApi.getPending().then(setTasks);
    robotsApi.getIdle().then(setIdleRobots);
  };

  const handleManualAssign = async (taskId: number, robotId: number) => {
    await tasksApi.assign(taskId, robotId);
    // Lists will update via SignalR
  };

  const getPriorityClass = (priority: TaskPriority) => {
    return `priority-${priority.toLowerCase()}`;
  };

  return (
    <div className="task-queue">
      <div className="header">
        <h2>Pending Tasks ({tasks.length})</h2>
        <button onClick={handleAutoAssign} className="btn-primary">
          Auto-Assign All
        </button>
      </div>
      
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Type</th>
            <th>Priority</th>
            <th>Target</th>
            <th>Created</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          {tasks.map(task => (
            <tr key={task.id}>
              <td>{task.id}</td>
              <td>{task.type}</td>
              <td className={getPriorityClass(task.priority as TaskPriority)}>
                {task.priority}
              </td>
              <td>{task.targetTableLabel || 'N/A'}</td>
              <td>{new Date(task.createdAt).toLocaleTimeString()}</td>
              <td>
                <select 
                  onChange={(e) => handleManualAssign(task.id, +e.target.value)}
                  defaultValue=""
                >
                  <option value="" disabled>Assign to...</option>
                  {idleRobots.map(robot => (
                    <option key={robot.id} value={robot.id}>
                      {robot.name} ({robot.batteryLevel}%)
                    </option>
                  ))}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

---

## 8. Error Handling

### API Error Handler
```typescript
// src/utils/errorHandler.ts

export class ApiError extends Error {
  constructor(
    message: string,
    public statusCode: number,
    public originalError?: unknown
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function handleApiError(error: unknown): never {
  if (error instanceof ApiError) {
    throw error;
  }
  
  if (error instanceof Error) {
    throw new ApiError(error.message, 500, error);
  }
  
  throw new ApiError('Unknown error occurred', 500, error);
}
```

### Custom Hook for API Calls
```typescript
// src/hooks/useApiCall.ts
import { useState, useEffect, useCallback } from 'react';

interface UseApiCallResult<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useApiCall<T>(
  apiCall: () => Promise<T>,
  deps: unknown[] = []
): UseApiCallResult<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await apiCall();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, deps);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
}

// Usage example:
// const { data: robots, loading, error, refetch } = useApiCall(
//   () => robotsApi.list(),
//   []
// );
```

### Error Boundary Component
```tsx
// src/components/ErrorBoundary.tsx
import React from 'react';

interface Props {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback || (
        <div className="error-fallback">
          <h2>Something went wrong</h2>
          <p>{this.state.error?.message}</p>
          <button onClick={() => this.setState({ hasError: false, error: null })}>
            Try Again
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
```

### HTTP Status Codes Reference
| Code | Meaning | Frontend Action |
|------|---------|-----------------|
| 200 | Success | Display data |
| 201 | Created | Refresh list, show success |
| 204 | No Content | Refresh list (delete success) |
| 400 | Bad Request | Show validation error |
| 404 | Not Found | Show "not found" message |
| 409 | Conflict | Show conflict message |
| 500 | Server Error | Show generic error, log details |

---

## 9. Long-Run Simulation

### Simulation Dashboard Component
```tsx
// src/components/SimulationDashboard.tsx
import { useEffect, useState } from 'react';
import { useSignalR } from '../hooks/useSignalR';
import { simulationApi } from '../services/simulationApi';

function SimulationDashboard() {
  const [progress, setProgress] = useState<SimulationProgressDto | null>(null);
  const [report, setReport] = useState<SimulationReportDto | null>(null);
  const [isStarting, setIsStarting] = useState(false);
  const { subscribeToSimulation, unsubscribeFromSimulation, onSimulationProgressUpdated } = useSignalR();

  useEffect(() => {
    // Subscribe to real-time updates
    subscribeToSimulation();
    
    onSimulationProgressUpdated((event) => {
      setProgress({
        simulationId: event.simulationId,
        state: event.state,
        currentSimulatedTime: event.currentSimulatedTime,
        progressPercent: event.progressPercent,
        realElapsedTime: event.realElapsedTime,
        estimatedTimeRemaining: event.estimatedTimeRemaining,
        eventsProcessed: event.eventsProcessed,
        totalEventsScheduled: event.totalEventsScheduled,
        guestsProcessed: event.guestsProcessed,
        tasksCreated: event.tasksCreated,
        tasksCompleted: event.tasksCompleted,
      } as SimulationProgressDto);

      // Load report when completed
      if (event.state === 'Completed') {
        simulationApi.getReport().then(setReport);
      }
    });

    // Check for existing simulation
    simulationApi.getProgress().then(setProgress).catch(() => {});

    return () => {
      unsubscribeFromSimulation();
    };
  }, []);

  const handleStart = async () => {
    setIsStarting(true);
    setReport(null);
    try {
      await simulationApi.start({
        simulatedStartTime: '2026-02-01T07:00:00',
        simulatedEndTime: '2026-03-01T23:00:00',
        accelerationFactor: 720,  // 1 month in ~1 hour
        robotCount: 5,
        tableCount: 20,
      });
    } finally {
      setIsStarting(false);
    }
  };

  const handlePause = () => simulationApi.pause();
  const handleResume = () => simulationApi.resume();
  const handleStop = () => simulationApi.stop();

  if (!progress) {
    return (
      <div className="simulation-setup">
        <h2>Long-Run Simulation</h2>
        <p>Simulate 1 month of restaurant operations in ~1 hour</p>
        <button onClick={handleStart} disabled={isStarting} className="btn-primary">
          {isStarting ? 'Starting...' : 'Start Monthly Simulation (720x)'}
        </button>
      </div>
    );
  }

  return (
    <div className="simulation-dashboard">
      <h2>Simulation: {progress.simulationId}</h2>
      <div className={`status status-${progress.state.toLowerCase()}`}>
        {progress.state}
      </div>

      <div className="progress-section">
        <div className="progress-bar">
          <div 
            className="progress-fill" 
            style={{ width: `${progress.progressPercent}%` }}
          />
        </div>
        <span>{progress.progressPercent.toFixed(1)}%</span>
      </div>

      <div className="metrics-grid">
        <div className="metric">
          <label>Simulated Time</label>
          <span>{new Date(progress.currentSimulatedTime).toLocaleString()}</span>
        </div>
        <div className="metric">
          <label>Real Elapsed</label>
          <span>{progress.realElapsedTime}</span>
        </div>
        <div className="metric">
          <label>ETA</label>
          <span>{progress.estimatedTimeRemaining}</span>
        </div>
        <div className="metric">
          <label>Events</label>
          <span>{progress.eventsProcessed.toLocaleString()} / {progress.totalEventsScheduled.toLocaleString()}</span>
        </div>
        <div className="metric">
          <label>Guests Processed</label>
          <span>{progress.guestsProcessed.toLocaleString()}</span>
        </div>
        <div className="metric">
          <label>Tasks</label>
          <span>{progress.tasksCompleted.toLocaleString()} completed</span>
        </div>
        <div className="metric">
          <label>Success Rate</label>
          <span>{progress.currentSuccessRate?.toFixed(1)}%</span>
        </div>
      </div>

      <div className="controls">
        {progress.state === 'Running' && (
          <button onClick={handlePause} className="btn-secondary">Pause</button>
        )}
        {progress.state === 'Paused' && (
          <button onClick={handleResume} className="btn-primary">Resume</button>
        )}
        <button onClick={handleStop} className="btn-danger">Stop</button>
      </div>

      {report && (
        <div className="report-section">
          <h3>Final Report</h3>
          <div className="report-summary">
            <div className="report-stat">
              <label>Total Guests</label>
              <span>{report.summary.totalGuests.toLocaleString()}</span>
            </div>
            <div className="report-stat">
              <label>Total Tasks</label>
              <span>{report.summary.totalTasks.toLocaleString()}</span>
            </div>
            <div className="report-stat">
              <label>Success Rate</label>
              <span>{report.summary.overallSuccessRate.toFixed(1)}%</span>
            </div>
            <div className="report-stat">
              <label>Avg Task Duration</label>
              <span>{report.summary.averageTaskDurationSeconds.toFixed(0)}s</span>
            </div>
            <div className="report-stat">
              <label>Peak Guests</label>
              <span>{report.summary.peakConcurrentGuests}</span>
            </div>
            <div className="report-stat">
              <label>Robot Utilization</label>
              <span>{(report.summary.averageRobotUtilization * 100).toFixed(1)}%</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default SimulationDashboard;
```

### Acceleration Presets
| Preset | Factor | Real Time for 1 Month | Use Case |
|--------|--------|----------------------|----------|
| Real-time | 1x | 30 days | Live demo |
| Fast | 10x | 3 days | Quick test |
| Turbo | 60x | 12 hours | Day simulation |
| Monthly | 720x | ~1 hour | Capacity planning |
| Yearly | 8640x | ~1 hour (1 year) | Long-term analysis |

---

## Quick Reference

### Common API Patterns
```typescript
// List with filters
GET /api/robots?status=Idle&isEnabled=true

// Get by ID
GET /api/robots/5

// Create
POST /api/robots
Body: { "name": "Bot-1", "model": "R2" }

// Update
PUT /api/robots/5
Body: { "name": "Bot-1-Updated" }

// Delete
DELETE /api/robots/5

// Action
POST /api/tasks/10/complete
```

### SignalR Quick Reference
```typescript
// Connect
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/restaurant')
  .withAutomaticReconnect()
  .build();
await connection.start();

// Subscribe
await connection.invoke('SubscribeToRobot', robotId);
await connection.invoke('SubscribeToAlerts');
await connection.invoke('SubscribeToSimulation');

// Listen
connection.on('RobotPositionUpdated', callback);
connection.on('TaskStatusChanged', callback);
connection.on('AlertCreated', callback);
connection.on('SimulationProgressUpdated', callback);
```

### Environment Variables
```env
# .env.local
REACT_APP_API_BASE=http://localhost:5199
REACT_APP_SIGNALR_HUB=http://localhost:5199/hubs/restaurant
```

---

## API Endpoints Summary

### Core Entities
| Entity | List | Get | Create | Update | Delete |
|--------|------|-----|--------|--------|--------|
| Robots | `GET /api/robots` | `GET /api/robots/{id}` | `POST /api/robots` | `PUT /api/robots/{id}` | `DELETE /api/robots/{id}` |
| Tasks | `GET /api/tasks` | `GET /api/tasks/{id}` | `POST /api/tasks` | - | `DELETE /api/tasks/{id}` |
| Tables | `GET /api/tables` | `GET /api/tables/{id}` | `POST /api/tables` | `PUT /api/tables/{id}` | `DELETE /api/tables/{id}` |
| Guests | `GET /api/guests` | `GET /api/guests/{id}` | `POST /api/guests` | - | `DELETE /api/guests/{id}` |
| Alerts | `GET /api/alerts` | `GET /api/alerts/{id}` | `POST /api/alerts` | - | `DELETE /api/alerts/{id}` |
| Zones | `GET /api/zones` | `GET /api/zones/{id}` | `POST /api/zones` | - | `DELETE /api/zones/{id}` |

### Special Operations
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/robots/{id}/command` | PATCH | Send command to robot |
| `/api/robots/{id}/position` | PUT | Update robot position |
| `/api/tasks/{id}/assign` | POST | Assign task to robot |
| `/api/tasks/{id}/start` | POST | Start task execution |
| `/api/tasks/{id}/complete` | POST | Mark task complete |
| `/api/tasks/{id}/fail` | POST | Mark task failed |
| `/api/guests/{id}/seat` | POST | Seat guest at table |
| `/api/guests/{id}/checkout` | POST | Check out guest |
| `/api/alerts/{id}/acknowledge` | POST | Acknowledge alert |
| `/api/dispatch/auto-assign` | POST | Trigger auto-dispatch |
| `/api/emergency/stop-all` | POST | Emergency stop |
| `/api/simulation/long-run` | POST | Start simulation |

### Dashboard & Summary
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/dashboard` | GET | Full dashboard summary |
| `/api/dashboard/robots` | GET | Robot summary only |
| `/api/dashboard/tables` | GET | Table summary only |
| `/api/tasks/summary` | GET | Task queue summary |
| `/api/alerts/summary` | GET | Alert summary |
| `/api/guests/waitlist` | GET | Waitlist summary |

---

*Generated from Don-Quixote Backend API - January 2026*
