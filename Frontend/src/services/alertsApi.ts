/**
 * @file alertsApi.ts
 * @description Alerts API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Alerts API)
 */

import { apiClient, ApiClient } from './apiClient';
import type { AlertDto, CreateAlertRequest, AlertSummary } from '../types/dtos';
import type { AlertSeverity } from '../types/enums';

export const alertsApi = {
  /**
   * List alerts with optional filters
   */
  list: (params?: { severity?: AlertSeverity; acknowledged?: boolean }): Promise<AlertDto[]> => {
    const query = ApiClient.buildQuery(params ?? {});
    return apiClient.get<AlertDto[]>(`/api/alerts${query}`);
  },

  /**
   * Get single alert by ID
   */
  get: (id: number): Promise<AlertDto> => {
    return apiClient.get<AlertDto>(`/api/alerts/${id}`);
  },

  /**
   * Create a new alert
   */
  create: (data: CreateAlertRequest): Promise<AlertDto> => {
    return apiClient.post<AlertDto>('/api/alerts', data);
  },

  /**
   * Delete alert
   */
  delete: (id: number): Promise<void> => {
    return apiClient.delete(`/api/alerts/${id}`);
  },

  /**
   * Get unacknowledged alerts
   */
  getUnacknowledged: (): Promise<AlertDto[]> => {
    return apiClient.get<AlertDto[]>('/api/alerts/unacknowledged');
  },

  /**
   * Acknowledge alert
   */
  acknowledge: (id: number, acknowledgedBy?: string): Promise<AlertDto> => {
    return apiClient.post<AlertDto>(`/api/alerts/${id}/acknowledge`, { acknowledgedBy });
  },

  /**
   * Resolve alert
   */
  resolve: (id: number): Promise<AlertDto> => {
    return apiClient.post<AlertDto>(`/api/alerts/${id}/resolve`);
  },

  /**
   * Get alerts by robot
   */
  getByRobot: (robotId: number): Promise<AlertDto[]> => {
    return apiClient.get<AlertDto[]>(`/api/alerts/robot/${robotId}`);
  },

  /**
   * Get alert summary
   */
  getSummary: (): Promise<AlertSummary> => {
    return apiClient.get<AlertSummary>('/api/alerts/summary');
  },
};
