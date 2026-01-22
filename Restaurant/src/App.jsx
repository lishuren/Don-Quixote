import { useState } from 'react';
import { VegaFlex, VegaFont, VegaBox } from '@heartlandone/vega-react';
import RobotMap from './components/RobotMap';
import ControlPanel from './components/ControlPanel';
import HeadBar from './components/HeadBar';



function App() {
  const [route, setRoute] = useState([]);
  const [isRunning, setIsRunning] = useState(false);
  const [targetTable, setTargetTable] = useState(null);
  const [currentAction, setCurrentAction] = useState(null);

  const handleStartDelivery = async (tableNumber, action = 'Delivery') => {
    setTargetTable(tableNumber);
    setCurrentAction(action);
    try {
      const response = await fetch(`/api/route/plan?tableNumber=${tableNumber}`);
      const data = await response.json();
      setRoute(data.path);
      setIsRunning(true);
    } catch (error) {
      console.error('Failed to get route:', error);
    }
  };

  const handleRouteComplete = () => {
    setIsRunning(false);
    setRoute([]);
    setTargetTable(null);
    setCurrentAction(null);
  };

  return (
   
    <VegaBox minHeight={'100vh'} padding={"size-32"} backgroundColor={"bg-page"}>
      <VegaFlex direction="col" gap="size-32" padding="size-12" alignItems="center">
        <VegaFlex gap="size-16" justifyContent="space-between" alignItems="center" direction={"col"} style={{ width:"100%" }}>
          <VegaFont variant="font-h3">🤖 RoboRunner - Delivery Robot Simulator</VegaFont>
          <HeadBar />
        </VegaFlex>
        <VegaBox width={'100%'}>
          <VegaFlex gap="size-20" justifyContent={"space-between"}>
            <VegaBox width="100%" flex={1}>
              <RobotMap 
                route={route} 
                isRunning={isRunning} 
                onRouteComplete={handleRouteComplete}
                currentAction={currentAction}
              />
            </VegaBox>
            <ControlPanel 
              onStartDelivery={handleStartDelivery}
              isRunning={isRunning}
              targetTable={targetTable}
              currentAction={currentAction}
            />
          </VegaFlex>
        </VegaBox>
      </VegaFlex>
    </VegaBox>

  );
}

export default App;
