/**
 * @file AlertPanel.tsx
 * @description Real-time alert panel with SignalR integration
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 7 (Alert Panel Component)
 */

import { useState, useEffect } from 'react';
import {
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaButton,
  VegaIcon,
} from '@heartlandone/vega-react';
import { useSignalR } from '../hooks/useSignalR';
import { alertsApi } from '../services/alertsApi';
import type { AlertDto } from '../types/dtos';
import type { AlertSeverity, AlertType } from '../types/enums';

const severityStyles: Record<AlertSeverity, { bg: string; color: string; icon: string }> = {
  Critical: { bg: '#fee', color: '#c00', icon: 'fa-solid fa-circle-exclamation' },
  Error: { bg: '#fee', color: '#c00', icon: 'fa-solid fa-triangle-exclamation' },
  Warning: { bg: '#ffd', color: '#a60', icon: 'fa-solid fa-exclamation' },
  Info: { bg: '#eef', color: '#06c', icon: 'fa-solid fa-info-circle' },
};

const typeIcons: Record<AlertType, string> = {
  LowBattery: 'fa-solid fa-battery-quarter',
  NavigationBlocked: 'fa-solid fa-road-barrier',
  RobotError: 'fa-solid fa-robot',
  TableService: 'fa-solid fa-bell-concierge',
  GuestWaiting: 'fa-solid fa-user-clock',
  Custom: 'fa-solid fa-flag',
};

interface AlertItemProps {
  alert: AlertDto;
  onAcknowledge: (id: number) => void;
  onResolve: (id: number) => void;
}

function AlertItem({ alert, onAcknowledge, onResolve }: AlertItemProps) {
  const style = severityStyles[alert.severity] || severityStyles.Info;
  const typeIcon = typeIcons[alert.type] || typeIcons.Custom;

  return (
    <VegaCard padding="size-12" variant="shadow" style={{ marginBottom: '8px' }}>
      <VegaFlex direction="col" gap="size-8">
        <VegaFlex justifyContent="space-between" alignItems="center">
          <VegaFlex gap="size-8" alignItems="center">
            <VegaIcon icon={style.icon} size="size-16" color="text-secondary" />
            <span
              style={{
                padding: '2px 8px',
                borderRadius: '4px',
                fontSize: '12px',
                backgroundColor: style.bg,
                color: style.color,
              }}
            >
              {alert.severity}
            </span>
            <VegaIcon icon={typeIcon} size="size-16" color="text-secondary" />
            <VegaFont variant="font-field-label" class="v-text-secondary">
              {alert.type}
            </VegaFont>
          </VegaFlex>
          <VegaFont variant="font-p2-short" class="v-text-secondary">
            {new Date(alert.createdAt).toLocaleTimeString()}
          </VegaFont>
        </VegaFlex>

        <VegaFont variant="font-p2-short" class="v-text-primary">
          {alert.title}
        </VegaFont>

        {alert.message && (
          <VegaFont variant="font-p2-short" class="v-text-secondary">
            {alert.message}
          </VegaFont>
        )}

        {alert.robotName && (
          <VegaFlex gap="size-4" alignItems="center">
            <VegaIcon icon="fa-solid fa-robot" size="size-12" color="text-secondary" />
            <VegaFont variant="font-p2-short" class="v-text-secondary">
              {alert.robotName}
            </VegaFont>
          </VegaFlex>
        )}

        <VegaFlex gap="size-8" justifyContent="end">
          {!alert.isAcknowledged && (
            <VegaButton
              variant="secondary"
              size="small"
              label="Acknowledge"
              icon="fa-solid fa-check"
              onVegaClick={() => onAcknowledge(alert.id)}
            />
          )}
          {!alert.isResolved && (
            <VegaButton
              variant="primary"
              size="small"
              label="Resolve"
              icon="fa-solid fa-check-double"
              onVegaClick={() => onResolve(alert.id)}
            />
          )}
        </VegaFlex>
      </VegaFlex>
    </VegaCard>
  );
}

export function AlertPanel() {
  const [alerts, setAlerts] = useState<AlertDto[]>([]);
  const [loading, setLoading] = useState(true);
  const { onAlertCreated, subscribeToAlerts } = useSignalR();

  useEffect(() => {
    // Initial load
    const loadAlerts = async () => {
      try {
        const data = await alertsApi.getUnacknowledged();
        setAlerts(data);
      } catch (err) {
        console.error('Failed to load alerts:', err);
      } finally {
        setLoading(false);
      }
    };

    loadAlerts();

    // Subscribe to new alerts
    subscribeToAlerts();
    onAlertCreated((event) => {
      const newAlert: AlertDto = {
        id: event.alertId,
        type: event.type,
        severity: event.severity,
        title: event.title,
        message: event.message,
        robotId: event.robotId,
        robotName: null,
        tableId: null,
        taskId: null,
        isAcknowledged: false,
        acknowledgedBy: null,
        acknowledgedAt: null,
        isResolved: false,
        resolvedAt: null,
        createdAt: event.timestamp,
      };
      setAlerts((prev) => [newAlert, ...prev]);
    });
  }, [onAlertCreated, subscribeToAlerts]);

  const handleAcknowledge = async (alertId: number) => {
    try {
      await alertsApi.acknowledge(alertId, 'CurrentUser');
      setAlerts((prev) =>
        prev.map((a) =>
          a.id === alertId ? { ...a, isAcknowledged: true, acknowledgedAt: new Date().toISOString() } : a
        )
      );
    } catch (err) {
      console.error('Failed to acknowledge alert:', err);
    }
  };

  const handleResolve = async (alertId: number) => {
    try {
      await alertsApi.resolve(alertId);
      setAlerts((prev) => prev.filter((a) => a.id !== alertId));
    } catch (err) {
      console.error('Failed to resolve alert:', err);
    }
  };

  const activeCount = alerts.filter((a) => !a.isResolved).length;
  const criticalCount = alerts.filter((a) => a.severity === 'Critical' && !a.isResolved).length;

  return (
    <VegaCard padding="size-16" style={{ height: '100%' }}>
      <VegaFlex direction="col" gap="size-12" style={{ height: '100%' }}>
        <VegaFlex justifyContent="space-between" alignItems="center">
          <VegaFlex gap="size-8" alignItems="center">
            <VegaIcon icon="fa-solid fa-bell" size="size-24" color="text-link-active" />
            <VegaFont variant="font-h5" class="v-text-primary">
              Active Alerts
            </VegaFont>
            {activeCount > 0 && (
              <span
                style={{
                  padding: '2px 10px',
                  borderRadius: '12px',
                  fontSize: '12px',
                  backgroundColor: criticalCount > 0 ? '#fee' : '#ffd',
                  color: criticalCount > 0 ? '#c00' : '#a60',
                }}
              >
                {activeCount}
              </span>
            )}
          </VegaFlex>
        </VegaFlex>

        <div style={{ flex: 1, overflowY: 'auto' }}>
          {loading ? (
            <VegaFont variant="font-p2-short" class="v-text-secondary">
              Loading alerts...
            </VegaFont>
          ) : alerts.length === 0 ? (
            <VegaFlex
              direction="col"
              gap="size-8"
              alignItems="center"
              justifyContent="center"
              style={{ height: '100px' }}
            >
              <VegaIcon icon="fa-solid fa-check-circle" size="size-32" color="text-success" />
              <VegaFont variant="font-p2-short" class="v-text-secondary">
                No active alerts
              </VegaFont>
            </VegaFlex>
          ) : (
            alerts.map((alert) => (
              <AlertItem
                key={alert.id}
                alert={alert}
                onAcknowledge={handleAcknowledge}
                onResolve={handleResolve}
              />
            ))
          )}
        </div>
      </VegaFlex>
    </VegaCard>
  );
}

export default AlertPanel;
