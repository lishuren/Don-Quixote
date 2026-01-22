import { VegaCard, VegaButton, VegaFlex, VegaFont, VegaBox, VegaChip, VegaGrid, VegaIcon } from '@heartlandone/vega-react';

function ControlPanel({ onStartDelivery, isRunning, targetTable, currentAction }) {
  const tables = [1, 2, 3, 4, 5, 6];

  const handleAction = (tableNum, action) => {
    console.log(`Table ${tableNum} - ${action}`);
    // All actions will trigger robot to go to that table
    onStartDelivery(tableNum, action);
  };

  const getStatusText = () => {
    if (!isRunning) return 'Standby';
    return `Going to Table ${targetTable} (${currentAction})...`;
  };

  return (
    <VegaCard padding="size-16">
      <VegaFlex direction="col" gap="size-16" >
        <VegaFont variant="font-h4" class="v-text-primary">Control Panel</VegaFont>
        <VegaFlex alignItems="center" gap="size-12">
          <span className={`status-indicator ${isRunning ? 'running' : 'idle'}`}></span>
          <VegaChip 
            variant={isRunning ? 'warning' : 'success'}
            label={getStatusText()}
          />
        </VegaFlex>

        <VegaFlex direction={"col"} gap={'size-12'}>
          <VegaFont variant="font-h6">Table Service:</VegaFont>
          {tables.map(tableNum => (
            <VegaCard key={tableNum} padding="size-16" variant="shadow"  backgroundColor="bg-page">
              <VegaFlex direction="col" justifyContent="space-between" alignItems="start" gap="size-8">
                <VegaFlex gap={"size-8"} alignItems="center">
                  <VegaIcon icon="fa-solid fa-table-picnic" size="size-20" color='text-link-active' />
                  <VegaFont variant="font-p2-short" >Table {tableNum}</VegaFont>
                </VegaFlex>
                
                <VegaFlex gap={"size-12"}>
                  <VegaButton
                    variant="primary"
                    label="Order"
                    size={`small`}
                    icon="fa-solid fa-bell-concierge"
                    onVegaClick={() => handleAction(tableNum, 'Order')}
                    disabled={isRunning}
                  />
                  <VegaButton
                    variant="secondary"
                    size={`small`}
                    icon="fa-solid fa-user"
                    onVegaClick={() => handleAction(tableNum, 'Call')}
                    disabled={isRunning}
                    label='Call'
                  />
                  <VegaButton
                    variant="secondary"
                    danger={true}
                    size={`small`}
                    icon="fa-solid fa-circle-pause"
                    onVegaClick={() => handleAction(tableNum, 'Holding')}
                    disabled={isRunning}
                    label='Holding'
                  />
                   <VegaButton
                    variant="primary"
                    danger={true}
                    size={`small`}
                    icon="fa-solid fa-house-person-leave"
                    onVegaClick={() => handleAction(tableNum, 'Checkout')}
                    disabled={isRunning}
                    label='Checkout'
                  />
                </VegaFlex>
              </VegaFlex>
            </VegaCard>
          ))}
          
        </VegaFlex>
      </VegaFlex>
    </VegaCard>
  );
}

export default ControlPanel;
