/**
 * @file kitchen.ts
 * @description Kitchen queue management types and utilities
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 6 (kitchenQueue implementation)
 */

import type { TableStatus } from './enums';

// ============================================
// Kitchen Types
// ============================================

export type KitchenQueueTaskStatus = 'pending' | 'complete';
export type TableId = number;

/**
 * Base task interface for kitchen queue items
 */
export interface BaseTask<TItem extends string = string> {
  id: string;
  orderId: string; // ORD-YYYY-MMDD-####
  tableId: TableId;
  items: TItem[];
  status: KitchenQueueTaskStatus;
  createdAt: number;
}

/**
 * Queue configuration options
 */
export interface QueueOptions {
  intervalMs?: number; // 1min default
}

// ============================================
// Food & Drink Enums
// ============================================

export enum FoodItem {
  SteakMediumRare = 'Steak medium-rare',
  CaesarSalad = 'Caesar salad',
  MargheritaPizza = 'Margherita pizza',
  GrilledSalmon = 'Grilled salmon',
  MushroomRisotto = 'Mushroom risotto',
  SpaghettiBolognese = 'Spaghetti bolognese',
}

export enum DrinkItem {
  Latte = 'Latte',
  Cappuccino = 'Cappuccino',
  IcedTea = 'Iced tea',
  OrangeJuice = 'Orange juice',
  SparklingWater = 'Sparkling water',
  Cola = 'Cola',
}

// ============================================
// Task Types
// ============================================

export interface FoodTask extends BaseTask<FoodItem | string> {
  startedAt?: number;
  tableLabel?: string;
}
export interface DrinkTask extends BaseTask<DrinkItem | string> {
  startedAt?: number;
  tableLabel?: string;
}
export interface CleanTask extends BaseTask<string> {
  requestedAt?: number;
  tableLabel?: string;
}

// ============================================
// Kitchen Manager Types
// ============================================

export interface PlaceOrderResult {
  orderId: string;
  foodTask: FoodTask;
  drinkTask: DrinkTask;
}

/**
 * Kitchen API interface for queue callbacks
 */
export interface KitchenApiInterface {
  foodReady: (tableId: number, orderId?: string, items?: string[]) => Promise<unknown>;
  drinksReady: (tableId: number, orderId?: string, items?: string[]) => Promise<unknown>;
  tableStatusChange: (tableId: number, newStatus: TableStatus) => Promise<unknown>;
}

// ============================================
// Helper Functions
// ============================================

const seqByDay = new Map<string, number>();

/**
 * Generate unique ID
 */
export const genId = (): string =>
  (crypto as { randomUUID?: () => string })?.randomUUID?.() ??
  `t_${Date.now()}_${Math.random().toString(16).slice(2)}`;

/**
 * Get next daily sequence index
 */
function nextDailyIndex(date = new Date()): string {
  const y = date.getFullYear();
  const mm = `${date.getMonth() + 1}`.padStart(2, '0');
  const dd = `${date.getDate()}`.padStart(2, '0');
  const key = `${y}${mm}${dd}`;
  const next = (seqByDay.get(key) ?? 0) + 1;
  seqByDay.set(key, next);
  return `${next}`.padStart(4, '0');
}

/**
 * Generate order ID in format: ORD-YYYY-MMDD-####
 */
export function generateOrderId(date = new Date()): string {
  const year = date.getFullYear();
  const mm = `${date.getMonth() + 1}`.padStart(2, '0');
  const dd = `${date.getDate()}`.padStart(2, '0');
  const md = `${mm}${dd}`;
  const index = nextDailyIndex(date);
  return `ORD-${year}-${md}-${index}`;
}

/**
 * Reset daily sequence (for testing)
 */
export function resetDailySequence(): void {
  seqByDay.clear();
}
