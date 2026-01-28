/**
 * @file kitchenApi.ts
 * @description Kitchen API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Kitchen API)
 */

import { apiClient } from './apiClient';
import type { TableStatus } from '../types/enums';

export const kitchenApi = {
  /**
   * Signal food ready for delivery
   * Creates high-priority Deliver task
   */
  foodReady: (tableId: number, orderId?: string, items?: string[]): Promise<void> => {
    return apiClient.post('/api/kitchen/food-ready', { tableId, orderId, items });
  },

  /**
   * Signal drinks ready for delivery
   */
  drinksReady: (tableId: number, orderId?: string, items?: string[]): Promise<void> => {
    return apiClient.post('/api/kitchen/drinks-ready', { tableId, orderId, items });
  },

  /**
   * Signal complete order ready
   * With optional rush flag for urgent priority
   */
  orderReady: (
    tableId: number,
    orderId?: string,
    note?: string,
    isRush?: boolean
  ): Promise<void> => {
    return apiClient.post('/api/kitchen/order-ready', { tableId, orderId, note, isRush });
  },

  /**
   * Trigger table status change (for cleaning after food service)
   * This is a convenience method that wraps eventsApi
   */
  tableStatusChange: (tableId: number, newStatus: TableStatus): Promise<void> => {
    return apiClient.post('/api/events/table-status', { tableId, newStatus });
  },
};
