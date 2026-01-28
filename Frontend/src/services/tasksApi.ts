/**
 * @file tasksApi.ts
 * @description Tasks API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Tasks API)
 */

import { apiClient, ApiClient } from './apiClient';
import type { TaskDto, CreateTaskRequest, TaskQueueSummary } from '../types/dtos';
import type { TaskStatus, TaskType } from '../types/enums';

export const tasksApi = {
  /**
   * List tasks with filters
   */
  list: (params?: {
    status?: TaskStatus;
    type?: TaskType;
    robotId?: number;
  }): Promise<TaskDto[]> => {
    const query = ApiClient.buildQuery(params ?? {});
    return apiClient.get<TaskDto[]>(`/api/tasks${query}`);
  },

  /**
   * Get single task by ID
   */
  get: (id: number): Promise<TaskDto> => {
    return apiClient.get<TaskDto>(`/api/tasks/${id}`);
  },

  /**
   * Create a new task
   */
  create: (data: CreateTaskRequest): Promise<TaskDto> => {
    return apiClient.post<TaskDto>('/api/tasks', data);
  },

  /**
   * Delete task
   */
  delete: (id: number): Promise<void> => {
    return apiClient.delete(`/api/tasks/${id}`);
  },

  /**
   * Get pending tasks
   */
  getPending: (): Promise<TaskDto[]> => {
    return apiClient.get<TaskDto[]>('/api/tasks/status/Pending');
  },

  /**
   * Get tasks by robot ID
   */
  getByRobot: (robotId: number): Promise<TaskDto[]> => {
    return apiClient.get<TaskDto[]>(`/api/tasks/robot/${robotId}`);
  },

  /**
   * Assign task to robot
   */
  assign: (taskId: number, robotId: number): Promise<TaskDto> => {
    return apiClient.post<TaskDto>(`/api/tasks/${taskId}/assign`, { robotId });
  },

  /**
   * Start task execution
   */
  start: (id: number): Promise<TaskDto> => {
    return apiClient.post<TaskDto>(`/api/tasks/${id}/start`);
  },

  /**
   * Mark task as completed
   */
  complete: (id: number): Promise<TaskDto> => {
    return apiClient.post<TaskDto>(`/api/tasks/${id}/complete`);
  },

  /**
   * Mark task as failed
   */
  fail: (id: number, reason?: string): Promise<TaskDto> => {
    return apiClient.post<TaskDto>(`/api/tasks/${id}/fail`, { reason });
  },

  /**
   * Unassign task from robot
   */
  unassign: (id: number): Promise<TaskDto> => {
    return apiClient.post<TaskDto>(`/api/tasks/${id}/unassign`);
  },

  /**
   * Get task queue summary
   */
  getSummary: (): Promise<TaskQueueSummary> => {
    return apiClient.get<TaskQueueSummary>('/api/tasks/summary');
  },
};
