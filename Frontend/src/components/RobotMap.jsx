/**
 * @file RobotMap.jsx
 * @description Konva-based restaurant map simulator with three robots + guest and MockAPI-like step logic
 */

import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useSelector } from 'react-redux';
import { Stage, Layer, Rect, Circle, Text, Group, Line } from 'react-konva';
import { VegaCard } from '@heartlandone/vega-react';
import { generateTableLayout, getMainAisles, getKitchenPosition } from '../utils/layoutGenerator.js';
import { postMap, planRoute, getMap } from '../utils/apiClient.js';
import { buildMapPayload, defaultStartBottomRight } from '../utils/mapSerializer.js';
// Local pathfinding removed; backend planning used exclusively

// Simulation parameters (can later be wired to Redux/props)
const DEFAULT_SPEED = 80; // px/s base speed per robot
const ROBOT_RADIUS = 16;

function getZoneCenter(zones, key) {
  const z = zones[key];
  if (!z) return { x: 0, y: 0 };
  return { x: z.x + z.width / 2, y: z.y + z.height / 2 };
}

// Path helpers -------------------------------------------------------------
function buildPathMetrics(points) {
  if (!points || points.length === 0) {
    return { points: [], segmentLengths: [], totalLength: 0 };
  }

  const segmentLengths = [];
  let total = 0;
  for (let i = 0; i < points.length - 1; i++) {
    const dx = points[i + 1].x - points[i].x;
    const dy = points[i + 1].y - points[i].y;
    const len = Math.hypot(dx, dy);
    segmentLengths.push(len);
    total += len;
  }
  return { points, segmentLengths, totalLength: total };
}

function pointAlongPath(path, distance) {
  const { points, segmentLengths, totalLength } = path;
  if (!points.length) return null;
  if (distance <= 0) return points[0];
  if (distance >= totalLength) return points[points.length - 1];

  let remaining = distance;
  for (let i = 0; i < segmentLengths.length; i++) {
    const segLen = segmentLengths[i];
    if (remaining > segLen) {
      remaining -= segLen;
    } else {
      const t = remaining / segLen;
      const a = points[i];
      const b = points[i + 1];
      return {
        x: a.x + (b.x - a.x) * t,
        y: a.y + (b.y - a.y) * t,
      };
    }
  }
  return points[points.length - 1];
}

/**
 * RobotMap component - Konva-based simulator
 */
function RobotMap({
  isRunning,
  onRouteComplete,
  currentAction,
  selectedTable,
  onTableSelect,
  isPaused = false,
}) {
  const { config, tableCount, speedRate } = useSelector((state) => state);

  const isPausedRef = useRef(isPaused);
  const lastTimeRef = useRef(0);
  const frameRef = useRef(null);
  const worldRef = useRef(null);
  const isRunningRef = useRef(isRunning);
  const speedRateRef = useRef(speedRate);
  const safetyBufferRef = useRef(config.safetyBuffer);
  const lastPathPointRef = useRef();

  useEffect(() => {
    isPausedRef.current = isPaused;
  }, [isPaused]);

  useEffect(() => {
    isRunningRef.current = isRunning;
  }, [isRunning]);

  useEffect(() => {
    speedRateRef.current = speedRate;
  }, [speedRate]);

  useEffect(() => {
    safetyBufferRef.current = config.safetyBuffer;
  }, [config.safetyBuffer]);

  // Layout --------------------------------------------------------------
  const tables = useMemo(() => {
    return generateTableLayout(config, tableCount);
  }, [config, tableCount]);

  const aisles = useMemo(() => getMainAisles(config), [config]);

  const kitchenPos = useMemo(() => getKitchenPosition(config), [config]);
  const receptionPos = useMemo(
    () => getZoneCenter(config.zones, 'reception'),
    [config.zones]
  );


  useEffect(() => {
    // No-op: uploading map happens on Start
  }, [config, tables]);

    // Local obstacle grid disabled; backend handles avoidance

  // Robot state from backend plan --------------------------------------
  const [robotPath, setRobotPath] = useState(buildPathMetrics([]));
  const [distanceAlong, setDistanceAlong] = useState(0);
  const [mapId, setMapId] = useState(null);

  const initializeWorld = useCallback(async () => {
    // Upload current map to backend whenever Start is pressed (always)
    try {
      debugger
      const payload = buildMapPayload(config, tables);
      console.log('Start pressed: posting map payload to backend');
      const upload = await postMap(payload);
      if (upload && upload.mapId) {
        console.log('Map uploaded with mapId:', upload.mapId);
        setMapId(upload.mapId);
      }
    } catch (e) {
      console.warn('postMap on start failed:', e);
    }

    // If we have no tables, skip planning but keep robot idle
    if (!tables || tables.length === 0) {
      console.warn('No tables available; skipping planning after map upload');
      setRobotPath(buildPathMetrics([]));
      setDistanceAlong(0);
      lastTimeRef.current = 0;
      return;
    }

  }, [tables, selectedTable, receptionPos, kitchenPos, config]);


  const triggerReplan = async () => {
    if(!selectedTable) return;
    const targetTableA = tables.find((t) => t.id === selectedTable) || tables[9];
    
    // Removed local ensurePath fallback; use backend plan only

    // Try server-side planning from bottom-right to target table
    let serverPath = null;
    try {
      let start = null;
      if (lastPathPointRef.current) {
        start = lastPathPointRef.current;
      } else {
        start = defaultStartBottomRight(config);
      }
      console.log('Planning route for table:', targetTableA?.id);
      const res = await planRoute({ 
        start, tableId: String(targetTableA.id), robotRadius: 16, 
        dockSide: "Bottom", 
        dockOffset: 16

      });
      if (res && res.success && Array.isArray(res.path) && res.path.length > 1) {
        serverPath = res.path.map(p => ({ x: p.x, y: p.y }));
        console.log('Received planned path points:', serverPath.length);
        lastPathPointRef.current = serverPath[serverPath.length - 1];
      } else {
        console.warn('Plan response unsuccessful or empty:', res);
      }
    } catch (e) {
      console.warn('planRoute failed:', e);
    }

    const pathMetrics = buildPathMetrics(serverPath || []);
    setRobotPath(pathMetrics);
    setDistanceAlong(0);
    lastTimeRef.current = 0;
  }

  useEffect(() => {
    triggerReplan();
  }, [selectedTable]);


  useEffect(() => {
    if (selectedTable == null) return;
    
  }, [selectedTable])

  useEffect(() => {
    debugger
    if (isRunning) {
      initializeWorld();
    }
  }, [isRunning, initializeWorld]);

  // Animation loop ------------------------------------------------------
  const animate = useCallback((timestamp) => {
    if (!lastTimeRef.current) {
      lastTimeRef.current = timestamp;
    }
    const dtMs = timestamp - lastTimeRef.current;
    lastTimeRef.current = timestamp;
    if (!isRunningRef.current || isPausedRef.current) {
      frameRef.current = requestAnimationFrame(animate);
      return;
    }
    const dtSec = dtMs / 1000;
    setDistanceAlong((d) => Math.min(d + DEFAULT_SPEED * speedRateRef.current * dtSec, robotPath.totalLength));
    frameRef.current = requestAnimationFrame(animate);
  }, [robotPath.totalLength]);

  useEffect(() => {
    frameRef.current = requestAnimationFrame(animate);
    return () => {
      if (frameRef.current) cancelAnimationFrame(frameRef.current);
    };
  }, [animate]);

  // Notify when robot finished
  useEffect(() => {
    if (robotPath.totalLength > 0 && distanceAlong >= robotPath.totalLength) {
      onRouteComplete?.();
    }
  }, [robotPath.totalLength, distanceAlong, onRouteComplete]);

  const { mapWidth, mapHeight, zones, diningArea } = config;
  const robotPos = useMemo(() => pointAlongPath(robotPath, distanceAlong), [robotPath, distanceAlong]);
  console.log("robotPos", robotPos)

  return (
    <VegaCard padding="size-16" style={{ height: '100%', overflow: 'hidden' }}>
      <Stage width={mapWidth} height={mapHeight}>
        <Layer>
          {/* Background */}
          <Rect x={0} y={0} width={mapWidth} height={mapHeight} fill="#f5f5f5" />

          {/* Dining area */}
          <Rect
            x={diningArea.x}
            y={diningArea.y}
            width={diningArea.width}
            height={diningArea.height}
            fill="#fff9e6"
            stroke="#ddd"
            strokeWidth={1}
          />

          {/* Main aisles (cross pattern) */}
          <Rect
            x={aisles.horizontal.x}
            y={aisles.horizontal.y}
            width={aisles.horizontal.width}
            height={aisles.horizontal.height}
            stroke="#ccc"
            strokeWidth={2}
            dash={[10, 5]}
          />
          <Rect
            x={aisles.vertical.x}
            y={aisles.vertical.y}
            width={aisles.vertical.width}
            height={aisles.vertical.height}
            stroke="#ccc"
            strokeWidth={2}
            dash={[10, 5]}
          />

          {/* Fixed zones */}
          {Object.entries(zones).map(([key, zone]) => (
            <Group key={key}>
              <Rect
                x={zone.x}
                y={zone.y}
                width={zone.width}
                height={zone.height}
                fill={zone.color}
                cornerRadius={5}
              />
              <Text
                x={zone.x}
                y={zone.y + zone.height / 2 - 8}
                width={zone.width}
                align="center"
                fill="white"
                fontSize={14}
                fontStyle="bold"
                text={zone.label}
              />
            </Group>
          ))}

          {/* Tables */}
          {tables.map((table) => {
            const isSelected = selectedTable === table.id;
            const fillColor = isSelected ? '#5D4037' : '#8B4513';
            const strokeColor = isSelected ? '#FFD700' : 'transparent';

            if (table.shape === 'round') {
              const cx = table.x + (table.radius || table.width / 2);
              const cy = table.y + (table.radius || table.height / 2);
              const r = table.radius || Math.min(table.width, table.height) / 2;
              return (
                <Group
                  key={table.id}
                  onClick={() => onTableSelect?.(table.id)}
                >
                  <Circle
                    x={cx}
                    y={cy}
                    radius={r}
                    fill={fillColor}
                    stroke={strokeColor}
                    strokeWidth={4}
                  />
                  <Text
                    x={table.x}
                    y={table.y + (table.height / 2) - 6}
                    width={table.width}
                    align="center"
                    fill="white"
                    fontSize={12}
                    fontStyle="bold"
                    text={`T${table.id}` + (table.capacity ? `(${table.capacity})` : '')}
                  />
                </Group>
              );
            }

            if (table.shape === 'rect') {
              return (
                <Group
                  key={table.id}
                  onClick={() => onTableSelect?.(table.id)}
                >
                  <Rect
                    x={table.x}
                    y={table.y}
                    width={table.width}
                    height={table.height}
                    fill={fillColor}
                    stroke={strokeColor}
                    strokeWidth={4}
                    cornerRadius={5}
                  />
                  <Text
                    x={table.x}
                    y={table.y + (table.height / 2) - 8}
                    width={table.width}
                    align="center"
                    fill="white"
                    fontSize={12}
                    fontStyle="bold"
                    text={`T${table.id}` + (table.capacity ? `(${table.capacity})` : '')}
                  />
                </Group>
              );
            }

            // square fallback (or other shapes with width/height)
            return (
              <Group
                key={table.id}
                onClick={() => onTableSelect?.(table.id)}
              >
                <Rect
                  x={table.x}
                  y={table.y}
                  width={table.width}
                  height={table.height}
                  fill={fillColor}
                  stroke={strokeColor}
                  strokeWidth={4}
                  cornerRadius={5}
                />
                <Text
                  x={table.x}
                  y={table.y + (table.height / 2) - 8}
                  width={table.width}
                  align="center"
                  fill="white"
                  fontSize={12}
                  fontStyle="bold"
                  text={`T${table.id}`}
                />
              </Group>
            );
          })}

         

          {/* Removed guest logic; only single robot follows backend path */}
        </Layer>
        <Layer id="robot-layer">
          {robotPos && (
            <Group x={robotPos.x} y={robotPos.y}>
              <Circle radius={ROBOT_RADIUS} fill="#2196F3" stroke="#1565C0" strokeWidth={2} />
              <Text x={-8} y={-8} width={16} align="center" fill="white" fontSize={12} fontStyle="bold" text="R" />
            </Group>
          )}
        </Layer>
      </Stage>
    </VegaCard>
  );
}

export default RobotMap;
