<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { authApi, type RoomSummary } from '@/api/authApi';
import { useAppStore } from '@/stores/app';

const router = useRouter();
const appStore = useAppStore();

const rooms = ref<RoomSummary[]>([]);
const isLoadingRooms = ref(false);
const filterStatus = ref<'All' | 'Waiting' | 'Playing'>('All');
const searchQuery = ref('');

// Tạo phòng mới modal
const showCreateModal = ref(false);
const newRoomName = ref('');

async function loadRooms() {
  isLoadingRooms.value = true;
  appStore.setLoading(true, 'Đang tải danh sách sảnh cờ...');
  try {
    rooms.value = await authApi.getRooms();
    appStore.setApiState('connected');
  } catch (err: any) {
    appStore.notify('Không thể tải danh sách phòng: ' + err.message, 'error');
  } finally {
    isLoadingRooms.value = false;
    appStore.setLoading(false);
  }
}

onMounted(() => {
  loadRooms();
});

const filteredRooms = computed(() => {
  return rooms.value.filter((r) => {
    const matchStatus = filterStatus.value === 'All' || r.status === filterStatus.value;
    const matchSearch =
      r.roomName.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      r.hostName.toLowerCase().includes(searchQuery.value.toLowerCase());
    return matchStatus && matchSearch;
  });
});

function enterRoom(roomId: number) {
  router.push(`/room/${roomId}`);
}

function handleCreateRoom() {
  if (!newRoomName.value.trim()) {
    appStore.notify('Vui lòng nhập tên phòng.', 'warning');
    return;
  }
  const createdId = Math.floor(Math.random() * 900) + 100;
  appStore.notify(`Đã tạo phòng #${createdId}: ${newRoomName.value}!`, 'success');
  showCreateModal.value = false;
  router.push(`/room/${createdId}`);
}

// Hàm kiểm thử cơ chế Retry và Xử lý lỗi theo tiêu chí GCT-32
async function testRetryMechanism(type: 'network' | 'timeout' | '500') {
  appStore.notify(`Đang phát lệnh kiểm thử API (${type})...`, 'info');
  try {
    await authApi.testSimulateError(type);
  } catch (err: any) {
    console.log('[Test Error Intercepted]', err);
  }
}
</script>

<template>
  <div class="lobby-page">
    <div class="lobby-container">
      <!-- Banner Sảnh & Nút Tạo Phòng -->
      <div class="lobby-hero glass-panel">
        <div class="hero-left">
          <h1 class="title-serif">SẢNH CHỜ THI ĐẤU</h1>
          <p class="hero-desc">Chọn phòng cờ để so tài hoặc tạo bàn đấu riêng cùng kỳ hữu</p>
        </div>
        <div class="hero-right">
          <button class="btn btn-gold" @click="showCreateModal = true">
            ➕ Tạo Phòng Mới
          </button>
          <button class="btn btn-secondary" @click="loadRooms" :disabled="isLoadingRooms">
            🔄 Làm Mới
          </button>
        </div>
      </div>

      <!-- Khu vực Kiểm Thử API & Phục Hồi Lỗi (GCT-32 Showcase) -->
      <div class="test-api-panel glass-panel">
        <div class="panel-header">
          <span class="badge badge-gold">KIỂM TRA CƠ CHẾ KẾT NỐI API & TỰ ĐỘNG THỬ LẠI (GCT-32)</span>
        </div>
        <p class="panel-desc">
          Nhấn các nút bên dưới để mô phỏng sự cố mạng, timeout hoặc lỗi 5xx. Hệ thống sẽ tự động thử lại 3 lần (Backoff 1s, 2s, 4s) và hiển thị nút "Thử lại ngay" trên thanh trạng thái mà không làm ứng dụng bị kẹt:
        </p>
        <div class="test-buttons">
          <button class="btn btn-secondary btn-sm" @click="testRetryMechanism('network')">
            🧪 Thử Lỗi Mạng (Network Error)
          </button>
          <button class="btn btn-secondary btn-sm" @click="testRetryMechanism('timeout')">
            🧪 Thử Quá Giờ Chờ (Timeout 10s)
          </button>
          <button class="btn btn-secondary btn-sm" @click="testRetryMechanism('500')">
            🧪 Thử Lỗi Máy Chủ (HTTP 500)
          </button>
        </div>
      </div>

      <!-- Thanh Tìm Kiếm & Bộ Lọc Trạng Thái -->
      <div class="filter-bar">
        <div class="filter-tabs">
          <button
            class="filter-tab"
            :class="{ active: filterStatus === 'All' }"
            @click="filterStatus = 'All'"
          >
            Tất Cả ({{ rooms.length }})
          </button>
          <button
            class="filter-tab"
            :class="{ active: filterStatus === 'Waiting' }"
            @click="filterStatus = 'Waiting'"
          >
            Đang Chờ
          </button>
          <button
            class="filter-tab"
            :class="{ active: filterStatus === 'Playing' }"
            @click="filterStatus = 'Playing'"
          >
            Đang Đấu
          </button>
        </div>

        <div class="search-box">
          <input
            v-model="searchQuery"
            type="text"
            class="form-input search-input"
            placeholder="🔍 Tìm tên phòng, chủ phòng..."
          />
        </div>
      </div>

      <!-- Danh Sách Phòng Cờ -->
      <div v-if="filteredRooms.length > 0" class="room-grid">
        <div
          v-for="room in filteredRooms"
          :key="room.roomId"
          class="room-card glass-panel"
          @click="enterRoom(room.roomId)"
        >
          <div class="room-card-header">
            <span class="room-id">#{{ room.roomId }}</span>
            <span
              class="badge"
              :class="room.status === 'Waiting' ? 'badge-green' : 'badge-red'"
            >
              {{ room.status === 'Waiting' ? 'Chờ Đấu Thủ' : 'Đang Thi Đấu' }}
            </span>
          </div>

          <h3 class="room-title">{{ room.roomName }}</h3>
          <p class="room-host">Chủ phòng: <strong>{{ room.hostName }}</strong></p>

          <div class="room-stats">
            <span class="stat-item">👥 {{ room.playerCount }}/2 Kỳ thủ</span>
            <span class="stat-item">👁️ {{ room.spectatorCount }} Khán giả</span>
          </div>

          <div class="room-card-footer">
            <button class="btn btn-primary btn-sm btn-join">
              {{ room.status === 'Waiting' ? 'Tham Gia' : 'Xem Trận' }} ➔
            </button>
          </div>
        </div>
      </div>

      <!-- Trạng Thái Trống -->
      <div v-else class="empty-state glass-panel">
        <div class="empty-icon">📭</div>
        <h3>Không tìm thấy phòng cờ nào</h3>
        <p>Hãy thử thay đổi bộ lọc hoặc tạo phòng mới để bắt đầu ván đấu!</p>
      </div>
    </div>

    <!-- Modal Tạo Phòng Mới -->
    <div v-if="showCreateModal" class="modal-backdrop">
      <div class="modal-card glass-panel">
        <h2 class="title-serif">Tạo Phòng Cờ Mới</h2>
        <div class="form-group" style="margin-top: 16px;">
          <label class="form-label">Tên phòng thi đấu</label>
          <input
            v-model="newRoomName"
            type="text"
            class="form-input"
            placeholder="vd: Bàn Cờ Đỉnh Núi"
            autofocus
          />
        </div>
        <div class="modal-actions">
          <button class="btn btn-secondary" @click="showCreateModal = false">Hủy</button>
          <button class="btn btn-gold" @click="handleCreateRoom">Xác Nhận Tạo</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.lobby-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px 20px 60px 20px;
}

.lobby-hero {
  padding: 28px 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  border-left: 4px solid var(--red-primary);
}

.hero-left h1 {
  font-size: 1.8rem;
  color: #ffffff;
}

.hero-desc {
  color: var(--text-muted);
  margin-top: 4px;
}

.hero-right {
  display: flex;
  gap: 12px;
}

.test-api-panel {
  padding: 16px 20px;
  margin-bottom: 24px;
  background: rgba(30, 41, 59, 0.5);
  border: 1px dashed rgba(245, 158, 11, 0.4);
}

.panel-desc {
  font-size: 0.85rem;
  color: var(--text-muted);
  margin: 8px 0 12px 0;
}

.test-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 16px;
  flex-wrap: wrap;
}

.filter-tabs {
  display: flex;
  gap: 8px;
}

.filter-tab {
  padding: 8px 16px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  color: var(--text-muted);
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.filter-tab.active {
  background: var(--red-primary);
  color: #ffffff;
  border-color: var(--red-primary);
}

.search-input {
  min-width: 260px;
  padding: 8px 14px;
}

.room-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(270px, 1fr));
  gap: 20px;
}

.room-card {
  padding: 20px;
  cursor: pointer;
  transition: all 0.25s ease;
  display: flex;
  flex-direction: column;
}

.room-card:hover {
  transform: translateY(-4px);
  border-color: var(--border-focus);
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.6);
}

.room-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.room-id {
  font-family: monospace;
  color: var(--text-dim);
  font-size: 0.85rem;
}

.room-title {
  font-size: 1.15rem;
  margin-bottom: 6px;
}

.room-host {
  font-size: 0.85rem;
  color: var(--text-muted);
  margin-bottom: 16px;
}

.room-stats {
  display: flex;
  gap: 12px;
  font-size: 0.8rem;
  color: var(--text-dim);
  margin-top: auto;
  padding-top: 12px;
  border-top: 1px solid var(--border-subtle);
}

.room-card-footer {
  margin-top: 14px;
}

.btn-join {
  width: 100%;
}

.empty-state {
  padding: 60px 20px;
  text-align: center;
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 12px;
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
}

.modal-card {
  width: 100%;
  max-width: 440px;
  padding: 28px;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}
</style>
