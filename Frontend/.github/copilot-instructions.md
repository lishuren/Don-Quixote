# GitHub Copilot Instructions for RoboRunner Frontend

> **Project**: RoboRunner Frontend  
> **Type**: React Single Page Application  
> **Location**: `Frontend/`

## Project Overview

This is a **Robot Route Simulation Frontend** built with React 19 and Konva.js for canvas rendering. The application provides a visual interface for managing restaurant delivery robots, displaying real-time robot movements on a restaurant floor map.

## Technology Stack

- **Framework**: React 19
- **State Management**: Redux 5 + React-Redux 9
- **Canvas Rendering**: Konva.js 10 + React-Konva 19
- **UI Components**: @heartlandone/vega-react (Vega Design System)
- **Pathfinding**: pathfinding.js library
- **Build Tool**: Webpack 5 + Babel
- **Styling**: CSS with Vega design tokens

## Project Structure

```
Frontend/
├── public/
│   └── index.html              # HTML template
├── src/
│   ├── App.jsx                 # Main application component
│   ├── index.js                # React entry point
│   ├── index.css               # Global styles
│   ├── components/
│   │   ├── RobotMap.jsx        # Konva canvas with restaurant map
│   │   ├── ControlPanel.jsx    # Side panel for robot/table controls
│   │   ├── HeadBar.jsx         # Top navigation with Start/Pause/End
│   │   └── map.js              # Map data and configuration
│   ├── store/
│   │   └── store.js            # Redux store configuration
│   └── utils/
│       ├── apiClient.js        # Backend API client
│       ├── layoutGenerator.js  # Generate table layouts
│       ├── mapSerializer.js    # Serialize/deserialize map data
│       ├── pathfinding.js      # A* pathfinding utilities
│       ├── robot.js            # Robot movement logic
│       └── rules.js            # Business rules and constraints
├── babel.config.js             # Babel configuration
├── webpack.config.js           # Webpack configuration
└── package.json
```

## Component Architecture

### Main Components

| Component | Purpose |
|-----------|---------|
| `App.jsx` | Root component, manages simulation state |
| `RobotMap.jsx` | Konva Stage rendering restaurant floor |
| `ControlPanel.jsx` | Robot controls, table selection, actions |
| `HeadBar.jsx` | Start/Pause/End simulation buttons |

### State Management

Application state is managed with a combination of:
- **React useState** for UI state (isRunning, isPaused, selectedTable)
- **Redux store** for shared state across components

## Coding Conventions

### Component Pattern

Use functional components with hooks:

```jsx
import { useState, useEffect } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { VegaBox, VegaFlex } from '@heartlandone/vega-react';

function ExampleComponent({ onAction, isEnabled }) {
  const [localState, setLocalState] = useState(null);
  const reduxState = useSelector(state => state.example);
  const dispatch = useDispatch();

  useEffect(() => {
    // Side effects here
  }, [dependency]);

  const handleClick = () => {
    onAction(localState);
  };

  return (
    <VegaBox>
      {/* Component JSX */}
    </VegaBox>
  );
}

export default ExampleComponent;
```

### Vega UI Components

Use Vega design system components for consistent UI:

```jsx
import { 
  VegaBox, 
  VegaFlex, 
  VegaFont, 
  VegaButton,
  VegaDivider 
} from '@heartlandone/vega-react';

// Layout
<VegaFlex direction="col" gap="size-16" alignItems="center">
  <VegaBox width="100%" padding="size-12">
    <VegaFont variant="font-h3">Title</VegaFont>
  </VegaBox>
</VegaFlex>

// Buttons
<VegaButton variant="primary" onClick={handleClick}>
  Start
</VegaButton>
```

### Konva Canvas Rendering

Use React-Konva for canvas elements:

```jsx
import { Stage, Layer, Rect, Circle, Line, Text, Group } from 'react-konva';

function MapCanvas({ width, height }) {
  return (
    <Stage width={width} height={height}>
      <Layer>
        {/* Static elements */}
        <Rect x={0} y={0} width={100} height={100} fill="#f0f0f0" />
      </Layer>
      <Layer>
        {/* Dynamic elements (robots, paths) */}
        <Circle x={50} y={50} radius={10} fill="blue" />
      </Layer>
    </Stage>
  );
}
```

### File Naming

- Components: `PascalCase.jsx` (e.g., `RobotMap.jsx`)
- Utilities: `camelCase.js` (e.g., `pathfinding.js`)
- CSS: `kebab-case.css` or component-scoped

## Key Utilities

### API Client (`utils/apiClient.js`)

```javascript
const API_BASE = 'http://localhost:5199/api';

export async function fetchRobots() {
  const response = await fetch(`${API_BASE}/robots`);
  return response.json();
}

export async function sendRobotCommand(robotId, command) {
  const response = await fetch(`${API_BASE}/robots/${robotId}/command`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ command })
  });
  return response.json();
}
```

### Pathfinding (`utils/pathfinding.js`)

Uses A* algorithm for robot navigation:

```javascript
import PF from 'pathfinding';

export function findPath(grid, startX, startY, endX, endY) {
  const finder = new PF.AStarFinder();
  const path = finder.findPath(startX, startY, endX, endY, grid.clone());
  return path;
}
```

### Layout Generator (`utils/layoutGenerator.js`)

Generates table positions for restaurant layout:

```javascript
export function generateTableLayout(config) {
  // Returns array of table positions
  return tables.map(t => ({
    id: t.id,
    x: t.x,
    y: t.y,
    width: t.width,
    height: t.height
  }));
}
```

## Application State Flow

```
User clicks "Start"
       ↓
showMap = true, isRunning = true
       ↓
RobotMap renders with Konva
       ↓
User selects table in ControlPanel
       ↓
targetTable set, robot pathfinding triggered
       ↓
Robot animates along path
       ↓
onRouteComplete callback
       ↓
Ready for next action
```

## Backend Integration

The frontend connects to the MapPlannerApi backend:

- **Base URL**: `http://localhost:5199/api`
- **WebSocket**: `http://localhost:5199/hubs/restaurant` (SignalR)

### SignalR Connection

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5199/hubs/restaurant')
  .build();

connection.on('RobotPositionUpdated', (event) => {
  // Update robot position on map
});

await connection.start();
```

## Development Commands

```bash
# Install dependencies
npm install

# Start development server (port 3000)
npm start

# Build for production
npm run build

# Development mode
npm run dev
```

## Do's and Don'ts

### Do
- Use Vega components for UI consistency
- Use functional components with hooks
- Keep canvas rendering in dedicated layers (static vs dynamic)
- Use `useCallback` for event handlers passed to Konva
- Handle loading and error states for API calls
- Use meaningful component prop names

### Don't
- Don't use class components
- Don't mutate Redux state directly
- Don't render heavy computations inside render (use useMemo)
- Don't forget to clean up subscriptions in useEffect
- Don't hardcode pixel values (use Vega spacing tokens)
- Don't put business logic in components (use utils/)

## Coordinate System

The map uses pixel coordinates matching the backend:

- **Origin**: Top-left corner (0, 0)
- **X-axis**: Left to right (increases →)
- **Y-axis**: Top to bottom (increases ↓)
- **Scale**: Configurable pixels-per-meter ratio

Convert between screen and physical coordinates when communicating with backend.
