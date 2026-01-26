/**
 * @file RobotMap.jsx
 * @description Konva-based restaurant map simulator with three robots + guest and MockAPI-like step logic
 */

import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useSelector } from 'react-redux';
import { Stage, Layer, Rect, Circle, Text, Group } from 'react-konva';
import { VegaCard } from '@heartlandone/vega-react';
import { generateTableLayout, getMainAisles, getKitchenPosition } from '../utils/layoutGenerator.js';
import { buildGrid, findPath, getObstacles } from '../utils/pathfinding.js';

// Simulation parameters (can later be wired to Redux/props)
const DEFAULT_SPEED = 80; // px/s base speed per robot
const ACCELERATE = 1; // global time scaling factor
const TRAIL_DIST = 40; // px guest trails behind robot A
const MIN_HEADWAY_SEC = 1.5;
const PREDICT_HORIZON_SEC = 3;
const PREDICT_STEP_SEC = 0.2;
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

// MockAPI-like step logic --------------------------------------------------
function simulateStep(world, dtRealSec, speedRate, safetyBuffer) {
  if (!world) return world;

  const dtSec = dtRealSec * ACCELERATE;
  const speed = DEFAULT_SPEED * speedRate * ACCELERATE;

  const robots = { ...world.robots };

  // First pass: update wait timers and propose new distances
  const proposals = {};
  for (const id of Object.keys(robots)) {
    const r = { ...robots[id] };

    if (r.status === 'FINISHED') {
      proposals[id] = r.distanceAlong;
      robots[id] = r;
      continue;
    }

    if (r.waitRemainingSec > 0) {
      const remaining = Math.max(0, r.waitRemainingSec - dtSec);
      r.waitRemainingSec = remaining;
      if (remaining === 0 && r.status === 'WAITING') {
        r.status = 'ACTIVE';
      }
      proposals[id] = r.distanceAlong;
      robots[id] = r;
      continue;
    }

    if (r.status === 'WAITING') {
      // Should not happen without waitRemainingSec, but be safe
      proposals[id] = r.distanceAlong;
      robots[id] = r;
      continue;
    }

    // ACTIVE
    const nextDist = Math.min(r.distanceAlong + speed * dtSec, r.path.totalLength);
    r.distanceAlong = nextDist;
    if (nextDist >= r.path.totalLength) {
      r.status = 'FINISHED';
    }
    proposals[id] = r.distanceAlong;
    robots[id] = r;
  }

  // Predict collisions and enforce headway
  const ids = Object.keys(robots);
  for (let i = 0; i < ids.length; i++) {
    for (let j = i + 1; j < ids.length; j++) {
      const idA = ids[i];
      const idB = ids[j];
      const A = robots[idA];
      const B = robots[idB];

      if (A.status === 'FINISHED' || B.status === 'FINISHED') continue;

      let willCollide = false;
      let aheadId = idA;

      for (
        let t = 0;
        t <= PREDICT_HORIZON_SEC && !willCollide;
        t += PREDICT_STEP_SEC
      ) {
        const dA = Math.min(
          A.path.totalLength,
          proposals[idA] + speed * t
        );
        const dB = Math.min(
          B.path.totalLength,
          proposals[idB] + speed * t
        );

        const pA = pointAlongPath(A.path, dA);
        const pB = pointAlongPath(B.path, dB);
        if (!pA || !pB) continue;

        const dx = pA.x - pB.x;
        const dy = pA.y - pB.y;
        const dist = Math.hypot(dx, dy);
        if (dist < ROBOT_RADIUS * 2 + safetyBuffer) {
          willCollide = true;
          // later arrival (further from goal) yields
          const remainingA = A.path.totalLength - proposals[idA];
          const remainingB = B.path.totalLength - proposals[idB];
          aheadId = remainingA < remainingB ? idA : idB;
        }
      }

      if (willCollide) {
        const yieldId = aheadId === idA ? idB : idA;
        const r = { ...robots[yieldId] };
        // revert this step move
        r.distanceAlong = proposals[yieldId] - speed * dtSec;
        if (r.distanceAlong < 0) r.distanceAlong = 0;
        r.status = 'WAITING';
        r.waitRemainingSec = Math.max(r.waitRemainingSec || 0, MIN_HEADWAY_SEC);
        proposals[yieldId] = r.distanceAlong;
        robots[yieldId] = r;
      }
    }
  }

  // Escort guest trailing robot A
  const robotA = robots['A'];
  let guest = { ...world.guest };
  if (robotA && robotA.path.totalLength > 0) {
    // Guest tries to stay TRAIL_DIST behind A
    const targetDist = Math.max(0, robotA.distanceAlong - TRAIL_DIST);
    const guestSpeed = speed * 0.9;
    const desiredMove = targetDist - guest.distanceAlong;
    const maxMove = guestSpeed * dtSec;
    const delta = Math.max(
      -maxMove,
      Math.min(maxMove, desiredMove)
    );
    guest.distanceAlong = Math.max(0, guest.distanceAlong + delta);

    const gap = robotA.distanceAlong - guest.distanceAlong;
    if (gap > TRAIL_DIST * 1.6) {
      // leash_stretch event -> A waits a bit
      const aCopy = { ...robotA };
      aCopy.status = 'WAITING';
      aCopy.waitRemainingSec = Math.max(
        aCopy.waitRemainingSec || 0,
        MIN_HEADWAY_SEC
      );
      robots['A'] = aCopy;
    }
  }

  return {
    ...world,
    robots,
    guest,
    time: world.time + dtSec,
  };
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

  const obstacles = useMemo(
    () => getObstacles(tables, config.zones, config.safetyBuffer),
    [tables, config.zones, config.safetyBuffer]
  );

  useEffect(() => {
    buildGrid(
      { width: config.mapWidth, height: config.mapHeight },
      obstacles,
      config.gridSize
    );
  }, [config.mapWidth, config.mapHeight, obstacles, config.gridSize]);

  // World state ---------------------------------------------------------
  const [world, setWorld] = useState(null);

  const initializeWorld = useCallback(() => {
    if (!tables || tables.length === 0) return;

    const targetTableA =
      tables.find((t) => t.id === selectedTable) || tables[0];
    const otherTables = tables.filter((t) => t.id !== targetTableA.id);

    const tableB = otherTables[0] || targetTableA;
    const tableC = otherTables[1] || targetTableA;

    // Helper: ensure we always have at least a straight-line path
    const ensurePath = (start, end) => {
      const p = findPath(start, end) || [];
      if (!p || p.length < 2) {
        return [start, end];
      }
      return p;
    };

    const pathAForward = ensurePath(receptionPos, targetTableA.center);
    const pathABack = ensurePath(targetTableA.center, receptionPos);
    const pathA = buildPathMetrics([
      ...pathAForward,
      ...pathABack.slice(1),
    ]);

    const pathBForward = ensurePath(kitchenPos, tableB.center);
    const pathBBack = ensurePath(tableB.center, kitchenPos);
    const pathB = buildPathMetrics([
      ...pathBForward,
      ...pathBBack.slice(1),
    ]);

    const pathCForward = ensurePath(kitchenPos, tableC.center);
    const pathCBack = ensurePath(tableC.center, kitchenPos);
    const pathC = buildPathMetrics([
      ...pathCForward,
      ...pathCBack.slice(1),
    ]);

    console.log('Init paths length:', {
      A: pathA.totalLength,
      B: pathB.totalLength,
      C: pathC.totalLength,
    });

    const robots = {
      A: {
        id: 'A',
        role: 'escort',
        path: pathA,
        distanceAlong: 0,
        status: 'ACTIVE',
        waitRemainingSec: 0,
      },
      B: {
        id: 'B',
        role: 'delivery',
        path: pathB,
        distanceAlong: 0,
        status: 'ACTIVE',
        waitRemainingSec: 0,
      },
      C: {
        id: 'C',
        role: 'delivery',
        path: pathC,
        distanceAlong: 0,
        status: 'ACTIVE',
        waitRemainingSec: 0,
      },
    };

    const initialWorld = {
      robots,
      guest: { distanceAlong: 0 },
      time: 0,
    };

    worldRef.current = initialWorld;
    setWorld(initialWorld);
    // reset time baseline so first frame dt = 0
    lastTimeRef.current = 0;
  }, [tables, selectedTable, receptionPos, kitchenPos]);

  useEffect(() => {
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

    if (!isRunningRef.current || isPausedRef.current || !worldRef.current) {
      frameRef.current = requestAnimationFrame(animate);
      return;
    }

    const dtSec = dtMs / 1000;

    setWorld((prev) => {
      const baseWorld = prev || worldRef.current;
      if (!baseWorld) return prev;
      const nextWorld = simulateStep(
        baseWorld,
        dtSec,
        speedRateRef.current,
        safetyBufferRef.current
      );
      worldRef.current = nextWorld;
      return nextWorld;
    });

    frameRef.current = requestAnimationFrame(animate);
  }, []);

  useEffect(() => {
    frameRef.current = requestAnimationFrame(animate);
    return () => {
      if (frameRef.current) cancelAnimationFrame(frameRef.current);
    };
  }, [animate]);

  // Notify when all robots finished
  useEffect(() => {
    if (!world) return;
    const allFinished = Object.values(world.robots || {}).every(
      (r) => r.status === 'FINISHED'
    );
    if (allFinished) {
      onRouteComplete?.();
    }
  }, [world, onRouteComplete]);

  const { mapWidth, mapHeight, zones, diningArea } = config;

  // Derive drawable positions
  const robotDrawData = useMemo(() => {
    if (!world) return [];
    return Object.values(world.robots).map((r) => ({
      id: r.id,
      role: r.role,
      status: r.status,
      pos: pointAlongPath(r.path, r.distanceAlong) || kitchenPos,
    }));
  }, [world, kitchenPos]);

  const guestPos = useMemo(() => {
    if (!world || !world.robots['A']) return null;
    return (
      pointAlongPath(world.robots['A'].path, world.guest.distanceAlong) ||
      receptionPos
    );
  }, [world, receptionPos]);

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
              return (
                <Group
                  key={table.id}
                  onClick={() => onTableSelect?.(table.id)}
                >
                  <Circle
                    x={table.center.x}
                    y={table.center.y}
                    radius={table.radius}
                    fill={fillColor}
                    stroke={strokeColor}
                    strokeWidth={4}
                  />
                  <Text
                    x={table.center.x - 20}
                    y={table.center.y - 6}
                    width={40}
                    align="center"
                    fill="white"
                    fontSize={12}
                    fontStyle="bold"
                    text={`T${table.id}`}
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
                    x={table.center.x - table.rectWidth / 2}
                    y={table.center.y - table.rectHeight / 2}
                    width={table.rectWidth}
                    height={table.rectHeight}
                    fill={fillColor}
                    stroke={strokeColor}
                    strokeWidth={4}
                    cornerRadius={5}
                  />
                  <Text
                    x={table.center.x - 20}
                    y={table.center.y - 8}
                    width={40}
                    align="center"
                    fill="white"
                    fontSize={12}
                    fontStyle="bold"
                    text={`T${table.id}`}
                  />
                </Group>
              );
            }

            // square fallback
            return (
              <Group
                key={table.id}
                onClick={() => onTableSelect?.(table.id)}
              >
                <Rect
                  x={table.center.x - table.side / 2}
                  y={table.center.y - table.side / 2}
                  width={table.side}
                  height={table.side}
                  fill={fillColor}
                  stroke={strokeColor}
                  strokeWidth={4}
                  cornerRadius={5}
                />
                <Text
                  x={table.center.x - 20}
                  y={table.center.y - 8}
                  width={40}
                  align="center"
                  fill="white"
                  fontSize={12}
                  fontStyle="bold"
                  text={`T${table.id}`}
                />
              </Group>
            );
          })}

          {/* Robots */}
          {robotDrawData.map((r) => (
            <Group key={r.id} x={r.pos.x} y={r.pos.y}>
              <Circle
                radius={ROBOT_RADIUS}
                fill={
                  r.role === 'escort'
                    ? '#2196F3'
                    : r.id === 'B'
                    ? '#4CAF50'
                    : '#FF9800'
                }
                stroke="#1565C0"
                strokeWidth={2}
              />
              <Text
                x={-8}
                y={-8}
                width={16}
                align="center"
                fill="white"
                fontSize={12}
                fontStyle="bold"
                text={r.id}
              />
            </Group>
          ))}

          {/* Guest following Robot A */}
          {guestPos && (
            <Group x={guestPos.x} y={guestPos.y}>
              <Circle radius={10} fill="#E91E63" stroke="#AD1457" strokeWidth={2} />
              <Text
                x={-10}
                y={-22}
                width={20}
                align="center"
                fill="#AD1457"
                fontSize={10}
                fontStyle="bold"
                text="G"
              />
            </Group>
          )}
        </Layer>
      </Stage>
    </VegaCard>
  );
}

export default RobotMap;
