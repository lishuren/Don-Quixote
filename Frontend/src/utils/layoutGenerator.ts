/**
 * @file layoutGenerator.ts
 * @description Auto-generate restaurant table layout based on rules
 */

import { validateConfig, validateTableCount } from './rules';
import type { CreateTableRequest, PositionDto } from '../types/dtos';
import type { MapConfig } from './rules';

type TableType = 't2' | 't4' | 't6' | 't8';

/**
 * Generate table layout based on configuration and table count
 * @param config - Map configuration from rules.js
 * @param tableCount - Number of tables to place
 * @returns MapConfig object (validated map) with generated tables and zones
 */
export function generateTableLayout(config: MapConfig | any, tableCount: number): MapConfig {
  const validConfig = validateConfig(config as MapConfig);
  const validCount = validateTableCount(tableCount);

  const { diningArea, mainAisle, minorAisle, seatBuffer, safetyBuffer, tableShape, tableSizes, tableMix } = validConfig;

  // Table type distribution
  const tableTypes = distributeTableTypes(validCount, tableMix);

  // Aisles (used to avoid placement inside them) - also represented as zones
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
  const tables: CreateTableRequest[] = [];
  let curX = startX;
  let curY = startY;
  const rowStartX = startX;

  const spacingX = safetyBuffer + seatBuffer; // spacing uses safetyBuffer, not rendering
  const spacingY = safetyBuffer + seatBuffer;

  for (let i = 0; i < tableTypes.length; i++) {
    const tableType = (tableTypes[i] || 't4') as TableType;
    const diameter = (tableSizes as Record<TableType, number>)[tableType] || 90; // base size by type

    // Real table width/height (no safetyBuffer in dimensions)
    let width: number, height: number, radius: number | null = null;
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

    // Map tableShape ('rect'|'round'|'square') to frontend TableShape strings
    const shapeMap: Record<string, 'Rectangle' | 'Circle' | 'Square'> = {
      round: 'Circle',
      rect: 'Rectangle',
      square: 'Square',
    };
    
    const table: CreateTableRequest = {
      label: `T${tables.length + 1}`,
      shape: shapeMap[tableShape] ?? 'Rectangle',
      center: {
        screenX: x,
        screenY: y,
        physicalX: x,
        physicalY: y,
      } as PositionDto,
      width,
      height,
      rotation: 0,
      capacity: parseInt(tableType.substring(1)) || 4,
    };

    tables.push(table);
    // Next table position based on previous
    curX = x + width + spacingX;
  }

  // Build final map config: include generated tables and represent aisles/diningArea as zones (array)
  const finalConfig: MapConfig = {
    ...validConfig,
    tables,
    // ensure zones exist and include aisles/dining area
    zones: [
      ...(Array.isArray(validConfig.zones) ? validConfig.zones : []),
      { x: aisleH.x, y: aisleH.y, width: aisleH.width , height: aisleH.height, name: 'MainAisleH', type: 'Corridor', color: '#CCCCCC', isNavigable: true },
      { x: aisleV.x, y: aisleV.y, width: aisleV.width, height: aisleV.height, name: 'MainAisleV', type: 'Corridor', color: '#CCCCCC', isNavigable: true },
    ],
    diningArea: { x: diningArea.x, y: diningArea.y, width: diningArea.width, height: diningArea.height, name: 'DiningArea', type: 'Dining', isNavigable: true }
  };

  return finalConfig;
}

/**
 * Distribute table types based on mix ratios
 */
function distributeTableTypes(count: number, mix: any): string[] {
  const total = mix.t2 + mix.t4 + mix.t6 + mix.t8;
  if (total === 0) {
    return Array(count).fill('t4'); // Default to t4 if no mix
  }

  const types: TableType[] = [];
  const ratios: Record<TableType, number> = {
    t2: mix.t2 / total,
    t4: mix.t4 / total,
    t6: mix.t6 / total,
    t8: mix.t8 / total,
  };

  // Calculate count for each type
  let remaining = count;
  const counts: Record<TableType, number> = { t2: 0, t4: 0, t6: 0, t8: 0 };
  for (const [type, ratio] of Object.entries(ratios)) {
    counts[type as TableType] = Math.round(count * (ratio as number));
    remaining -= counts[type as TableType];
  }

  // Distribute remaining to most common type
  if (remaining !== 0) {
    const maxType = (Object.entries(ratios).reduce((a, b) => (b[1] > a[1] ? b : a))[0]) as TableType;
    counts[maxType] += remaining;
  }

  // Build array
  for (const type of (Object.keys(counts) as TableType[])) {
    for (let i = 0; i < counts[type]; i++) types.push(type);
  }

  return types as string[];
}

/**
 * Calculate main aisle positions for rendering
 */
export function getMainAisles(config: any) {
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


export function getTablePlacementBounds(diningArea: any, zones: any, margin: number) {
  const zoneArr = Array.isArray(zones) ? zones : Object.values(zones || {});
  const topZones = zoneArr.filter((z: any) => z.y === 0);
  const bottomZones = zoneArr.filter((z: any) => z.y + z.height === diningArea.height);

  const topZoneMaxY = topZones.length > 0 ? Math.max(...topZones.map((z: any) => z.y + z.height)) : 0;
  const bottomZoneMinY = bottomZones.length > 0 ? Math.min(...bottomZones.map((z: any) => z.y)) : diningArea.height;

  const startX = diningArea.x + margin;
  const startY = diningArea.y + margin + topZoneMaxY;
  const endX = diningArea.x + diningArea.width - margin;
  const endY = bottomZoneMinY - margin;

  return { startX, startY, endX, endY };
}

// Rectangle overlap helper
function rectsOverlap(a: any, b: any) {
  return !(a.x + a.width <= b.x || a.x >= b.x + b.width || a.y + a.height <= b.y || a.y >= b.y + b.height);
}
