/**
 * @file signalRService.ts
 * @description SignalR real-time connection service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 5 (SignalR Real-Time Integration)
 */

import * as signalR from '@microsoft/signalr';
import {
  SignalREvents,
  SignalRMethods,
  type RobotPositionEvent,
  type RobotStatusEvent,
  type TaskStatusEvent,
  type AlertEvent,
  type TableStatusEvent,
  type GuestEvent,
  type SimulationProgressEvent,
  type RobotPositionCallback,
  type RobotStatusCallback,
  type TaskStatusCallback,
  type AlertCallback,
  type TableStatusCallback,
  type GuestCallback,
  type SimulationProgressCallback,
} from '../types/signalREvents';

const HUB_URL = process.env.REACT_APP_SIGNALR_HUB || 'http://localhost:5199/hubs/restaurant';

/**
 * Connection state type
 */
export type ConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting';

/**
 * SignalR service for real-time communication
 */
class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private subscribedRobots: Set<number> = new Set();
  private subscribedTables: Set<number> = new Set();
  private subscribedZones: Set<number> = new Set();
  private isAlertSubscribed = false;
  private isTaskSubscribed = false;
  private isSimulationSubscribed = false;

  /**
   * Get current connection state
   */
  getState(): ConnectionState {
    if (!this.connection) return 'Disconnected';
    switch (this.connection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      default:
        return 'Disconnected';
    }
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Connect to SignalR hub
   */
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext: { previousRetryCount: number }) => {
          // Exponential backoff: 0, 2, 4, 8, 16, 32 seconds (max 30s)
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

  /**
   * Setup connection event handlers
   */
  private setupConnectionHandlers(): void {
    if (!this.connection) return;

    this.connection.onreconnecting((error?: Error) => {
      console.warn('SignalR reconnecting...', error);
    });

    this.connection.onreconnected((connectionId?: string) => {
      console.log('SignalR reconnected:', connectionId);
      this.resubscribeAll();
    });

    this.connection.onclose((error?: Error) => {
      console.error('SignalR disconnected:', error);
    });
  }

  /**
   * Disconnect from SignalR hub
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.clearSubscriptions();
    }
  }

  /**
   * Clear all subscription tracking
   */
  private clearSubscriptions(): void {
    this.subscribedRobots.clear();
    this.subscribedTables.clear();
    this.subscribedZones.clear();
    this.isAlertSubscribed = false;
    this.isTaskSubscribed = false;
    this.isSimulationSubscribed = false;
  }

  /**
   * Re-subscribe to all previously active subscriptions after reconnect
   */
  private async resubscribeAll(): Promise<void> {
    const promises: Promise<void>[] = [];

    // Re-subscribe to robots
    for (const robotId of this.subscribedRobots) {
      promises.push(this.subscribeToRobot(robotId));
    }

    // Re-subscribe to tables
    for (const tableId of this.subscribedTables) {
      promises.push(this.subscribeToTable(tableId));
    }

    // Re-subscribe to zones
    for (const zoneId of this.subscribedZones) {
      promises.push(this.subscribeToZone(zoneId));
    }

    // Re-subscribe to alerts
    if (this.isAlertSubscribed) {
      promises.push(this.subscribeToAlerts());
    }

    // Re-subscribe to tasks
    if (this.isTaskSubscribed) {
      promises.push(this.subscribeToTasks());
    }

    // Re-subscribe to simulation
    if (this.isSimulationSubscribed) {
      promises.push(this.subscribeToSimulation());
    }

    await Promise.all(promises);
  }

  // ============================================
  // Subscription Methods
  // ============================================

  async subscribeToRobot(robotId: number): Promise<void> {
    await this.connection?.invoke(SignalRMethods.SUBSCRIBE_TO_ROBOT, robotId);
    this.subscribedRobots.add(robotId);
  }

  async unsubscribeFromRobot(robotId: number): Promise<void> {
    await this.connection?.invoke(SignalRMethods.UNSUBSCRIBE_FROM_ROBOT, robotId);
    this.subscribedRobots.delete(robotId);
  }

  async subscribeToZone(zoneId: number): Promise<void> {
    await this.connection?.invoke(SignalRMethods.SUBSCRIBE_TO_ZONE, zoneId);
    this.subscribedZones.add(zoneId);
  }

  async subscribeToTable(tableId: number): Promise<void> {
    await this.connection?.invoke(SignalRMethods.SUBSCRIBE_TO_TABLE, tableId);
    this.subscribedTables.add(tableId);
  }

  async subscribeToAlerts(): Promise<void> {
    await this.connection?.invoke(SignalRMethods.SUBSCRIBE_TO_ALERTS);
    this.isAlertSubscribed = true;
  }

  async subscribeToTasks(): Promise<void> {
    await this.connection?.invoke(SignalRMethods.SUBSCRIBE_TO_TASKS);
    this.isTaskSubscribed = true;
  }

  async subscribeToSimulation(): Promise<void> {
    await this.connection?.invoke(SignalRMethods.SUBSCRIBE_TO_SIMULATION);
    this.isSimulationSubscribed = true;
  }

  async unsubscribeFromSimulation(): Promise<void> {
    await this.connection?.invoke(SignalRMethods.UNSUBSCRIBE_FROM_SIMULATION);
    this.isSimulationSubscribed = false;
  }

  async ping(): Promise<string> {
    return (await this.connection?.invoke(SignalRMethods.PING)) || '';
  }

  // ============================================
  // Event Handlers
  // ============================================

  onRobotPositionUpdated(callback: RobotPositionCallback): void {
    this.connection?.on(SignalREvents.ROBOT_POSITION_UPDATED, callback);
  }

  onRobotStatusChanged(callback: RobotStatusCallback): void {
    this.connection?.on(SignalREvents.ROBOT_STATUS_CHANGED, callback);
  }

  onTaskStatusChanged(callback: TaskStatusCallback): void {
    this.connection?.on(SignalREvents.TASK_STATUS_CHANGED, callback);
  }

  onAlertCreated(callback: AlertCallback): void {
    this.connection?.on(SignalREvents.ALERT_CREATED, callback);
  }

  onTableStatusChanged(callback: TableStatusCallback): void {
    this.connection?.on(SignalREvents.TABLE_STATUS_CHANGED, callback);
  }

  onGuestEvent(callback: GuestCallback): void {
    this.connection?.on(SignalREvents.GUEST_EVENT, callback);
  }

  onSimulationProgressUpdated(callback: SimulationProgressCallback): void {
    this.connection?.on(SignalREvents.SIMULATION_PROGRESS_UPDATED, callback);
  }

  /**
   * Remove event handler
   */
  off(eventName: string): void {
    this.connection?.off(eventName);
  }

  /**
   * Remove all handlers for an event
   */
  offAll(eventName: keyof typeof SignalREvents): void {
    this.connection?.off(SignalREvents[eventName]);
  }
}

// Export singleton instance
export const signalRService = new SignalRService();

// Also export class for testing
export { SignalRService };

// Export typed event interfaces for external use
export type {
  RobotPositionEvent,
  RobotStatusEvent,
  TaskStatusEvent,
  AlertEvent,
  TableStatusEvent,
  GuestEvent,
  SimulationProgressEvent,
};
