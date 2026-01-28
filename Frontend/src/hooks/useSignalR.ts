/**
 * @file useSignalR.ts
 * @description React hook for SignalR connection and subscriptions
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 5 (React Hook for SignalR)
 */

import { useEffect, useRef, useCallback } from 'react';
import { signalRService } from '../services/signalRService';
import type {
  RobotPositionCallback,
  RobotStatusCallback,
  TaskStatusCallback,
  AlertCallback,
  TableStatusCallback,
  GuestCallback,
  SimulationProgressCallback,
} from '../types/signalREvents';

/**
 * Hook for SignalR connection and event subscriptions
 */
export function useSignalR() {
  const isConnected = useRef(false);

  useEffect(() => {
    const connect = async () => {
      if (!isConnected.current) {
        try {
          await signalRService.connect();
          isConnected.current = true;
        } catch (error) {
          console.error('Failed to connect to SignalR:', error);
        }
      }
    };

    connect();

    // Don't disconnect on unmount - connection is shared across components
    return () => {};
  }, []);

  const subscribeToRobot = useCallback((robotId: number) => {
    return signalRService.subscribeToRobot(robotId);
  }, []);

  const unsubscribeFromRobot = useCallback((robotId: number) => {
    return signalRService.unsubscribeFromRobot(robotId);
  }, []);

  const subscribeToAlerts = useCallback(() => {
    return signalRService.subscribeToAlerts();
  }, []);

  const subscribeToTasks = useCallback(() => {
    return signalRService.subscribeToTasks();
  }, []);

  const subscribeToSimulation = useCallback(() => {
    return signalRService.subscribeToSimulation();
  }, []);

  const unsubscribeFromSimulation = useCallback(() => {
    return signalRService.unsubscribeFromSimulation();
  }, []);

  const subscribeToTable = useCallback((tableId: number) => {
    return signalRService.subscribeToTable(tableId);
  }, []);

  const subscribeToZone = useCallback((zoneId: number) => {
    return signalRService.subscribeToZone(zoneId);
  }, []);

  const onRobotPositionUpdated = useCallback((callback: RobotPositionCallback) => {
    signalRService.onRobotPositionUpdated(callback);
  }, []);

  const onRobotStatusChanged = useCallback((callback: RobotStatusCallback) => {
    signalRService.onRobotStatusChanged(callback);
  }, []);

  const onTaskStatusChanged = useCallback((callback: TaskStatusCallback) => {
    signalRService.onTaskStatusChanged(callback);
  }, []);

  const onAlertCreated = useCallback((callback: AlertCallback) => {
    signalRService.onAlertCreated(callback);
  }, []);

  const onTableStatusChanged = useCallback((callback: TableStatusCallback) => {
    signalRService.onTableStatusChanged(callback);
  }, []);

  const onGuestEvent = useCallback((callback: GuestCallback) => {
    signalRService.onGuestEvent(callback);
  }, []);

  const onSimulationProgressUpdated = useCallback((callback: SimulationProgressCallback) => {
    signalRService.onSimulationProgressUpdated(callback);
  }, []);

  const off = useCallback((eventName: string) => {
    signalRService.off(eventName);
  }, []);

  return {
    // Subscription methods
    subscribeToRobot,
    unsubscribeFromRobot,
    subscribeToAlerts,
    subscribeToTasks,
    subscribeToSimulation,
    unsubscribeFromSimulation,
    subscribeToTable,
    subscribeToZone,

    // Event handlers
    onRobotPositionUpdated,
    onRobotStatusChanged,
    onTaskStatusChanged,
    onAlertCreated,
    onTableStatusChanged,
    onGuestEvent,
    onSimulationProgressUpdated,
    off,

    // Connection status
    isConnected: () => signalRService.isConnected(),
    getState: () => signalRService.getState(),
  };
}
