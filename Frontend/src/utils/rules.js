/**
 * @file rules.js
 * @description Map configuration defaults and validation utilities for restaurant layout
 */

/**
 * Default table sizes (diameter in pixels) by capacity
 */
export const TABLE_SIZES = {
  t2: 70,
  t4: 90,
  t6: 110,
  t8: 130,
};

/**
 * Default table mix distribution
 */
export const DEFAULT_TABLE_MIX = {
  t2: 1,
  t4: 6,
  t6: 2,
  t8: 1,
};

/**
 * Default map configuration
 * @type {MapConfig}
 */
const MapConfig = {
  // Map dimensions
  mapWidth: 900,
  mapHeight: 600,

  // Aisle widths
  mainAisle: 120,
  minorAisle: 90,

  // Buffers
  safetyBuffer: 10,
  seatBuffer: 20,

  // Table configuration
  tableShape: 'rect', // 'rect' | 'round' | 'square'
  tableSizes: { ...TABLE_SIZES },
  tableMix: { ...DEFAULT_TABLE_MIX },

  // Grid resolution for pathfinding
  gridSize: 20,

  // Fixed zones (positions relative to map)
  zones: {
    kitchen: { x: 50, y: 500, width: 120, height: 80, label: 'Kitchen', color: '#FF5722' },
    reception: { x: 750, y: 20, width: 130, height: 60, label: 'Reception', color: '#9C27B0' },
    cashier: { x: 750, y: 100, width: 130, height: 60, label: 'Cashier', color: '#3F51B5' },
    restrooms: { x: 750, y: 500, width: 130, height: 80, label: 'Restrooms', color: '#607D8B' },
  },

  // Dining area boundaries (auto-calculated based on fixed zones)
  diningArea: {
    x: 50,
    y: 50,
    width: 680,
    height: 420,
  },
};

/**
 * Validation bounds for config parameters
 */
const BOUNDS = {
  tableCount: { min: 2, max: 50 },
  mainAisle: { min: 80, max: 200 },
  minorAisle: { min: 60, max: 150 },
  safetyBuffer: { min: 5, max: 30 },
  seatBuffer: { min: 10, max: 50 },
  mapWidth: { min: 600, max: 1600 },
  mapHeight: { min: 400, max: 1200 },
  tableSizeMin: 50,
  tableSizeMax: 200,
};

/**
 * Clamp a value between min and max
 * @param {number} value - Value to clamp
 * @param {number} min - Minimum bound
 * @param {number} max - Maximum bound
 * @returns {number} Clamped value
 */
const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

/**
 * Validate and sanitize user input to safe ranges
 * @param {Partial<MapConfig>} userConfig - User-provided configuration
 * @returns {MapConfig} Validated configuration merged with defaults
 */
export function validateConfig(userConfig = {}) {
  // Start with defaults, then merge with user config to preserve existing values
  const config = {
    ...MapConfig,
    tableSizes: { ...MapConfig.tableSizes },
    tableMix: { ...MapConfig.tableMix },
    zones: { ...MapConfig.zones },
    diningArea: { ...MapConfig.diningArea },
  };

  // Apply user config values with validation
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

  // Recalculate dining area based on map dimensions
  config.diningArea = calculateDiningArea(config);

  // Update zone positions based on map dimensions
  config.zones = calculateZonePositions(config);

  return config;
}

/**
 * Calculate dining area boundaries based on fixed zones
 * @param {MapConfig} config - Map configuration
 * @returns {Object} Dining area bounds {x, y, width, height}
 */
function calculateDiningArea(config) {
  const padding = 20;
  const rightZoneWidth = 150;
  return {
    x: padding,
    y: padding,
    width: config.mapWidth - rightZoneWidth - padding * 2,
    height: config.mapHeight - 120 - padding,
  };
}

/**
 * Calculate zone positions based on map dimensions
 * @param {MapConfig} config - Map configuration
 * @returns {Object} Zone positions
 */
function calculateZonePositions(config) {
  const { mapWidth, mapHeight } = config;
  return {
    kitchen: { x: 50, y: mapHeight - 100, width: 120, height: 80, label: 'Kitchen', color: '#FF5722' },
    reception: { x: mapWidth - 150, y: 20, width: 130, height: 60, label: 'Reception', color: '#9C27B0' },
    cashier: { x: mapWidth - 150, y: 100, width: 130, height: 60, label: 'Cashier', color: '#3F51B5' },
    restrooms: { x: mapWidth - 150, y: mapHeight - 100, width: 130, height: 80, label: 'Restrooms', color: '#607D8B' },
  };
}

/**
 * Validate table count input
 * @param {number} count - Table count to validate
 * @returns {number} Validated table count
 */
export function validateTableCount(count) {
  return clamp(Math.floor(count) || BOUNDS.tableCount.min, BOUNDS.tableCount.min, BOUNDS.tableCount.max);
}

/**
 * Get validation bounds for UI display
 * @returns {Object} Bounds object
 */
export function getBounds() {
  return { ...BOUNDS };
}

export default MapConfig;
