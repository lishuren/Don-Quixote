# How to Run the Frontend

## Prerequisites

- Node.js 18+ (LTS recommended)
- npm 9+
- Backend API running at `http://localhost:5199`

## Installation

```bash
cd Frontend
npm install
```

## Development

Start the development server:

```bash
npm start
```

This will start the webpack dev server at `http://localhost:3000`.

## Available Scripts

| Script | Description |
|--------|-------------|
| `npm start` | Start development server at port 3000 |
| `npm run build` | Build production bundle to `dist/` |
| `npm run typecheck` | Run TypeScript type checking |
| `npm run lint` | Run ESLint |
| `npm run lint:fix` | Run ESLint with auto-fix |
| `npm run format` | Format code with Prettier |

## Environment Variables

The following environment variables can be configured:

| Variable | Default | Description |
|----------|---------|-------------|
| `API_BASE_URL` | `http://localhost:5199/api` | Backend REST API base URL |
| `SIGNALR_HUB_URL` | `ws://localhost:5199/hubs/restaurant` | SignalR hub URL |

## Project Structure

```
Frontend/
├── src/
│   ├── components/       # React components (TSX)
│   │   ├── App.tsx              # Main app with navigation
│   │   ├── Dashboard.tsx        # Summary cards view
│   │   ├── AlertPanel.tsx       # Real-time alerts
│   │   ├── TaskQueue.tsx        # Pending task management
│   │   ├── SimulationDashboard.tsx  # Long-run simulation
│   │   ├── KitchenDisplay.tsx   # Kitchen order queues
│   │   ├── RobotMap.tsx         # Konva-based map view
│   │   ├── ControlPanel.tsx     # Table/robot controls
│   │   └── HeadBar.tsx          # Settings controls
│   ├── hooks/            # Custom React hooks
│   │   ├── useSignalR.ts        # SignalR connection hook
│   │   └── useApiCall.ts        # API call with loading/error
│   ├── services/         # API and SignalR clients
│   │   ├── apiClient.ts         # Base HTTP client
│   │   ├── signalRService.ts    # SignalR connection manager
│   │   ├── KitchenManager.ts    # Kitchen queue state
│   │   └── *Api.ts              # Domain-specific APIs
│   ├── store/            # Redux store
│   │   └── store.ts             # State, actions, reducers
│   ├── types/            # TypeScript type definitions
│   │   ├── index.ts             # Central export
│   │   ├── enums.ts             # String enums
│   │   ├── dtos.ts              # API response types
│   │   ├── signalREvents.ts     # WebSocket event types
│   │   └── kitchen.ts           # Kitchen domain types
│   └── utils/            # Utility functions
├── public/
│   └── index.html        # HTML template
├── tsconfig.json         # TypeScript configuration
├── webpack.config.js     # Webpack bundler config
├── .eslintrc.json        # ESLint rules
└── .prettierrc           # Prettier formatting
```

## Features

### Navigation Tabs

1. **Robot Map** - Konva-based visualization of restaurant layout with robot movement
2. **Dashboard** - Summary statistics for robots, tables, tasks, and alerts
3. **Tasks & Alerts** - Task queue with auto-assign, plus real-time alert panel
4. **Kitchen** - Food, drink, and cleaning queue management
5. **Simulation** - Long-run simulation (720x accelerated) dashboard

### Real-time Updates

The app uses SignalR for real-time updates:
- Robot position changes
- Task status changes
- New alerts
- Table status changes
- Simulation progress

### Kitchen Manager

The KitchenManager tracks:
- **Food Orders** - With ORD-YYYY-MMDD-#### format
- **Drink Orders** - Quick beverage preparation
- **Clean Tasks** - Tables needing cleanup

## Building for Production

```bash
npm run build
```

The production bundle will be in the `dist/` directory.

## Troubleshooting

### SignalR Connection Issues

If real-time updates aren't working:
1. Ensure the backend is running at `http://localhost:5199`
2. Check browser console for WebSocket errors
3. SignalR auto-reconnects on disconnect

### Type Errors

Run `npm run typecheck` to identify TypeScript issues.

### CORS Issues

The backend should have CORS configured to allow `http://localhost:3000`.
