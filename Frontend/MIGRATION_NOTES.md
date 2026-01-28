# Migration Notes: JS to TypeScript

## Overview

This document describes the migration from JavaScript React to TypeScript React for the Don-Quixote Frontend project.

## Migration Strategy

The migration followed a **gradual, additive approach**:
1. Added TypeScript tooling alongside existing JS
2. Created new TypeScript files for types, services, and hooks
3. Converted existing JSX components to TSX
4. Maintained backward compatibility during transition

## Files Created (TypeScript)

### Types (`src/types/`)
- `index.ts` - Central export for all types
- `enums.ts` - String enums matching backend
- `dtos.ts` - Data Transfer Objects for API responses
- `signalREvents.ts` - WebSocket event type definitions
- `kitchen.ts` - Kitchen domain types and helpers

### Services (`src/services/`)
- `apiClient.ts` - Type-safe HTTP client with generics
- `signalRService.ts` - SignalR connection manager
- `KitchenManager.ts` - Kitchen queue state management
- `robotsApi.ts` - Robot CRUD operations
- `tasksApi.ts` - Task lifecycle management
- `tablesApi.ts` - Table status management
- `guestsApi.ts` - Guest/waitlist management
- `alertsApi.ts` - Alert CRUD operations
- `dashboardApi.ts` - Dashboard summary
- `dispatchApi.ts` - Auto-dispatch configuration
- `eventsApi.ts` - Event triggers
- `kitchenApi.ts` - Kitchen endpoints
- `emergencyApi.ts` - Emergency stop/resume
- `simulationApi.ts` - Long-run simulation

### Hooks (`src/hooks/`)
- `useSignalR.ts` - SignalR React hook with event subscriptions
- `useApiCall.ts` - API call hook with loading/error states

### Components (`src/components/`)
- `Dashboard.tsx` - Summary statistics view
- `AlertPanel.tsx` - Real-time alert management
- `TaskQueue.tsx` - Task assignment queue
- `SimulationDashboard.tsx` - Long-run simulation UI
- `KitchenDisplay.tsx` - Kitchen queue display

### Store (`src/store/`)
- `store.ts` - Redux store with TypeScript types

### Entry Point
- `index.tsx` - Application entry with Provider
- `App.tsx` - Main app with navigation tabs

## Files Converted (JSX → TSX)

| Original | Converted |
|----------|-----------|
| `App.jsx` | `App.tsx` |
| `RobotMap.jsx` | `RobotMap.tsx` |
| `ControlPanel.jsx` | `ControlPanel.tsx` |
| `HeadBar.jsx` | `HeadBar.tsx` |
| `store.js` | `store.ts` |
| `index.js` | `index.tsx` |

## Type Safety Approach

### 1. String Enums
All enums use string values to match backend API:
```typescript
export type RobotStatus = 'Idle' | 'Navigating' | 'Delivering' | ...;
```

### 2. DTOs Match Backend
All DTOs mirror the C# DTOs exactly:
```typescript
interface RobotDto {
  id: number;
  name: string;
  status: RobotStatus;
  ...
}
```

### 3. SignalR Events Typed
Each SignalR event has a dedicated type:
```typescript
interface RobotPositionEvent {
  robotId: number;
  x: number;
  y: number;
  ...
}
```

### 4. API Client Generics
The API client uses TypeScript generics for type-safe responses:
```typescript
async get<T>(url: string): Promise<T> { ... }
```

## Redux Typing

The store is fully typed:
```typescript
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

## Configuration Files Added

| File | Purpose |
|------|---------|
| `tsconfig.json` | TypeScript compiler configuration |
| `.eslintrc.json` | ESLint rules for TypeScript/React |
| `.prettierrc` | Code formatting rules |

## Webpack Changes

- Added `ts-loader` for TypeScript compilation
- Updated resolve extensions to include `.ts`, `.tsx`
- Added path aliases (`@/`, `@components/`, etc.)
- Environment variables via `DefinePlugin`

## Package.json Changes

### Dependencies Added
- `@microsoft/signalr@8.0` - Real-time communication
- `@types/react`, `@types/react-dom` - React type definitions

### Dev Dependencies Added
- `typescript@5.4`
- `ts-loader@9.5`
- `@typescript-eslint/parser`
- `@typescript-eslint/eslint-plugin`
- `eslint-plugin-react`
- `eslint-plugin-react-hooks`
- `prettier`

### Scripts Added
- `typecheck` - TypeScript type checking
- `lint` / `lint:fix` - ESLint
- `format` - Prettier formatting

## Known Limitations

1. **Utils Still JS**: Some utility files (`layoutGenerator.js`, `mapSerializer.js`, `rules.js`) remain JavaScript and are imported with `any` types where needed.

2. **Vega Component Types**: The `@heartlandone/vega-react` library types may be incomplete; some props use `any`.

3. **Redux Legacy API**: Using `legacy_createStore` from Redux to maintain compatibility.

## Future Improvements

1. Convert remaining utility files to TypeScript
2. Add comprehensive unit tests
3. Migrate to Redux Toolkit for better TypeScript integration
4. Add strict null checks throughout
