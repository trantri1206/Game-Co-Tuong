import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface UserProfile {
  userId: number;
  username: string;
  email: string;
  displayName: string;
}

/**
 * Store Quản Lý Phiên Xác Thực (GCT-34):
 * - TUYỆT ĐỐI KHÔNG LƯU TOKEN HOẶC MẬT KHẨU TRONG LOCALSTORAGE.
 * - Toàn bộ Access Token và User Profile chỉ được lưu trữ trong bộ nhớ tạm (In-Memory RAM).
 * - Khi người dùng tải lại trang (F5/Reload), bộ nhớ sẽ reset về null và yêu cầu đăng nhập lại.
 */
export const useAuthStore = defineStore('auth', () => {
  // Trạng thái thuần túy trong RAM (In-Memory)
  const token = ref<string | null>(null);
  const user = ref<UserProfile | null>(null);

  // Dọn dẹp triệt để bất kỳ khóa lưu trữ nhạy cảm cũ nào trong localStorage nếu có
  try {
    localStorage.removeItem('cotuong_access_token');
    localStorage.removeItem('cotuong_user_profile');
    localStorage.removeItem('token');
    localStorage.removeItem('password');
    localStorage.removeItem('access_token');
  } catch {
    // Không làm gián đoạn nếu môi trường chặn localStorage
  }

  const isAuthenticated = computed(() => !!token.value);

  function setAuth(newToken: string, newUser: UserProfile) {
    // Chỉ lưu trong bộ nhớ ứng dụng
    token.value = newToken;
    user.value = newUser;
  }

  function clearAuth() {
    token.value = null;
    user.value = null;

    // Đảm bảo không còn dữ liệu trong storage
    try {
      localStorage.removeItem('cotuong_access_token');
      localStorage.removeItem('cotuong_user_profile');
      sessionStorage.clear();
    } catch {}
  }

  return {
    token,
    user,
    isAuthenticated,
    setAuth,
    clearAuth
  };
});
