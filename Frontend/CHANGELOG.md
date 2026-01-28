# Changelog

All notable changes to the Don-Quixote Frontend.

## [2.0.0] - 2024-XX-XX

### Added

#### TypeScript Migration
- Full TypeScript support with strict mode
- Type definitions for all DTOs, enums, and events
- Type-safe API client with generics
- Typed Redux store with `RootState` and `AppDispatch`

#### New Features
- **Dashboard** - Summary statistics view with cards for robots, tables, tasks, alerts
- **AlertPanel** - Real-time alert management with acknowledge/resolve actions
- **TaskQueue** - Pending task queue with manual and auto-assign capabilities
- **SimulationDashboard** - Long-run simulation (720x accelerated) with progress tracking
- **KitchenDisplay** - Food, drink, and cleaning queue management
- **Navigation Tabs** - Tab-based navigation between features

#### SignalR Integration
- Real-time updates via SignalR WebSocket connection
- `useSignalR` hook for React components
- Event subscriptions for robots, tasks, alerts, tables, simulation
- Auto-reconnect on connection loss

#### Kitchen Manager
- `KitchenManager` service for queue state management
- Order ID generation with format `ORD-YYYY-MMDD-####`
- Food, drink, and clean queue tracking
- Table-order registry for associating orders with tables

#### API Services
- Type-safe API services for all backend endpoints
- `robotsApi` - Robot CRUD and commands
- `tasksApi` - Task lifecycle management
- `tablesApi` - Table status management
- `guestsApi` - Guest and waitlist operations
- `alertsApi` - Alert management
- `dashboardApi` - Dashboard summary
- `dispatchApi` - Auto-dispatch configuration
- `eventsApi` - Event triggers
- `kitchenApi` - Kitchen order endpoints
- `emergencyApi` - Emergency stop/resume
- `simulationApi` - Long-run simulation control

#### Development Tools
- ESLint configuration for TypeScript/React
- Prettier code formatting
- TypeScript type checking script
- Path aliases for cleaner imports

### Changed
- Migrated all components from JSX to TSX
- Updated webpack configuration for TypeScript support
- Updated package.json with TypeScript dependencies
- Redux store now fully typed

### Technical Details
- React 19.1
- TypeScript 5.4+
- @microsoft/signalr 8.0
- Webpack 5.x with ts-loader

---

## [1.0.0] - Previous Release

### Features
- Initial JavaScript React implementation
- Konva-based robot map visualization
- Control panel for table/robot actions
- HeadBar with simulation settings
- Redux state management
- Backend API integration for map and path planning
