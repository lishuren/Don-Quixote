/**
 * @file SimulationDashboard.tsx
 * @description Long-run simulation dashboard with real-time progress
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 9 (Long-Run Simulation)
 */

import { useState, useEffect } from 'react';
import {
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaButton,
  VegaIcon,
  VegaGrid,
} from '@heartlandone/vega-react';
import { useSignalR } from '../hooks/useSignalR';
import { simulationApi } from '../services/simulationApi';
import type { SimulationProgressDto, SimulationReportDto } from '../types/dtos';
import type { SimulationState } from '../types/enums';

const stateStyles: Record<SimulationState, { bg: string; color: string; icon: string }> = {
  Running: { bg: '#e6f4ea', color: '#1a7f37', icon: 'fa-solid fa-play' },
  Paused: { bg: '#ffd', color: '#a60', icon: 'fa-solid fa-pause' },
  Completed: { bg: '#eef', color: '#06c', icon: 'fa-solid fa-check' },
  Cancelled: { bg: '#eee', color: '#666', icon: 'fa-solid fa-stop' },
};

interface MetricCardProps {
  label: string;
  value: string | number;
  icon?: string;
}

function MetricCard({ label, value, icon }: MetricCardProps) {
  return (
    <VegaCard padding="size-12" variant="shadow">
      <VegaFlex direction="col" gap="size-4">
        <VegaFlex gap="size-4" alignItems="center">
          {icon && <VegaIcon icon={icon} size="size-12" color="text-secondary" />}
          <VegaFont variant="font-field-label" class="v-text-secondary">
            {label}
          </VegaFont>
        </VegaFlex>
        <VegaFont variant="font-h5" class="v-text-primary">
          {value}
        </VegaFont>
      </VegaFlex>
    </VegaCard>
  );
}

function ProgressBar({ value, max }: { value: number; max: number }) {
  const percent = Math.min(100, Math.max(0, (value / max) * 100));
  return (
    <div style={{ width: '100%', height: '8px', backgroundColor: '#e0e0e0', borderRadius: '4px', overflow: 'hidden' }}>
      <div
        style={{
          width: `${percent}%`,
          height: '100%',
          backgroundColor: '#06c',
          borderRadius: '4px',
          transition: 'width 0.3s ease',
        }}
      />
    </div>
  );
}

export function SimulationDashboard() {
  const [progress, setProgress] = useState<SimulationProgressDto | null>(null);
  const [report, setReport] = useState<SimulationReportDto | null>(null);
  const [isStarting, setIsStarting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { subscribeToSimulation, unsubscribeFromSimulation, onSimulationProgressUpdated } =
    useSignalR();

  useEffect(() => {
    // Subscribe to real-time updates
    subscribeToSimulation();

    onSimulationProgressUpdated((event) => {
      setProgress({
        simulationId: event.simulationId,
        state: event.state,
        currentSimulatedTime: event.currentSimulatedTime,
        progressPercent: event.progressPercent,
        realElapsedTime: event.realElapsedTime,
        estimatedTimeRemaining: event.estimatedTimeRemaining,
        eventsProcessed: event.eventsProcessed,
        totalEventsScheduled: event.totalEventsScheduled,
        guestsProcessed: event.guestsProcessed,
        tasksCreated: event.tasksCreated,
        tasksCompleted: event.tasksCompleted,
        currentSuccessRate: event.currentSuccessRate,
        simulatedStartTime: '',
        simulatedEndTime: '',
        accelerationFactor: 0,
      });

      // Load report when completed
      if (event.state === 'Completed') {
        simulationApi.getReport().then(setReport).catch(console.error);
      }
    });

    // Check for existing simulation
    simulationApi
      .getProgress()
      .then(setProgress)
      .catch(() => {
        // No active simulation
      });

    return () => {
      unsubscribeFromSimulation();
    };
  }, [subscribeToSimulation, unsubscribeFromSimulation, onSimulationProgressUpdated]);

  const handleStart = async () => {
    setIsStarting(true);
    setReport(null);
    setError(null);
    try {
      const now = new Date();
      const oneMonthLater = new Date(now);
      oneMonthLater.setMonth(oneMonthLater.getMonth() + 1);

      await simulationApi.start({
        simulatedStartTime: now.toISOString(),
        simulatedEndTime: oneMonthLater.toISOString(),
        accelerationFactor: 720, // 1 month in ~1 hour
        robotCount: 5,
        tableCount: 20,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start simulation');
    } finally {
      setIsStarting(false);
    }
  };

  const handlePause = () => {
    simulationApi.pause().catch(console.error);
  };

  const handleResume = () => {
    simulationApi.resume().catch(console.error);
  };

  const handleStop = () => {
    simulationApi.stop().catch(console.error);
  };

  if (!progress) {
    return (
      <VegaCard padding="size-24">
        <VegaFlex direction="col" gap="size-16" alignItems="center">
          <VegaIcon icon="fa-solid fa-chart-line" size="size-48" color="text-link-active" />
          <VegaFont variant="font-h4" class="v-text-primary">
            Long-Run Simulation
          </VegaFont>
          <VegaFont variant="font-p1-short" class="v-text-secondary">
            Simulate 1 month of restaurant operations in ~1 hour
          </VegaFont>
          {error && (
            <VegaCard padding="size-8" style={{ backgroundColor: '#fee' }}>
              <VegaFont variant="font-p2-short" style={{ color: '#c00' }}>
                {error}
              </VegaFont>
            </VegaCard>
          )}
          <VegaButton
            variant="primary"
            size="large"
            label={isStarting ? 'Starting...' : 'Start Monthly Simulation (720x)'}
            icon="fa-solid fa-play"
            onVegaClick={handleStart}
            disabled={isStarting}
          />
        </VegaFlex>
      </VegaCard>
    );
  }

  const style = stateStyles[progress.state] || stateStyles.Running;

  return (
    <VegaCard padding="size-16">
      <VegaFlex direction="col" gap="size-16">
        <VegaFlex justifyContent="space-between" alignItems="center">
          <VegaFlex gap="size-12" alignItems="center">
            <VegaIcon icon="fa-solid fa-chart-line" size="size-24" color="text-link-active" />
            <VegaFont variant="font-h5">Simulation: {progress.simulationId.slice(0, 8)}...</VegaFont>
            <span
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '4px',
                padding: '4px 10px',
                borderRadius: '4px',
                fontSize: '12px',
                fontWeight: 500,
                backgroundColor: style.bg,
                color: style.color,
              }}
            >
              <VegaIcon icon={style.icon} size="size-12" />
              {progress.state}
            </span>
          </VegaFlex>

          <VegaFlex gap="size-8">
            {progress.state === 'Running' && (
              <VegaButton
                variant="secondary"
                size="small"
                label="Pause"
                icon="fa-solid fa-pause"
                onVegaClick={handlePause}
              />
            )}
            {progress.state === 'Paused' && (
              <VegaButton
                variant="primary"
                size="small"
                label="Resume"
                icon="fa-solid fa-play"
                onVegaClick={handleResume}
              />
            )}
            {(progress.state === 'Running' || progress.state === 'Paused') && (
              <VegaButton
                variant="primary"
                danger
                size="small"
                label="Stop"
                icon="fa-solid fa-stop"
                onVegaClick={handleStop}
              />
            )}
          </VegaFlex>
        </VegaFlex>

        {/* Progress Bar */}
        <VegaFlex direction="col" gap="size-8">
          <VegaFlex justifyContent="space-between">
            <VegaFont variant="font-p2-short" class="v-text-secondary">
              Progress
            </VegaFont>
            <VegaFont variant="font-p2-short" class="v-text-primary">
              {progress.progressPercent.toFixed(1)}%
            </VegaFont>
          </VegaFlex>
          <ProgressBar value={progress.progressPercent} max={100} />
        </VegaFlex>

        {/* Metrics Grid */}
        <VegaGrid column={4} gap="size-12">
          <MetricCard
            label="Simulated Time"
            value={new Date(progress.currentSimulatedTime).toLocaleDateString()}
            icon="fa-solid fa-calendar"
          />
          <MetricCard
            label="Real Elapsed"
            value={progress.realElapsedTime}
            icon="fa-solid fa-clock"
          />
          <MetricCard
            label="ETA"
            value={progress.estimatedTimeRemaining}
            icon="fa-solid fa-hourglass-half"
          />
          <MetricCard
            label="Success Rate"
            value={`${progress.currentSuccessRate?.toFixed(1) || 0}%`}
            icon="fa-solid fa-percent"
          />
        </VegaGrid>

        <VegaGrid column={4} gap="size-12">
          <MetricCard
            label="Events Processed"
            value={`${progress.eventsProcessed.toLocaleString()} / ${progress.totalEventsScheduled.toLocaleString()}`}
            icon="fa-solid fa-bolt"
          />
          <MetricCard
            label="Guests Processed"
            value={progress.guestsProcessed.toLocaleString()}
            icon="fa-solid fa-users"
          />
          <MetricCard
            label="Tasks Created"
            value={progress.tasksCreated.toLocaleString()}
            icon="fa-solid fa-list-check"
          />
          <MetricCard
            label="Tasks Completed"
            value={progress.tasksCompleted.toLocaleString()}
            icon="fa-solid fa-check"
          />
        </VegaGrid>

        {/* Report Section */}
        {report && (
          <VegaCard padding="size-16" style={{ backgroundColor: '#e6f4ea' }}>
            <VegaFlex direction="col" gap="size-12">
              <VegaFlex gap="size-8" alignItems="center">
                <VegaIcon icon="fa-solid fa-file-chart-pie" size="size-20" color="text-link-active" />
                <VegaFont variant="font-h6" style={{ color: '#1a7f37' }}>
                  Final Report
                </VegaFont>
              </VegaFlex>

              <VegaGrid column={3} gap="size-12">
                <MetricCard
                  label="Total Guests"
                  value={report.summary.totalGuests.toLocaleString()}
                />
                <MetricCard
                  label="Total Tasks"
                  value={report.summary.totalTasks.toLocaleString()}
                />
                <MetricCard
                  label="Overall Success Rate"
                  value={`${report.summary.overallSuccessRate.toFixed(1)}%`}
                />
                <MetricCard
                  label="Avg Task Duration"
                  value={`${report.summary.averageTaskDurationSeconds.toFixed(0)}s`}
                />
                <MetricCard
                  label="Peak Concurrent Guests"
                  value={report.summary.peakConcurrentGuests}
                />
                <MetricCard
                  label="Robot Utilization"
                  value={`${(report.summary.averageRobotUtilization * 100).toFixed(1)}%`}
                />
              </VegaGrid>
            </VegaFlex>
          </VegaCard>
        )}
      </VegaFlex>
    </VegaCard>
  );
}

export default SimulationDashboard;
