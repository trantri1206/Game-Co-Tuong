<script setup lang="ts">
import { computed } from 'vue';
import { useAppStore } from '@/stores/app';

const appStore = useAppStore();
const isLoading = computed(() => appStore.isLoading);
const loadingText = computed(() => appStore.loadingText);
</script>

<template>
  <transition name="fade">
    <div v-if="isLoading" class="loading-overlay">
      <div class="loading-box glass-panel">
        <div class="chess-spinner animate-spin">
          <span>帥</span>
        </div>
        <p class="loading-message">{{ loadingText }}</p>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.loading-overlay {
  position: fixed;
  inset: 0;
  z-index: 999;
  background: rgba(10, 15, 22, 0.75);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
}

.loading-box {
  padding: 32px 48px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 18px;
  border: 1px solid var(--border-focus);
}

.chess-spinner {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  border: 3px solid rgba(220, 38, 38, 0.2);
  border-top-color: var(--gold-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-serif);
  font-size: 1.5rem;
  color: #fef08a;
  background: radial-gradient(circle, #7f1d1d, #450a0a);
}

.loading-message {
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--text-main);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
