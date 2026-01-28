/**
 * @file simulationApi.ts
 * @description Simulation API service for long-run simulations
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 4 & 9 (Simulation API)
 */

import { apiClient } from './apiClient';
import type {
  AccelerationPreset,
  StartSimulationRequest,
  StartSimulationResponse,
  SimulationProgressDto,
  SimulationReportDto,
} from '../types/dtos';

export const simulationApi = {
  /**
   * Get available acceleration presets
   */
  getPresets: (): Promise<AccelerationPreset[]> => {
    return apiClient.get<AccelerationPreset[]>('/api/simulation/long-run/acceleration-presets');
  },

  /**
   * Start a long-run simulation
   */
  start: (request: StartSimulationRequest): Promise<StartSimulationResponse> => {
    return apiClient.post<StartSimulationResponse>('/api/simulation/long-run', request);
  },

  /**
   * Get current simulation progress
   */
  getProgress: (): Promise<SimulationProgressDto> => {
    return apiClient.get<SimulationProgressDto>('/api/simulation/long-run/progress');
  },

  /**
   * Pause running simulation
   */
  pause: (): Promise<void> => {
    return apiClient.post('/api/simulation/long-run/pause');
  },

  /**
   * Resume paused simulation
   */
  resume: (): Promise<void> => {
    return apiClient.post('/api/simulation/long-run/resume');
  },

  /**
   * Stop simulation
   */
  stop: (): Promise<void> => {
    return apiClient.post('/api/simulation/long-run/stop');
  },

  /**
   * Get final simulation report
   */
  getReport: (): Promise<SimulationReportDto> => {
    return apiClient.get<SimulationReportDto>('/api/simulation/long-run/report');
  },
};
