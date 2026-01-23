/**
 * @file RobotMap.jsx
 * @description Restaurant map component with auto-generated layout and pathfinding
 */

import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useSelector } from 'react-redux';
import { VegaCard } from '@heartlandone/vega-react';
import { generateTableLayout, getMainAisles, getKitchenPosition } from '../utils/layoutGenerator.js';
import { buildGrid, findPath, getObstacles } from '../utils/pathfinding.js';

/**
 * RobotMap component - renders restaurant floor plan with robot animation
 * @param {Object} props - Component props
 * @param {boolean} props.isRunning - Whether animation is running
 * @param {Function} props.onRouteComplete - Callback when route animation completes
 * @param {string} props.currentAction - Current action type (Order, Call, etc.)
 * @param {number|null} props.selectedTable - Selected table ID for delivery
 * @param {Function} props.onTableSelect - Callback when a table is selected
 * @param {boolean} props.isPaused - Whether animation is paused
 */
function RobotMap({ 
  isRunning, 
  onRouteComplete, 
  currentAction, 
  selectedTable,
  onTableSelect,
  isPaused = false
}) {
  const { config, tableCount, speedRate } = useSelector((state) => state);
  
  const [robotPosition, setRobotPosition] = useState(null);
  const [currentPath, setCurrentPath] = useState([]);
  const [pathIndex, setPathIndex] = useState(0);
  
  const animationRef = useRef(null);
  const lastTimeRef = useRef(0);
  const isPausedRef = useRef(isPaused);

  // Update pause ref
  useEffect(() => {
    isPausedRef.current = isPaused;
  }, [isPaused]);

  // Memoize table layout
  const tables = useMemo(() => {
    return generateTableLayout(config, tableCount);
  }, [config, tableCount]);

  // Memoize main aisles
  const aisles = useMemo(() => {
    return getMainAisles(config);
  }, [config]);

  // Memoize kitchen position
  const kitchenPos = useMemo(() => {
    return getKitchenPosition(config);
  }, [config]);

  // Initialize robot at kitchen
  useEffect(() => {
    setRobotPosition(kitchenPos);
  }, [kitchenPos]);

  // Build pathfinding grid when layout changes
  const obstacles = useMemo(() => {
    return getObstacles(tables, config.zones, config.safetyBuffer);
  }, [tables, config.zones, config.safetyBuffer]);

  useEffect(() => {
    buildGrid(
      { width: config.mapWidth, height: config.mapHeight },
      obstacles,
      config.gridSize
    );
  }, [config.mapWidth, config.mapHeight, obstacles, config.gridSize]);

  // Calculate path when selectedTable changes and isRunning
  useEffect(() => {
    if (isRunning && selectedTable && robotPosition) {
      const targetTable = tables.find(t => t.id === selectedTable);
      if (targetTable) {
        // Path: Kitchen → Table → Kitchen
        const pathToTable = findPath(kitchenPos, targetTable.center);
        const pathBack = findPath(targetTable.center, kitchenPos);
        
        const fullPath = [...pathToTable, ...pathBack.slice(1)];
        setCurrentPath(fullPath);
        setPathIndex(0);
        setRobotPosition(kitchenPos);
      }
    }
  }, [isRunning, selectedTable, tables, kitchenPos]);

  // Animation loop
  const animate = useCallback((timestamp) => {
    if (!lastTimeRef.current) {
      lastTimeRef.current = timestamp;
    }

    if (isPausedRef.current) {
      animationRef.current = requestAnimationFrame(animate);
      return;
    }

    const deltaTime = timestamp - lastTimeRef.current;
    lastTimeRef.current = timestamp;

    setPathIndex((prevIndex) => {
      if (prevIndex >= currentPath.length - 1) {
        onRouteComplete?.();
        setCurrentPath([]);
        return 0;
      }

      const speed = 3 * speedRate * (deltaTime / 16); // Base speed adjusted by rate
      const current = currentPath[prevIndex];
      const next = currentPath[prevIndex + 1];

      if (!current || !next) return prevIndex;

      const dx = next.x - current.x;
      const dy = next.y - current.y;
      const distance = Math.sqrt(dx * dx + dy * dy);

      if (distance <= speed) {
        setRobotPosition(next);
        return prevIndex + 1;
      } else {
        const ratio = speed / distance;
        setRobotPosition((prev) => ({
          x: prev.x + dx * ratio,
          y: prev.y + dy * ratio,
        }));
        return prevIndex;
      }
    });

    animationRef.current = requestAnimationFrame(animate);
  }, [currentPath, speedRate, onRouteComplete]);

  // Start/stop animation
  useEffect(() => {
    if (isRunning && currentPath.length > 0) {
      lastTimeRef.current = 0;
      animationRef.current = requestAnimationFrame(animate);
    }

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [isRunning, currentPath, animate]);

  const { mapWidth, mapHeight, zones, diningArea } = config;

  return (
    <VegaCard padding="size-16" style={{ height: "100%", overflow: "hidden" }}>
      <svg 
        width="100%" 
        height="100%" 
        viewBox={`0 0 ${mapWidth} ${mapHeight}`}
        style={{ maxHeight: '100%' }}
      >
        {/* Background */}
        <rect x="0" y="0" width={mapWidth} height={mapHeight} fill="#f5f5f5" />

        {/* Dining area */}
        <rect
          x={diningArea.x}
          y={diningArea.y}
          width={diningArea.width}
          height={diningArea.height}
          fill="#fff9e6"
          stroke="#ddd"
          strokeWidth="1"
        />

        {/* Main aisles (cross pattern) */}
        <rect
          x={aisles.horizontal.x}
          y={aisles.horizontal.y}
          width={aisles.horizontal.width}
          height={aisles.horizontal.height}
          fill="none"
          stroke="#ccc"
          strokeWidth="2"
          strokeDasharray="10,5"
        />
        <rect
          x={aisles.vertical.x}
          y={aisles.vertical.y}
          width={aisles.vertical.width}
          height={aisles.vertical.height}
          fill="none"
          stroke="#ccc"
          strokeWidth="2"
          strokeDasharray="10,5"
        />

        {/* Fixed zones */}
        {Object.entries(zones).map(([key, zone]) => (
          <g key={key}>
            <rect
              x={zone.x}
              y={zone.y}
              width={zone.width}
              height={zone.height}
              fill={zone.color}
              rx="5"
            />
            <text
              x={zone.x + zone.width / 2}
              y={zone.y + zone.height / 2 + 5}
              textAnchor="middle"
              fill="white"
              fontSize="14"
              fontWeight="bold"
            >
              {zone.label}
            </text>
          </g>
        ))}

        {/* Draw route path */}
        {currentPath.length > 1 && (
          <polyline
            points={currentPath.map(p => `${p.x},${p.y}`).join(' ')}
            fill="none"
            stroke="#4CAF50"
            strokeWidth="3"
            strokeDasharray="8,4"
            opacity="0.7"
          />
        )}

        {/* Draw tables */}
        {tables.map(table => {
          const isSelected = selectedTable === table.id;
          const fillColor = isSelected ? '#5D4037' : '#8B4513';
          
          return (
            <g 
              key={table.id} 
              onClick={() => onTableSelect?.(table.id)}
              style={{ cursor: 'pointer' }}
            >
              {table.shape === 'round' ? (
                <circle
                  cx={table.center.x}
                  cy={table.center.y}
                  r={table.radius}
                  fill={fillColor}
                  stroke={isSelected ? '#FFD700' : 'none'}
                  strokeWidth={isSelected ? 4 : 0}
                />
              ) : table.shape === 'rect' ? (
                <rect
                  x={table.center.x - table.rectWidth / 2}
                  y={table.center.y - table.rectHeight / 2}
                  width={table.rectWidth}
                  height={table.rectHeight}
                  fill={fillColor}
                  rx="5"
                  stroke={isSelected ? '#FFD700' : 'none'}
                  strokeWidth={isSelected ? 4 : 0}
                />
              ) : (
                <rect
                  x={table.center.x - table.side / 2}
                  y={table.center.y - table.side / 2}
                  width={table.side}
                  height={table.side}
                  fill={fillColor}
                  rx="5"
                  stroke={isSelected ? '#FFD700' : 'none'}
                  strokeWidth={isSelected ? 4 : 0}
                />
              )}
              <text
                x={table.center.x}
                y={table.center.y + 5}
                textAnchor="middle"
                fill="white"
                fontSize="12"
                fontWeight="bold"
              >
                T{table.id}
              </text>
              {/* Capacity indicator */}
              <text
                x={table.center.x}
                y={table.center.y + 18}
                textAnchor="middle"
                fill="white"
                fontSize="9"
                opacity="0.8"
              >
                ({table.capacity})
              </text>
            </g>
          );
        })}

        {/* Draw robot */}
        {robotPosition && (
          <g transform={`translate(${robotPosition.x}, ${robotPosition.y})`}>
            <circle r="18" fill="#2196F3" stroke="#1565C0" strokeWidth="2" />
            <text
              y="6"
              textAnchor="middle"
              fill="white"
              fontSize="18"
            >
              🤖
            </text>
          </g>
        )}

        {/* Status indicator */}
        {isRunning && currentAction && (
          <g>
            <rect
              x={mapWidth / 2 - 80}
              y={10}
              width={160}
              height={30}
              fill="rgba(33, 150, 243, 0.9)"
              rx="15"
            />
            <text
              x={mapWidth / 2}
              y={30}
              textAnchor="middle"
              fill="white"
              fontSize="12"
              fontWeight="bold"
            >
              {currentAction} → T{selectedTable}
            </text>
          </g>
        )}
      </svg>
    </VegaCard>
  );
}

export default RobotMap;
