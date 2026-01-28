/**
 * @file enums.ts
 * @description All string enum types matching backend API responses
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md
 */

// Robot Status
export type RobotStatus =
  | 'Idle'
  | 'Navigating'
  | 'Delivering'
  | 'Returning'
  | 'Charging'
  | 'Error'
  | 'Offline';

// Task Types
export type TaskType =
  | 'Deliver'
  | 'Return'
  | 'Charge'
  | 'Patrol'
  | 'Escort'
  | 'Greeting'
  | 'Service'
  | 'Cleaning'
  | 'Custom';

// Task Status
export type TaskStatus =
  | 'Pending'
  | 'Assigned'
  | 'InProgress'
  | 'Completed'
  | 'Failed'
  | 'Cancelled';

// Task Priority
export type TaskPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

// Table Status
export type TableStatus = 'Available' | 'Occupied' | 'Reserved' | 'NeedsService' | 'Cleaning';

// Table Shape
export type TableShape = 'Rectangle' | 'Circle' | 'Square';

// Guest Status
export type GuestStatus = 'Waiting' | 'Seated' | 'Departed';

// Alert Types
export type AlertType =
  | 'LowBattery'
  | 'NavigationBlocked'
  | 'RobotError'
  | 'TableService'
  | 'GuestWaiting'
  | 'Custom';

// Alert Severity
export type AlertSeverity = 'Info' | 'Warning' | 'Error' | 'Critical';

// Zone Types
export type ZoneType = 'Dining' | 'Kitchen' | 'Charging' | 'Entrance' | 'Restroom';

// Dispatch Algorithms
export type DispatchAlgorithm = 'nearest' | 'round_robin' | 'load_balanced' | 'priority';

// Robot Commands
export type RobotCommand =
  | 'go_to'
  | 'return_home'
  | 'pause'
  | 'resume'
  | 'emergency_stop'
  | 'clear_error';

// Simulation State
export type SimulationState = 'Running' | 'Paused' | 'Completed' | 'Cancelled';

// Guest Event Types
export type GuestEventType = 'Arrived' | 'Seated' | 'Left';

// Kitchen Task Status
export type KitchenTaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';

// Table Event Types for eventsApi
export type TableEventType =
  | 'TableNeedsService'
  | 'TableNeedsCleaning'
  | 'TableNeedsBussing'
  | 'TableNeedsSetup';
