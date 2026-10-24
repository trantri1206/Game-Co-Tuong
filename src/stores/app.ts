import { defineStore } from 'pinia';
import { ref } from 'vue';

export type ApiConnectionState = 'idle' | 'loading' | 'connected' | 'retrying' | 'error';

export interface AppError {
  message: string;
  statusCode?: number;
  endpoint?: string;
  timestamp: number;
  canRetry?: boolean;
  retryAction?: () => Promise<any>;
}

export interface ToastNotification {
  id: string;
  type: 'info' | 'success' | 'warning' | 'error';
  message: string;
}

export const useAppStore = defineStore('app', () => {
  const isLoading = ref(false);
  const loadingText = ref('Đang xử lý...');

  const apiState = ref<ApiConnectionState>('idle');
  const activeRetryCount = ref(0);
  const maxRetries = ref(3);
  const lastError = ref<AppError | null>(null);

  const notifications = ref<ToastNotification[]>([]);

  function setLoading(loading: boolean, text = 'Đang xử lý...') {
    isLoading.value = loading;
    loadingText.value = text;
  }

  function setApiState(
    state: ApiConnectionState,
    error: AppError | null = null,
    retryCount = 0
  ) {
    apiState.value = state;
    activeRetryCount.value = retryCount;
    if (error) {
      lastError.value = error;
    } else if (state === 'connected' || state === 'idle') {
      lastError.value = null;
    }
  }

  function clearError() {
    lastError.value = null;
    if (apiState.value === 'error') {
      apiState.value = 'idle';
    }
  }

  async function triggerManualRetry() {
    if (lastError.value?.retryAction) {
      const action = lastError.value.retryAction;
      clearError();
      setLoading(true, 'Đang thử lại kết nối...');
      try {
        await action();
        setApiState('connected');
        notify('Kết nối lại thành công!', 'success');
      } catch (err: any) {
        // Error will be caught and set by client interceptor
      } finally {
        setLoading(false);
      }
    }
  }

  function notify(message: string, type: 'info' | 'success' | 'warning' | 'error' = 'info', duration = 4000) {
    const id = `${Date.now()}_${Math.random().toString(36).substring(2, 6)}`;
    notifications.value.push({ id, type, message });
    if (duration > 0) {
      setTimeout(() => {
        removeNotification(id);
      }, duration);
    }
  }

  function removeNotification(id: string) {
    notifications.value = notifications.value.filter((n) => n.id !== id);
  }

  return {
    isLoading,
    loadingText,
    apiState,
    activeRetryCount,
    maxRetries,
    lastError,
    notifications,
    setLoading,
    setApiState,
    clearError,
    triggerManualRetry,
    notify,
    removeNotification
  };
});
