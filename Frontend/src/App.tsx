/**
 * @file App.tsx
 * @description Main application component with navigation tabs
 * Migrated from App.jsx with TypeScript and new feature screens
 */

import { useState } from 'react';
import {
  VegaFlex,
  VegaFont,
  VegaBox,
  VegaButton,
} from '@heartlandone/vega-react';
import RobotMap from './components/RobotMap';
import ControlPanel from './components/ControlPanel';
import HeadBar from './components/HeadBar';
import { Dashboard } from './components/Dashboard';
import { AlertPanel } from './components/AlertPanel';
import { TaskQueue } from './components/TaskQueue';
import { SimulationDashboard } from './components/SimulationDashboard';
import { KitchenDisplay } from './components/KitchenDisplay';

type TabType = 'map' | 'dashboard' | 'tasks' | 'alerts' | 'kitchen' | 'simulation';

function App() {
  const [isRunning, setIsRunning] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [targetTable, setTargetTable] = useState<number | null>(null);
  const [currentAction, setCurrentAction] = useState<string | null>(null);
  const [_selectedTable, setSelectedTable] = useState<number | null>(null);
  const [showMap, setShowMap] = useState(false);
  const [activeTab, setActiveTab] = useState<TabType>('map');

  const handleStartDelivery = (tableNumber: number, action = 'Delivery') => {
    setTargetTable(tableNumber);
    setCurrentAction(action);
  };

  const handleStart = () => {
    setShowMap(true);
    setIsRunning(true);
  };

  const handleEnd = () => {
    setIsRunning(false);
    setIsPaused(false);
    setShowMap(false);
    setTargetTable(null);
    setCurrentAction(null);
    setSelectedTable(null);
  };

  const handlePause = () => {
    setIsPaused(!isPaused);
  };

  const handleRouteComplete = () => {
    setTargetTable(null);
    setCurrentAction(null);
  };

  const handleTableSelect = (tableId: number) => {
    setSelectedTable(tableId);
  };

  const tabs: { label: string; value: TabType; icon: string }[] = [
    { label: 'Robot Map', value: 'map', icon: 'fa-solid fa-map' },
    { label: 'Dashboard', value: 'dashboard', icon: 'fa-solid fa-gauge' },
    { label: 'Tasks & Alerts', value: 'tasks', icon: 'fa-solid fa-list-check' },
    { label: 'Kitchen', value: 'kitchen', icon: 'fa-solid fa-utensils' },
    { label: 'Simulation', value: 'simulation', icon: 'fa-solid fa-chart-line' },
  ];

  const renderContent = () => {
    switch (activeTab) {
      case 'dashboard':
        return (
          <VegaBox width="100%" height="100%" style={{ padding: '16px' }}>
            <Dashboard />
          </VegaBox>
        );

      case 'tasks':
        return (
          <VegaFlex gap="size-16" style={{ height: '100%', padding: '16px' }}>
            <VegaBox flex={2}>
              <TaskQueue />
            </VegaBox>
            <VegaBox flex={1}>
              <AlertPanel />
            </VegaBox>
          </VegaFlex>
        );

      case 'alerts':
        return (
          <VegaBox width="100%" height="100%" style={{ padding: '16px' }}>
            <AlertPanel />
          </VegaBox>
        );

      case 'kitchen':
        return (
          <VegaBox width="100%" height="100%" style={{ padding: '16px' }}>
            <KitchenDisplay />
          </VegaBox>
        );

      case 'simulation':
        return (
          <VegaBox width="100%" height="100%" style={{ padding: '16px' }}>
            <SimulationDashboard />
          </VegaBox>
        );

      case 'map':
      default:
        return (
          <VegaFlex gap="size-20" justifyContent="space-between" style={{ height: '100%' }}>
            <VegaBox width="100%" flex={1}>
              {showMap ? (
                <RobotMap
                  isRunning={isRunning}
                  onRouteComplete={handleRouteComplete}
                  currentAction={currentAction}
                  selectedTable={targetTable}
                  onTableSelect={handleTableSelect}
                  isPaused={isPaused}
                />
              ) : (
                <VegaFlex
                  justifyContent="center"
                  alignItems="center"
                  style={{ height: '100%', border: '2px dashed #ccc', borderRadius: '8px' }}
                >
                  <VegaFont variant="font-h5" style={{ color: '#999' }}>
                    Click "Start" to render the map
                  </VegaFont>
                </VegaFlex>
              )}
            </VegaBox>
            <ControlPanel
              onStartDelivery={handleStartDelivery}
              isRunning={isRunning}
              targetTable={targetTable}
              currentAction={currentAction}
              showMap={showMap}
            />
          </VegaFlex>
        );
    }
  };

  return (
    <VegaBox height="100vh" backgroundColor="bg-page" style={{ overflow: 'hidden' }}>
      <VegaFlex direction="col" gap="size-16" alignItems="center" style={{ padding: '12px' }}>
        <VegaFlex
          id="header"
          gap="size-8"
          alignItems="center"
          direction="col"
          style={{
            width: '100%',
            height: '18vh',
            paddingBottom: '12px',
            borderBottom: '1px solid #ABC6D8',
          }}
        >
          <VegaFont variant="font-h3">Direction Assistant - Delivery Robot Simulator</VegaFont>
          <HeadBar
            onStart={handleStart}
            onPause={handlePause}
            onEnd={handleEnd}
            isRunning={isRunning}
            isPaused={isPaused}
          />
        </VegaFlex>

        {/* Navigation Tabs */}
        <VegaBox width="100%">
          <VegaFlex gap="size-8" style={{ borderBottom: '2px solid #e0e0e0', paddingBottom: '8px' }}>
            {tabs.map((tab) => (
              <VegaButton
                key={tab.value}
                variant={activeTab === tab.value ? 'primary' : 'secondary'}
                size="small"
                label={tab.label}
                icon={tab.icon}
                onVegaClick={() => setActiveTab(tab.value)}
              />
            ))}
          </VegaFlex>
        </VegaBox>

        <VegaBox id="container" width="100%" height="calc(75vh - 70px)">
          {renderContent()}
        </VegaBox>
      </VegaFlex>
    </VegaBox>
  );
}

export default App;
