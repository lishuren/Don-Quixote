/**
 * @file rules.ts
 * @description Map configuration defaults and validation utilities for restaurant layout (TypeScript)
 */

import type { CreateTableRequest } from '../types/dtos';


// Default table sizes (diameter in pixels) by capacity
export const TABLE_SIZES: { t2: number; t4: number; t6: number; t8: number } = {
  t2: 60,
  t4: 80,
  t6: 100,
  t8: 120,
};

// Default table mix distribution
export const DEFAULT_TABLE_MIX: { t2: number; t4: number; t6: number; t8: number } = {
  t2: 5,
  t4: 10,
  t6: 3,
  t8: 2,
};

export interface ZoneConfig {
  x: number;
  y: number;
  width: number;
  height: number;
  name?: string;
  type?: string;
  color?: string;
  isNavigable?: boolean;
}

export type DiningArea = ZoneConfig;

export interface MapConfig {
  mapWidth: number;
  mapHeight: number;
  mainAisle: number;
  minorAisle: number;
  safetyBuffer: number;
  seatBuffer: number;
  tableShape: 'rect' | 'round' | 'square';
  tableSizes: { t2: number; t4: number; t6: number; t8: number };
  tableMix: { t2: number; t4: number; t6: number; t8: number };
  gridSize: number;
  zones: ZoneConfig[];
  diningArea: DiningArea;
  tables?: CreateTableRequest[];
}

const MapConfigDefaults: MapConfig = {
  mapWidth: 1200,
  mapHeight: 600,
  mainAisle: 100,
  minorAisle: 60,
  safetyBuffer: 40,
  seatBuffer: 20,
  tableShape: 'rect',
  tableSizes: { ...TABLE_SIZES },
  tableMix: { ...DEFAULT_TABLE_MIX },
  gridSize: 10,
  zones: [],
  diningArea: {
    x: 50,
    y: 50,
    width: 680,
    height: 420,
    name: 'DiningArea',
    type: 'Dining',
    isNavigable: true
  },
};

// Validation bounds
export const BOUNDS = {
  tableCount: { min: 1, max: 50 },
  mainAisle: { min: 80, max: 200 },
  minorAisle: { min: 60, max: 150 },
  safetyBuffer: { min: 5, max: 30 },
  seatBuffer: { min: 10, max: 50 },
  mapWidth: { min: 600, max: 1600 },
  mapHeight: { min: 400, max: 1200 },
  tableSizeMin: 50,
  tableSizeMax: 200,
};

const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, min), max);

export function validateConfig(userConfig: Partial<MapConfig> = {}): MapConfig {
  const config: MapConfig = {
    ...MapConfigDefaults,
    tableSizes: { ...MapConfigDefaults.tableSizes },
    tableMix: { ...MapConfigDefaults.tableMix },
    zones: { ...MapConfigDefaults.zones },
    diningArea: { ...MapConfigDefaults.diningArea },
  };

  if (userConfig.mainAisle !== undefined) {
    config.mainAisle = clamp(userConfig.mainAisle, BOUNDS.mainAisle.min, BOUNDS.mainAisle.max);
  }
  if (userConfig.minorAisle !== undefined) {
    config.minorAisle = clamp(userConfig.minorAisle, BOUNDS.minorAisle.min, BOUNDS.minorAisle.max);
  }
  if (userConfig.safetyBuffer !== undefined) {
    config.safetyBuffer = clamp(userConfig.safetyBuffer, BOUNDS.safetyBuffer.min, BOUNDS.safetyBuffer.max);
  }
  if (userConfig.seatBuffer !== undefined) {
    config.seatBuffer = clamp(userConfig.seatBuffer, BOUNDS.seatBuffer.min, BOUNDS.seatBuffer.max);
  }
  if (userConfig.mapWidth !== undefined) {
    config.mapWidth = clamp(userConfig.mapWidth, BOUNDS.mapWidth.min, BOUNDS.mapWidth.max);
  }
  if (userConfig.mapHeight !== undefined) {
    config.mapHeight = clamp(userConfig.mapHeight, BOUNDS.mapHeight.min, BOUNDS.mapHeight.max);
  }
  if (userConfig.tableShape === 'round' || userConfig.tableShape === 'square' || userConfig.tableShape === 'rect') {
    config.tableShape = userConfig.tableShape;
  }
  if (userConfig.tableSizes) {
    config.tableSizes = {
      t2: clamp(userConfig.tableSizes.t2 ?? TABLE_SIZES.t2, BOUNDS.tableSizeMin, BOUNDS.tableSizeMax),
      t4: clamp(userConfig.tableSizes.t4 ?? TABLE_SIZES.t4, BOUNDS.tableSizeMin, BOUNDS.tableSizeMax),
      t6: clamp(userConfig.tableSizes.t6 ?? TABLE_SIZES.t6, BOUNDS.tableSizeMin, BOUNDS.tableSizeMax),
      t8: clamp(userConfig.tableSizes.t8 ?? TABLE_SIZES.t8, BOUNDS.tableSizeMin, BOUNDS.tableSizeMax),
    };
  }
  if (userConfig.tableMix) {
    config.tableMix = {
      t2: Math.max(0, userConfig.tableMix.t2 ?? DEFAULT_TABLE_MIX.t2),
      t4: Math.max(0, userConfig.tableMix.t4 ?? DEFAULT_TABLE_MIX.t4),
      t6: Math.max(0, userConfig.tableMix.t6 ?? DEFAULT_TABLE_MIX.t6),
      t8: Math.max(0, userConfig.tableMix.t8 ?? DEFAULT_TABLE_MIX.t8),
    };
  }
  if (userConfig.gridSize !== undefined) {
    config.gridSize = clamp(userConfig.gridSize, 10, 50);
  }

  // Recalculate dining area and zones
  config.diningArea = calculateDiningArea(config);
  config.zones = calculateZonePositions(config);

  console.log('Validated map config:', config);

  return config;
}

function calculateDiningArea(config: MapConfig): DiningArea {
  return {
    x: 0,
    y: 0,
    width: config.mapWidth,
    height: config.mapHeight,
  };
}

function calculateZonePositions(config: MapConfig): ZoneConfig[] {
  const { diningArea } = config;
  // All returned zones are CreateZoneRequest-like; default to non-navigable for fixed service areas
  const rightX = Math.max(0, diningArea.width - 100);
  const bottomY = Math.max(0, diningArea.height - 80);
  return [
    { x: 50, y: diningArea.height - 30, width: 100, height: 30, name: 'Kitchen', type: 'Kitchen', color: '#FF5722', isNavigable: false },
    { x: diningArea.width / 2 - config.mainAisle/2, y: 0, width: 100, height: 30, name: 'Entrance', type: 'Entrance', color: '#9C27B0', isNavigable: false },
    { x: diningArea.width / 2 + 150, y: diningArea.height - 30, width: 100, height: 30, name: 'Restrooms', type: 'Restroom', color: '#607D8B', isNavigable: false },
    // Storage and Charging on bottom-right, stacked vertically
    { x: rightX, y: bottomY, width: 100, height: 50, name: 'Storage', type: 'Storage', color: '#795548', isNavigable: false },
    { x: rightX, y: bottomY + 50, width: 100, height: 50, name: 'Charging', type: 'Charging', color: '#607D8B', isNavigable: false },
  ];
}

export function validateTableCount(count: number) {
  return clamp(Math.floor(count) || BOUNDS.tableCount.min, BOUNDS.tableCount.min, BOUNDS.tableCount.max);
}

export function getBounds() {
  console.log('Providing validation bounds:', BOUNDS);
  return { ...BOUNDS };
}

export default MapConfigDefaults;
