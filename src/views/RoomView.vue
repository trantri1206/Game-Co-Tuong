<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { authApi } from '@/api/authApi';
import { useAppStore } from '@/stores/app';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();

const roomId = Number(route.params.id) || 1;
const room = ref<any>(null);
const isReady = ref(false);

async function loadRoomDetails() {
  appStore.setLoading(true, `Đang vào phòng #${roomId}...`);
  try {
    room.value = await authApi.getRoomDetails(roomId);
  } catch (err: any) {
    appStore.notify('Không thể tải thông tin phòng: ' + err.message, 'error');
  } finally {
    appStore.setLoading(false);
  }
}

onMounted(() => {
  loadRoomDetails();
});

function toggleReady() {
  isReady.value = !isReady.value;
  appStore.notify(isReady.value ? 'Bạn đã sẵn sàng!' : 'Đã hủy sẵn sàng.', 'info');
}

function startMatch() {
  appStore.notify('Bắt đầu ván đấu!', 'success');
  router.push(`/match/${roomId}`);
}

function leaveRoom() {
  router.push('/lobby');
}
</script>

<template>
  <div class="room-page">
    <div v-if="room" class="room-container">
      <!-- Header Phòng -->
      <div class="room-header glass-panel">
        <div class="header-info">
          <button class="btn btn-secondary btn-sm" @click="leaveRoom">
            ⬅ Rời Sảnh
          </button>
          <h1 class="title-serif">{{ room.roomName }}</h1>
          <span class="badge badge-gold">Mã Phòng #{{ roomId }}</span>
        </div>

        <div class="header-actions">
          <button
            class="btn"
            :class="isReady ? 'btn-gold' : 'btn-secondary'"
            @click="toggleReady"
          >
            {{ isReady ? '✅ Đã Sẵn Sàng' : '⏳ Bấm Sẵn Sàng' }}
          </button>
          <button class="btn btn-primary" @click="startMatch">
            ⚔️ Bắt Đầu Ván Đấu
          </button>
        </div>
      </div>

      <!-- Khu Vực Kỳ Thủ So Tài -->
      <div class="versus-arena">
        <!-- Phe Đỏ -->
        <div class="player-slot glass-panel slot-red">
          <div class="slot-badge badge-red">PHE ĐỎ (TIÊN CƠ)</div>
          <div class="chess-piece piece-red">帥</div>
          <h3 class="player-name">{{ room.redPlayer?.name || 'Trống' }}</h3>
          <span class="status-tag ready">Đã sẵn sàng</span>
        </div>

        <!-- VS Divider -->
        <div class="vs-divider title-serif">VS</div>

        <!-- Phe Đen -->
        <div class="player-slot glass-panel slot-black">
          <div class="slot-badge badge-gray">PHE ĐEN (HẬU CƠ)</div>
          <div class="chess-piece piece-black">將</div>
          <h3 class="player-name">{{ room.blackPlayer?.name || 'Đang chờ đấu thủ...' }}</h3>
          <span class="status-tag not-ready">{{ isReady ? 'Sẵn sàng' : 'Chưa sẵn sàng' }}</span>
        </div>
      </div>

      <!-- Danh Sách Khán Giả -->
      <div class="spectators-box glass-panel">
        <h3 class="box-title">👁️ Khán Giả Đang Xem ({{ room.spectators?.length || 0 }})</h3>
        <div class="spectator-list">
          <span
            v-for="(spec, idx) in room.spectators"
            :key="idx"
            class="spectator-pill"
          >
            ♟️ {{ spec }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.room-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 30px 20px 60px 20px;
}

.room-header {
  padding: 20px 28px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 30px;
  gap: 20px;
  flex-wrap: wrap;
}

.header-info {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-info h1 {
  font-size: 1.5rem;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.versus-arena {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  gap: 24px;
  margin-bottom: 30px;
}

.player-slot {
  padding: 36px 24px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  border-radius: var(--radius-lg);
}

.slot-red {
  border-top: 4px solid var(--red-primary);
}

.slot-black {
  border-top: 4px solid #475569;
}

.slot-badge {
  font-size: 0.75rem;
  font-weight: 700;
  padding: 4px 12px;
  border-radius: var(--radius-full);
  margin-bottom: 20px;
}

.chess-piece {
  width: 76px;
  height: 76px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-serif);
  font-size: 2.8rem;
  font-weight: 800;
  margin-bottom: 16px;
  box-shadow: var(--shadow-md);
}

.piece-red {
  background: radial-gradient(circle, #b91c1c, #450a0a);
  color: #fef08a;
  border: 3px solid #f59e0b;
}

.piece-black {
  background: radial-gradient(circle, #334155, #0f172a);
  color: #f1f5f9;
  border: 3px solid #64748b;
}

.player-name {
  font-size: 1.25rem;
  margin-bottom: 8px;
}

.status-tag {
  font-size: 0.8rem;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: var(--radius-full);
}
.status-tag.ready {
  background: rgba(16, 185, 129, 0.15);
  color: #6ee7b7;
}
.status-tag.not-ready {
  background: rgba(245, 158, 11, 0.15);
  color: #fde68a;
}

.vs-divider {
  font-size: 2rem;
  font-weight: 800;
  color: var(--text-dim);
}

.spectators-box {
  padding: 24px;
}

.box-title {
  font-size: 1rem;
  color: var(--text-muted);
  margin-bottom: 14px;
}

.spectator-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.spectator-pill {
  background: rgba(255, 255, 255, 0.05);
  padding: 6px 14px;
  border-radius: var(--radius-full);
  font-size: 0.85rem;
  color: var(--text-muted);
  border: 1px solid var(--border-subtle);
}
</style>
