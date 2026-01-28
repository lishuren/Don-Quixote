/**
 * @file KitchenManager.ts
 * @description Kitchen queue management for food, drink, and cleaning tasks
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 6 (kitchenQueue implementation)
 */

import { kitchenApi } from './kitchenApi';
import {
  generateOrderId,
  genId,
  FoodItem,
  DrinkItem,
  type BaseTask,
  type FoodTask,
  type DrinkTask,
  type CleanTask,
  type QueueOptions,
  type PlaceOrderResult,
  type TableId,
} from '../types/kitchen';

/**
 * Generic base queue class for kitchen tasks
 */
class BaseQueue<TTask extends BaseTask> {
  protected q: TTask[] = [];
  private timer: ReturnType<typeof setInterval> | null = null;
  private running = false;

  constructor(
    private readonly opts: QueueOptions,
    private readonly onReadyApi: (task: TTask) => Promise<unknown>
  ) {}

  /**
   * Add task to queue
   */
  enqueue(
    input: Omit<TTask, 'id' | 'status' | 'createdAt' | 'orderId'> & { orderId?: string }
  ): TTask {
    const task = {
      id: genId(),
      orderId: input.orderId ?? generateOrderId(),
      status: 'pending' as const,
      createdAt: Date.now(),
      ...input,
      items: Array.isArray((input as { items?: unknown }).items)
        ? (input as { items: unknown[] }).items
        : [],
    } as TTask;

    this.q.push(task);
    return task;
  }

  /**
   * Process one pending task
   */
  protected async processOne(): Promise<void> {
    if (!this.q.length) return;
    const idx = this.q.findIndex((t) => t.status === 'pending');
    if (idx === -1) return;

    const task = this.q[idx];

    await this.onReadyApi(task);
    task.status = 'complete';
    this.q.splice(idx, 1);
  }

  /**
   * Start automatic queue processing
   */
  start(): void {
    if (this.running) return;
    this.running = true;

    const interval = this.opts.intervalMs ?? 60_000;
    this.timer = setInterval(() => {
      void this.processOne();
    }, interval);
  }

  /**
   * Stop automatic queue processing
   */
  stop(): void {
    this.running = false;
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  /**
   * Clear all tasks from queue
   */
  clear(): void {
    this.q = [];
  }

  /**
   * Get queue size
   */
  size(): number {
    return this.q.length;
  }

  /**
   * Get snapshot of current queue
   */
  snapshot(): ReadonlyArray<TTask> {
    return this.q.slice();
  }

  /**
   * Process next task immediately
   */
  async processNext(): Promise<void> {
    await this.processOne();
  }
}

/**
 * Food order queue
 */
export class FoodQueue extends BaseQueue<FoodTask> {
  constructor(opts: QueueOptions = {}) {
    super({ intervalMs: opts.intervalMs ?? 60_000 }, (task) =>
      kitchenApi.foodReady(task.tableId, task.orderId, task.items)
    );
  }
}

/**
 * Drink order queue
 */
export class DrinkQueue extends BaseQueue<DrinkTask> {
  constructor(opts: QueueOptions = {}) {
    super({ intervalMs: opts.intervalMs ?? 60_000 }, (task) =>
      kitchenApi.drinksReady(task.tableId, task.orderId, task.items)
    );
  }
}

/**
 * Table cleaning queue
 */
export class CleanQueue extends BaseQueue<CleanTask> {
  constructor(opts: QueueOptions = {}) {
    super(
      { intervalMs: opts.intervalMs ?? 30_000 }, // 30s default for cleaning
      (task) => kitchenApi.tableStatusChange(task.tableId, 'Available')
    );
  }
}

/**
 * Registry for tracking active orders per table
 */
export class TableOrderRegistry {
  private map = new Map<TableId, string>();

  /**
   * Get existing order ID or create new one
   */
  getOrCreate(tableId: TableId): string {
    const existing = this.map.get(tableId);
    if (existing) return existing;
    const fresh = generateOrderId();
    this.map.set(tableId, fresh);
    return fresh;
  }

  /**
   * Start a new order for table
   */
  startNew(tableId: TableId): string {
    const fresh = generateOrderId();
    this.map.set(tableId, fresh);
    return fresh;
  }

  /**
   * End order for table
   */
  end(tableId: TableId): void {
    this.map.delete(tableId);
  }

  /**
   * Get current order ID for table
   */
  get(tableId: TableId): string | undefined {
    return this.map.get(tableId);
  }
}

/**
 * Kitchen Manager - orchestrates all kitchen queues
 */
export class KitchenManager {
  public readonly foodQ: FoodQueue;
  public readonly drinkQ: DrinkQueue;
  public readonly cleanQ: CleanQueue;
  private readonly orders: TableOrderRegistry;

  constructor(
    foodQueueOpts?: QueueOptions,
    drinkQueueOpts?: QueueOptions,
    cleanQueueOpts?: QueueOptions
  ) {
    this.foodQ = new FoodQueue(foodQueueOpts);
    this.drinkQ = new DrinkQueue(drinkQueueOpts);
    this.cleanQ = new CleanQueue(cleanQueueOpts);
    this.orders = new TableOrderRegistry();
  }

  /**
   * Start all queue processing
   */
  startAll(): void {
    this.foodQ.start();
    this.drinkQ.start();
    this.cleanQ.start();
  }

  /**
   * Stop all queue processing
   */
  stopAll(): void {
    this.foodQ.stop();
    this.drinkQ.stop();
    this.cleanQ.stop();
  }

  /**
   * Place complete order (food + drinks) for table
   * Food and drinks share the same orderId
   */
  placeOrder(
    tableId: TableId,
    foodItems: (FoodItem | string)[],
    drinkItems: (DrinkItem | string)[]
  ): PlaceOrderResult {
    const orderId = this.orders.getOrCreate(tableId);
    const foodTask = this.foodQ.enqueue({ orderId, tableId, items: foodItems });
    const drinkTask = this.drinkQ.enqueue({ orderId, tableId, items: drinkItems });
    return { orderId, foodTask, drinkTask };
  }

  /**
   * Enqueue food items only
   */
  enqueueFood(
    tableId: TableId,
    items: (FoodItem | string)[],
    orderId?: string
  ): FoodTask {
    const oid = orderId ?? this.orders.getOrCreate(tableId);
    return this.foodQ.enqueue({ orderId: oid, tableId, items });
  }

  /**
   * Enqueue drink items only
   */
  enqueueDrink(
    tableId: TableId,
    items: (DrinkItem | string)[],
    orderId?: string
  ): DrinkTask {
    const oid = orderId ?? this.orders.getOrCreate(tableId);
    return this.drinkQ.enqueue({ orderId: oid, tableId, items });
  }

  /**
   * Start a new order for table (generates new orderId)
   */
  startNewOrder(tableId: TableId): string {
    return this.orders.startNew(tableId);
  }

  /**
   * End order for table
   */
  endOrder(tableId: TableId): void {
    this.orders.end(tableId);
  }

  /**
   * Schedule table cleaning
   */
  scheduleClean(tableId: TableId): CleanTask {
    return this.cleanQ.enqueue({ tableId, items: [] });
  }

  /**
   * Get current order ID for table
   */
  getOrderId(tableId: TableId): string | undefined {
    return this.orders.get(tableId);
  }
}

// Export singleton instance
export const kitchenManager = new KitchenManager();

// Re-export enums for convenience
export { FoodItem, DrinkItem };
