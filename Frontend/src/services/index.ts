/**
 * @file index.ts
 * @description Central export for all API services
 */

export { apiClient, ApiClient, ApiClientError } from './apiClient';
export { robotsApi } from './robotsApi';
export { tasksApi } from './tasksApi';
export { tablesApi } from './tablesApi';
export { guestsApi } from './guestsApi';
export { alertsApi } from './alertsApi';
export { dashboardApi } from './dashboardApi';
export { dispatchApi } from './dispatchApi';
export { eventsApi } from './eventsApi';
export { kitchenApi } from './kitchenApi';
export { emergencyApi } from './emergencyApi';
export { simulationApi } from './simulationApi';
export { signalRService, SignalRService } from './signalRService';
export type { ConnectionState } from './signalRService';
