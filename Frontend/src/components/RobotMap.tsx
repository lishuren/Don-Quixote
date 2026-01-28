/**
 * @file RobotMap.tsx
 * @description Konva-based restaurant map simulator with robots
 * Migrated from RobotMap.jsx with TypeScript support
 */

import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { Stage, Layer, Rect, Circle, Text, Group } from 'react-konva';
import { VegaCard } from '@heartlandone/vega-react';
import { generateTableLayout, getMainAisles } from '../utils/layoutGenerator';
import { postMap, planRoute } from '../utils/apiClient';
import { robotsApi } from '../services/robotsApi';
import { guestsApi } from '../services/guestsApi';
import { eventsApi } from '../services/eventsApi';
import { signalRService } from '../services/signalRService';
import type { TableDto, ZoneDto } from '../types/dtos';
import { buildMapPayload, defaultStartBottomRight } from '../utils/mapSerializer';
import type { AppState, RootState, ZoneState, GuestState } from '../store/store';
import { updateRobots, updateGuests, setMapConfig, setRunning } from '../store/store';

// Simulation parameters
const DEFAULT_SPEED = 80; // px/s base speed per robot
const ROBOT_RADIUS = 16;

interface Point {
  x: number;
  y: number;
}

type Table = TableDto;

interface Aisles {
  horizontal: { x: number; y: number; width: number; height: number };
  vertical: { x: number; y: number; width: number; height: number };
}

interface PathMetrics {
  points: Point[];
  segmentLengths: number[];
  totalLength: number;
}


interface RobotMapProps {
  isRunning: boolean;
  onRouteComplete?: () => void;
  currentAction?: string | null;
  selectedTable?: number | null;
  onTableSelect?: (tableId: number) => void;
  isPaused?: boolean;
}

function zoneBounds(zone: any) {
  if (!zone) return { x: 0, y: 0, width: 0, height: 0 };
  if (zone.bounds) return zone.bounds;
  return { x: zone.x ?? 0, y: zone.y ?? 0, width: zone.width ?? 0, height: zone.height ?? 0 };
}

function readableTextColor(hex?: string) {
  if (!hex) return '#ffffff';
  try {
    const c = hex.replace('#', '');
    const r = parseInt(c.substring(0, 2), 16);
    const g = parseInt(c.substring(2, 4), 16);
    const b = parseInt(c.substring(4, 6), 16);
    const lum = 0.299 * r + 0.587 * g + 0.114 * b;
    return lum > 180 ? '#000000' : '#ffffff';
  } catch (e) {
    return '#ffffff';
  }
}

function getZoneCenter(zones: any[], key: string): Point {
  if (!Array.isArray(zones)) return { x: 0, y: 0 };
  const z = zones.find((zz: any) => zz.name === key || (zz.type && zz.type.toLowerCase() === key.toLowerCase()));
  if (!z) return { x: 0, y: 0 };
  const b = zoneBounds(z);
  return { x: b.x + b.width / 2, y: b.y + b.height / 2 };
}

function buildPathMetrics(points: Point[]): PathMetrics {
  if (!points || points.length === 0) {
    return { points: [], segmentLengths: [], totalLength: 0 };
  }

  const segmentLengths: number[] = [];
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

function pointAlongPath(path: PathMetrics, distance: number): Point | null {
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

function RobotMap({
  isRunning,
  onRouteComplete,
  currentAction: _currentAction,
  selectedTable,
  onTableSelect,
  isPaused = false,
}: RobotMapProps) {
  const { mapConfig: config, tableCount, speedRate, robotCount, guestCount, robots, guests } = useSelector((state: AppState) => state);
  const dispatch = useDispatch();

  // Local guest wait list (component-level) to limit display count
  const [guestWaitList, setGuestWaitList] = useState<GuestState[]>([]);

  const isPausedRef = useRef(isPaused);
  const initializedRef = useRef(false);
  const lastTimeRef = useRef<number>(0);
  const frameRef = useRef<number | null>(null);
  const isRunningRef = useRef(isRunning);
  const speedRateRef = useRef(speedRate);
  const lastPathPointRef = useRef<Point | undefined>(undefined);
  // track which guests we've already posted arrival events for
  const postedGuestsRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    isPausedRef.current = isPaused;
  }, [isPaused]);

  useEffect(() => {
    isRunningRef.current = isRunning;
  }, [isRunning]);

  useEffect(() => {
    speedRateRef.current = speedRate;
  }, [speedRate]);

  // Layout: generate full map config (includes tables and zones)
  const generatedMap = useMemo(() => generateTableLayout(config, tableCount) as any, [config, tableCount]);
  const tables = useMemo<Table[]>(() => (generatedMap?.tables ?? []) as Table[], [generatedMap]);

  const aisles = useMemo<Aisles>(() => {
    const zonesArr = (generatedMap?.zones || []) as any[];
    const aisleH = zonesArr.find((z) => z.name === 'MainAisleH');
    const aisleV = zonesArr.find((z) => z.name === 'MainAisleV');
    return {
      horizontal: aisleH ? { x: aisleH.x, y: aisleH.y, width: aisleH.width, height: aisleH.height } : getMainAisles(config).horizontal,
      vertical: aisleV ? { x: aisleV.x, y: aisleV.y, width: aisleV.width, height: aisleV.height } : getMainAisles(config).vertical,
    } as Aisles;
  }, [generatedMap, config]);


  // Robot state from backend plan
  const [robotPath, setRobotPath] = useState<PathMetrics>(buildPathMetrics([]));
  const [distanceAlong, setDistanceAlong] = useState(0);
  const [zoneMap, setZoneMap] = useState<Record<string, ZoneState>>({});
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const [mapId, setMapId] = useState<string | null>(null);

  useEffect(() => {
    const zoneMapTemp: Record<string, ZoneState> = {};
    config.zones.forEach((z: ZoneState) => {
      zoneMapTemp[z.type] = z;
    });
    setZoneMap(zoneMapTemp);
},[config.zones])

  // Keep a component-level wait list = first 20 guests in global state
  useEffect(() => {
    try {
      if (Array.isArray(guests)) {
        const wait = guests.slice(0, 20);
        setGuestWaitList(wait);
        // Post GuestArrived events for new guests only
        wait.forEach((g) => {
          const key = `${g.id ?? g.name ?? 'anon'}-${g.partySize ?? 1}`;
          if (!postedGuestsRef.current.has(key)) {
            // Fire-and-forget the guest-arrived event; backend will handle duplicates defensively
            eventsApi.guestArrived(g.partySize || 1, g.name || undefined).catch((err) => {
              console.warn('guestArrived event failed for', g, err);
            });
            postedGuestsRef.current.add(key);
          }
        });
      } else {
        setGuestWaitList([]);
      }
    } catch (e) {
      setGuestWaitList([]);
    }
  }, [guests]);

  const initializeWorld = useCallback(async () => {
    if (initializedRef.current) return;
    initializedRef.current = true;
    try {
      // Ensure SignalR is connected before initializing world
      try {
        if (!signalRService.isConnected()) {
          console.log('SignalR not connected — attempting to connect');
          await signalRService.connect();
          console.log('SignalR connected');
        } else {
          console.log('SignalR already connected');
        }
        // mark app as running only after SignalR is connected
        dispatch(setRunning(true) as any);
      } catch (connErr) {
        console.warn('SignalR connection failed, aborting world initialization:', connErr);
        return;
      }
      const payload = generatedMap || buildMapPayload(config, tables);
      console.log('Start pressed: posting map payload to backend');
      const upload = await postMap(payload);
      if (upload && upload.entities) {
        console.log('Map uploaded with mapId:', upload.mapId);
        setMapId(upload.mapId);
        // Map API response to state format
        const zones = upload.entities.zones.map((z: any) => ({
          id: z.id,
          name: z.name,
          type: z.type,
          x: z.x,
          y: z.y,
          width: z.width,
          height: z.height,
          color: z.color,
          isNavigable: z.isNavigable,
          speedLimit: z.speedLimit
        }));
        const tables = upload.entities.tables.map((t: any) => ({
          id: t.id,
          label: t.label,
          x: t.center.screenX - t.width / 2,
          y: t.center.screenY - t.height / 2,
          width: t.width,
          height: t.height,
          shape: t.shape === 'Rectangle' ? 'square' : 'round',
          status: t.status,
          capacity: t.capacity
        }));
        dispatch(setMapConfig({ zones, tables, ...upload.mapConfig }) as any);

        // Create initial robots in storage area (batch create)
        try {
          const storageZone = (upload.entities?.zones || zones).find((z: any) => z.name === 'Storage') || zones.find((z: any) => z.name === 'Storage');
          const storageX = storageZone?.x ?? (config.mapWidth - 120);
          const storageY = storageZone?.y ?? (config.mapHeight - 80);
          const storageW = storageZone?.width ?? 100;
          const storageH = storageZone?.height ?? 50;

          const count = Math.max(1, Number(robotCount || 1));
          const robotRequests: any[] = [];
          for (let i = 0; i < count; i++) {
            const sx = Math.round(storageX + 8 + (i % Math.max(1, Math.floor(storageW / 32))) * 32);
            const sy = Math.round(storageY + 8 + Math.floor(i / Math.max(1, Math.floor(storageW / 32))) * 32);
            robotRequests.push({ name: `Robot-${i + 1}`, position: { screenX: sx, screenY: sy, physicalX: sx, physicalY: sy }, isEnabled: true });
          }

          const created = await robotsApi.create(robotRequests) as any[];
          if (Array.isArray(created) && created.length) {
            const mapped = created.map((r: any) => ({ id: r.id, name: r.name, x: r.position?.screenX ?? 0, y: r.position?.screenY ?? 0, status: r.status, battery: r.batteryLevel }));
            dispatch(updateRobots(mapped) as any);
          }
        } catch (e) {
          console.warn('Failed to create initial robots:', e);
        }

        // Create sample guests at Entrance (batch create)
        try {
          const entrance = (upload.entities?.zones || zones).find((z: any) => z.name === 'Entrance') || zones.find((z: any) => z.name === 'Entrance');
          const count = Math.max(0, Number(guestCount || 0));
          if (entrance && count > 0) {
            const centerX = Math.round((entrance.x ?? 0) + (entrance.width ?? 0) / 2);
            const centerY = Math.round((entrance.y ?? 0) + (entrance.height ?? 0) / 2);
            const guestRequests: any[] = [];
            for (let i = 0; i < count; i++) {
              guestRequests.push({ name: `Guest-${i + 1}`, partySize: 2 });
            }

            const created = await guestsApi.create(guestRequests) as any[];
            if (Array.isArray(created) && created.length) {
              const mapped = created.map((g: any, i: number) => ({ id: g.id, name: g.name, partySize: g.partySize, status: g.status, x: centerX + (i % 5) * 16, y: centerY + Math.floor(i / 5) * 16 }));
              dispatch(updateGuests(mapped) as any);
            }
          }
        } catch (e) {
          console.warn('Failed to create initial guests:', e);
        }
      }
      // NOTE: intentionally no automatic store updates here — caller manages entity state
    } catch (e) {
      console.warn('postMap on start failed:', e);
    }

    if (!tables || tables.length === 0) {
      console.warn('No tables available; skipping planning after map upload');
      setRobotPath(buildPathMetrics([]));
      setDistanceAlong(0);
      lastTimeRef.current = 0;
      return;
    }
  }, [tables, config]);
  
  useEffect(() => {
    if (!isRunning) {
      // reset guard when stopped so start can be triggered again
      initializedRef.current = false;
    }
  }, [isRunning]);

  const triggerReplan = async () => {
    if (!selectedTable) return;
    const targetTableA = tables.find((t) => t.id === selectedTable) || tables[9];

    let serverPath: Point[] | null = null;
    try {
      let start: Point;
      if (lastPathPointRef.current) {
        start = lastPathPointRef.current;
      } else {
        start = defaultStartBottomRight(config);
      }
      console.log('Planning route for table:', targetTableA?.id);
      const res = await planRoute({
        start,
        tableId: String(targetTableA.id),
        robotRadius: 16,
        dockSide: 'Bottom',
        dockOffset: 16,
      });
      if (res && res.success && Array.isArray(res.path) && res.path.length > 1) {
        serverPath = res.path.map((p: Point) => ({ x: p.x, y: p.y }));
        console.log('Received planned path points:', serverPath?.length);
        if (serverPath) {
          lastPathPointRef.current = serverPath[serverPath.length - 1];
        }
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
  };

  useEffect(() => {
    triggerReplan();
  }, [selectedTable]);

  useEffect(() => {
    if (isRunning) {
      initializeWorld();
    }
  }, [isRunning, initializeWorld]);

  // Animation loop
  const animate = useCallback(
    (timestamp: number) => {
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
      setDistanceAlong((d) =>
        Math.min(d + DEFAULT_SPEED * speedRateRef.current * dtSec, robotPath.totalLength)
      );
      frameRef.current = requestAnimationFrame(animate);
    },
    [robotPath.totalLength]
  );

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

  const configAny = config as any;
  const { mapWidth = 800, mapHeight = 600, zones = [] } = configAny;
  console.log("configAny", configAny, zones)
  const robotPos = useMemo(() => pointAlongPath(robotPath, distanceAlong), [robotPath, distanceAlong]);

  return (
    <VegaCard padding="size-16" style={{ height: '100%', overflow: 'hidden' }}>
      <Stage width={mapWidth} height={mapHeight}>
        <Layer>
          {/* Background */}
          <Rect x={0} y={0} width={mapWidth} height={mapHeight} fill="#f5f5f5" />

          {/* Fixed zones */}
          {Array.isArray(zones) && zones.map((zone: any, idx: number) => {
            const b = zoneBounds(zone);
            console.log("zone", zone);
            const fill = zone.color ?? (zone.type === 'Dining' ? '#fff9e6' : '#607D8B');
            const label = zone.name ?? `zone-${idx}`;
            const textColor = readableTextColor(fill);
            const metaParts: string[] = [];
            if (zone.type) metaParts.push(zone.type);
            if (zone.isNavigable === false) metaParts.push('No Nav');
            if (zone.speedLimit) metaParts.push(`Speed ${zone.speedLimit}`);
            if (zone.type === 'Corridor') {
              return (
                <>
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
                </>
              );
            }

            return (
              <Group key={zone.id ?? label ?? idx}>
                <Rect x={b.x} y={b.y} width={b.width} height={b.height} fill={fill} cornerRadius={5} />
                <Text
                  x={b.x}
                  y={b.y + 6}
                  width={b.width}
                  align="center"
                  fill={textColor}
                  fontSize={12}
                  fontStyle="bold"
                  text={label}
                />
              </Group>
            );
          })}

          {/* Tables */}
          {tables.map((table) => {
            const isSelected = selectedTable === table.id;
            const fillColor = isSelected ? '#5D4037' : '#8B4513';
            const strokeColor = isSelected ? '#FFD700' : 'transparent';

            const cx = table.center?.screenX ?? 0;
            const cy = table.center?.screenY ?? 0;

            if (table.shape === 'Circle') {
              const r = Math.min(table.width, table.height) / 2;
              return (
                <Group key={table.id} onClick={() => onTableSelect?.(table.id)}>
                  <Circle x={cx + r} y={cy + r} radius={r} fill={fillColor} stroke={strokeColor} strokeWidth={4} />
                  <Text
                    x={cx}
                    y={cy + table.height / 2 - 6}
                    width={table.width}
                    align="center"
                    fill="white"
                    fontSize={12}
                    fontStyle="bold"
                    text={`${table.label}` + (table.capacity ? `(${table.capacity})` : '')}
                  />
                </Group>
              );
            }

            return (
              <Group key={table.id} onClick={() => onTableSelect?.(table.id)}>
                <Rect
                  x={cx}
                  y={cy}
                  width={table.width}
                  height={table.height}
                  fill={fillColor}
                  stroke={strokeColor}
                  strokeWidth={4}
                  cornerRadius={5}
                />
                <Text
                  x={cx}
                  y={cy + table.height / 2 - 8}
                  width={table.width}
                  align="center"
                  fill="white"
                  fontSize={12}
                  fontStyle="bold"
                  text={`${table.label}` + (table.capacity ? `(${table.capacity})` : '')}
                />
              </Group>
            );
          })}
        </Layer>
        <Layer id="robot-layer">
          {/* Render robots from state */}
          {Array.isArray(robots) && robots.map((r) => (
            <Group key={`robot-${r.id}`} x={r.x} y={r.y}>
              <Circle radius={ROBOT_RADIUS} fill="#2196F3" stroke="#1565C0" strokeWidth={2} />
              <Text x={-8} y={-8} width={16} align="center" fill="white" fontSize={12} fontStyle="bold" text={r.name ? r.name.charAt(0) : 'R'} />
            </Group>
          ))}

          {/* Render only guestWaitList; default to Entrance left-side if guest has no coords */}
          {Array.isArray(guestWaitList) && (() => {
            const entranceZone: ZoneState | undefined = zoneMap['Entrance'] || zoneMap['entrance'];
            const guestOffset = 24; // pixels to nudge guests left
            const startX = entranceZone ? Math.round((entranceZone.x ?? 0) + 8 - guestOffset) : Math.round(mapWidth * 0.25) - guestOffset;
            const centerY = entranceZone ? Math.round((entranceZone.y ?? 0) + (entranceZone.height ?? 0) / 2) : Math.round(mapHeight / 6);
            const spacing = 18;
            const perRow = Math.max(1, Math.floor((entranceZone?.width ?? 200) / spacing));
            return guestWaitList.map((g, i) => {
              const gx = (g.x !== undefined && g.x !== null) ? g.x : startX + (i % perRow) * spacing;
              const gy = (g.y !== undefined && g.y !== null) ? g.y : centerY + Math.floor(i / perRow) * (spacing + 2);
              return (
                <Group key={`guest-${g.id ?? i}`} x={gx} y={gy}>
                  <Circle radius={10} fill="#4CAF50" stroke="#2E7D32" strokeWidth={2} />
                  <Text x={-6} y={-6} width={12} align="center" fill="white" fontSize={10} fontStyle="bold" text={g.name ? g.name.charAt(0) : 'G'} />
                </Group>
              );
            });
          })()}
        </Layer>
      </Stage>
    </VegaCard>
  );
}

export default RobotMap;
