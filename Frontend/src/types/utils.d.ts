/**
 * Type declarations for utility modules that remain in JavaScript
 */

// layoutGenerator.js
declare module '../utils/layoutGenerator' {
  export function generateTableLayout(config: any, tableCount: number): any[];
  export function getMainAisles(config: any): {
    horizontal: { x: number; y: number; width: number; height: number };
    vertical: { x: number; y: number; width: number; height: number };
  };
  export function getKitchenPosition(config: any): { x: number; y: number };
}

// mapSerializer.js
declare module '../utils/mapSerializer' {
  export function buildMapPayload(config: any, tables: any[]): any;
  export function defaultStartBottomRight(config: any): { x: number; y: number };
}

// rules.js
declare module '../utils/rules' {
  export function getBounds(): {
    tableCount: { min: number; max: number };
    mainAisle: { min: number; max: number };
    minorAisle: { min: number; max: number };
    safetyBuffer: { min: number; max: number };
    seatBuffer: { min: number; max: number };
  };
}

// apiClient.js (legacy JS version)
declare module '../utils/apiClient' {
  export function postMap(payload: any): Promise<{ mapId?: string }>;
  export function planRoute(params: {
    start: { x: number; y: number };
    tableId: string;
    robotRadius: number;
    dockSide: string;
    dockOffset: number;
  }): Promise<{
    success: boolean;
    path?: { x: number; y: number }[];
  }>;
  export function getMap(): Promise<any>;
}
