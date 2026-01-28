/**
 * @file store.ts
 * @description Redux store with TypeScript support
 * Migrated from store.js with proper type definitions
 */

import { legacy_createStore as createStore } from 'redux';

// State Types
export interface MapConfig {
  tableCount: number;
  robotCount: number;
  guestCount: number;
  speedRate: number;
  mainAislePos: number;
  horizontalAisleCount: number;
  verticalAisleCount: number;
  tableWidth: number;
  tableHeight: number;
  tableWidthPx: number;
  tableHeightPx: number;
  aisleWidth: number;
  topBuffer: number;
  bottomBuffer: number;
  sideBuffer: number;
  squareTableMix: number;
  roundTableMix: number;
  mixedTableMix: number;
  mapWidth: number;
  mapHeight: number;
  tables: TableState[];
  zones: ZoneState[];
}

export interface RobotState {
  id: number;
  name: string;
  x: number;
  y: number;
  status: string;
  battery: number;
}

export interface GuestState {
  id: number;
  name?: string | null;
  partySize: number;
  status: string;
  x?: number;
  y?: number;
}

export interface TableState {
  id: number;
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
  shape: 'square' | 'round';
  status: string;
  capacity: number;
}

export interface ZoneState {
  id: number;
  name: string;
  type: string;
  x: number;
  y: number;
  width: number;
  height: number;
  color?: string;
  isNavigable: boolean;
}

export interface AppState {
  tableCount: number;
  robotCount: number;
  guestCount: number;
  speedRate: number;
  mainAislePos: number;
  horizontalAisleCount: number;
  verticalAisleCount: number;
  aisleWidth: number;
  topBuffer: number;
  bottomBuffer: number;
  sideBuffer: number;
  squareTableMix: number;
  roundTableMix: number;
  mixedTableMix: number;
  // tables are now stored inside `config.tables`
  robots: RobotState[];
  guests: GuestState[];
  mapConfig: MapConfig;
  isRunning: boolean;
  isPaused: boolean;
  simulationTime: number;
}

// Action Types
export const SET_TABLE_COUNT = 'SET_TABLE_COUNT';
export const SET_ROBOT_COUNT = 'SET_ROBOT_COUNT';
export const SET_GUEST_COUNT = 'SET_GUEST_COUNT';
export const SET_SPEED_RATE = 'SET_SPEED_RATE';
export const SET_CONFIG = 'SET_CONFIG';
export const SET_MAIN_AISLE = 'SET_MAIN_AISLE';
export const SET_H_AISLE_COUNT = 'SET_H_AISLE_COUNT';
export const SET_V_AISLE_COUNT = 'SET_V_AISLE_COUNT';
export const SET_AISLE_WIDTH = 'SET_AISLE_WIDTH';
export const SET_TOP_BUFFER = 'SET_TOP_BUFFER';
export const SET_BOTTOM_BUFFER = 'SET_BOTTOM_BUFFER';
export const SET_SIDE_BUFFER = 'SET_SIDE_BUFFER';
export const SET_SQUARE_MIX = 'SET_SQUARE_MIX';
export const SET_ROUND_MIX = 'SET_ROUND_MIX';
export const SET_MIXED_MIX = 'SET_MIXED_MIX';
export const UPDATE_TABLES = 'UPDATE_TABLES';
export const UPDATE_ZONES = 'UPDATE_ZONES';
export const UPDATE_ROBOTS = 'UPDATE_ROBOTS';
export const UPDATE_GUESTS = 'UPDATE_GUESTS';
export const SET_RUNNING = 'SET_RUNNING';
export const SET_PAUSED = 'SET_PAUSED';
export const UPDATE_SIMULATION_TIME = 'UPDATE_SIMULATION_TIME';

// Action Type Definitions
interface SetTableCountAction {
  type: typeof SET_TABLE_COUNT;
  payload: number;
}

interface SetRobotCountAction {
  type: typeof SET_ROBOT_COUNT;
  payload: number;
}

interface SetGuestCountAction {
  type: typeof SET_GUEST_COUNT;
  payload: number;
}

interface SetSpeedRateAction {
  type: typeof SET_SPEED_RATE;
  payload: number;
}

interface SetConfigAction {
  type: typeof SET_CONFIG;
  payload: Partial<MapConfig>;
}

interface SetMainAisleAction {
  type: typeof SET_MAIN_AISLE;
  payload: number;
}

interface SetHAisleCountAction {
  type: typeof SET_H_AISLE_COUNT;
  payload: number;
}

interface SetVAisleCountAction {
  type: typeof SET_V_AISLE_COUNT;
  payload: number;
}

interface SetAisleWidthAction {
  type: typeof SET_AISLE_WIDTH;
  payload: number;
}

interface SetTopBufferAction {
  type: typeof SET_TOP_BUFFER;
  payload: number;
}

interface SetBottomBufferAction {
  type: typeof SET_BOTTOM_BUFFER;
  payload: number;
}

interface SetSideBufferAction {
  type: typeof SET_SIDE_BUFFER;
  payload: number;
}

interface SetSquareMixAction {
  type: typeof SET_SQUARE_MIX;
  payload: number;
}

interface SetRoundMixAction {
  type: typeof SET_ROUND_MIX;
  payload: number;
}

interface SetMixedMixAction {
  type: typeof SET_MIXED_MIX;
  payload: number;
}

interface UpdateTablesAction {
  type: typeof UPDATE_TABLES;
  payload: TableState[];
}

interface UpdateZonesAction {
  type: typeof UPDATE_ZONES;
  payload: ZoneState[];
}

interface UpdateRobotsAction {
  type: typeof UPDATE_ROBOTS;
  payload: RobotState[];
}

interface UpdateGuestsAction {
  type: typeof UPDATE_GUESTS;
  payload: GuestState[];
}

interface SetRunningAction {
  type: typeof SET_RUNNING;
  payload: boolean;
}

interface SetPausedAction {
  type: typeof SET_PAUSED;
  payload: boolean;
}

interface UpdateSimulationTimeAction {
  type: typeof UPDATE_SIMULATION_TIME;
  payload: number;
}

export type AppAction =
  | SetTableCountAction
  | SetRobotCountAction
  | SetGuestCountAction
  | SetSpeedRateAction
  | SetConfigAction
  | SetMainAisleAction
  | SetHAisleCountAction
  | SetVAisleCountAction
  | SetAisleWidthAction
  | SetTopBufferAction
  | SetBottomBufferAction
  | SetSideBufferAction
  | SetSquareMixAction
  | SetRoundMixAction
  | SetMixedMixAction
  | UpdateTablesAction
  | UpdateZonesAction
  | UpdateRobotsAction
  | UpdateGuestsAction
  | SetRunningAction
  | SetPausedAction
  | UpdateSimulationTimeAction;

// Action Creators
export const setTableCount = (count: number): SetTableCountAction => ({
  type: SET_TABLE_COUNT,
  payload: count,
});

export const setRobotCount = (count: number): SetRobotCountAction => ({
  type: SET_ROBOT_COUNT,
  payload: count,
});

export const setGuestCount = (count: number): SetGuestCountAction => ({
  type: SET_GUEST_COUNT,
  payload: count,
});

export const setSpeedRate = (rate: number): SetSpeedRateAction => ({
  type: SET_SPEED_RATE,
  payload: rate,
});

export const setMapConfig = (mapConfig: Partial<MapConfig>): SetConfigAction => ({
  type: SET_CONFIG,
  payload: mapConfig,
});

export const setMainAisle = (pos: number): SetMainAisleAction => ({
  type: SET_MAIN_AISLE,
  payload: pos,
});

export const setHAisleCount = (count: number): SetHAisleCountAction => ({
  type: SET_H_AISLE_COUNT,
  payload: count,
});

export const setVAisleCount = (count: number): SetVAisleCountAction => ({
  type: SET_V_AISLE_COUNT,
  payload: count,
});

export const setAisleWidth = (width: number): SetAisleWidthAction => ({
  type: SET_AISLE_WIDTH,
  payload: width,
});

export const setTopBuffer = (buffer: number): SetTopBufferAction => ({
  type: SET_TOP_BUFFER,
  payload: buffer,
});

export const setBottomBuffer = (buffer: number): SetBottomBufferAction => ({
  type: SET_BOTTOM_BUFFER,
  payload: buffer,
});

export const setSideBuffer = (buffer: number): SetSideBufferAction => ({
  type: SET_SIDE_BUFFER,
  payload: buffer,
});

export const setSquareMix = (mix: number): SetSquareMixAction => ({
  type: SET_SQUARE_MIX,
  payload: mix,
});

export const setRoundMix = (mix: number): SetRoundMixAction => ({
  type: SET_ROUND_MIX,
  payload: mix,
});

export const setMixedMix = (mix: number): SetMixedMixAction => ({
  type: SET_MIXED_MIX,
  payload: mix,
});

export const updateTables = (tables: TableState[]): UpdateTablesAction => ({
  type: UPDATE_TABLES,
  payload: tables,
});

export const updateZones = (zones: ZoneState[]): UpdateZonesAction => ({
  type: UPDATE_ZONES,
  payload: zones,
});

export const updateRobots = (robots: RobotState[]): UpdateRobotsAction => ({
  type: UPDATE_ROBOTS,
  payload: robots,
});

export const updateGuests = (guests: GuestState[]): UpdateGuestsAction => ({
  type: UPDATE_GUESTS,
  payload: guests,
});

export const setRunning = (running: boolean): SetRunningAction => ({
  type: SET_RUNNING,
  payload: running,
});

export const setPaused = (paused: boolean): SetPausedAction => ({
  type: SET_PAUSED,
  payload: paused,
});

export const updateSimulationTime = (time: number): UpdateSimulationTimeAction => ({
  type: UPDATE_SIMULATION_TIME,
  payload: time,
});

// Initial State
const initialState: AppState = {
  tableCount: 20,
  robotCount: 3,
  guestCount: 10,
  speedRate: 1,
  mainAislePos: 0.35,
  horizontalAisleCount: 2,
  verticalAisleCount: 2,
  aisleWidth: 1.2,
  topBuffer: 0.5,
  bottomBuffer: 0.5,
  sideBuffer: 0.5,
  squareTableMix: 0.6,
  roundTableMix: 0.3,
  mixedTableMix: 0.1,
  robots: [],
  guests: [],
  mapConfig: {
    tableCount: 20,
    robotCount: 3,
    guestCount: 0,
    speedRate: 1,
    mainAislePos: 0.35,
    horizontalAisleCount: 2,
    verticalAisleCount: 2,
    tableWidth: 0.8,
    tableHeight: 0.8,
    tableWidthPx: 80,
    tableHeightPx: 80,
    aisleWidth: 1.2,
    topBuffer: 0.5,
    bottomBuffer: 0.5,
    sideBuffer: 0.5,
    squareTableMix: 0.6,
    roundTableMix: 0.3,
    mixedTableMix: 0.1,
    tables: [],
    zones: [],
    mapWidth: 1100,
    mapHeight: 600
  },
  isRunning: false,
  isPaused: false,
  simulationTime: 0,
};

// Reducer
function reducer(state = initialState, action: AppAction): AppState {
  switch (action.type) {
    case SET_TABLE_COUNT:
      return { ...state, tableCount: action.payload };
    case SET_ROBOT_COUNT:
      return { ...state, robotCount: action.payload };
    case SET_GUEST_COUNT:
      return { ...state, guestCount: action.payload };
    case SET_SPEED_RATE:
      return { ...state, speedRate: action.payload };
    case SET_CONFIG:
      return { ...state, mapConfig: { ...state.mapConfig, ...action.payload } };
    case SET_MAIN_AISLE:
      return { ...state, mainAislePos: action.payload };
    case SET_H_AISLE_COUNT:
      return { ...state, horizontalAisleCount: action.payload };
    case SET_V_AISLE_COUNT:
      return { ...state, verticalAisleCount: action.payload };
    case SET_AISLE_WIDTH:
      return { ...state, aisleWidth: action.payload };
    case SET_TOP_BUFFER:
      return { ...state, topBuffer: action.payload };
    case SET_BOTTOM_BUFFER:
      return { ...state, bottomBuffer: action.payload };
    case SET_SIDE_BUFFER:
      return { ...state, sideBuffer: action.payload };
    case SET_SQUARE_MIX:
      return { ...state, squareTableMix: action.payload };
    case SET_ROUND_MIX:
      return { ...state, roundTableMix: action.payload };
    case SET_MIXED_MIX:
      return { ...state, mixedTableMix: action.payload };
    case UPDATE_TABLES:
      return {
        ...state,
        mapConfig: { ...state.mapConfig, tables: [...(state.mapConfig.tables || []), ...(action.payload || [])] },
      };
    case UPDATE_ZONES:
      return {
        ...state,
        mapConfig: { ...state.mapConfig, zones: [...(state.mapConfig.zones || []), ...(action.payload || [])] },
      };
      case UPDATE_GUESTS:
        return { ...state, guests: action.payload };
    case UPDATE_ROBOTS:
      return { ...state, robots: action.payload };
    case SET_RUNNING:
      return { ...state, isRunning: action.payload };
    case SET_PAUSED:
      return { ...state, isPaused: action.payload };
    case UPDATE_SIMULATION_TIME:
      return { ...state, simulationTime: action.payload };
    default:
      return state;
  }
}

// Create Store
export const store = createStore(reducer);

// Export types for useSelector
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export default store;
