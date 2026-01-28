/**
 * @file ControlPanel.tsx
 * @description Table and robot service controls
 * Migrated from ControlPanel.jsx with TypeScript support
 */

import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import {
  VegaCard,
  VegaButton,
  VegaFlex,
  VegaFont,
  VegaBox,
  VegaGrid,
  VegaIcon,
  VegaDivider,
} from '@heartlandone/vega-react';
import type { RootState } from '../store/store';

interface ControlPanelProps {
  onStartDelivery: (tableNumber: number, action?: string) => void;
  isRunning: boolean;
  targetTable: number | null;
  currentAction: string | null;
  showMap: boolean;
}

function ControlPanel({
  onStartDelivery,
  isRunning,
  targetTable,
  currentAction,
  showMap,
}: ControlPanelProps) {
  const { tableCount, robotCount } = useSelector((state: RootState) => state);

  // Generate table list based on tableCount
  const tables = useMemo(() => {
    return Array.from({ length: tableCount }, (_, i) => i + 1);
  }, [tableCount]);

  // Generate robot list based on robotCount
  const robots = useMemo(() => {
    return Array.from({ length: robotCount }, (_, i) => i + 1);
  }, [robotCount]);

  const handleAction = (tableNum: number, action: string) => {
    console.log(`Table ${tableNum} - ${action}`);
    onStartDelivery(tableNum, action);
  };

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const getStatusText = () => {
    if (!isRunning) return 'Standby';
    return `Going to Table ${targetTable} (${currentAction})...`;
  };

  return (
    <VegaCard padding="size-16" style={{ height: '100%' }}>
      <VegaFlex direction="col" gap="size-12" style={{ height: '100%' }}>
        <VegaFont variant="font-h4" class="v-text-primary">
          Control Panel
        </VegaFont>
        <VegaFlex direction="col" gap="size-12" style={{ height: '70%' }}>
          <VegaFont variant="font-h6">
            Table Service{' '}
            <VegaFont variant="font-field-label-xs-em">Total ({tables.length}):</VegaFont>
          </VegaFont>
          <div style={{ position: 'relative', height: '90%' }}>
            <VegaFlex direction="col" gap="size-12" style={{ overflowY: 'auto', height: '100%' }}>
              {tables.map((tableNum) => (
                <VegaCard key={tableNum} padding="size-16" variant="shadow" backgroundColor="bg-page">
                  <VegaFlex
                    direction="col"
                    justifyContent="space-between"
                    alignItems="start"
                    gap="size-8"
                  >
                    <VegaFlex gap="size-8" alignItems="center">
                      <VegaIcon
                        icon="fa-solid fa-table-picnic"
                        size="size-20"
                        color="text-link-active"
                      />
                      <VegaFont variant="font-p2-short">Table {tableNum}</VegaFont>
                    </VegaFlex>
                    <VegaFlex gap="size-12">
                      <VegaButton
                        variant="primary"
                        label="Order"
                        size="small"
                        icon="fa-solid fa-bell-concierge"
                        onVegaClick={() => handleAction(tableNum, 'Order')}
                        disabled={!showMap}
                      />
                      <VegaButton
                        variant="secondary"
                        size="small"
                        icon="fa-solid fa-user"
                        onVegaClick={() => handleAction(tableNum, 'Call')}
                        disabled={!showMap}
                        label="Call"
                      />
                      <VegaButton
                        variant="secondary"
                        danger
                        size="small"
                        icon="fa-solid fa-circle-pause"
                        onVegaClick={() => handleAction(tableNum, 'Holding')}
                        disabled={!showMap}
                        label="Holding"
                      />
                      <VegaButton
                        variant="primary"
                        danger
                        size="small"
                        icon="fa-solid fa-house-person-leave"
                        onVegaClick={() => handleAction(tableNum, 'Checkout')}
                        disabled={!showMap}
                        label="Checkout"
                      />
                    </VegaFlex>
                  </VegaFlex>
                </VegaCard>
              ))}
            </VegaFlex>
            {/* linear-gradient */}
            <div
              style={{
                position: 'absolute',
                left: 0,
                right: 0,
                bottom: 0,
                height: '32px',
                pointerEvents: 'none',
                background: 'linear-gradient(to bottom, rgba(255,255,255,0) 0%, #fff 100%)',
              }}
            />
          </div>
        </VegaFlex>
        <VegaDivider />
        <VegaFlex direction="col" gap="size-12" style={{ height: '19%' }}>
          <VegaFont variant="font-h6">
            Robot Service{' '}
            <VegaFont variant="font-field-label-xs-em">Total ({robots.length}):</VegaFont>
          </VegaFont>
          <VegaBox style={{ overflowY: 'auto', height: '90%' }}>
            <VegaGrid column={3} gap="size-12">
              {robots.map((robotNum) => (
                <VegaBox key={robotNum}>
                  <VegaFlex gap="size-8" alignItems="center">
                    <VegaIcon icon="fa-solid fa-robot" size="size-20" color="text-link-active" />
                    <VegaFont variant="font-p2-short">Robot {robotNum}</VegaFont>
                  </VegaFlex>
                </VegaBox>
              ))}
            </VegaGrid>
          </VegaBox>
        </VegaFlex>
      </VegaFlex>
    </VegaCard>
  );
}

export default ControlPanel;
