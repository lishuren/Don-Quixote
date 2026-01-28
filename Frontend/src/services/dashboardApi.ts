/**
 * @file dashboardApi.ts
 * @description Dashboard API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Dashboard API)
 */

import { apiClient } from './apiClient';
import type { DashboardSummary, RobotSummary, TableStatusSummary } from '../types/dtos';

export const dashboardApi = {
  /**
   * Get full dashboard summary
   */
  getSummary: (): Promise<DashboardSummary> => {
    return apiClient.get<DashboardSummary>('/api/dashboard');
  },

  /**
   * Get robot summary only
   */
  getRobotSummary: (): Promise<RobotSummary> => {
    return apiClient.get<RobotSummary>('/api/dashboard/robots');
  },

  /**
   * Get table summary only
   */
  getTableSummary: (): Promise<TableStatusSummary> => {
    return apiClient.get<TableStatusSummary>('/api/dashboard/tables');
  },
};
