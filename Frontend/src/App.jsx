import { useState } from 'react';
import { Provider } from 'react-redux';
import { VegaFlex, VegaFont, VegaBox, VegaDivider } from '@heartlandone/vega-react';
import RobotMap from './components/RobotMap.jsx';
import ControlPanel from './components/ControlPanel.jsx';
import HeadBar from './components/HeadBar.jsx';
import store from './store/store.js';

function AppContent() {
  const [isRunning, setIsRunning] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [targetTable, setTargetTable] = useState(null);
  const [currentAction, setCurrentAction] = useState(null);
  const [selectedTable, setSelectedTable] = useState(null);
  // Control whether the map should be rendered
  const [showMap, setShowMap] = useState(false);

  const handleStartDelivery = (tableNumber, action = 'Delivery') => {
    // Set target table for robot delivery
    setTargetTable(tableNumber);
    setCurrentAction(action);
  };

  const handleStart = () => {
    // Show map when start is clicked
    setShowMap(true);
    setIsRunning(true);
  };

  // End the simulation and hide the map
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
    // Route complete but keep running state, allow further actions
    setTargetTable(null);
    setCurrentAction(null);
  };

  const handleTableSelect = (tableId) => {
    setSelectedTable(tableId);
  };

  return (
    <VegaBox height={'100vh'} backgroundColor={"bg-page"} style={{overflow: "hidden"}}>
      <VegaFlex direction="col" gap="size-16" alignItems="center" style={{padding: "12px"}}>
        <VegaFlex id="header" gap="size-8"  alignItems="center" direction={"col"} style={{ width:"100%", height: "18vh", paddingBottom: "12px", borderBottom: "1px solid #ABC6D8"}}>
          <VegaFont variant="font-h3">Direction assistant - Delivery Robot Simulator</VegaFont>
          <HeadBar 
            onStart={handleStart}
            onPause={handlePause}
            onEnd={handleEnd}
            isRunning={isRunning}
            isPaused={isPaused}
          />
        </VegaFlex>
        <VegaBox id="container"  width={'100%'} height={'calc(82vh - 70px)'} >
          <VegaFlex gap="size-20" justifyContent={"space-between"} style={{height: "100%"}}>
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
                  style={{ height: "100%", border: "2px dashed #ccc", borderRadius: "8px" }}
                >
                  <VegaFont variant="font-h5" style={{ color: "#999" }}>
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
        </VegaBox>
      </VegaFlex>
    </VegaBox>
  );
}

function App() {
  return (
    <Provider store={store}>
      <AppContent />
    </Provider>
  );
}

export default App;
