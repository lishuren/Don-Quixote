/**
 * @file eventsApi.ts
 * @description Events API service for triggering actions
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Events API)
 */

import { apiClient } from './apiClient';
import type { TableStatus } from '../types/enums';

export const eventsApi = {
  /**
   * Register guest arrival at restaurant
   * Creates Escort task automatically
   */
  guestArrived: (partySize: number, guestName?: string): Promise<void> => {
    return apiClient.post('/api/events/guest-arrived', { partySize, guestName });
  },

  /**
   * Guest needs help at table
   * Creates high-priority Service task
   */
  guestNeedsHelp: (tableId: number, reason?: string, isUrgent?: boolean): Promise<void> => {
    return apiClient.post('/api/events/guest-help', { tableId, reason, isUrgent });
  },

  /**
   * Trigger table status change
   * Can trigger various table-related tasks (cleaning, service, etc.)
   */
  tableStatusChange: (tableId: number, newStatus: TableStatus, notes?: string): Promise<void> => {
    return apiClient.post('/api/events/table-status', { tableId, newStatus, notes });
  },

  /**
   * Generic event trigger
   */
  trigger: (
    eventType: string,
    payload: Record<string, unknown>
  ): Promise<void> => {
    return apiClient.post('/api/events/trigger', { eventType, ...payload });
  },

  /**
   * Get list of available event types
   */
  getTypes: (): Promise<string[]> => {
    return apiClient.get<string[]>('/api/events/types');
  },
};
