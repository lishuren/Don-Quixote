/**
 * @file dtos.ts
 * @description All Data Transfer Objects (DTOs) matching backend API responses
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md
 */

import {
  RobotStatus,
  TaskType,
  TaskStatus,
  TaskPriority,
  TableStatus,
  TableShape,
  GuestStatus,
  AlertType,
  AlertSeverity,
  DispatchAlgorithm,
  RobotCommand,
  SimulationState,
  KitchenTaskStatus,
} from './enums';

// ============================================
// Position DTOs
// ============================================

/**
 * Position DTO - Used everywhere for dual coordinate system
 * Screen coordinates (pixels) for UI, Physical coordinates (meters) for navigation
 */
export interface PositionDto {
  screenX: number;
  screenY: number;
  physicalX: number;
  physicalY: number;
}

// ============================================
// Robot DTOs
// ============================================

export interface RobotDto {
  id: number;
  name: string;
  model: string | null;
  status: RobotStatus;
  position: PositionDto;
  heading: number; // 0-360 degrees
  batteryLevel: number; // 0-100
  velocity: number; // m/s
  currentTaskId: number | null;
  isEnabled: boolean;
  lastUpdated: string; // ISO 8601
}

export interface CreateRobotRequest {
  name: string;
  model?: string;
  position?: PositionDto;
  isEnabled?: boolean;
}

export interface UpdateRobotRequest {
  name?: string;
  model?: string;
  status?: RobotStatus;
  isEnabled?: boolean;
}

export interface RobotPositionUpdate {
  screenX: number;
  screenY: number;
  physicalX?: number;
  physicalY?: number;
  heading: number;
  velocity: number;
}

export interface RobotCommandRequest {
  command: RobotCommand;
  targetPosition?: PositionDto;
  targetTableId?: number;
  speed?: number;
  reason?: string;
}

export interface CommandAcknowledgment {
  robotId: number;
  command: string;
  accepted: boolean;
  message: string | null;
  estimatedCompletionSeconds: number | null;
  timestamp: string;
}

export interface RobotHistoryEntry {
  taskId: number;
  taskType: string;
  status: string;
  startedAt: string | null;
  completedAt: string | null;
  durationSeconds: number | null;
  targetTableLabel: string | null;
}

export interface RobotHistoryResponse {
  robotId: number;
  robotName: string;
  history: RobotHistoryEntry[];
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  averageTaskDurationSeconds: number;
}

// ============================================
// Task DTOs
// ============================================

export interface TaskDto {
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

export interface CreateTaskRequest {
  type: TaskType;
  priority?: TaskPriority;
  robotId?: number;
  targetTableId?: number;
  startPosition?: PositionDto;
  targetPosition?: PositionDto;
}

export interface TaskQueueSummary {
  pendingCount: number;
  assignedCount: number;
  inProgressCount: number;
  completedToday: number;
  failedToday: number;
}

// ============================================
// Table DTOs
// ============================================

export interface TableDto {
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

export interface CreateTableRequest {
  label?: string;
  shape?: TableShape;
  center?: PositionDto;
  width?: number;
  height?: number;
  rotation?: number;
  capacity?: number;
  zoneId?: number;
}

export interface UpdateTableRequest {
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

export interface TableStatusSummary {
  total: number;
  available: number;
  occupied: number;
  reserved: number;
  needsService: number;
  cleaning: number;
  occupancyPercent: number;
}

// ============================================
// Guest DTOs
// ============================================

export interface GuestDto {
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

export interface CreateGuestRequest {
  name?: string;
  partySize?: number;
  notes?: string;
  phoneNumber?: string;
}

export interface WaitlistSummary {
  totalWaiting: number;
  averageWaitMinutes: number;
  guests: GuestDto[];
}

// ============================================
// Alert DTOs
// ============================================

export interface AlertDto {
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

export interface CreateAlertRequest {
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  message?: string;
  robotId?: number;
  tableId?: number;
  taskId?: number;
}

export interface AlertSummary {
  totalActive: number;
  critical: number;
  errors: number;
  warnings: number;
  unacknowledged: number;
}

// ============================================
// Dashboard DTOs
// ============================================

export interface RobotSummary {
  total: number;
  idle: number;
  navigating: number;
  delivering: number;
  charging: number;
  error: number;
  offline: number;
  averageBattery: number;
}

export interface DashboardSummary {
  robots: RobotSummary;
  tables: TableStatusSummary;
  tasks: TaskQueueSummary;
  alerts: AlertSummary;
  activeGuests: number;
  waitlistCount: number;
}

// ============================================
// Dispatch DTOs
// ============================================

export interface DispatchConfig {
  algorithm: DispatchAlgorithm;
  maxTasksPerRobot: number;
  minBatteryForAssignment: number;
  autoAssignEnabled: boolean;
  assignmentIntervalSeconds: number;
}

export interface DispatchQueueItem {
  taskId: number;
  taskType: string;
  priority: string;
  createdAt: string;
  suggestedRobotId: number | null;
  suggestedRobotName: string | null;
  estimatedDistance: number | null;
}

export interface AutoAssignResult {
  tasksAssigned: number;
  tasksSkipped: number;
  assignments: TaskAssignment[];
}

export interface TaskAssignment {
  taskId: number;
  robotId: number;
  robotName: string;
  reason: string;
}

// ============================================
// Simulation DTOs
// ============================================

export interface SimulationEventPatternDto {
  hourlyArrivalRates?: number[];
  dayOfWeekMultipliers?: number[];
  averageMealDurationMinutes?: number;
  guestNeedsHelpProbability?: number;
  averagePartySize?: number;
}

export interface StartSimulationRequest {
  simulatedStartTime?: string;
  simulatedEndTime?: string;
  accelerationFactor?: number; // default: 720
  robotCount?: number; // default: 5
  tableCount?: number; // default: 20
  randomSeed?: number;
  eventPatterns?: SimulationEventPatternDto;
}

export interface StartSimulationResponse {
  simulationId: string;
  status: string;
  simulatedStartTime: string;
  simulatedEndTime: string;
  accelerationFactor: number;
  estimatedEventsCount: number;
  estimatedRealDuration: string;
}

export interface SimulationProgressDto {
  simulationId: string;
  state: SimulationState;
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

export interface SimulationDailyMetricsDto {
  date: string;
  totalGuests: number;
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  peakConcurrentGuests: number;
}

export interface SimulationRobotMetricsDto {
  robotId: number;
  robotName: string;
  tasksCompleted: number;
  tasksFailed: number;
  totalDistanceMeters: number;
  utilizationPercent: number;
}

export interface SimulationReportSummaryDto {
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

export interface SimulationReportDto {
  summary: SimulationReportSummaryDto;
  dailyBreakdown: SimulationDailyMetricsDto[];
  robotMetrics: SimulationRobotMetricsDto[];
}

export interface AccelerationPreset {
  name: string;
  factor: number;
  description: string;
}

// ============================================
// Kitchen DTOs
// ============================================

export interface KitchenTaskDto {
  id: string;
  tableId: number;
  orderId: string;
  items: string[];
  status: KitchenTaskStatus;
}

// ============================================
// Zone DTOs
// ============================================

export interface ZoneDto {
  id: number;
  name: string;
  type: string;
  bounds: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
}

// ============================================
// API Response Types
// ============================================

export interface ApiError {
  error: string;
}

export interface RobotSuggestion {
  robotId: number;
  robotName: string;
  reason: string;
}
