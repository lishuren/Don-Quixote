/**
 * @file KitchenDisplay.tsx
 * @description Kitchen queue display with food, drink, and cleaning queues
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 8 (KitchenManager)
 */

import { useState, useEffect, useCallback } from 'react';
import {
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaButton,
  VegaIcon,
  VegaGrid,
} from '@heartlandone/vega-react';
import { kitchenManager, FoodItem, DrinkItem } from '../services/KitchenManager';
// import { kitchenApi } from '../services/kitchenApi'; // Reserved for future API integration
import type { FoodTask, DrinkTask, CleanTask } from '../types/kitchen';

interface FoodTaskCardProps {
  task: FoodTask;
  onReady: (orderId: string) => void;
}

function FoodTaskCard({ task, onReady }: FoodTaskCardProps) {
  const elapsedMs = Date.now() - (task.startedAt || task.createdAt);
  const elapsedMins = Math.floor(elapsedMs / 60000);

  return (
    <VegaCard padding="size-12" variant="shadow" style={{ marginBottom: '8px' }}>
      <VegaFlex direction="col" gap="size-8">
        <VegaFlex justifyContent="space-between" alignItems="center">
          <VegaFont variant="font-field-label" class="v-text-primary">
            {task.orderId}
          </VegaFont>
          <span
            style={{
              padding: '2px 8px',
              borderRadius: '4px',
              fontSize: '12px',
              backgroundColor: elapsedMins > 15 ? '#fee' : elapsedMins > 10 ? '#ffd' : '#eef',
              color: elapsedMins > 15 ? '#c00' : elapsedMins > 10 ? '#a60' : '#06c',
            }}
          >
            {elapsedMins}m
          </span>
        </VegaFlex>

        <VegaFlex gap="size-4" alignItems="center">
          <VegaIcon icon="fa-solid fa-table-picnic" size="size-12" color="text-secondary" />
          <VegaFont variant="font-p2-short">{task.tableLabel || `Table ${task.tableId}`}</VegaFont>
        </VegaFlex>

        <VegaFont variant="font-p2-short" class="v-text-secondary">
          {task.items.map((item) => (typeof item === 'string' ? item : String(item))).join(', ')}
        </VegaFont>

        <VegaButton
          variant="primary"
          size="small"
          label="Ready for Pickup"
          icon="fa-solid fa-check"
          onVegaClick={() => onReady(task.orderId)}
        />
      </VegaFlex>
    </VegaCard>
  );
}

interface DrinkTaskCardProps {
  task: DrinkTask;
  onReady: (orderId: string) => void;
}

function DrinkTaskCard({ task, onReady }: DrinkTaskCardProps) {
  const elapsedMs = Date.now() - (task.startedAt || task.createdAt);
  const elapsedMins = Math.floor(elapsedMs / 60000);

  return (
    <VegaCard padding="size-12" variant="shadow" style={{ marginBottom: '8px' }}>
      <VegaFlex direction="col" gap="size-8">
        <VegaFlex justifyContent="space-between" alignItems="center">
          <VegaFont variant="font-field-label" class="v-text-primary">
            {task.orderId}
          </VegaFont>
          <span
            style={{
              padding: '2px 8px',
              borderRadius: '4px',
              fontSize: '12px',
              backgroundColor: elapsedMins > 5 ? '#ffd' : '#eef',
              color: elapsedMins > 5 ? '#a60' : '#06c',
            }}
          >
            {elapsedMins}m
          </span>
        </VegaFlex>

        <VegaFlex gap="size-4" alignItems="center">
          <VegaIcon icon="fa-solid fa-table-picnic" size="size-12" color="text-secondary" />
          <VegaFont variant="font-p2-short">{task.tableLabel || `Table ${task.tableId}`}</VegaFont>
        </VegaFlex>

        <VegaFont variant="font-p2-short" class="v-text-secondary">
          {task.items.map((item) => (typeof item === 'string' ? item : String(item))).join(', ')}
        </VegaFont>

        <VegaButton
          variant="secondary"
          size="small"
          label="Ready"
          icon="fa-solid fa-martini-glass"
          onVegaClick={() => onReady(task.orderId)}
        />
      </VegaFlex>
    </VegaCard>
  );
}

interface CleanTaskCardProps {
  task: CleanTask;
  onComplete: (tableId: number) => void;
}

function CleanTaskCard({ task, onComplete }: CleanTaskCardProps) {
  const elapsedMs = Date.now() - (task.requestedAt || task.createdAt);
  const elapsedMins = Math.floor(elapsedMs / 60000);

  return (
    <VegaCard padding="size-12" variant="shadow" style={{ marginBottom: '8px' }}>
      <VegaFlex justifyContent="space-between" alignItems="center">
        <VegaFlex direction="col" gap="size-4">
          <VegaFont variant="font-field-label" class="v-text-primary">
            {task.tableLabel || `Table ${task.tableId}`}
          </VegaFont>
          <span
            style={{
              padding: '2px 8px',
              borderRadius: '4px',
              fontSize: '11px',
              backgroundColor: elapsedMins > 10 ? '#fee' : '#eef',
              color: elapsedMins > 10 ? '#c00' : '#06c',
            }}
          >
            Waiting {elapsedMins}m
          </span>
        </VegaFlex>
        <VegaButton
          variant="secondary"
          size="small"
          label="Done"
          icon="fa-solid fa-broom"
          onVegaClick={() => onComplete(task.tableId)}
        />
      </VegaFlex>
    </VegaCard>
  );
}

export function KitchenDisplay() {
  const [foodQueue, setFoodQueue] = useState<FoodTask[]>([]);
  const [drinkQueue, setDrinkQueue] = useState<DrinkTask[]>([]);
  const [cleanQueue, setCleanQueue] = useState<CleanTask[]>([]);
  const [, forceUpdate] = useState({});

  // Force re-render every 30s to update elapsed times
  useEffect(() => {
    const interval = setInterval(() => forceUpdate({}), 30000);
    return () => clearInterval(interval);
  }, []);

  // Load queues from KitchenManager
  useEffect(() => {
    const updateQueues = () => {
      setFoodQueue(kitchenManager.foodQ.snapshot() as FoodTask[]);
      setDrinkQueue(kitchenManager.drinkQ.snapshot() as DrinkTask[]);
      setCleanQueue(kitchenManager.cleanQ.snapshot() as CleanTask[]);
    };

    updateQueues();
    const interval = setInterval(updateQueues, 5000);
    return () => clearInterval(interval);
  }, []);

  const handleFoodReady = useCallback(async (_orderId: string) => {
    try {
      // The foodQ handles its own API calls in processNext
      await kitchenManager.foodQ.processNext();
      setFoodQueue(kitchenManager.foodQ.snapshot() as FoodTask[]);
    } catch (err) {
      console.error('Failed to mark food ready:', err);
    }
  }, []);

  const handleDrinkReady = useCallback(async (_orderId: string) => {
    try {
      await kitchenManager.drinkQ.processNext();
      setDrinkQueue(kitchenManager.drinkQ.snapshot() as DrinkTask[]);
    } catch (err) {
      console.error('Failed to mark drink ready:', err);
    }
  }, []);

  const handleCleanComplete = useCallback(async (_tableId: number) => {
    try {
      await kitchenManager.cleanQ.processNext();
      setCleanQueue(kitchenManager.cleanQ.snapshot() as CleanTask[]);
    } catch (err) {
      console.error('Failed to complete cleaning:', err);
    }
  }, []);

  // Demo: Add test orders
  const addTestFood = () => {
    kitchenManager.enqueueFood(1, [FoodItem.SteakMediumRare, FoodItem.CaesarSalad]);
    setFoodQueue(kitchenManager.foodQ.snapshot() as FoodTask[]);
  };

  const addTestDrink = () => {
    kitchenManager.enqueueDrink(1, [DrinkItem.Latte, DrinkItem.SparklingWater]);
    setDrinkQueue(kitchenManager.drinkQ.snapshot() as DrinkTask[]);
  };

  const addTestClean = () => {
    kitchenManager.scheduleClean(1);
    setCleanQueue(kitchenManager.cleanQ.snapshot() as CleanTask[]);
  };

  return (
    <VegaFlex direction="col" gap="size-16">
      <VegaFlex justifyContent="space-between" alignItems="center">
        <VegaFont variant="font-h4" class="v-text-primary">
          Kitchen Display
        </VegaFont>
        <VegaFlex gap="size-8">
          <VegaButton size="small" label="+Food" onVegaClick={addTestFood} />
          <VegaButton size="small" label="+Drink" onVegaClick={addTestDrink} />
          <VegaButton size="small" label="+Clean" onVegaClick={addTestClean} />
        </VegaFlex>
      </VegaFlex>

      <VegaGrid column={3} gap="size-16">
        {/* Food Queue */}
        <VegaCard padding="size-16" style={{ minHeight: '400px' }}>
          <VegaFlex direction="col" gap="size-12">
            <VegaFlex gap="size-8" alignItems="center">
              <VegaIcon icon="fa-solid fa-utensils" size="size-24" color="text-brand" />
              <VegaFont variant="font-h5" class="v-text-primary">
                Food Orders
              </VegaFont>
              {foodQueue.length > 0 && (
                <span
                  style={{
                    padding: '2px 8px',
                    borderRadius: '12px',
                    fontSize: '12px',
                    backgroundColor: '#ffd',
                    color: '#a60',
                  }}
                >
                  {foodQueue.length}
                </span>
              )}
            </VegaFlex>
            <div style={{ maxHeight: '350px', overflowY: 'auto' }}>
              {foodQueue.length === 0 ? (
                <VegaFlex alignItems="center" justifyContent="center" style={{ height: '100px' }}>
                  <VegaFont variant="font-p2-short" class="v-text-secondary">
                    No pending food orders
                  </VegaFont>
                </VegaFlex>
              ) : (
                foodQueue.map((task) => (
                  <FoodTaskCard key={task.id} task={task} onReady={handleFoodReady} />
                ))
              )}
            </div>
          </VegaFlex>
        </VegaCard>

        {/* Drink Queue */}
        <VegaCard padding="size-16" style={{ minHeight: '400px' }}>
          <VegaFlex direction="col" gap="size-12">
            <VegaFlex gap="size-8" alignItems="center">
              <VegaIcon icon="fa-solid fa-martini-glass" size="size-24" color="text-link-active" />
              <VegaFont variant="font-h5" class="v-text-primary">
                Drink Orders
              </VegaFont>
              {drinkQueue.length > 0 && (
                <span
                  style={{
                    padding: '2px 8px',
                    borderRadius: '12px',
                    fontSize: '12px',
                    backgroundColor: '#eef',
                    color: '#06c',
                  }}
                >
                  {drinkQueue.length}
                </span>
              )}
            </VegaFlex>
            <div style={{ maxHeight: '350px', overflowY: 'auto' }}>
              {drinkQueue.length === 0 ? (
                <VegaFlex alignItems="center" justifyContent="center" style={{ height: '100px' }}>
                  <VegaFont variant="font-p2-short" class="v-text-secondary">
                    No pending drink orders
                  </VegaFont>
                </VegaFlex>
              ) : (
                drinkQueue.map((task) => (
                  <DrinkTaskCard key={task.id} task={task} onReady={handleDrinkReady} />
                ))
              )}
            </div>
          </VegaFlex>
        </VegaCard>

        {/* Clean Queue */}
        <VegaCard padding="size-16" style={{ minHeight: '400px' }}>
          <VegaFlex direction="col" gap="size-12">
            <VegaFlex gap="size-8" alignItems="center">
              <VegaIcon icon="fa-solid fa-broom" size="size-24" color="text-success" />
              <VegaFont variant="font-h5" class="v-text-primary">
                Tables to Clean
              </VegaFont>
              {cleanQueue.length > 0 && (
                <span
                  style={{
                    padding: '2px 8px',
                    borderRadius: '12px',
                    fontSize: '12px',
                    backgroundColor: '#fee',
                    color: '#c00',
                  }}
                >
                  {cleanQueue.length}
                </span>
              )}
            </VegaFlex>
            <div style={{ maxHeight: '350px', overflowY: 'auto' }}>
              {cleanQueue.length === 0 ? (
                <VegaFlex alignItems="center" justifyContent="center" style={{ height: '100px' }}>
                  <VegaFont variant="font-p2-short" class="v-text-secondary">
                    All tables clean
                  </VegaFont>
                </VegaFlex>
              ) : (
                cleanQueue.map((task) => (
                  <CleanTaskCard key={task.id} task={task} onComplete={handleCleanComplete} />
                ))
              )}
            </div>
          </VegaFlex>
        </VegaCard>
      </VegaGrid>
    </VegaFlex>
  );
}

export default KitchenDisplay;
