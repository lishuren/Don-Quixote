import { useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  VegaBox,
  VegaButton,
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaInput,
  VegaInputSelect,
  VegaModal,
} from "@heartlandone/vega-react";
import Slider from "rc-slider";
import "rc-slider/assets/index.css";
import {
  setTableCount,
  setSpeedRate,
  setRobotCount,
  setGuestCount,
  setMainAisle,
  setMinorAisle,
  setSafetyBuffer,
  setSeatBuffer,
  setTableShape,
  setTableMix,
} from "../store/store.js";
import { getBounds } from "../utils/rules.js";

/**
 * HeadBar component - controls for simulation settings
 * @param {Object} props - Component props
 * @param {Function} props.onStart - Callback when Start button is clicked
 * @param {Function} props.onPause - Callback when Pause button is clicked
 * @param {Function} props.onEnd - Callback when End button is clicked
 * @param {boolean} props.isRunning - Whether simulation is running
 * @param {boolean} props.isPaused - Whether simulation is paused
 */
const HeadBar = ({ onStart, onPause, onEnd, isRunning, isPaused }) => {
  const dispatch = useDispatch();
  const { tableCount, robotCount, guestCount, speedRate, config } = useSelector((state) => state);
  const [showAdvanced, setShowAdvanced] = useState(false);

  const bounds = getBounds();

  const handleTableCountChange = (e) => {
    console.log("Setting table count to", e, parseInt(e.detail));
    dispatch(setTableCount(parseInt(e.detail)));
  };

  const handleRobotCountChange = (e) => {
    const value = parseInt(e.detail) || 1;
    dispatch(setRobotCount(value));
  };

  const handleGuestCountChange = (e) => {
    const value = parseInt(e.detail) || 0;
    dispatch(setGuestCount(value));
  };

  const handleSpeedChange = (value) => {
    dispatch(setSpeedRate(value));
  };

  return (
    <VegaCard padding={"size-12"} style={{width: "100%"}}>
      <VegaFlex gap={"size-24"} alignItems={"stretch"} style={{width: "100%"}}>
        <VegaFlex gap={"size-12"} direction={"col"} alignItems="left" justifyContent="start" flex={4} >
          <VegaFont variant="font-field-label" class="v-text-on-primary">
            Accelerate
          </VegaFont>
          <Slider
            min={0.5}
            max={2}
            step={0.25}
            marks={{ 0.5: "0.5x", 1: "1x", 1.5: "1.5x", 2: "2x" }}
            value={speedRate}
            onChange={handleSpeedChange}
            styles={{
              track: { backgroundColor: "#1677ff" },
              handle: { borderColor: "#1677ff" },
            }}
          />
        </VegaFlex>
        <VegaFlex gap={"size-16"} alignItems="end" justifyContent="flex-end" flex={5}>
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
            <VegaButton 
              icon="fa-solid fa-play" 
              label="Start"
              onVegaClick={onStart}
            />
          ) : (
            <VegaFlex gap="size-8">
              <VegaButton 
                icon={isPaused ? "fa-solid fa-play" : "fa-solid fa-pause"} 
                label={isPaused ? "Resume" : "Pause"}
                variant="secondary"
                onVegaClick={onPause}
              />
              <VegaButton 
                icon="fa-solid fa-stop" 
                label="End"
                variant="primary"
                danger={true}
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
        heading="Advanced Settings"
        size="medium"
      >
        <VegaFlex direction="col" gap="size-16" style={{ padding: "16px" }}>
          {/* Aisle Settings */}
          <VegaFont variant="font-h6">Aisle Widths (px)</VegaFont>
          <VegaFlex gap="size-16">
            <VegaInput
              label="Main Aisle"
              type="number"
              value={config.mainAisle.toString()}
              onVegaChange={(e) => dispatch(setMainAisle(parseInt(e.detail) || 120))}
              min={bounds.mainAisle.min}
              max={bounds.mainAisle.max}
            />
            <VegaInput
              label="Minor Aisle"
              type="number"
              value={config.minorAisle.toString()}
              onVegaChange={(e) => dispatch(setMinorAisle(parseInt(e.detail) || 90))}
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
              value={config.safetyBuffer.toString()}
              onVegaChange={(e) => dispatch(setSafetyBuffer(parseInt(e.detail) || 10))}
              min={bounds.safetyBuffer.min}
              max={bounds.safetyBuffer.max}
            />
            <VegaInput
              label="Seat Buffer"
              type="number"
              value={config.seatBuffer.toString()}
              onVegaChange={(e) => dispatch(setSeatBuffer(parseInt(e.detail) || 20))}
              min={bounds.seatBuffer.min}
              max={bounds.seatBuffer.max}
            />
          </VegaFlex>

          {/* Table Shape */}
          <VegaFont variant="font-h6">Table Settings</VegaFont>
          <VegaInputSelect
            label="Table Shape"
            value={config.tableShape}
            onVegaChange={(e) => dispatch(setTableShape(e.detail))}
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
              value={config.tableMix.t2.toString()}
              onVegaChange={(e) => dispatch(setTableMix({ ...config.tableMix, t2: parseInt(e.detail) || 0 }))}
              min={0}
              max={20}
              disabled={isRunning}
            />
            <VegaInput
              label="4-seat"
              type="number"
              value={config.tableMix.t4.toString()}
              onVegaChange={(e) => dispatch(setTableMix({ ...config.tableMix, t4: parseInt(e.detail) || 0 }))}
              min={0}
              max={20}
              disabled={isRunning}
            />
            <VegaInput
              label="6-seat"
              type="number"
              value={config.tableMix.t6.toString()}
              onVegaChange={(e) => dispatch(setTableMix({ ...config.tableMix, t6: parseInt(e.detail) || 0 }))}
              min={0}
              max={20}
              disabled={isRunning}
            />
            <VegaInput
              label="8-seat"
              type="number"
              value={config.tableMix.t8.toString()}
              onVegaChange={(e) => dispatch(setTableMix({ ...config.tableMix, t8: parseInt(e.detail) || 0 }))}
              min={0}
              max={20}
              disabled={isRunning}
            />
          </VegaFlex>

          <VegaFlex justifyContent="flex-end" gap="size-12" style={{ marginTop: "16px" }}>
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
