<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { signalrService } from '@/services/signalrService';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const appStore = useAppStore();

const user = computed(() => authStore.user);
const isAuthenticated = computed(() => authStore.isAuthenticated);

async function handleLogout() {
  if (confirm('Bạn có chắc chắn muốn đăng xuất không?')) {
    await signalrService.disconnect();
    authStore.clearAuth();
    appStore.notify('Đã đăng xuất thành công! Toàn bộ trạng thái đã được giải phóng.', 'info');
    await router.push('/login');
  }
}
</script>

<template>
  <header class="app-header glass-panel">
    <div class="header-container">
      <!-- Logo Thương Hiệu -->
      <router-link to="/lobby" class="brand">
        <div class="chess-icon">帥</div>
        <div class="brand-text">
          <span class="brand-title title-serif">KỲ ĐẠO</span>
          <span class="brand-sub">CỜ TƯỚNG TRỰC TUYẾN</span>
        </div>
      </router-link>

      <!-- Menu Điều Hướng Chính -->
      <nav v-if="isAuthenticated" class="nav-links">
        <router-link to="/lobby" class="nav-item" :class="{ active: route.path.startsWith('/lobby') }">
          <span class="icon">🏛️</span> Sảnh Chờ
        </router-link>
        <router-link to="/history" class="nav-item" :class="{ active: route.path.startsWith('/history') }">
          <span class="icon">📜</span> Lịch Sử Đấu
        </router-link>
      </nav>

      <!-- Khu Vực Tài Khoản / Đăng Xuất -->
      <div class="user-section">
        <template v-if="isAuthenticated">
          <div class="user-pill">
            <span class="avatar-circle">♟️</span>
            <div class="user-info">
              <span class="user-name">{{ user?.displayName || user?.username || 'Kỳ Thủ' }}</span>
              <span class="badge badge-gold">Cấp 1</span>
            </div>
          </div>
          <button class="btn btn-secondary btn-sm" @click="handleLogout" title="Đăng xuất khỏi hệ thống">
            🚪 Đăng xuất
          </button>
        </template>
        <template v-else>
          <router-link to="/login" class="btn btn-primary btn-sm">
            🔑 Đăng nhập / Đăng ký
          </router-link>
        </template>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  position: sticky;
  top: 0;
  z-index: 100;
  border-radius: 0;
  border-left: none;
  border-right: none;
  border-top: none;
  border-bottom: 1px solid var(--border-subtle);
  background: rgba(15, 20, 28, 0.92);
}

.header-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 12px 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
}

.chess-icon {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: radial-gradient(circle, #b91c1c, #7f1d1d);
  color: #fef08a;
  border: 2px solid #f59e0b;
  box-shadow: 0 0 12px rgba(220, 38, 38, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-serif);
  font-size: 1.5rem;
  font-weight: 800;
}

.brand-text {
  display: flex;
  flex-direction: column;
}

.brand-title {
  font-size: 1.35rem;
  font-weight: 800;
  color: #f8fafc;
  line-height: 1.1;
  background: linear-gradient(135deg, #ffffff, #cbd5e1);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.brand-sub {
  font-size: 0.65rem;
  letter-spacing: 0.15em;
  color: var(--gold-primary);
  font-weight: 600;
}

.nav-links {
  display: flex;
  align-items: center;
  gap: 16px;
}

.nav-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border-radius: var(--radius-sm);
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--text-muted);
  transition: all 0.2s ease;
}

.nav-item:hover {
  color: var(--text-main);
  background: rgba(255, 255, 255, 0.05);
}

.nav-item.active {
  color: #ffffff;
  background: rgba(220, 38, 38, 0.2);
  border: 1px solid rgba(220, 38, 38, 0.4);
}

.user-section {
  display: flex;
  align-items: center;
  gap: 14px;
}

.user-pill {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(255, 255, 255, 0.04);
  padding: 6px 12px;
  border-radius: var(--radius-full);
  border: 1px solid var(--border-subtle);
}

.avatar-circle {
  font-size: 1.1rem;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.user-name {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-main);
}
</style>
