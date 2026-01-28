/**
 * @file useApiCall.ts
 * @description Custom hook for API calls with loading/error state
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 8 (Custom Hook for API Calls)
 */

import { useState, useEffect, useCallback } from 'react';

interface UseApiCallResult<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

/**
 * Hook for making API calls with automatic loading/error handling
 * @param apiCall - Function that returns a Promise of the data
 * @param deps - Dependencies array for triggering refetch
 */
export function useApiCall<T>(
  apiCall: () => Promise<T>,
  deps: unknown[] = []
): UseApiCallResult<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await apiCall();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return { data, loading, error, refetch: fetchData };
}

/**
 * Hook for making API calls that are triggered manually
 * @param apiCall - Function that takes args and returns a Promise
 */
export function useManualApiCall<TArgs extends unknown[], TResult>(
  apiCall: (...args: TArgs) => Promise<TResult>
) {
  const [data, setData] = useState<TResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const execute = useCallback(
    async (...args: TArgs) => {
      try {
        setLoading(true);
        setError(null);
        const result = await apiCall(...args);
        setData(result);
        return result;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'An error occurred';
        setError(errorMessage);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    [apiCall]
  );

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setLoading(false);
  }, []);

  return { data, loading, error, execute, reset };
}

/**
 * Hook for polling API at regular intervals
 * @param apiCall - Function that returns a Promise of the data
 * @param intervalMs - Polling interval in milliseconds
 * @param enabled - Whether polling is enabled
 */
export function usePollingApiCall<T>(
  apiCall: () => Promise<T>,
  intervalMs: number = 30000,
  enabled: boolean = true
): UseApiCallResult<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const result = await apiCall();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [apiCall]);

  useEffect(() => {
    if (!enabled) return;

    // Initial fetch
    fetchData();

    // Setup polling
    const intervalId = setInterval(fetchData, intervalMs);

    return () => clearInterval(intervalId);
  }, [fetchData, intervalMs, enabled]);

  return { data, loading, error, refetch: fetchData };
}
