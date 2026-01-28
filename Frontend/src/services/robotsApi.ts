/**
 * @file robotsApi.ts
 * @description Robots API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Robots API)
 */

import { apiClient, ApiClient } from './apiClient';
import type {
  RobotDto,
  CreateRobotRequest,
  UpdateRobotRequest,
  RobotPositionUpdate,
  RobotCommandRequest,
  CommandAcknowledgment,
  RobotHistoryResponse,
} from '../types/dtos';
import type { RobotStatus } from '../types/enums';

export const robotsApi = {
  /**
   * List robots with optional filters
   */
  list: (params?: { status?: RobotStatus; isEnabled?: boolean }): Promise<RobotDto[]> => {
    const query = ApiClient.buildQuery(params ?? {});
    return apiClient.get<RobotDto[]>(`/api/robots${query}`);
  },

  /**
   * Get single robot by ID
   */
  get: (id: number): Promise<RobotDto> => {
    return apiClient.get<RobotDto>(`/api/robots/${id}`);
  },

  /**
   * Create one or more robots. Accepts an array of CreateRobotRequest and returns created RobotDto[]
   */
  create: (data: CreateRobotRequest[] | CreateRobotRequest): Promise<RobotDto[] | RobotDto> => {
    // If caller passed single object, wrap in array
    const body = Array.isArray(data) ? data : [data];
    return apiClient.post<RobotDto[]>('/api/robots', body as any);
  },

  /**
   * Update robot
   */
  update: (id: number, data: UpdateRobotRequest): Promise<RobotDto> => {
    return apiClient.put<RobotDto>(`/api/robots/${id}`, data);
  },

  /**
   * Delete robot
   */
  delete: (id: number): Promise<void> => {
    return apiClient.delete(`/api/robots/${id}`);
  },

  /**
   * Get robots by status
   */
  getByStatus: (status: RobotStatus): Promise<RobotDto[]> => {
    return apiClient.get<RobotDto[]>(`/api/robots/status/${status}`);
  },

  /**
   * Get idle robots
   */
  getIdle: (): Promise<RobotDto[]> => {
    return apiClient.get<RobotDto[]>('/api/robots/idle');
  },

  /**
   * Update robot position
   */
  updatePosition: (id: number, position: RobotPositionUpdate): Promise<RobotDto> => {
    return apiClient.put<RobotDto>(`/api/robots/${id}/position`, position);
  },

  /**
   * Update robot battery level
   */
  updateBattery: (id: number, level: number): Promise<RobotDto> => {
    return apiClient.put<RobotDto>(`/api/robots/${id}/battery`, { batteryLevel: level });
  },

  /**
   * Send command to robot
   */
  sendCommand: (id: number, command: RobotCommandRequest): Promise<CommandAcknowledgment> => {
    return apiClient.patch<CommandAcknowledgment>(`/api/robots/${id}/command`, command);
  },

  /**
   * Get robot task history
   */
  getHistory: (id: number, limit?: number): Promise<RobotHistoryResponse> => {
    const query = limit ? `?limit=${limit}` : '';
    return apiClient.get<RobotHistoryResponse>(`/api/robots/${id}/history${query}`);
  },
};
