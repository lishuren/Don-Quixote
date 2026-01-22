import { useState, useEffect, useRef } from 'react';
import { VegaCard } from '@heartlandone/vega-react';

// Restaurant layout configuration
const TABLES = [
  { id: 1, x: 100, y: 100 },
  { id: 2, x: 300, y: 100 },
  { id: 3, x: 500, y: 100 },
  { id: 4, x: 100, y: 300 },
  { id: 5, x: 300, y: 300 },
  { id: 6, x: 500, y: 300 },
];

const KITCHEN = { x: 50, y: 450, label: 'Kitchen' };

function RobotMap({ route, isRunning, onRouteComplete }) {
  const [robotPosition, setRobotPosition] = useState({ x: KITCHEN.x, y: KITCHEN.y });
  const [currentPathIndex, setCurrentPathIndex] = useState(0);
  const animationRef = useRef(null);

  useEffect(() => {
    if (isRunning && route.length > 0) {
      setCurrentPathIndex(0);
      animateRobot();
    }
    
    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [isRunning, route]);

  const animateRobot = () => {
    let index = 0;
    const speed = 5; // Pixels per frame

    const move = () => {
      if (index >= route.length) {
        onRouteComplete();
        setRobotPosition({ x: KITCHEN.x, y: KITCHEN.y });
        return;
      }

      const target = route[index];
      const dx = target.x - robotPosition.x;
      const dy = target.y - robotPosition.y;
      const distance = Math.sqrt(dx * dx + dy * dy);

      if (distance < speed) {
        setRobotPosition({ x: target.x, y: target.y });
        index++;
        setCurrentPathIndex(index);
      } else {
        const ratio = speed / distance;
        setRobotPosition(prev => ({
          x: prev.x + dx * ratio,
          y: prev.y + dy * ratio,
        }));
      }

      animationRef.current = requestAnimationFrame(move);
    };

    move();
  };

  return (
    <VegaCard >
      <svg width="600" height="500">
        {/* Draw route path */}
        {route.length > 1 && (
          <polyline
            points={route.map(p => `${p.x},${p.y}`).join(' ')}
            fill="none"
            stroke="#4CAF50"
            strokeWidth="2"
            strokeDasharray="5,5"
          />
        )}

        {/* Draw tables */}
        {TABLES.map(table => (
          <g key={table.id}>
            <rect
              x={table.x - 30}
              y={table.y - 30}
              width="60"
              height="60"
              fill="#8B4513"
              rx="5"
            />
            <text
              x={table.x}
              y={table.y + 5}
              textAnchor="middle"
              fill="white"
              fontSize="14"
            >
              T{table.id}
            </text>
          </g>
        ))}

        {/* Draw kitchen */}
        <g>
          <rect
            x={KITCHEN.x - 40}
            y={KITCHEN.y - 20}
            width="80"
            height="40"
            fill="#FF5722"
            rx="5"
          />
          <text
            x={KITCHEN.x}
            y={KITCHEN.y + 5}
            textAnchor="middle"
            fill="white"
            fontSize="14"
          >
            {KITCHEN.label}
          </text>
        </g>

        {/* Draw robot */}
        <g transform={`translate(${robotPosition.x}, ${robotPosition.y})`}>
          <circle r="20" fill="#2196F3" />
          <text
            y="5"
            textAnchor="middle"
            fill="white"
            fontSize="20"
          >
            🤖
          </text>
        </g>
      </svg>
    </VegaCard>
  );
}

export default RobotMap;
