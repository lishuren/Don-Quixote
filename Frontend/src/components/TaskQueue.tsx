/**
 * @file TaskQueue.tsx
 * @description Task queue component with real-time updates
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 7 (Task Queue Component)
 */

import { useState, useEffect } from 'react';
import {
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaButton,
  VegaIcon,
  VegaInputSelect,
} from '@heartlandone/vega-react';
import { useSignalR } from '../hooks/useSignalR';
import { tasksApi } from '../services/tasksApi';
import { robotsApi } from '../services/robotsApi';
import { dispatchApi } from '../services/dispatchApi';
import type { TaskDto, RobotDto } from '../types/dtos';
import type { TaskPriority, TaskType } from '../types/enums';

const priorityStyles: Record<TaskPriority, { bg: string; color: string; weight: number }> = {
  Urgent: { bg: '#fee', color: '#c00', weight: 3 },
  High: { bg: '#ffd', color: '#a60', weight: 2 },
  Normal: { bg: '#eef', color: '#06c', weight: 1 },
  Low: { bg: '#eee', color: '#666', weight: 0 },
};

const typeIcons: Record<TaskType, string> = {
  Deliver: 'fa-solid fa-truck',
  Return: 'fa-solid fa-rotate-left',
  Charge: 'fa-solid fa-bolt',
  Patrol: 'fa-solid fa-route',
  Escort: 'fa-solid fa-person-walking',
  Greeting: 'fa-solid fa-hand-wave',
  Service: 'fa-solid fa-bell-concierge',
  Cleaning: 'fa-solid fa-broom',
  Custom: 'fa-solid fa-gear',
};

interface TaskRowProps {
  task: TaskDto;
  idleRobots: RobotDto[];
  onAssign: (taskId: number, robotId: number) => void;
}

function TaskRow({ task, idleRobots, onAssign }: TaskRowProps) {
  const style = priorityStyles[task.priority] || priorityStyles.Normal;
  const icon = typeIcons[task.type] || typeIcons.Custom;

  const handleAssign = (e: CustomEvent<string>) => {
    const robotId = parseInt(e.detail);
    if (robotId) {
      onAssign(task.id, robotId);
    }
  };

  return (
    <VegaCard padding="size-12" variant="shadow" style={{ marginBottom: '8px' }}>
      <VegaFlex alignItems="center" gap="size-12">
        <VegaFlex gap="size-8" alignItems="center" style={{ minWidth: '60px' }}>
          <VegaFont variant="font-field-label" class="v-text-secondary">
            #{task.id}
          </VegaFont>
        </VegaFlex>

        <VegaFlex gap="size-8" alignItems="center" style={{ minWidth: '100px' }}>
          <VegaIcon icon={icon} size="size-16" color="text-secondary" />
          <VegaFont variant="font-p2-short">{task.type}</VegaFont>
        </VegaFlex>

        <span
          style={{
            display: 'inline-block',
            padding: '2px 8px',
            borderRadius: '4px',
            fontSize: '12px',
            fontWeight: 500,
            backgroundColor: style.bg,
            color: style.color,
          }}
        >
          {task.priority}
        </span>

        <VegaFlex gap="size-4" alignItems="center" style={{ minWidth: '80px' }}>
          {task.targetTableLabel ? (
            <>
              <VegaIcon icon="fa-solid fa-table-picnic" size="size-12" color="text-secondary" />
              <VegaFont variant="font-p2-short">{task.targetTableLabel}</VegaFont>
            </>
          ) : (
            <VegaFont variant="font-p2-short" class="v-text-secondary">
              N/A
            </VegaFont>
          )}
        </VegaFlex>

        <VegaFont variant="font-p2-short" class="v-text-secondary" style={{ minWidth: '70px' }}>
          {new Date(task.createdAt).toLocaleTimeString()}
        </VegaFont>

        <div style={{ flex: 1 }}>
          <VegaInputSelect
            size="small"
            placeholder="Assign to..."
            onVegaChange={handleAssign as (e: CustomEvent<string | string[] | null>) => void}
            disabled={idleRobots.length === 0}
          >
            <option value="">Select robot...</option>
            {idleRobots.map((robot) => (
              <option key={robot.id} value={robot.id.toString()}>
                {robot.name} ({robot.batteryLevel}%)
              </option>
            ))}
          </VegaInputSelect>
        </div>
      </VegaFlex>
    </VegaCard>
  );
}

export function TaskQueue() {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [idleRobots, setIdleRobots] = useState<RobotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [assigning, setAssigning] = useState(false);
  const { onTaskStatusChanged, subscribeToTasks } = useSignalR();

  useEffect(() => {
    const loadData = async () => {
      try {
        const [taskData, robotData] = await Promise.all([
          tasksApi.getPending(),
          robotsApi.getIdle(),
        ]);
        setTasks(taskData);
        setIdleRobots(robotData);
      } catch (err) {
        console.error('Failed to load data:', err);
      } finally {
        setLoading(false);
      }
    };

    loadData();

    // Subscribe to task updates
    subscribeToTasks();
    onTaskStatusChanged((event) => {
      if (event.newStatus === 'Pending') {
        // New pending task - refetch
        tasksApi.get(event.taskId).then((task) => {
          setTasks((prev) => [...prev, task]);
        });
      } else {
        // Task no longer pending
        setTasks((prev) => prev.filter((t) => t.id !== event.taskId));
      }

      // Also refresh idle robots when tasks change
      robotsApi.getIdle().then(setIdleRobots);
    });
  }, [onTaskStatusChanged, subscribeToTasks]);

  const handleAutoAssign = async () => {
    try {
      setAssigning(true);
      const result = await dispatchApi.triggerAutoAssign();
      alert(`Assigned ${result.tasksAssigned} tasks, ${result.tasksSkipped} skipped`);
      
      // Refresh data
      const [taskData, robotData] = await Promise.all([
        tasksApi.getPending(),
        robotsApi.getIdle(),
      ]);
      setTasks(taskData);
      setIdleRobots(robotData);
    } catch (err) {
      console.error('Auto-assign failed:', err);
      alert('Auto-assign failed');
    } finally {
      setAssigning(false);
    }
  };

  const handleManualAssign = async (taskId: number, robotId: number) => {
    try {
      await tasksApi.assign(taskId, robotId);
      // Lists will update via SignalR
    } catch (err) {
      console.error('Assignment failed:', err);
      alert('Assignment failed');
    }
  };

  // Sort by priority (descending) then creation time
  const sortedTasks = [...tasks].sort((a, b) => {
    const priorityDiff =
      (priorityStyles[b.priority]?.weight || 0) - (priorityStyles[a.priority]?.weight || 0);
    if (priorityDiff !== 0) return priorityDiff;
    return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
  });

  return (
    <VegaCard padding="size-16" style={{ height: '100%' }}>
      <VegaFlex direction="col" gap="size-12" style={{ height: '100%' }}>
        <VegaFlex justifyContent="space-between" alignItems="center">
          <VegaFlex gap="size-8" alignItems="center">
            <VegaIcon icon="fa-solid fa-list-check" size="size-24" color="text-link-active" />
            <VegaFont variant="font-h5" class="v-text-primary">
              Pending Tasks
            </VegaFont>
            {tasks.length > 0 && (
              <span
                style={{
                  display: 'inline-block',
                  padding: '2px 8px',
                  borderRadius: '4px',
                  fontSize: '12px',
                  fontWeight: 500,
                  backgroundColor: '#ffd',
                  color: '#a60',
                }}
              >
                {tasks.length}
              </span>
            )}
          </VegaFlex>

          <VegaButton
            variant="primary"
            size="small"
            label="Auto-Assign All"
            icon="fa-solid fa-wand-magic-sparkles"
            onVegaClick={handleAutoAssign}
            disabled={tasks.length === 0 || idleRobots.length === 0 || assigning}
          />
        </VegaFlex>

        {idleRobots.length === 0 && tasks.length > 0 && (
          <VegaCard padding="size-8" style={{ backgroundColor: '#fffbe6' }}>
            <VegaFlex gap="size-8" alignItems="center">
              <VegaIcon icon="fa-solid fa-warning" size="size-16" color="text-secondary" />
              <VegaFont variant="font-p2-short" style={{ color: '#a60' }}>
                No idle robots available for assignment
              </VegaFont>
            </VegaFlex>
          </VegaCard>
        )}

        <div style={{ flex: 1, overflowY: 'auto' }}>
          {loading ? (
            <VegaFont variant="font-p2-short" class="v-text-secondary">
              Loading tasks...
            </VegaFont>
          ) : sortedTasks.length === 0 ? (
            <VegaFlex
              direction="col"
              gap="size-8"
              alignItems="center"
              justifyContent="center"
              style={{ height: '100px' }}
            >
              <VegaIcon icon="fa-solid fa-check-circle" size="size-32" color="text-link-active" />
              <VegaFont variant="font-p2-short" class="v-text-secondary">
                No pending tasks
              </VegaFont>
            </VegaFlex>
          ) : (
            sortedTasks.map((task) => (
              <TaskRow
                key={task.id}
                task={task}
                idleRobots={idleRobots}
                onAssign={handleManualAssign}
              />
            ))
          )}
        </div>
      </VegaFlex>
    </VegaCard>
  );
}

export default TaskQueue;
