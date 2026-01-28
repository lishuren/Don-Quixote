/**
 * @file dispatchApi.ts
 * @description Dispatch API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Dispatch API)
 */

import { apiClient } from './apiClient';
import type {
  DispatchConfig,
  DispatchQueueItem,
  AutoAssignResult,
  RobotSuggestion,
} from '../types/dtos';

export const dispatchApi = {
  /**
   * Get dispatch queue
   */
  getQueue: (): Promise<DispatchQueueItem[]> => {
    return apiClient.get<DispatchQueueItem[]>('/api/dispatch/queue');
  },

  /**
   * Trigger auto-assign for pending tasks
   */
  triggerAutoAssign: (): Promise<AutoAssignResult> => {
    return apiClient.post<AutoAssignResult>('/api/dispatch/auto-assign');
  },

  /**
   * Get dispatch configuration
   */
  getConfig: (): Promise<DispatchConfig> => {
    return apiClient.get<DispatchConfig>('/api/dispatch/config');
  },

  /**
   * Update dispatch configuration
   */
  updateConfig: (config: Partial<DispatchConfig>): Promise<DispatchConfig> => {
    return apiClient.patch<DispatchConfig>('/api/dispatch/config', config);
  },

  /**
   * Get robot suggestion for a task
   */
  suggestRobot: (taskId: number): Promise<RobotSuggestion> => {
    return apiClient.post<RobotSuggestion>(`/api/dispatch/suggest/${taskId}`);
  },
};
