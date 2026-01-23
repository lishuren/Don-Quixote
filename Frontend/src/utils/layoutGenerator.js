/**
 * @file layoutGenerator.js
 * @description Auto-generate restaurant table layout based on rules
 */

import MapConfig, { validateConfig, validateTableCount } from './rules.js';

/**
 * Generate table layout based on configuration and table count
 * @param {Object} config - Map configuration from rules.js
 * @param {number} tableCount - Number of tables to place
 * @returns {Array<Object>} Array of table objects with positions and bounds
 */
export function generateTableLayout(config, tableCount) {
  console.log("Generating layout with config:", config, "and tableCount:", tableCount);
  const validConfig = validateConfig(config);
  const validCount = validateTableCount(tableCount);

  const { diningArea, mainAisle, minorAisle, seatBuffer, safetyBuffer, tableShape, tableSizes, tableMix } = validConfig;

  // Generate table type distribution
  const tableTypes = distributeTableTypes(validCount, tableMix);

  // Use smaller cell size for denser placement
  const avgTableSize = (tableSizes.t2 + tableSizes.t4 + tableSizes.t6 + tableSizes.t8) / 4;
  // Use smaller cell size - just table + seat buffer for tighter packing
  const cellSize = avgTableSize + seatBuffer;

  // Calculate main aisle positions (center cross)
  const aisleH = {
    x: diningArea.x,
    y: diningArea.y + diningArea.height / 2 - mainAisle / 2,
    width: diningArea.width,
    height: mainAisle,
  };

  const aisleV = {
    x: diningArea.x + diningArea.width / 2 - mainAisle / 2,
    y: diningArea.y,
    width: mainAisle,
    height: diningArea.height,
  };

  // Collect all valid positions first
  const validPositions = [];

  // Use smaller buffer for collision checking - use minimum table size
  const minTableSize = Math.min(tableSizes.t2, tableSizes.t4, tableSizes.t6, tableSizes.t8);
  const collisionRadius = minTableSize / 2;

  // Calculate grid dimensions for entire dining area with proper margins
  const margin = collisionRadius + safetyBuffer;
  const startX = diningArea.x + margin;
  const startY = diningArea.y + margin;
  const endX = diningArea.x + diningArea.width - margin;
  const endY = diningArea.y + diningArea.height - margin;

  for (let y = startY; y < endY; y += cellSize) {
    for (let x = startX; x < endX; x += cellSize) {
      // Center position is the cell position itself (not offset)
      const centerX = x;
      const centerY = y;
      
      // Check if this position overlaps with main aisles (use collision radius)
      const inHorizontalAisle = (centerY + collisionRadius > aisleH.y) && (centerY - collisionRadius < aisleH.y + aisleH.height);
      const inVerticalAisle = (centerX + collisionRadius > aisleV.x) && (centerX - collisionRadius < aisleV.x + aisleV.width);

      if (inHorizontalAisle || inVerticalAisle) {
        continue; // Skip positions in the aisles
      }

      // Check if position is within dining area bounds
      if (centerX - collisionRadius < diningArea.x || centerX + collisionRadius > diningArea.x + diningArea.width ||
          centerY - collisionRadius < diningArea.y || centerY + collisionRadius > diningArea.y + diningArea.height) {
        continue;
      }

      validPositions.push({ x: centerX, y: centerY });
    }
  }

  console.log("Valid positions found:", validPositions.length, "for tableCount:", validCount);

  // Place tables at valid positions
  const tables = [];
  let typeIndex = 0;

  for (let i = 0; i < Math.min(validCount, validPositions.length); i++) {
    const pos = validPositions[i];
    
    if (typeIndex >= tableTypes.length) {
      typeIndex = 0;
    }

    const tableType = tableTypes[typeIndex] || 't4';
    const diameter = tableSizes[tableType] || 90;
    const radius = diameter / 2;

    // For rectangular tables: width is diameter, height is diameter * 0.6
    const rectWidth = diameter;
    const rectHeight = diameter * 0.6;

    const table = {
      id: i + 1,
      type: tableType,
      capacity: parseInt(tableType.substring(1)) || 4,
      shape: tableShape,
      center: { x: pos.x, y: pos.y },
      radius: tableShape === 'round' ? radius : null,
      side: tableShape === 'square' ? diameter * 0.9 : null,
      rectWidth: tableShape === 'rect' ? rectWidth : null,
      rectHeight: tableShape === 'rect' ? rectHeight : null,
      bounds: {
        x: pos.x - radius - safetyBuffer,
        y: pos.y - radius - safetyBuffer,
        width: diameter + safetyBuffer * 2,
        height: diameter + safetyBuffer * 2,
      },
    };

    tables.push(table);
    typeIndex++;
  }

  return tables;
}

/**
 * Distribute table types based on mix ratios
 * @param {number} count - Total table count
 * @param {Object} mix - Table mix ratios {t2, t4, t6, t8}
 * @returns {Array<string>} Array of table type strings
 */
function distributeTableTypes(count, mix) {
  const total = mix.t2 + mix.t4 + mix.t6 + mix.t8;
  if (total === 0) {
    return Array(count).fill('t4'); // Default to t4 if no mix
  }

  const types = [];
  const ratios = {
    t2: mix.t2 / total,
    t4: mix.t4 / total,
    t6: mix.t6 / total,
    t8: mix.t8 / total,
  };

  // Calculate count for each type
  let remaining = count;
  const counts = {};
  
  for (const [type, ratio] of Object.entries(ratios)) {
    counts[type] = Math.round(count * ratio);
    remaining -= counts[type];
  }

  // Distribute remaining to most common type
  if (remaining !== 0) {
    const maxType = Object.entries(ratios).reduce((a, b) => (b[1] > a[1] ? b : a))[0];
    counts[maxType] += remaining;
  }

  // Build array
  for (const [type, c] of Object.entries(counts)) {
    for (let i = 0; i < c; i++) {
      types.push(type);
    }
  }

  // Shuffle for variety
  return shuffleArray(types);
}

/**
 * Get placement zones (4 quadrants around main aisle cross)
 * @param {Object} diningArea - Dining area bounds
 * @param {Object} aisleH - Horizontal aisle bounds
 * @param {Object} aisleV - Vertical aisle bounds
 * @returns {Array<Object>} Array of zone bounds
 */
function getPlacementZones(diningArea, aisleH, aisleV) {
  return [
    // Top-left quadrant
    {
      x: diningArea.x,
      y: diningArea.y,
      width: aisleV.x - diningArea.x,
      height: aisleH.y - diningArea.y,
    },
    // Top-right quadrant
    {
      x: aisleV.x + aisleV.width,
      y: diningArea.y,
      width: diningArea.x + diningArea.width - (aisleV.x + aisleV.width),
      height: aisleH.y - diningArea.y,
    },
    // Bottom-left quadrant
    {
      x: diningArea.x,
      y: aisleH.y + aisleH.height,
      width: aisleV.x - diningArea.x,
      height: diningArea.y + diningArea.height - (aisleH.y + aisleH.height),
    },
    // Bottom-right quadrant
    {
      x: aisleV.x + aisleV.width,
      y: aisleH.y + aisleH.height,
      width: diningArea.x + diningArea.width - (aisleV.x + aisleV.width),
      height: diningArea.y + diningArea.height - (aisleH.y + aisleH.height),
    },
  ];
}

/**
 * Calculate main aisle positions for rendering
 * @param {Object} config - Map configuration
 * @returns {Object} Aisle bounds {horizontal, vertical}
 */
export function getMainAisles(config) {
  const { diningArea, mainAisle } = validateConfig(config);

  return {
    horizontal: {
      x: diningArea.x,
      y: diningArea.y + diningArea.height / 2 - mainAisle / 2,
      width: diningArea.width,
      height: mainAisle,
    },
    vertical: {
      x: diningArea.x + diningArea.width / 2 - mainAisle / 2,
      y: diningArea.y,
      width: mainAisle,
      height: diningArea.height,
    },
  };
}

/**
 * Shuffle array using Fisher-Yates algorithm
 * @param {Array} array - Array to shuffle
 * @returns {Array} Shuffled array
 */
function shuffleArray(array) {
  const result = [...array];
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}

/**
 * Get kitchen position for pathfinding
 * @param {Object} config - Map configuration
 * @returns {Object} Kitchen center position {x, y}
 */
export function getKitchenPosition(config) {
  const { zones } = validateConfig(config);
  const kitchen = zones.kitchen;
  return {
    x: kitchen.x + kitchen.width / 2,
    y: kitchen.y + kitchen.height / 2,
  };
}
