/**
 * @file guestsApi.ts
 * @description Guests API service
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 (Guests API)
 */

import { apiClient, ApiClient } from './apiClient';
import type { GuestDto, CreateGuestRequest, WaitlistSummary } from '../types/dtos';
import type { GuestStatus } from '../types/enums';

export const guestsApi = {
  /**
   * List guests with optional status filter
   */
  list: (params?: { status?: GuestStatus }): Promise<GuestDto[]> => {
    const query = ApiClient.buildQuery(params ?? {});
    return apiClient.get<GuestDto[]>(`/api/guests${query}`);
  },

  /**
   * Get single guest by ID
   */
  get: (id: number): Promise<GuestDto> => {
    return apiClient.get<GuestDto>(`/api/guests/${id}`);
  },

  /**
   * Add guest to waitlist
   */
  create: (data: CreateGuestRequest[] | CreateGuestRequest): Promise<GuestDto[] | GuestDto> => {
    const body = Array.isArray(data) ? data : [data];
    return apiClient.post<GuestDto[]>('/api/guests', body as any);
  },

  /**
   * Remove guest
   */
  delete: (id: number): Promise<void> => {
    return apiClient.delete(`/api/guests/${id}`);
  },

  /**
   * Get waitlist summary
   */
  getWaitlist: (): Promise<WaitlistSummary> => {
    return apiClient.get<WaitlistSummary>('/api/guests/waitlist');
  },

  /**
   * Seat guest at table
   */
  seat: (guestId: number, tableId: number): Promise<GuestDto> => {
    return apiClient.post<GuestDto>(`/api/guests/${guestId}/seat`, { tableId });
  },

  /**
   * Check out guest
   */
  checkout: (id: number): Promise<GuestDto> => {
    return apiClient.post<GuestDto>(`/api/guests/${id}/checkout`);
  },

  /**
   * Get guests by table
   */
  getByTable: (tableId: number): Promise<GuestDto[]> => {
    return apiClient.get<GuestDto[]>(`/api/guests/table/${tableId}`);
  },
};
