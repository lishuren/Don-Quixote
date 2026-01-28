/**
 * @file HeadBar.tsx
 * @description Controls for simulation settings
 * Migrated from HeadBar.jsx with TypeScript support
 */

import { useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  VegaButton,
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaInput,
  VegaInputSelect,
  VegaModal,
} from '@heartlandone/vega-react';
import Slider from 'rc-slider';
import 'rc-slider/assets/index.css';
import {
  setTableCount,
  setSpeedRate,
  setRobotCount,
  setGuestCount,
  setMapConfig,
  type RootState,
  type AppDispatch,
} from '../store/store';
import { getBounds } from '../utils/rules';

interface HeadBarProps {
  onStart: () => void;
  onPause: () => void;
  onEnd: () => void;
  isRunning: boolean;
  isPaused: boolean;
}

interface Bounds {
  tableCount: { min: number; max: number };
  mainAisle: { min: number; max: number };
  minorAisle: { min: number; max: number };
  safetyBuffer: { min: number; max: number };
  seatBuffer: { min: number; max: number };
}

const HeadBar = ({ onStart, onPause, onEnd, isRunning, isPaused }: HeadBarProps) => {
  const dispatch = useDispatch<AppDispatch>();
  const { tableCount, robotCount, guestCount, speedRate, mapConfig: config } = useSelector(
    (state: RootState) => state
  );
  const [showAdvanced, setShowAdvanced] = useState(false);

  const bounds: Bounds = getBounds() as Bounds;

  const handleTableCountChange = (e: CustomEvent<string>) => {
    console.log('Setting table count to', e, parseInt(e.detail));
    dispatch(setTableCount(parseInt(e.detail)));
  };

  const handleRobotCountChange = (e: CustomEvent<string>) => {
    const value = parseInt(e.detail) || 1;
    dispatch(setRobotCount(value));
  };

  const handleGuestCountChange = (e: CustomEvent<string>) => {
    const value = parseInt(e.detail) || 0;
    dispatch(setGuestCount(value));
  };

  const handleSpeedChange = (value: number | number[]) => {
    if (typeof value === 'number') {
      dispatch(setSpeedRate(value));
    }
  };

  const configAny = config as any;

  return (
    <VegaCard padding="size-12" style={{ width: '100%' }}>
      <VegaFlex gap="size-24" alignItems="stretch" style={{ width: '100%', paddingLeft: "8px", paddingRight: "8px"}}>
        <VegaFlex gap="size-12" direction="col" alignItems="start" justifyContent="start" flex={1}>
          <VegaFont variant="font-field-label" class="v-text-on-primary">
            Accelerate
          </VegaFont>
          <Slider
            min={0.5}
            max={2}
            step={0.25}
            marks={{ 0.5: '0.5x', 1: '1x', 1.5: '1.5x', 2: '2x' }}
            value={speedRate}
            onChange={handleSpeedChange}
            styles={{
              track: { backgroundColor: '#1677ff' },
              handle: { borderColor: '#1677ff' },
            }}
          />
        </VegaFlex>
        <VegaFlex gap="size-16" alignItems="end" justifyContent="end" >
          <VegaInput
            size="small"
            label="Tables"
            type="number"
            value={tableCount.toString()}
            onVegaChange={handleTableCountChange}
            min={bounds.tableCount.min}
            max={bounds.tableCount.max}
            disabled={isRunning}
          />
          <VegaInput
            size="small"
            label="Robots"
            type="number"
            value={robotCount.toString()}
            disabled={isRunning}
            onVegaChange={handleRobotCountChange}
            min={1}
            max={10}
          />
          <VegaInput
            size="small"
            label="Guests"
            type="number"
            value={guestCount.toString()}
            onVegaChange={handleGuestCountChange}
            min={0}
            max={100}
            disabled={isRunning}
          />
          <VegaButton
            variant="secondary"
            icon="fa-solid fa-gear"
            label="Settings"
            disabled={isRunning}
            onVegaClick={() => setShowAdvanced(true)}
          />
          {!isRunning ? (
            <VegaButton icon="fa-solid fa-play" label="Start" onVegaClick={onStart} />
          ) : (
            <VegaFlex gap="size-8">
              <VegaButton
                icon={isPaused ? 'fa-solid fa-play' : 'fa-solid fa-pause'}
                label={isPaused ? 'Resume' : 'Pause'}
                variant="secondary"
                onVegaClick={onPause}
              />
              <VegaButton
                icon="fa-solid fa-stop"
                label="End"
                variant="primary"
                danger
                onVegaClick={onEnd}
              />
            </VegaFlex>
          )}
        </VegaFlex>
      </VegaFlex>

      {/* Advanced Settings Modal */}
      <VegaModal
        open={showAdvanced}
        onVegaClose={() => setShowAdvanced(false)}
      >
        <VegaFlex direction="col" gap="size-16" style={{ padding: '16px' }}>
          <VegaFont variant="font-h5">Advanced Settings</VegaFont>
          {/* Aisle Settings */}
          <VegaFont variant="font-h6">Aisle Widths (px)</VegaFont>
          <VegaFlex gap="size-16">
            <VegaInput
              label="Main Aisle"
              type="number"
              value={(configAny.mainAisle || 120).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(setMapConfig({ mainAisle: parseInt(e.detail) || 120 } as any))
              }
              min={bounds.mainAisle.min}
              max={bounds.mainAisle.max}
            />
            <VegaInput
              label="Minor Aisle"
              type="number"
              value={(configAny.minorAisle || 90).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(setMapConfig({ minorAisle: parseInt(e.detail) || 90 } as any))
              }
              min={bounds.minorAisle.min}
              max={bounds.minorAisle.max}
            />
          </VegaFlex>

          {/* Buffer Settings */}
          <VegaFont variant="font-h6">Buffers (px)</VegaFont>
          <VegaFlex gap="size-16">
            <VegaInput
              label="Safety Buffer"
              type="number"
              value={(configAny.safetyBuffer || 10).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(setMapConfig({ safetyBuffer: parseInt(e.detail) || 10 } as any))
              }
              min={bounds.safetyBuffer.min}
              max={bounds.safetyBuffer.max}
            />
            <VegaInput
              label="Seat Buffer"
              type="number"
              value={(configAny.seatBuffer || 20).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(setMapConfig({ seatBuffer: parseInt(e.detail) || 20 } as any))
              }
              min={bounds.seatBuffer.min}
              max={bounds.seatBuffer.max}
            />
          </VegaFlex>

          {/* Table Shape */}
          <VegaFont variant="font-h6">Table Settings</VegaFont>
          <VegaInputSelect
            label="Table Shape"
            value={configAny.tableShape || 'rect'}
            onVegaChange={(e) => {
              const val = e.detail;
              dispatch(setMapConfig({ tableShape: typeof val === 'string' ? val : 'rect' } as Partial<RootState['mapConfig']>));
            }}
            disabled={isRunning}
          >
            <option value="rect">Rectangle</option>
            <option value="round">Round</option>
            <option value="square">Square</option>
          </VegaInputSelect>

          {/* Table Mix */}
          <VegaFont variant="font-h6">Table Mix (ratio)</VegaFont>
          <VegaFlex gap="size-12">
            <VegaInput
              label="2-seat"
              type="number"
              value={(configAny.tableMix?.t2 || 0).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(
                  setMapConfig({
                    tableMix: { ...configAny.tableMix, t2: parseInt(e.detail) || 0 },
                  } as any)
                )
              }
              min={0}
              max={20}
              disabled={isRunning}
            />
            <VegaInput
              label="4-seat"
              type="number"
              value={(configAny.tableMix?.t4 || 0).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(
                  setMapConfig({
                    tableMix: { ...configAny.tableMix, t4: parseInt(e.detail) || 0 },
                  } as any)
                )
              }
              min={0}
              max={20}
              disabled={isRunning}
            />
            <VegaInput
              label="6-seat"
              type="number"
              value={(configAny.tableMix?.t6 || 0).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(
                  setMapConfig({
                    tableMix: { ...configAny.tableMix, t6: parseInt(e.detail) || 0 },
                  } as any)
                )
              }
              min={0}
              max={20}
              disabled={isRunning}
            />
            <VegaInput
              label="8-seat"
              type="number"
              value={(configAny.tableMix?.t8 || 0).toString()}
              onVegaChange={(e: CustomEvent<string>) =>
                dispatch(
                  setMapConfig({
                    tableMix: { ...configAny.tableMix, t8: parseInt(e.detail) || 0 },
                  } as any)
                )
              }
              min={0}
              max={20}
              disabled={isRunning}
            />
          </VegaFlex>

          <VegaFlex justifyContent="end" gap="size-12" style={{ marginTop: '16px' }}>
            <VegaButton
              variant="secondary"
              label="Close"
              onVegaClick={() => setShowAdvanced(false)}
            />
          </VegaFlex>
        </VegaFlex>
      </VegaModal>
    </VegaCard>
  );
};

export default HeadBar;
