/**
 * @file Dashboard.tsx
 * @description Main dashboard component with summary cards
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 7 (Dashboard Component)
 */

import { useState, useEffect } from 'react';
import {
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaGrid,
  VegaIcon,
} from '@heartlandone/vega-react';
import { dashboardApi } from '../services/dashboardApi';
import type { DashboardSummary } from '../types/dtos';

interface StatCardProps {
  title: string;
  value: number | string;
  icon?: string;
  colorClass?: string;
  subtitle?: string;
}

function StatCard({ title, value, icon, colorClass = 'v-text-primary', subtitle }: StatCardProps) {
  return (
    <VegaCard padding="size-16" variant="shadow">
      <VegaFlex direction="col" gap="size-8">
        <VegaFlex gap="size-8" alignItems="center">
          {icon && <VegaIcon icon={icon} size="size-20" color="text-secondary" />}
          <VegaFont variant="font-field-label" class="v-text-secondary">
            {title}
          </VegaFont>
        </VegaFlex>
        <VegaFont variant="font-h3" class={colorClass}>
          {value}
        </VegaFont>
        {subtitle && (
          <VegaFont variant="font-p2-short" class="v-text-secondary">
            {subtitle}
          </VegaFont>
        )}
      </VegaFlex>
    </VegaCard>
  );
}

interface SummaryCardProps {
  title: string;
  icon: string;
  children: React.ReactNode;
}

function SummaryCard({ title, icon, children }: SummaryCardProps) {
  return (
    <VegaCard padding="size-16" variant="shadow">
      <VegaFlex direction="col" gap="size-12">
        <VegaFlex gap="size-8" alignItems="center">
          <VegaIcon icon={icon} size="size-24" color="text-link-active" />
          <VegaFont variant="font-h5" class="v-text-primary">
            {title}
          </VegaFont>
        </VegaFlex>
        {children}
      </VegaFlex>
    </VegaCard>
  );
}

export function Dashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        setLoading(true);
        const data = await dashboardApi.getSummary();
        setSummary(data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load dashboard');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
    const interval = setInterval(fetchDashboard, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, []);

  if (loading && !summary) {
    return (
      <VegaFlex justifyContent="center" alignItems="center" style={{ minHeight: '200px' }}>
        <VegaFont variant="font-p1-short">Loading...</VegaFont>
      </VegaFlex>
    );
  }

  if (error) {
    return (
      <VegaCard padding="size-16" variant="shadow">
        <VegaFlex direction="col" gap="size-8" alignItems="center">
          <VegaIcon icon="fa-solid fa-exclamation-triangle" size="size-32" color="text-on-danger" />
          <VegaFont variant="font-p1-short" class="v-text-danger">
            {error}
          </VegaFont>
        </VegaFlex>
      </VegaCard>
    );
  }

  if (!summary) return null;

  return (
    <VegaFlex direction="col" gap="size-16">
      <VegaFont variant="font-h4" class="v-text-primary">
        Dashboard
      </VegaFont>

      <VegaGrid column={4} gap="size-16">
        {/* Robots Card */}
        <SummaryCard title="Robots" icon="fa-solid fa-robot">
          <VegaGrid column={2} gap="size-8">
            <StatCard
              title="Idle"
              value={summary.robots.idle}
              colorClass="v-text-success"
            />
            <StatCard
              title="Active"
              value={summary.robots.navigating + summary.robots.delivering}
              colorClass="v-text-link-active"
            />
            <StatCard
              title="Charging"
              value={summary.robots.charging}
              colorClass="v-text-warning"
            />
            <StatCard
              title="Error"
              value={summary.robots.error}
              colorClass="v-text-danger"
            />
          </VegaGrid>
          <VegaFont variant="font-p2-short" class="v-text-secondary">
            Avg Battery: {summary.robots.averageBattery.toFixed(0)}%
          </VegaFont>
        </SummaryCard>

        {/* Tables Card */}
        <SummaryCard title="Tables" icon="fa-solid fa-table-picnic">
          <VegaGrid column={2} gap="size-8">
            <StatCard
              title="Available"
              value={summary.tables.available}
              colorClass="v-text-success"
            />
            <StatCard
              title="Occupied"
              value={summary.tables.occupied}
              colorClass="v-text-warning"
            />
            <StatCard
              title="Needs Service"
              value={summary.tables.needsService}
              colorClass="v-text-danger"
            />
            <StatCard
              title="Cleaning"
              value={summary.tables.cleaning}
              colorClass="v-text-secondary"
            />
          </VegaGrid>
          <VegaFont variant="font-p2-short" class="v-text-secondary">
            Occupancy: {summary.tables.occupancyPercent.toFixed(0)}%
          </VegaFont>
        </SummaryCard>

        {/* Tasks Card */}
        <SummaryCard title="Tasks" icon="fa-solid fa-list-check">
          <VegaGrid column={2} gap="size-8">
            <StatCard
              title="Pending"
              value={summary.tasks.pendingCount}
              colorClass="v-text-warning"
            />
            <StatCard
              title="In Progress"
              value={summary.tasks.inProgressCount}
              colorClass="v-text-link-active"
            />
            <StatCard
              title="Completed"
              value={summary.tasks.completedToday}
              colorClass="v-text-success"
            />
            <StatCard
              title="Failed"
              value={summary.tasks.failedToday}
              colorClass="v-text-danger"
            />
          </VegaGrid>
        </SummaryCard>

        {/* Alerts Card */}
        <SummaryCard title="Alerts" icon="fa-solid fa-bell">
          <VegaGrid column={2} gap="size-8">
            <StatCard
              title="Critical"
              value={summary.alerts.critical}
              colorClass="v-text-danger"
            />
            <StatCard
              title="Errors"
              value={summary.alerts.errors}
              colorClass="v-text-danger"
            />
            <StatCard
              title="Warnings"
              value={summary.alerts.warnings}
              colorClass="v-text-warning"
            />
            <StatCard
              title="Unacknowledged"
              value={summary.alerts.unacknowledged}
              colorClass="v-text-warning"
            />
          </VegaGrid>
        </SummaryCard>
      </VegaGrid>

      {/* Quick Stats */}
      <VegaFlex gap="size-16">
        <StatCard
          title="Active Guests"
          value={summary.activeGuests}
          icon="fa-solid fa-users"
          colorClass="v-text-link-active"
        />
        <StatCard
          title="Waitlist"
          value={summary.waitlistCount}
          icon="fa-solid fa-clock"
          colorClass="v-text-secondary"
        />
      </VegaFlex>
    </VegaFlex>
  );
}

export default Dashboard;
