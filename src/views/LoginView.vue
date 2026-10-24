<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { authApi } from '@/api/authApi';
import { signalrService } from '@/services/signalrService';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const appStore = useAppStore();

const activeTab = ref<'login' | 'register'>('login');
const isSubmitting = ref(false);

// 1. Dữ liệu Form Đăng nhập & Lỗi từng trường
const loginForm = reactive({
  usernameOrEmail: 'player_red_demo',
  password: 'Password123!'
});

const loginErrors = reactive<{
  usernameOrEmail?: string;
  password?: string;
  general?: string;
}>({});

// 2. Dữ liệu Form Đăng ký & Lỗi từng trường
const registerForm = reactive({
  username: '',
  email: '',
  displayName: '',
  password: '',
  confirmPassword: ''
});

const registerErrors = reactive<{
  username?: string;
  email?: string;
  displayName?: string;
  password?: string;
  confirmPassword?: string;
  general?: string;
}>({});

function clearLoginErrors(field?: 'usernameOrEmail' | 'password') {
  if (field) {
    delete loginErrors[field];
  } else {
    Object.keys(loginErrors).forEach((key) => delete (loginErrors as any)[key]);
  }
  delete loginErrors.general;
}

function clearRegisterErrors(field?: 'username' | 'email' | 'displayName' | 'password' | 'confirmPassword') {
  if (field) {
    delete registerErrors[field];
  } else {
    Object.keys(registerErrors).forEach((key) => delete (registerErrors as any)[key]);
  }
  delete registerErrors.general;
}

function switchTab(tab: 'login' | 'register') {
  activeTab.value = tab;
  clearLoginErrors();
  clearRegisterErrors();
}

// 3. Kiểm tra tính hợp lệ Client-side cho Đăng nhập
function validateLogin(): boolean {
  clearLoginErrors();
  let isValid = true;

  if (!loginForm.usernameOrEmail.trim()) {
    loginErrors.usernameOrEmail = 'Tên đăng nhập hoặc email không được để trống.';
    isValid = false;
  }

  if (!loginForm.password) {
    loginErrors.password = 'Mật khẩu không được để trống.';
    isValid = false;
  }

  return isValid;
}

// 4. Kiểm tra tính hợp lệ Client-side cho Đăng ký
function validateRegister(): boolean {
  clearRegisterErrors();
  let isValid = true;

  // Username
  const username = registerForm.username.trim();
  if (!username) {
    registerErrors.username = 'Tên đăng nhập không được để trống.';
    isValid = false;
  } else if (username.length < 3 || username.length > 30) {
    registerErrors.username = 'Tên đăng nhập phải có độ dài từ 3 đến 30 ký tự.';
    isValid = false;
  } else if (!/^[a-zA-Z0-9_]+$/.test(username)) {
    registerErrors.username = 'Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới (_), không chứa khoảng trắng.';
    isValid = false;
  }

  // Email
  const email = registerForm.email.trim();
  if (!email) {
    registerErrors.email = 'Email không được để trống.';
    isValid = false;
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    registerErrors.email = 'Email không đúng định dạng hợp lệ (ví dụ: user@example.com).';
    isValid = false;
  }

  // Password
  const pwd = registerForm.password;
  if (!pwd) {
    registerErrors.password = 'Mật khẩu không được để trống.';
    isValid = false;
  } else {
    if (pwd.length < 8) {
      registerErrors.password = 'Mật khẩu phải có độ dài tối thiểu 8 ký tự.';
      isValid = false;
    } else if (!/[A-Z]/.test(pwd)) {
      registerErrors.password = 'Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).';
      isValid = false;
    } else if (!/[a-z]/.test(pwd)) {
      registerErrors.password = 'Mật khẩu phải chứa ít nhất 1 chữ cái in thường (a-z).';
      isValid = false;
    } else if (!/[0-9]/.test(pwd)) {
      registerErrors.password = 'Mật khẩu phải chứa ít nhất 1 chữ số (0-9).';
      isValid = false;
    } else if (!/[!@#$%^&*(),.?":{}|<>]/.test(pwd)) {
      registerErrors.password = 'Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (ví dụ: !@#$%^&*).';
      isValid = false;
    }
  }

  // Confirm Password
  if (!registerForm.confirmPassword) {
    registerErrors.confirmPassword = 'Vui lòng xác nhận lại mật khẩu.';
    isValid = false;
  } else if (registerForm.confirmPassword !== registerForm.password) {
    registerErrors.confirmPassword = 'Mật khẩu xác nhận không khớp với mật khẩu.';
    isValid = false;
  }

  return isValid;
}

// 5. Xử lý Đăng nhập với cơ chế Chống click liên tục & Gọi API Thật
async function handleLogin() {
  // Ngăn chặn bấm liên tục gửi nhiều yêu cầu đồng thời
  if (isSubmitting.value) return;

  if (!validateLogin()) return;

  isSubmitting.value = true;
  appStore.setLoading(true, 'Đang xác thực thông tin đăng nhập...');

  try {
    const data = await authApi.login({
      usernameOrEmail: loginForm.usernameOrEmail.trim(),
      password: loginForm.password
    });

    authStore.setAuth(data.token, {
      userId: data.user.userId,
      username: data.user.username,
      email: data.user.email,
      displayName: data.user.displayName
    });

    // Kết nối SignalR bằng access token trong bộ nhớ RAM
    await signalrService.connect();

    appStore.notify('Đăng nhập thành công! Đang chuyển vào màn hình yêu cầu...', 'success');

    // Đăng nhập xong về đúng màn hình cần mở & Chống lặp chuyển trang (GCT-34)
    let targetPath = '/lobby';
    if (route.query.redirect && typeof route.query.redirect === 'string') {
      const decoded = decodeURIComponent(route.query.redirect);
      // Chỉ chấp nhận đường dẫn tương đối nội bộ và không phải /login để tránh lặp
      if (decoded.startsWith('/') && !decoded.startsWith('//') && decoded !== '/login') {
        targetPath = decoded;
      }
    }
    await router.push(targetPath);
  } catch (err: any) {
    // Luôn mở lại nút khi có lỗi
    if (err.statusCode === 401) {
      loginErrors.general = 'Tên đăng nhập hoặc mật khẩu không chính xác.';
    } else if (err.isNetworkError) {
      loginErrors.general = 'Không thể kết nối tới máy chủ backend. Vui lòng kiểm tra lại dịch vụ.';
    } else {
      loginErrors.general = err.message || 'Đăng nhập không thành công.';
    }
  } finally {
    isSubmitting.value = false;
    appStore.setLoading(false);
  }
}

// 6. Xử lý Đăng ký với cơ chế Phân phối Lỗi từng trường
async function handleRegister() {
  // Ngăn chặn bấm liên tục gửi nhiều yêu cầu đồng thời
  if (isSubmitting.value) return;

  if (!validateRegister()) return;

  isSubmitting.value = true;
  appStore.setLoading(true, 'Đang khởi tạo tài khoản mới...');

  try {
    await authApi.register({
      username: registerForm.username.trim(),
      email: registerForm.email.trim(),
      displayName: registerForm.displayName.trim() || registerForm.username.trim(),
      password: registerForm.password,
      confirmPassword: registerForm.confirmPassword
    });

    appStore.notify('Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay.', 'success');

    // Chuyển sang tab đăng nhập và điền sẵn tên tài khoản
    loginForm.usernameOrEmail = registerForm.username.trim();
    loginForm.password = '';
    switchTab('login');
  } catch (err: any) {
    // Phân tích lỗi từ máy chủ để gán chính xác vào từng trường
    const errors: string[] = err.errors || (err.message ? [err.message] : []);
    let hasMappedField = false;

    errors.forEach((msg) => {
      const lower = msg.toLowerCase();
      if (lower.includes('tên đăng nhập') || lower.includes('username')) {
        registerErrors.username = msg;
        hasMappedField = true;
      } else if (lower.includes('email')) {
        registerErrors.email = msg;
        hasMappedField = true;
      } else if (lower.includes('mật khẩu') || lower.includes('password')) {
        registerErrors.password = msg;
        hasMappedField = true;
      } else if (lower.includes('xác nhận')) {
        registerErrors.confirmPassword = msg;
        hasMappedField = true;
      }
    });

    if (!hasMappedField) {
      registerErrors.general = err.message || 'Đăng ký tài khoản không thành công.';
    }
  } finally {
    // Luôn mở lại nút khi có lỗi
    isSubmitting.value = false;
    appStore.setLoading(false);
  }
}

function fillDemo(userType: 'red' | 'black') {
  clearLoginErrors();
  if (userType === 'red') {
    loginForm.usernameOrEmail = 'player_red_demo';
    loginForm.password = 'Password123!';
  } else {
    loginForm.usernameOrEmail = 'player_black_demo';
    loginForm.password = 'Password123!';
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
        <p class="subtitle">Nền tảng Cờ Tướng trực tuyến kết nối kỳ thủ bốn phương</p>
      </div>

      <!-- Tabs Chuyển Đổi Đăng Nhập / Đăng Ký -->
      <div class="tab-switcher">
        <button
          type="button"
          class="tab-btn"
          :class="{ active: activeTab === 'login' }"
          @click="switchTab('login')"
        >
          Đăng Nhập
        </button>
        <button
          type="button"
          class="tab-btn"
          :class="{ active: activeTab === 'register' }"
          @click="switchTab('register')"
        >
          Đăng Ký
        </button>
      </div>

      <!-- ================= FORM ĐĂNG NHẬP ================= -->
      <form v-if="activeTab === 'login'" @submit.prevent="handleLogin" class="auth-form" novalidate>
        <!-- Lỗi chung toàn form -->
        <div v-if="loginErrors.general" class="form-error-alert">
          <span>⚠️ {{ loginErrors.general }}</span>
        </div>

        <!-- Ô Tên đăng nhập / Email -->
        <div class="form-group">
          <label class="form-label" for="login-username">
            Tên đăng nhập hoặc Email <span class="required-star">*</span>
          </label>
          <input
            id="login-username"
            v-model="loginForm.usernameOrEmail"
            type="text"
            class="form-input"
            :class="{ 'has-error': !!loginErrors.usernameOrEmail }"
            placeholder="Nhập tên đăng nhập hoặc email..."
            autocomplete="username"
            :disabled="isSubmitting"
            @input="clearLoginErrors('usernameOrEmail')"
          />
          <span v-if="loginErrors.usernameOrEmail" class="field-error-msg">
            ⚠️ {{ loginErrors.usernameOrEmail }}
          </span>
        </div>

        <!-- Ô Mật khẩu -->
        <div class="form-group">
          <label class="form-label" for="login-password">
            Mật khẩu <span class="required-star">*</span>
          </label>
          <input
            id="login-password"
            v-model="loginForm.password"
            type="password"
            class="form-input"
            :class="{ 'has-error': !!loginErrors.password }"
            placeholder="••••••••"
            autocomplete="current-password"
            :disabled="isSubmitting"
            @input="clearLoginErrors('password')"
          />
          <span v-if="loginErrors.password" class="field-error-msg">
            ⚠️ {{ loginErrors.password }}
          </span>
        </div>

        <!-- Nút Đăng Nhập (Khóa khi đang gửi & Chống click liên tục) -->
        <button
          type="submit"
          class="btn btn-primary btn-block"
          :disabled="isSubmitting"
          :class="{ 'btn-submitting': isSubmitting }"
        >
          <span v-if="isSubmitting" class="spinner-icon animate-spin">🔄</span>
          <span>{{ isSubmitting ? 'Đang xác thực thông tin...' : 'Đăng Nhập Vào Sảnh ➔' }}</span>
        </button>

        <!-- Điền nhanh tài khoản mẫu -->
        <div class="demo-quick-fill">
          <span class="demo-label">Tài khoản thử nghiệm sẵn có:</span>
          <div class="demo-buttons">
            <button
              type="button"
              class="btn btn-secondary btn-sm"
              :disabled="isSubmitting"
              @click="fillDemo('red')"
            >
              🔴 Kỳ Thủ Đỏ Demo
            </button>
            <button
              type="button"
              class="btn btn-secondary btn-sm"
              :disabled="isSubmitting"
              @click="fillDemo('black')"
            >
              ⚫ Kỳ Thủ Đen Demo
            </button>
          </div>
        </div>
      </form>

      <!-- ================= FORM ĐĂNG KÝ ================= -->
      <form v-else @submit.prevent="handleRegister" class="auth-form" novalidate>
        <!-- Lỗi chung toàn form -->
        <div v-if="registerErrors.general" class="form-error-alert">
          <span>⚠️ {{ registerErrors.general }}</span>
        </div>

        <!-- Ô Tên đăng nhập -->
        <div class="form-group">
          <label class="form-label" for="reg-username">
            Tên đăng nhập <span class="required-star">*</span>
          </label>
          <input
            id="reg-username"
            v-model="registerForm.username"
            type="text"
            class="form-input"
            :class="{ 'has-error': !!registerErrors.username }"
            placeholder="vd: ky_thu_2026 (3-30 ký tự)"
            autocomplete="username"
            :disabled="isSubmitting"
            @input="clearRegisterErrors('username')"
          />
          <span v-if="registerErrors.username" class="field-error-msg">
            ⚠️ {{ registerErrors.username }}
          </span>
        </div>

        <!-- Ô Email -->
        <div class="form-group">
          <label class="form-label" for="reg-email">
            Địa chỉ Email <span class="required-star">*</span>
          </label>
          <input
            id="reg-email"
            v-model="registerForm.email"
            type="email"
            class="form-input"
            :class="{ 'has-error': !!registerErrors.email }"
            placeholder="vd: kythu@example.com"
            autocomplete="email"
            :disabled="isSubmitting"
            @input="clearRegisterErrors('email')"
          />
          <span v-if="registerErrors.email" class="field-error-msg">
            ⚠️ {{ registerErrors.email }}
          </span>
        </div>

        <!-- Ô Tên hiển thị -->
        <div class="form-group">
          <label class="form-label" for="reg-displayname">
            Tên hiển thị <span class="optional-tag">(Tùy chọn)</span>
          </label>
          <input
            id="reg-displayname"
            v-model="registerForm.displayName"
            type="text"
            class="form-input"
            :class="{ 'has-error': !!registerErrors.displayName }"
            placeholder="vd: Kỳ Vương Phương Nam"
            autocomplete="nickname"
            :disabled="isSubmitting"
            @input="clearRegisterErrors('displayName')"
          />
          <span v-if="registerErrors.displayName" class="field-error-msg">
            ⚠️ {{ registerErrors.displayName }}
          </span>
        </div>

        <!-- Ô Mật khẩu -->
        <div class="form-group">
          <label class="form-label" for="reg-password">
            Mật khẩu bảo mật <span class="required-star">*</span>
          </label>
          <input
            id="reg-password"
            v-model="registerForm.password"
            type="password"
            class="form-input"
            :class="{ 'has-error': !!registerErrors.password }"
            placeholder="Tối thiểu 8 ký tự, gồm chữ hoa, thường, số và ký tự đặc biệt"
            autocomplete="new-password"
            :disabled="isSubmitting"
            @input="clearRegisterErrors('password')"
          />
          <span v-if="registerErrors.password" class="field-error-msg">
            ⚠️ {{ registerErrors.password }}
          </span>
        </div>

        <!-- Ô Xác nhận mật khẩu -->
        <div class="form-group">
          <label class="form-label" for="reg-confirm-password">
            Xác nhận mật khẩu <span class="required-star">*</span>
          </label>
          <input
            id="reg-confirm-password"
            v-model="registerForm.confirmPassword"
            type="password"
            class="form-input"
            :class="{ 'has-error': !!registerErrors.confirmPassword }"
            placeholder="Nhập lại chính xác mật khẩu ở trên"
            autocomplete="new-password"
            :disabled="isSubmitting"
            @input="clearRegisterErrors('confirmPassword')"
          />
          <span v-if="registerErrors.confirmPassword" class="field-error-msg">
            ⚠️ {{ registerErrors.confirmPassword }}
          </span>
        </div>

        <!-- Nút Đăng Ký (Khóa khi đang gửi & Chống click liên tục) -->
        <button
          type="submit"
          class="btn btn-primary btn-block"
          :disabled="isSubmitting"
          :class="{ 'btn-submitting': isSubmitting }"
        >
          <span v-if="isSubmitting" class="spinner-icon animate-spin">🔄</span>
          <span>{{ isSubmitting ? 'Đang khởi tạo tài khoản...' : 'Tạo Tài Khoản Mới ➔' }}</span>
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
  max-width: 480px;
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
  font-size: 0.85rem;
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

.form-group {
  margin-bottom: 16px;
}

.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-muted);
  margin-bottom: 6px;
}

.required-star {
  color: #ef4444;
}

.optional-tag {
  color: var(--text-dim);
  font-size: 0.75rem;
  font-weight: normal;
}

.form-input.has-error {
  border-color: #ef4444;
  background: rgba(239, 68, 68, 0.05);
  box-shadow: 0 0 0 1px rgba(239, 68, 68, 0.3);
}

.field-error-msg {
  display: block;
  font-size: 0.8rem;
  color: #f87171;
  margin-top: 5px;
  line-height: 1.3;
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
  margin-top: 10px;
}

.btn-submitting {
  opacity: 0.8;
  cursor: not-allowed;
}

.spinner-icon {
  display: inline-block;
  margin-right: 6px;
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
