<script setup lang="ts">
import { computed } from 'vue';
import AppHeader from '@/components/AppHeader.vue';
import ApiStatusBar from '@/components/ApiStatusBar.vue';
import LoadingOverlay from '@/components/LoadingOverlay.vue';
import { useAppStore } from '@/stores/app';

const appStore = useAppStore();
const notifications = computed(() => appStore.notifications);

function closeToast(id: string) {
  appStore.removeNotification(id);
}
</script>

<template>
  <div class="app-root">
    <!-- Header Dùng Chung -->
    <AppHeader />

    <!-- Thanh Trạng Thái API & Thông Báo Lỗi Thử Lại -->
    <ApiStatusBar />

    <!-- Khu Vực Hiển Thị Các Màn Hình Router -->
    <main class="main-content">
      <router-view v-slot="{ Component }">
        <transition name="page-fade" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </main>

    <!-- Loading Toàn Cục -->
    <LoadingOverlay />

    <!-- Toast Notifications Toast Container -->
    <div class="toast-container">
      <transition-group name="toast">
        <div
          v-for="toast in notifications"
          :key="toast.id"
          class="toast-pill"
          :class="'toast-' + toast.type"
          @click="closeToast(toast.id)"
        >
          <span class="toast-icon">
            {{ toast.type === 'success' ? '✅' : toast.type === 'error' ? '❌' : toast.type === 'warning' ? '⚠️' : 'ℹ️' }}
          </span>
          <span class="toast-message">{{ toast.message }}</span>
        </div>
      </transition-group>
    </div>
  </div>
</template>

<style scoped>
.app-root {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main-content {
  flex: 1;
}

/* Page Transition */
.page-fade-enter-active,
.page-fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.page-fade-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.page-fade-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* Toast Container */
.toast-container {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.toast-pill {
  padding: 12px 18px;
  border-radius: var(--radius-sm);
  background: rgba(15, 20, 28, 0.95);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  border: 1px solid var(--border-subtle);
  max-width: 380px;
}

.toast-success {
  border-color: rgba(16, 185, 129, 0.5);
  color: #6ee7b7;
}

.toast-error {
  border-color: rgba(239, 68, 68, 0.5);
  color: #fca5a5;
}

.toast-warning {
  border-color: rgba(245, 158, 11, 0.5);
  color: #fde68a;
}

.toast-info {
  border-color: rgba(56, 189, 248, 0.5);
  color: #7dd3fc;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(30px);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
