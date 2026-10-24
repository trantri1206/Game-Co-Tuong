<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { authApi } from '@/api/authApi';
import { useAppStore } from '@/stores/app';

const route = useRoute();
const router = useRouter();
const appStore = useAppStore();

const matchId = Number(route.params.id) || 1;
const match = ref<any>(null);

async function loadMatch() {
  appStore.setLoading(true, `Đang kết nối ván cờ #${matchId}...`);
  try {
    match.value = await authApi.getMatchDetails(matchId);
  } catch (err: any) {
    appStore.notify('Lỗi tải ván cờ: ' + err.message, 'error');
  } finally {
    appStore.setLoading(false);
  }
}

onMounted(() => {
  loadMatch();
});

function handleSurrender() {
  if (confirm('Bạn có chắc chắn muốn đầu hàng và xử thua không?')) {
    appStore.notify('Bạn đã nhận thua ván đấu.', 'warning');
    router.push('/history');
  }
}

function handleOfferDraw() {
  appStore.notify('Đã gửi đề nghị cầu hòa tới đối thủ.', 'info');
}

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}
</script>

<template>
  <div class="match-page">
    <div v-if="match" class="match-layout">
      <!-- Cột Trái: Bàn Cờ Tướng -->
      <div class="board-column glass-panel">
        <!-- Đối Thủ (Phe Đen) -->
        <div class="player-bar bar-black">
          <div class="player-profile">
            <span class="avatar-badge">將</span>
            <div>
              <div class="name">{{ match.blackPlayer }} (Phe Đen)</div>
              <span class="badge badge-gray">Kỳ Thủ</span>
            </div>
          </div>
          <div class="clock-display" :class="{ active: match.currentTurn === 'Black' }">
            ⏱️ {{ formatTime(match.blackTimeSeconds) }}
          </div>
        </div>

        <!-- Khung Bàn Cờ Minh Họa -->
        <div class="xiangqi-board">
          <div class="board-surface">
            <!-- Sông Sở Hà - Hán Giới -->
            <div class="river">
              <span class="river-text title-serif">楚 河</span>
              <span class="river-divider">〜〜〜〜</span>
              <span class="river-text title-serif">漢 界</span>
            </div>

            <!-- Các quân cờ đại diện trực quan -->
            <div class="sample-pieces">
              <div class="piece p-black" style="top: 10%; left: 10%;">車</div>
              <div class="piece p-black" style="top: 10%; left: 30%;">馬</div>
              <div class="piece p-black" style="top: 10%; left: 50%;">將</div>
              <div class="piece p-black" style="top: 10%; left: 70%;">馬</div>
              <div class="piece p-black" style="top: 10%; left: 90%;">車</div>
              <div class="piece p-black" style="top: 25%; left: 20%;">砲</div>
              <div class="piece p-black" style="top: 25%; left: 80%;">砲</div>

              <div class="piece p-red" style="bottom: 25%; left: 20%;">炮</div>
              <div class="piece p-red" style="bottom: 25%; left: 80%;">炮</div>
              <div class="piece p-red" style="bottom: 10%; left: 10%;">俥</div>
              <div class="piece p-red" style="bottom: 10%; left: 30%;">傌</div>
              <div class="piece p-red" style="bottom: 10%; left: 50%;">帥</div>
              <div class="piece p-red" style="bottom: 10%; left: 70%;">傌</div>
              <div class="piece p-red" style="bottom: 10%; left: 90%;">俥</div>
            </div>
          </div>
        </div>

        <!-- Bản Thân (Phe Đỏ) -->
        <div class="player-bar bar-red">
          <div class="player-profile">
            <span class="avatar-badge red">帥</span>
            <div>
              <div class="name">{{ match.redPlayer }} (Phe Đỏ)</div>
              <span class="badge badge-red">Lượt của bạn</span>
            </div>
          </div>
          <div class="clock-display" :class="{ active: match.currentTurn === 'Red' }">
            ⏱️ {{ formatTime(match.redTimeSeconds) }}
          </div>
        </div>
      </div>

      <!-- Cột Phải: Lịch Sử Nước Đi & Điều Khiển -->
      <div class="control-column">
        <!-- Bảng Biên Bản Nước Đi -->
        <div class="moves-box glass-panel">
          <div class="box-header">
            <h3 class="title-serif">Biên Bản Nước Đi</h3>
            <span class="badge badge-gold">{{ match.moves.length }} Nước</span>
          </div>

          <div class="moves-list">
            <div
              v-for="m in match.moves"
              :key="m.order"
              class="move-row"
              :class="{ 'move-red': m.side === 'Red' }"
            >
              <span class="move-num">{{ m.order }}.</span>
              <span class="move-side-icon">{{ m.side === 'Red' ? '🔴' : '⚫' }}</span>
              <span class="move-notation">{{ m.notation }}</span>
            </div>
          </div>
        </div>

        <!-- Thao Tác Trận Đấu -->
        <div class="actions-box glass-panel">
          <button class="btn btn-secondary btn-block" @click="handleOfferDraw">
            🤝 Xin Hòa
          </button>
          <button class="btn btn-primary btn-block" @click="handleSurrender">
            🏳️ Đầu Hàng
          </button>
          <router-link to="/lobby" class="btn btn-secondary btn-block">
            🚪 Rời Trận
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.match-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px 20px 60px 20px;
}

.match-layout {
  display: grid;
  grid-template-columns: 1fr 340px;
  gap: 24px;
}

.board-column {
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.player-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 18px;
  border-radius: var(--radius-sm);
  background: rgba(15, 20, 28, 0.6);
  border: 1px solid var(--border-subtle);
}

.player-profile {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar-badge {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: #334155;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-serif);
  font-weight: 800;
  font-size: 1.2rem;
  border: 2px solid #64748b;
}

.avatar-badge.red {
  background: #991b1b;
  color: #fef08a;
  border-color: #f59e0b;
}

.name {
  font-weight: 600;
  font-size: 0.95rem;
}

.clock-display {
  font-family: monospace;
  font-size: 1.25rem;
  font-weight: 700;
  padding: 4px 12px;
  border-radius: 6px;
  background: rgba(0, 0, 0, 0.4);
  color: var(--text-muted);
}
.clock-display.active {
  color: #fbbf24;
  border: 1px solid rgba(245, 158, 11, 0.4);
  box-shadow: 0 0 10px rgba(245, 158, 11, 0.2);
}

.xiangqi-board {
  background: #2b1f1d;
  background: radial-gradient(circle, #3d2817 0%, #201309 100%);
  border: 3px solid #78350f;
  border-radius: var(--radius-sm);
  padding: 20px;
  box-shadow: inset 0 0 20px rgba(0, 0, 0, 0.8), 0 8px 30px rgba(0, 0, 0, 0.5);
}

.board-surface {
  aspect-ratio: 9 / 10;
  max-height: 520px;
  margin: 0 auto;
  border: 2px solid #b45309;
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.river {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: space-around;
  color: #fde68a;
  font-size: 1.2rem;
  letter-spacing: 0.2em;
  border-top: 1px solid #78350f;
  border-bottom: 1px solid #78350f;
  background: rgba(0, 0, 0, 0.15);
}

.piece {
  position: absolute;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-serif);
  font-weight: 800;
  font-size: 1.15rem;
  transform: translate(-50%, -50%);
  cursor: pointer;
  transition: transform 0.15s ease;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.6);
}
.piece:hover {
  transform: translate(-50%, -50%) scale(1.15);
}

.p-red {
  background: radial-gradient(circle, #b91c1c, #7f1d1d);
  color: #fef08a;
  border: 2px solid #f59e0b;
}

.p-black {
  background: radial-gradient(circle, #334155, #0f172a);
  color: #f1f5f9;
  border: 2px solid #94a3b8;
}

.control-column {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.moves-box {
  padding: 20px;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.box-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-subtle);
}

.moves-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow-y: auto;
  max-height: 400px;
}

.move-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.02);
  font-size: 0.9rem;
}

.move-num {
  font-family: monospace;
  color: var(--text-dim);
  width: 24px;
}

.move-notation {
  font-weight: 500;
}

.actions-box {
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.btn-block {
  width: 100%;
}
</style>
