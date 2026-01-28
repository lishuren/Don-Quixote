/**
 * @file signalREvents.ts
 * @description TypeScript definitions for SignalR WebSocket events
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md
 */

import {
  RobotStatus,
  TaskType,
  TaskStatus,
  AlertType,
  AlertSeverity,
  TableStatus,
  GuestEventType,
  SimulationState,
} from './enums';
import { PositionDto } from './dtos';

/**
 * Robot position update event
 * Fired when a robot moves
 */
export interface RobotPositionEvent {
  robotId: number;
  robotName: string;
  position: PositionDto;
  heading: number;
  velocity: number;
  timestamp: string;
}

/**
 * Robot status change event
 * Fired on status transitions (e.g., Idle → Navigating)
 */
export interface RobotStatusEvent {
  robotId: number;
  robotName: string;
  oldStatus: RobotStatus;
  newStatus: RobotStatus;
  reason: string | null;
  timestamp: string;
}

/**
 * Task status change event
 * Fired on task state transitions
 */
export interface TaskStatusEvent {
  taskId: number;
  taskType: TaskType;
  oldStatus: TaskStatus;
  newStatus: TaskStatus;
  robotId: number | null;
  robotName: string | null;
  timestamp: string;
}

/**
 * Alert created event
 * Fired when new alerts are generated
 */
export interface AlertEvent {
  alertId: number;
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  message: string | null;
  robotId: number | null;
  timestamp: string;
}

/**
 * Table status change event
 * Fired on table occupancy/status changes
 */
export interface TableStatusEvent {
  tableId: number;
  label: string | null;
  oldStatus: TableStatus;
  newStatus: TableStatus;
  guestId: number | null;
  timestamp: string;
}

/**
 * Guest event
 * Fired on guest arrival, seating, or departure
 */
export interface GuestEvent {
  guestId: number;
  guestName: string | null;
  eventType: GuestEventType;
  tableId: number | null;
  tableLabel: string | null;
  timestamp: string;
}

/**
 * Simulation progress event
 * Fired periodically during long-run simulations
 */
export interface SimulationProgressEvent {
  simulationId: string;
  state: SimulationState;
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

/**
 * SignalR event names as constants
 */
export const SignalREvents = {
  ROBOT_POSITION_UPDATED: 'RobotPositionUpdated',
  ROBOT_STATUS_CHANGED: 'RobotStatusChanged',
  TASK_STATUS_CHANGED: 'TaskStatusChanged',
  ALERT_CREATED: 'AlertCreated',
  TABLE_STATUS_CHANGED: 'TableStatusChanged',
  GUEST_EVENT: 'GuestEvent',
  SIMULATION_PROGRESS_UPDATED: 'SimulationProgressUpdated',
} as const;

/**
 * SignalR hub methods
 */
export const SignalRMethods = {
  SUBSCRIBE_TO_ROBOT: 'SubscribeToRobot',
  UNSUBSCRIBE_FROM_ROBOT: 'UnsubscribeFromRobot',
  SUBSCRIBE_TO_ZONE: 'SubscribeToZone',
  SUBSCRIBE_TO_TABLE: 'SubscribeToTable',
  SUBSCRIBE_TO_ALERTS: 'SubscribeToAlerts',
  SUBSCRIBE_TO_TASKS: 'SubscribeToTasks',
  SUBSCRIBE_TO_SIMULATION: 'SubscribeToSimulation',
  UNSUBSCRIBE_FROM_SIMULATION: 'UnsubscribeFromSimulation',
  PING: 'Ping',
} as const;

/**
 * Callback types for SignalR event handlers
 */
export type RobotPositionCallback = (event: RobotPositionEvent) => void;
export type RobotStatusCallback = (event: RobotStatusEvent) => void;
export type TaskStatusCallback = (event: TaskStatusEvent) => void;
export type AlertCallback = (event: AlertEvent) => void;
export type TableStatusCallback = (event: TableStatusEvent) => void;
export type GuestCallback = (event: GuestEvent) => void;
export type SimulationProgressCallback = (event: SimulationProgressEvent) => void;
