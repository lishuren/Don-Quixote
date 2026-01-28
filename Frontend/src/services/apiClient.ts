/**
 * @file apiClient.ts
 * @description Centralized HTTP client for backend API communication
 * Source: FRONTEND_IMPLEMENTATION_GUIDE.md - Section 3
 */

import type { ApiError } from '../types/dtos';

const API_BASE = process.env.REACT_APP_API_BASE || 'http://localhost:5199';

/**
 * Custom error class for API errors
 */
export class ApiClientError extends Error {
  constructor(
    message: string,
    public statusCode: number,
    public originalError?: unknown
  ) {
    super(message);
    this.name = 'ApiClientError';
  }
}

/**
 * Centralized API client with typed methods
 */
class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE) {
    this.baseUrl = baseUrl;
  }

  /**
   * Core request method with error handling
   */
  private async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${path}`;

    try {
      const response = await fetch(url, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          ...options.headers,
        },
      });

      // Handle no content response
      if (response.status === 204) {
        return undefined as T;
      }

      // Handle error responses
      if (!response.ok) {
        let errorMessage = `HTTP ${response.status}`;
        try {
          const error: ApiError = await response.json();
          errorMessage = error.error || errorMessage;
        } catch {
          // Response body is not JSON
        }
        throw new ApiClientError(errorMessage, response.status);
      }

      // Parse JSON response
      return response.json();
    } catch (error) {
      if (error instanceof ApiClientError) {
        throw error;
      }
      throw new ApiClientError(
        error instanceof Error ? error.message : 'Network error',
        0,
        error
      );
    }
  }

  /**
   * GET request
   */
  get<T>(path: string): Promise<T> {
    return this.request<T>(path, { method: 'GET' });
  }

  /**
   * POST request
   */
  post<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  /**
   * PUT request
   */
  put<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  /**
   * PATCH request
   */
  patch<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>(path, {
      method: 'PATCH',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  /**
   * DELETE request
   */
  delete(path: string): Promise<void> {
    return this.request<void>(path, { method: 'DELETE' });
  }

  /**
   * Build query string from params object
   */
  static buildQuery(params: Record<string, string | number | boolean | undefined>): string {
    const filtered = Object.entries(params).filter(
      ([, value]) => value !== undefined && value !== null
    );
    if (filtered.length === 0) return '';
    return '?' + new URLSearchParams(filtered.map(([k, v]) => [k, String(v)])).toString();
  }
}

// Export singleton instance
export const apiClient = new ApiClient();

// Also export class for testing/mocking
export { ApiClient };
