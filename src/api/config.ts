/**
 * Cấu hình API tập trung cho hệ thống Cờ Tướng Online
 * Tự động đọc từ biến môi trường Vite (.env) và có giá trị dự phòng an toàn.
 */

export interface ApiConfig {
  baseUrl: string;
  timeoutMs: number;
  maxRetries: number;
  retryBaseDelayMs: number;
}

export const apiConfig: ApiConfig = {
  baseUrl: (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api').replace(/\/+$/, ''),
  timeoutMs: Number(import.meta.env.VITE_API_TIMEOUT_MS) || 10000,
  maxRetries: Number(import.meta.env.VITE_API_MAX_RETRIES) || 3,
  retryBaseDelayMs: 1000
};

export function getFullApiUrl(endpoint: string): string {
  const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  return `${apiConfig.baseUrl}${cleanEndpoint}`;
}
