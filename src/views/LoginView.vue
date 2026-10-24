<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { authApi } from '@/api/authApi';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const appStore = useAppStore();

const activeTab = ref<'login' | 'register'>('login');

// Login Form
const loginForm = ref({
  usernameOrEmail: 'player_red_demo',
  password: 'Password123!'
});

// Register Form
const registerForm = ref({
  username: '',
  email: '',
  displayName: '',
  password: '',
  confirmPassword: ''
});

const isSubmitting = ref(false);
const formError = ref<string | null>(null);

async function handleLogin() {
  formError.value = null;
  if (!loginForm.value.usernameOrEmail || !loginForm.value.password) {
    formError.value = 'Vui lòng nhập tên đăng nhập/email và mật khẩu.';
    return;
  }

  isSubmitting.value = true;
  appStore.setLoading(true, 'Đang xác thực thông tin đăng nhập...');

  try {
    const data = await authApi.login({
      usernameOrEmail: loginForm.value.usernameOrEmail,
      password: loginForm.value.password
    });

    authStore.setAuth(data.token, {
      userId: data.user.userId,
      username: data.user.username,
      email: data.user.email,
      displayName: data.user.displayName
    });

    appStore.notify('Đăng nhập thành công! Chào mừng bạn vào sảnh cờ.', 'success');
    const redirectPath = (route.query.redirect as string) || '/lobby';
    router.push(redirectPath);
  } catch (err: any) {
    // Nếu API backend đang tắt, hỗ trợ đăng nhập mẫu demo để trải nghiệm giao diện
    if (err.isNetworkError || err.statusCode === 404) {
      console.warn('Backend chưa sẵn sàng, kích hoạt chế độ đăng nhập giả lập cho demo shell.');
      authStore.setAuth('mock_jwt_token_demo_' + Date.now(), {
        userId: 1,
        username: loginForm.value.usernameOrEmail || 'player_red_demo',
        email: 'player_red@example.com',
        displayName: 'Kỳ Thủ Đỏ Demo'
      });
      appStore.notify('Đã đăng nhập chế độ Demo (Backend Offline)', 'info');
      router.push('/lobby');
    } else {
      formError.value = err.message || 'Đăng nhập không thành công.';
    }
  } finally {
    isSubmitting.value = false;
    appStore.setLoading(false);
  }
}

async function handleRegister() {
  formError.value = null;
  if (!registerForm.value.username || !registerForm.value.email || !registerForm.value.password) {
    formError.value = 'Vui lòng điền đầy đủ các thông tin bắt buộc.';
    return;
  }

  if (registerForm.value.password !== registerForm.value.confirmPassword) {
    formError.value = 'Mật khẩu xác nhận không khớp.';
    return;
  }

  isSubmitting.value = true;
  appStore.setLoading(true, 'Đang tạo tài khoản mới...');

  try {
    await authApi.register({
      username: registerForm.value.username,
      email: registerForm.value.email,
      displayName: registerForm.value.displayName || registerForm.value.username,
      password: registerForm.value.password,
      confirmPassword: registerForm.value.confirmPassword
    });

    appStore.notify('Đăng ký thành công! Hãy đăng nhập bằng tài khoản mới.', 'success');
    loginForm.value.usernameOrEmail = registerForm.value.username;
    loginForm.value.password = registerForm.value.password;
    activeTab.value = 'login';
  } catch (err: any) {
    formError.value = err.message || 'Đăng ký tài khoản không thành công.';
  } finally {
    isSubmitting.value = false;
    appStore.setLoading(false);
  }
}

function fillDemo(userType: 'red' | 'black') {
  if (userType === 'red') {
    loginForm.value.usernameOrEmail = 'player_red_demo';
    loginForm.value.password = 'Password123!';
  } else {
    loginForm.value.usernameOrEmail = 'player_black_demo';
    loginForm.value.password = 'Password123!';
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-card glass-panel">
      <!-- Header Card -->
      <div class="card-header">
        <div class="chess-badge">帥</div>
        <h1 class="title-serif">KỲ ĐẠO TINH HOA</h1>
        <p class="subtitle">Đăng nhập để vào sảnh cờ và bắt đầu ván đấu</p>
      </div>

      <!-- Tabs chuyển đổi -->
      <div class="tab-switcher">
        <button
          class="tab-btn"
          :class="{ active: activeTab === 'login' }"
          @click="activeTab = 'login'; formError = null"
        >
          Đăng Nhập
        </button>
        <button
          class="tab-btn"
          :class="{ active: activeTab === 'register' }"
          @click="activeTab = 'register'; formError = null"
        >
          Đăng Ký
        </button>
      </div>

      <!-- Thông báo lỗi form -->
      <div v-if="formError" class="form-error-alert">
        <span>⚠️ {{ formError }}</span>
      </div>

      <!-- Form Đăng Nhập -->
      <form v-if="activeTab === 'login'" @submit.prevent="handleLogin" class="auth-form">
        <div class="form-group">
          <label class="form-label">Tên đăng nhập hoặc Email</label>
          <input
            v-model="loginForm.usernameOrEmail"
            type="text"
            class="form-input"
            placeholder="vd: player_red_demo"
            required
          />
        </div>

        <div class="form-group">
          <label class="form-label">Mật khẩu</label>
          <input
            v-model="loginForm.password"
            type="password"
            class="form-input"
            placeholder="••••••••"
            required
          />
        </div>

        <button type="submit" class="btn btn-primary btn-block" :disabled="isSubmitting">
          {{ isSubmitting ? 'Đang đăng nhập...' : 'Vào Sảnh Cờ ➔' }}
        </button>

        <!-- Nút nhanh điền tài khoản mẫu -->
        <div class="demo-quick-fill">
          <span class="demo-label">Tài khoản thử nghiệm:</span>
          <div class="demo-buttons">
            <button type="button" class="btn btn-secondary btn-sm" @click="fillDemo('red')">
              🔴 Kỳ Thủ Đỏ
            </button>
            <button type="button" class="btn btn-secondary btn-sm" @click="fillDemo('black')">
              ⚫ Kỳ Thủ Đen
            </button>
          </div>
        </div>
      </form>

      <!-- Form Đăng Ký -->
      <form v-else @submit.prevent="handleRegister" class="auth-form">
        <div class="form-group">
          <label class="form-label">Tên đăng nhập</label>
          <input
            v-model="registerForm.username"
            type="text"
            class="form-input"
            placeholder="Chữ cái, số và dấu gạch dưới (3-30 ký tự)"
            required
          />
        </div>

        <div class="form-group">
          <label class="form-label">Địa chỉ Email</label>
          <input
            v-model="registerForm.email"
            type="email"
            class="form-input"
            placeholder="user@example.com"
            required
          />
        </div>

        <div class="form-group">
          <label class="form-label">Tên hiển thị (Tùy chọn)</label>
          <input
            v-model="registerForm.displayName"
            type="text"
            class="form-input"
            placeholder="vd: Kỳ Vương Phương Nam"
          />
        </div>

        <div class="form-group">
          <label class="form-label">Mật khẩu</label>
          <input
            v-model="registerForm.password"
            type="password"
            class="form-input"
            placeholder="Ít nhất 8 ký tự, gồm hoa, thường, số, ký tự đặc biệt"
            required
          />
        </div>

        <div class="form-group">
          <label class="form-label">Xác nhận mật khẩu</label>
          <input
            v-model="registerForm.confirmPassword"
            type="password"
            class="form-input"
            placeholder="Nhập lại mật khẩu"
            required
          />
        </div>

        <button type="submit" class="btn btn-primary btn-block" :disabled="isSubmitting">
          {{ isSubmitting ? 'Đang tạo...' : 'Tạo Tài Khoản ➔' }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  min-height: calc(100vh - 120px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
}

.login-card {
  width: 100%;
  max-width: 460px;
  padding: 36px 32px;
  border-radius: var(--radius-lg);
  border: 1px solid rgba(220, 38, 38, 0.3);
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.6);
}

.card-header {
  text-align: center;
  margin-bottom: 24px;
}

.chess-badge {
  width: 52px;
  height: 52px;
  margin: 0 auto 12px auto;
  border-radius: 50%;
  background: radial-gradient(circle, #b91c1c, #450a0a);
  border: 2px solid var(--gold-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-serif);
  font-size: 2rem;
  color: #fef08a;
  box-shadow: 0 0 16px var(--red-glow);
}

.title-serif {
  font-size: 1.6rem;
  color: #ffffff;
}

.subtitle {
  font-size: 0.875rem;
  color: var(--text-muted);
  margin-top: 4px;
}

.tab-switcher {
  display: flex;
  background: rgba(15, 20, 28, 0.7);
  border-radius: var(--radius-sm);
  padding: 4px;
  margin-bottom: 20px;
  border: 1px solid var(--border-subtle);
}

.tab-btn {
  flex: 1;
  padding: 8px 12px;
  border: none;
  background: transparent;
  color: var(--text-muted);
  font-family: inherit;
  font-weight: 600;
  font-size: 0.9rem;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.tab-btn.active {
  background: var(--red-primary);
  color: #ffffff;
  box-shadow: 0 2px 8px var(--red-glow);
}

.form-error-alert {
  padding: 10px 14px;
  background: rgba(220, 38, 38, 0.15);
  border: 1px solid rgba(220, 38, 38, 0.4);
  border-radius: var(--radius-sm);
  color: #fca5a5;
  font-size: 0.85rem;
  margin-bottom: 16px;
}

.btn-block {
  width: 100%;
  padding: 12px;
  font-size: 1rem;
  margin-top: 8px;
}

.demo-quick-fill {
  margin-top: 24px;
  padding-top: 18px;
  border-top: 1px solid var(--border-subtle);
  text-align: center;
}

.demo-label {
  display: block;
  font-size: 0.8rem;
  color: var(--text-dim);
  margin-bottom: 8px;
}

.demo-buttons {
  display: flex;
  gap: 8px;
  justify-content: center;
}
</style>
