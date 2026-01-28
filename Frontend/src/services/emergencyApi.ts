/**
 * @file emergencyApi.ts
 * @description Emergency API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Emergency API)
 */

import { apiClient } from './apiClient';

export const emergencyApi = {
  /**
   * Emergency stop all robots
   */
  stopAll: (): Promise<void> => {
    return apiClient.post('/api/emergency/stop-all');
  },

  /**
   * Trigger evacuation mode
   */
  evacuate: (): Promise<void> => {
    return apiClient.post('/api/emergency/evacuate');
  },

  /**
   * Resume normal operations
   */
  resume: (): Promise<void> => {
    return apiClient.post('/api/emergency/resume');
  },
};
