<script setup lang="ts">
import { computed } from 'vue';
import { useAppStore } from '@/stores/app';
import { apiConfig } from '@/api/config';

const appStore = useAppStore();

const apiState = computed(() => appStore.apiState);
const lastError = computed(() => appStore.lastError);
const activeRetry = computed(() => appStore.activeRetryCount);
const maxRetries = computed(() => appStore.maxRetries);

async function handleManualRetry() {
  await appStore.triggerManualRetry();
}

function handleDismissError() {
  appStore.clearError();
}
</script>

<template>
  <div class="api-status-wrapper">
    <!-- 1. Thanh trạng thái kết nối tinh gọn -->
    <div class="status-bar">
      <div class="status-left">
        <span class="status-dot" :class="apiState"></span>
        <span class="status-label">API:</span>
        <code class="api-endpoint">{{ apiConfig.baseUrl }}</code>
      </div>

      <div class="status-right">
        <template v-if="apiState === 'loading'">
          <span class="badge badge-gray animate-pulse">⏳ Đang gửi yêu cầu...</span>
        </template>
        <template v-else-if="apiState === 'retrying'">
          <span class="badge badge-gold animate-pulse">
            🔄 Đang thử lại (Lần {{ activeRetry }}/{{ maxRetries }})...
          </span>
        </template>
        <template v-else-if="apiState === 'connected'">
          <span class="badge badge-green">🟢 Đã kết nối</span>
        </template>
        <template v-else-if="apiState === 'error'">
          <span class="badge badge-red">🔴 Mất kết nối</span>
        </template>
      </div>
    </div>

    <!-- 2. Hộp thoại cảnh báo lỗi và nút Thử Lại (Không làm người dùng bị mắc kẹt) -->
    <transition name="fade">
      <div v-if="lastError" class="error-banner">
        <div class="error-icon">⚠️</div>
        <div class="error-content">
          <div class="error-title">Lỗi kết nối máy chủ</div>
          <div class="error-desc">{{ lastError.message }}</div>
          <div v-if="lastError.endpoint" class="error-meta">
            Đích: <code>{{ lastError.endpoint }}</code>
          </div>
        </div>
        <div class="error-actions">
          <button
            v-if="lastError.retryAction"
            class="btn btn-gold btn-sm"
            @click="handleManualRetry"
          >
            🔄 Thử lại ngay
          </button>
          <button class="btn btn-secondary btn-sm" @click="handleDismissError">
            Đóng
          </button>
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.api-status-wrapper {
  width: 100%;
}

.status-bar {
  max-width: 1200px;
  margin: 0 auto;
  padding: 6px 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 0.8rem;
  color: var(--text-dim);
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
}

.status-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #64748b;
}
.status-dot.connected {
  background: #10b981;
  box-shadow: 0 0 8px rgba(16, 185, 129, 0.6);
}
.status-dot.loading {
  background: #38bdf8;
  box-shadow: 0 0 8px rgba(56, 189, 248, 0.6);
}
.status-dot.retrying {
  background: #f59e0b;
  box-shadow: 0 0 8px rgba(245, 158, 11, 0.6);
}
.status-dot.error {
  background: #ef4444;
  box-shadow: 0 0 8px rgba(239, 68, 68, 0.6);
}

.api-endpoint {
  color: var(--text-muted);
  background: rgba(255, 255, 255, 0.04);
  padding: 2px 6px;
  border-radius: 4px;
  font-family: monospace;
}

.error-banner {
  max-width: 1200px;
  margin: 12px auto 0 auto;
  padding: 12px 20px;
  background: rgba(185, 28, 28, 0.2);
  border: 1px solid rgba(239, 68, 68, 0.5);
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: var(--shadow-sm);
}

.error-icon {
  font-size: 1.5rem;
}

.error-content {
  flex: 1;
}

.error-title {
  font-weight: 700;
  color: #fca5a5;
  font-size: 0.9rem;
}

.error-desc {
  font-size: 0.85rem;
  color: #fecaca;
  margin-top: 2px;
}

.error-meta {
  font-size: 0.75rem;
  color: var(--text-dim);
  margin-top: 4px;
}

.error-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
