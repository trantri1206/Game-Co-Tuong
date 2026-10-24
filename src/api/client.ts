import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import { apiConfig } from './config';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';

// Mở rộng AxiosRequestConfig để theo dõi số lần thử lại (retry count)
declare module 'axios' {
  export interface AxiosRequestConfig {
    _retryCount?: number;
    _skipRetry?: boolean;
    _skipGlobalLoading?: boolean;
  }
}

export interface ApiClientError {
  message: string;
  statusCode?: number;
  isNetworkError: boolean;
  isTimeout: boolean;
  canRetry: boolean;
  endpoint: string;
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: apiConfig.baseUrl,
  timeout: apiConfig.timeoutMs,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json'
  }
});

// Helper tính độ trễ backoff: 1s, 2s, 4s...
function getBackoffDelay(retryCount: number): number {
  return apiConfig.retryBaseDelayMs * Math.pow(2, retryCount - 1);
}

// Helper tạm dừng (sleep)
function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// REQUEST INTERCEPTOR
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // 1. Gắn JWT Token nếu có
    const authStore = useAuthStore();
    if (authStore.token && !config.headers.Authorization) {
      config.headers.Authorization = `Bearer ${authStore.token}`;
    }

    // 2. Cập nhật trạng thái loading nếu không tắt
    const appStore = useAppStore();
    if (!config._skipGlobalLoading && appStore.apiState !== 'retrying') {
      appStore.apiState = 'loading';
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// RESPONSE INTERCEPTOR
apiClient.interceptors.response.use(
  (response) => {
    const appStore = useAppStore();
    appStore.setApiState('connected');
    return response;
  },
  async (error: AxiosError) => {
    const appStore = useAppStore();
    const config = error.config;

    if (!config) {
      return Promise.reject(normalizeError(error));
    }

    const currentRetry = config._retryCount || 0;
    const isTimeout = error.code === 'ECONNABORTED' || error.message.toLowerCase().includes('timeout');
    const isNetworkError = !error.response && !!error.request;
    const isServerError = error.response ? error.response.status >= 500 : false;

    // Điều kiện thử lại tự động: Lỗi mạng, Timeout hoặc Lỗi 5xx từ máy chủ
    const canRetry = (isNetworkError || isTimeout || isServerError) && !config._skipRetry;

    if (canRetry && currentRetry < apiConfig.maxRetries) {
      config._retryCount = currentRetry + 1;
      const waitTime = getBackoffDelay(config._retryCount);

      // Thông báo cho giao diện biết đang thử lại để không bị kẹt
      appStore.setApiState('retrying', null, config._retryCount);
      console.warn(
        `[API Client] Tự động thử lại lần ${config._retryCount}/${apiConfig.maxRetries} sau ${waitTime}ms cho ${config.url}`
      );

      await delay(waitTime);
      return apiClient(config);
    }

    // Nếu đã hết số lần thử lại hoặc lỗi không thể tự phục hồi (4xx)
    const normalized = normalizeError(error, config.url || '');

    // Cung cấp hàm thử lại thủ công (Manual Retry Action)
    const retryAction = () => {
      config._retryCount = 0;
      return apiClient(config);
    };

    appStore.setApiState(
      'error',
      {
        message: normalized.message,
        statusCode: normalized.statusCode,
        endpoint: normalized.endpoint,
        timestamp: Date.now(),
        canRetry: normalized.canRetry,
        retryAction
      },
      currentRetry
    );

    // Xử lý riêng cho 401 Unauthorized (Phiên hết hạn hoặc bị thu hồi)
    if (normalized.statusCode === 401) {
      const authStore = useAuthStore();
      authStore.clearAuth();
      appStore.notify('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.', 'warning');
    }

    return Promise.reject(normalized);
  }
);

// Chuẩn hóa lỗi thân thiện với người dùng
function normalizeError(error: AxiosError<any>, url = ''): ApiClientError {
  let message = 'Đã có lỗi xảy ra khi kết nối máy chủ.';
  const statusCode = error.response?.status;
  const isTimeout = error.code === 'ECONNABORTED' || error.message.toLowerCase().includes('timeout');
  const isNetworkError = !error.response && !!error.request;
  const canRetry = isNetworkError || isTimeout || (statusCode ? statusCode >= 500 : false);

  if (isTimeout) {
    message = `Yêu cầu máy chủ quá thời gian chờ (${apiConfig.timeoutMs / 1000}s). Vui lòng thử lại.`;
  } else if (isNetworkError) {
    message = `Không thể kết nối đến máy chủ tại ${apiConfig.baseUrl}. Vui lòng kiểm tra mạng hoặc dịch vụ backend.`;
  } else if (error.response?.data?.message) {
    message = error.response.data.message;
  } else if (error.response?.data?.errors && Array.isArray(error.response.data.errors)) {
    message = error.response.data.errors.join(', ');
  } else if (statusCode === 400) {
    message = 'Dữ liệu yêu cầu không hợp lệ.';
  } else if (statusCode === 401) {
    message = 'Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.';
  } else if (statusCode === 403) {
    message = 'Bạn không có quyền thực hiện thao tác này.';
  } else if (statusCode === 404) {
    message = 'Không tìm thấy tài nguyên yêu cầu trên hệ thống.';
  } else if (statusCode && statusCode >= 500) {
    message = `Máy chủ gặp sự cố nội bộ (${statusCode}). Hệ thống đã thử lại nhưng chưa thành công.`;
  }

  return {
    message,
    statusCode,
    isNetworkError,
    isTimeout,
    canRetry,
    endpoint: url
  };
}
