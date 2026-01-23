/**
 * @file pathfinding.js
 * @description Grid-based A* pathfinding wrapper using the 'pathfinding' library
 */

import PF from 'pathfinding';

let gridInstance = null;
let gridSize = 20;
let mapWidth = 0;
let mapHeight = 0;

/**
 * Build a navigation grid from the map and obstacles
 * @param {Object} map - Map dimensions {width, height}
 * @param {Array<Object>} obstacles - Array of obstacle bounds {x, y, width, height}
 * @param {number} cellSize - Grid cell size in pixels (default: 20)
 * @returns {PF.Grid} Pathfinding grid instance
 */
export function buildGrid(map, obstacles = [], cellSize = 20) {
  gridSize = cellSize;
  mapWidth = map.width;
  mapHeight = map.height;

  const cols = Math.ceil(mapWidth / gridSize);
  const rows = Math.ceil(mapHeight / gridSize);

  gridInstance = new PF.Grid(cols, rows);

  // Mark obstacle cells as unwalkable
  for (const obs of obstacles) {
    const startCol = Math.floor(obs.x / gridSize);
    const startRow = Math.floor(obs.y / gridSize);
    const endCol = Math.ceil((obs.x + obs.width) / gridSize);
    const endRow = Math.ceil((obs.y + obs.height) / gridSize);

    for (let row = startRow; row < endRow; row++) {
      for (let col = startCol; col < endCol; col++) {
        if (col >= 0 && col < cols && row >= 0 && row < rows) {
          gridInstance.setWalkableAt(col, row, false);
        }
      }
    }
  }

  return gridInstance;
}

/**
 * Find path between two pixel coordinates
 * @param {Object} startPx - Start position {x, y} in pixels
 * @param {Object} goalPx - Goal position {x, y} in pixels
 * @param {Object} options - Optional pathfinding options
 * @returns {Array<{x: number, y: number}>} Array of pixel coordinates forming the path
 */
export function findPath(startPx, goalPx, options = {}) {
  if (!gridInstance) {
    console.warn('Grid not initialized. Call buildGrid first.');
    return [];
  }

  const startCol = Math.floor(startPx.x / gridSize);
  const startRow = Math.floor(startPx.y / gridSize);
  const goalCol = Math.floor(goalPx.x / gridSize);
  const goalRow = Math.floor(goalPx.y / gridSize);

  const cols = gridInstance.width;
  const rows = gridInstance.height;

  // Clamp to grid bounds
  const clampCol = (c) => Math.max(0, Math.min(c, cols - 1));
  const clampRow = (r) => Math.max(0, Math.min(r, rows - 1));

  const sCol = clampCol(startCol);
  const sRow = clampRow(startRow);
  const gCol = clampCol(goalCol);
  const gRow = clampRow(goalRow);

  // Clone grid for pathfinding (required by pathfinding lib)
  const gridClone = gridInstance.clone();

  // Ensure start and goal are walkable
  gridClone.setWalkableAt(sCol, sRow, true);
  gridClone.setWalkableAt(gCol, gRow, true);

  // Use A* finder with no diagonal movement
  const finder = new PF.AStarFinder({
    allowDiagonal: false,
    dontCrossCorners: true,
    ...options,
  });

  const rawPath = finder.findPath(sCol, sRow, gCol, gRow, gridClone);

  // Convert grid coordinates back to pixel coordinates (center of cells)
  const pixelPath = rawPath.map(([col, row]) => ({
    x: col * gridSize + gridSize / 2,
    y: row * gridSize + gridSize / 2,
  }));

  // Smooth the path by removing collinear points
  return smoothPath(pixelPath);
}

/**
 * Smooth path by removing collinear points
 * @param {Array<{x: number, y: number}>} path - Raw pixel path
 * @returns {Array<{x: number, y: number}>} Smoothed path
 */
function smoothPath(path) {
  if (path.length <= 2) return path;

  const smoothed = [path[0]];

  for (let i = 1; i < path.length - 1; i++) {
    const prev = smoothed[smoothed.length - 1];
    const curr = path[i];
    const next = path[i + 1];

    // Check if direction changes
    const dx1 = curr.x - prev.x;
    const dy1 = curr.y - prev.y;
    const dx2 = next.x - curr.x;
    const dy2 = next.y - curr.y;

    // If direction changes, keep this point
    if (dx1 !== dx2 || dy1 !== dy2) {
      smoothed.push(curr);
    }
  }

  smoothed.push(path[path.length - 1]);
  return smoothed;
}

/**
 * Get obstacles from tables and zones for grid building
 * @param {Array<Object>} tables - Table objects with bounds
 * @param {Object} zones - Zone configuration from MapConfig
 * @param {number} buffer - Safety buffer around obstacles
 * @returns {Array<Object>} Array of obstacle bounds
 */
export function getObstacles(tables, zones, buffer = 10) {
  const obstacles = [];

  // Add table obstacles with buffer
  for (const table of tables) {
    if (table.bounds) {
      obstacles.push({
        x: table.bounds.x - buffer,
        y: table.bounds.y - buffer,
        width: table.bounds.width + buffer * 2,
        height: table.bounds.height + buffer * 2,
      });
    }
  }

  // Add zone obstacles (except kitchen which is the destination)
  for (const [key, zone] of Object.entries(zones)) {
    if (key !== 'kitchen') {
      obstacles.push({
        x: zone.x,
        y: zone.y,
        width: zone.width,
        height: zone.height,
      });
    }
  }

  return obstacles;
}

/**
 * Get current grid info for debugging
 * @returns {Object|null} Grid info or null if not initialized
 */
export function getGridInfo() {
  if (!gridInstance) return null;
  return {
    cols: gridInstance.width,
    rows: gridInstance.height,
    cellSize: gridSize,
    mapWidth,
    mapHeight,
  };
}
