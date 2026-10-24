import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/lobby'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: {
      title: 'Đăng Nhập / Đăng Ký - Cờ Tướng Online',
      guestOnly: true
    }
  },
  {
    path: '/lobby',
    name: 'Lobby',
    component: () => import('@/views/LobbyView.vue'),
    meta: {
      title: 'Sảnh Chờ Cờ Tướng',
      requiresAuth: true
    }
  },
  {
    path: '/room/:id',
    name: 'Room',
    component: () => import('@/views/RoomView.vue'),
    props: true,
    meta: {
      title: 'Phòng Cờ',
      requiresAuth: true
    }
  },
  {
    path: '/match/:id',
    name: 'Match',
    component: () => import('@/views/MatchView.vue'),
    props: true,
    meta: {
      title: 'Ván Đấu Cờ Tướng Trực Tiếp',
      requiresAuth: true
    }
  },
  {
    path: '/history',
    name: 'History',
    component: () => import('@/views/HistoryView.vue'),
    meta: {
      title: 'Lịch Sử Đấu & Kỳ Phổ',
      requiresAuth: true
    }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/lobby'
  }
];

export const router = createRouter({
  history: createWebHistory(),
  routes
});

// Navigation Guard: Kiểm tra phiên đăng nhập và bảo vệ các màn hình (GCT-34)
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore();

  // Đổi title trang
  if (to.meta.title) {
    document.title = `${to.meta.title} | Kỳ Đạo`;
  }

  // 1. Chặn màn hình riêng khi chưa đăng nhập (Bảo vệ màn hình riêng & Chống lặp chuyển trang)
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // Nếu đích đến đã là /login thì cho qua để tránh loop
    if (to.path === '/login') {
      return next();
    }
    return next({
      path: '/login',
      query: { redirect: to.fullPath }
    });
  }

  // 2. Nếu đã đăng nhập mà cố vào lại /login -> chuyển về /lobby (chống lặp)
  if (to.meta.guestOnly && authStore.isAuthenticated) {
    if (to.path === '/lobby') {
      return next();
    }
    return next('/lobby');
  }

  next();
});

export default router;
