import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface UserProfile {
  userId: number;
  username: string;
  email: string;
  displayName: string;
}

const TOKEN_KEY = 'cotuong_access_token';
const USER_KEY = 'cotuong_user_profile';

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY));
  const user = ref<UserProfile | null>(null);

  // Khôi phục thông tin user từ localStorage nếu có
  const savedUser = localStorage.getItem(USER_KEY);
  if (savedUser) {
    try {
      user.value = JSON.parse(savedUser);
    } catch {
      localStorage.removeItem(USER_KEY);
    }
  }

  const isAuthenticated = computed(() => !!token.value);

  function setAuth(newToken: string, newUser: UserProfile) {
    token.value = newToken;
    user.value = newUser;
    localStorage.setItem(TOKEN_KEY, newToken);
    localStorage.setItem(USER_KEY, JSON.stringify(newUser));
  }

  function clearAuth() {
    token.value = null;
    user.value = null;
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  return {
    token,
    user,
    isAuthenticated,
    setAuth,
    clearAuth
  };
});
