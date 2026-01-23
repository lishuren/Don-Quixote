/**
 * @file store.js
 * @description Redux store for managing map configuration state
 */

import { createStore } from 'redux';
import MapConfig, { validateConfig, validateTableCount, getBounds } from '../utils/rules.js';

// Action Types
export const SET_TABLE_COUNT = 'SET_TABLE_COUNT';
export const SET_CONFIG = 'SET_CONFIG';
export const SET_MAIN_AISLE = 'SET_MAIN_AISLE';
export const SET_MINOR_AISLE = 'SET_MINOR_AISLE';
export const SET_SAFETY_BUFFER = 'SET_SAFETY_BUFFER';
export const SET_SEAT_BUFFER = 'SET_SEAT_BUFFER';
export const SET_TABLE_SHAPE = 'SET_TABLE_SHAPE';
export const SET_TABLE_MIX = 'SET_TABLE_MIX';
export const SET_TABLE_SIZES = 'SET_TABLE_SIZES';
export const SET_MAP_DIMENSIONS = 'SET_MAP_DIMENSIONS';
export const SET_SPEED_RATE = 'SET_SPEED_RATE';
export const SET_ROBOT_COUNT = 'SET_ROBOT_COUNT';
export const SET_GUEST_COUNT = 'SET_GUEST_COUNT';
export const RESET_CONFIG = 'RESET_CONFIG';

// Action Creators
export const setTableCount = (count) => ({
  type: SET_TABLE_COUNT,
  payload: validateTableCount(count),
});

export const setConfig = (config) => ({
  type: SET_CONFIG,
  payload: validateConfig(config),
});

export const setMainAisle = (value) => ({
  type: SET_MAIN_AISLE,
  payload: value,
});

export const setMinorAisle = (value) => ({
  type: SET_MINOR_AISLE,
  payload: value,
});

export const setSafetyBuffer = (value) => ({
  type: SET_SAFETY_BUFFER,
  payload: value,
});

export const setSeatBuffer = (value) => ({
  type: SET_SEAT_BUFFER,
  payload: value,
});

export const setTableShape = (shape) => ({
  type: SET_TABLE_SHAPE,
  payload: shape,
});

export const setTableMix = (mix) => ({
  type: SET_TABLE_MIX,
  payload: mix,
});

export const setTableSizes = (sizes) => ({
  type: SET_TABLE_SIZES,
  payload: sizes,
});

export const setMapDimensions = (width, height) => ({
  type: SET_MAP_DIMENSIONS,
  payload: { mapWidth: width, mapHeight: height },
});

export const setSpeedRate = (rate) => ({
  type: SET_SPEED_RATE,
  payload: rate,
});

export const setRobotCount = (count) => ({
  type: SET_ROBOT_COUNT,
  payload: count,
});

export const setGuestCount = (count) => ({
  type: SET_GUEST_COUNT,
  payload: count,
});

export const resetConfig = () => ({
  type: RESET_CONFIG,
});

// Initial State
const initialState = {
  tableCount: 10,
  robotCount: 3,
  guestCount: 0,
  speedRate: 1,
  config: validateConfig(MapConfig),
};

// Reducer
function mapReducer(state = initialState, action) {
  switch (action.type) {
    case SET_TABLE_COUNT:
      return { ...state, tableCount: action.payload };

    case SET_CONFIG:
      return { ...state, config: action.payload };

    case SET_MAIN_AISLE:
      return {
        ...state,
        config: validateConfig({ ...state.config, mainAisle: action.payload }),
      };

    case SET_MINOR_AISLE:
      return {
        ...state,
        config: validateConfig({ ...state.config, minorAisle: action.payload }),
      };

    case SET_SAFETY_BUFFER:
      return {
        ...state,
        config: validateConfig({ ...state.config, safetyBuffer: action.payload }),
      };

    case SET_SEAT_BUFFER:
      return {
        ...state,
        config: validateConfig({ ...state.config, seatBuffer: action.payload }),
      };

    case SET_TABLE_SHAPE:
      return {
        ...state,
        config: validateConfig({ ...state.config, tableShape: action.payload }),
      };

    case SET_TABLE_MIX:
      return {
        ...state,
        config: validateConfig({ ...state.config, tableMix: action.payload }),
      };

    case SET_TABLE_SIZES:
      return {
        ...state,
        config: validateConfig({ ...state.config, tableSizes: action.payload }),
      };

    case SET_MAP_DIMENSIONS:
      return {
        ...state,
        config: validateConfig({
          ...state.config,
          mapWidth: action.payload.mapWidth,
          mapHeight: action.payload.mapHeight,
        }),
      };

    case SET_SPEED_RATE:
      return { ...state, speedRate: action.payload };

    case SET_ROBOT_COUNT:
      return { ...state, robotCount: action.payload };

    case SET_GUEST_COUNT:
      return { ...state, guestCount: action.payload };

    case RESET_CONFIG:
      return { ...initialState };

    default:
      return state;
  }
}

// Create Store
const store = createStore(
  mapReducer,
  // Enable Redux DevTools Extension if available
  typeof window !== 'undefined' && window.__REDUX_DEVTOOLS_EXTENSION__
    ? window.__REDUX_DEVTOOLS_EXTENSION__()
    : undefined
);

export default store;
