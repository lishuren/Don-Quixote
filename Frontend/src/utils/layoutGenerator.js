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

  // Table type distribution
  const tableTypes = distributeTableTypes(validCount, tableMix);

  // Aisles (used to avoid placement inside them)
  const aisleH = {
    x: diningArea.x,
    y: diningArea.y + diningArea.height / 2 - minorAisle / 2,
    width: diningArea.width,
    height: minorAisle,
  };
  const aisleV = {
    x: diningArea.x + diningArea.width / 2 - mainAisle / 2,
    y: diningArea.y,
    width: mainAisle,
    height: diningArea.height,
  };

  // Placement bounds (avoid top/bottom zones)
  const bounds = getTablePlacementBounds(validConfig.diningArea, validConfig.zones, safetyBuffer);
  const startX = bounds.startX;
  const startY = bounds.startY;
  const endX = bounds.endX;
  const endY = bounds.endY;

  // Sequential placement using previous table position
  const tables = [];
  let curX = startX;
  let curY = startY;
  const rowStartX = startX;

  const spacingX = safetyBuffer + seatBuffer; // spacing uses safetyBuffer, not rendering
  const spacingY = safetyBuffer + seatBuffer;

  console.log("spacingX:", spacingX, "spacingY:", spacingY);

  for (let i = 0; i < tableTypes.length; i++) {
    const tableType = tableTypes[i] || 't4';
    const diameter = tableSizes[tableType] || 90; // base size by type

    // Real table width/height (no safetyBuffer in dimensions)
    let width, height, radius = null;
    if (tableShape === 'round') {
      radius = diameter / 2;
      width = diameter;
      height = diameter;
    } else if (tableShape === 'rect') {
      width = diameter;
      height = Math.round(diameter * 0.6);
    } else if (tableShape === 'square') {
      width = Math.round(diameter * 0.9);
      height = width;
    } else {
      // default to rectangular
      width = diameter;
      height = Math.round(diameter * 0.6);
    }

    // Ensure the table fits horizontally; wrap to next row when needed
    if (curX + width > endX) {
      curX = rowStartX;
      curY += height + spacingY;
    }
    // If we exceed vertical bounds, stop placing more tables
    if (curY + height > endY) break;

    // Try to find a valid position that doesn't overlap aisles
    let attempts = 0;
    while (attempts < 50) {
      const candidate = { x: curX, y: curY, width, height };
      const overlapsH = rectsOverlap(candidate, aisleH);
      const overlapsV = rectsOverlap(candidate, aisleV);

      // Also ensure inside dining area bounds
      const outOfBounds = candidate.x < startX || candidate.y < startY || candidate.x + width > endX || candidate.y + height > endY;

      if (!overlapsH && !overlapsV && !outOfBounds) break;

      // If overlaps vertical aisle, move just past it
      if (overlapsV) {
        curX = aisleV.x + aisleV.width + spacingX;
      }
      // If overlaps horizontal aisle, move below it
      if (overlapsH) {
        curY = aisleH.y + aisleH.height + spacingY;
      }
      // Wrap if we overflow horizontally
      if (curX + width > endX) {
        curX = rowStartX;
        curY += height + spacingY;
      }
      // Stop if we exceed vertical bounds
      if (curY + height > endY) break;
      attempts++;
    }
    if (curY + height > endY) break;

    const x = curX;
    const y = curY;

    console.log(`Placing table ${i + 1} of type ${tableType} at (${x}, ${y}) with size (${width}x${height})`);

    const table = {
      id: tables.length + 1,
      type: tableType,
      capacity: parseInt(tableType.substring(1)) || 4,
      shape: tableShape,
      // unified top-left
      x,
      y,
      width,
      height,
      radius: tableShape === 'round' ? radius : null,
      // bounds used by backend (includes safetyBuffer for obstacle inflation)
      bounds: {
        x: x - safetyBuffer,
        y: y - safetyBuffer,
        width: width + safetyBuffer,
        height: height + safetyBuffer,
      },
    };

    tables.push(table);
    // Next table position based on previous
    curX = x + width + spacingX;
  }

  console.log("Generated tables:", tables);
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
    t4: mix.t2 / total,
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
  // return shuffleArray(types);
  return types;
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

/**
 * Calculate zones based on diningArea
 * @param {Object} diningArea - The dining area bounds
 * @returns {Object} zones - The calculated zones with positions and sizes
 */
export function calculateZones(diningArea) {
  return {
    kitchen: { x: 50, y: diningArea.height - 30, width: 100, height: 30, label: 'Kitchen', color: '#FF5722' },
    reception: { x: 150, y: 0, width: 100, height: 30, label: 'Reception', color: '#9C27B0' },
    cashier: { x: 300, y: 0, width: 100, height: 30, label: 'Cashier', color: '#3F51B5' },
    restrooms: { x: diningArea.width / 2 + 150, y: diningArea.height - 30, width: 100, height: 30, label: 'Restrooms', color: '#607D8B' },
  };
}

/**
 * Calculate valid startX and startY for tables to avoid overlapping with zones
 * @param {Object} diningArea - The dining area bounds
 * @param {Object} zones - The zones object
 * @param {number} margin - The margin to use for table placement
 * @returns {Object} { startX, startY, endX, endY }
 */
export function getTablePlacementBounds(diningArea, zones, margin) {
  // Find the max Y of top zones and min Y of bottom zones
  const topZones = Object.values(zones).filter(z => z.y === 0);
  const bottomZones = Object.values(zones).filter(z => z.y + z.height === diningArea.height);

  const topZoneMaxY = topZones.length > 0 ? Math.max(...topZones.map(z => z.y + z.height)) : 0;
  const bottomZoneMinY = bottomZones.length > 0 ? Math.min(...bottomZones.map(z => z.y)) : diningArea.height;

  const startX = diningArea.x + margin;
  const startY = diningArea.y + margin + topZoneMaxY;
  const endX = diningArea.x + diningArea.width - margin;
  const endY = bottomZoneMinY - margin;

  return { startX, startY, endX, endY };
}

// Rectangle overlap helper
function rectsOverlap(a, b) {
  return !(a.x + a.width <= b.x || a.x >= b.x + b.width || a.y + a.height <= b.y || a.y >= b.y + b.height);
}
