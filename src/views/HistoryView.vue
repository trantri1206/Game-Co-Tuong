<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { authApi, type MatchSummary } from '@/api/authApi';
import { useAppStore } from '@/stores/app';

const router = useRouter();
const appStore = useAppStore();

const history = ref<MatchSummary[]>([]);
const isLoading = ref(false);

async function loadHistory() {
  isLoading.value = true;
  appStore.setLoading(true, 'Đang tải lịch sử thi đấu...');
  try {
    history.value = await authApi.getMatchHistory();
  } catch (err: any) {
    appStore.notify('Lỗi tải lịch sử: ' + err.message, 'error');
  } finally {
    isLoading.value = false;
    appStore.setLoading(false);
  }
}

onMounted(() => {
  loadHistory();
});

function viewMatch(matchId: number) {
  router.push(`/match/${matchId}`);
}

function getResultBadge(result?: string) {
  if (result === 'RedWin') return { text: '🔴 Đỏ Thắng', class: 'badge-red' };
  if (result === 'BlackWin') return { text: '⚫ Đen Thắng', class: 'badge-gray' };
  return { text: '🤝 Hòa', class: 'badge-gold' };
}
</script>

<template>
  <div class="history-page">
    <div class="history-container">
      <!-- Header -->
      <div class="history-header glass-panel">
        <div>
          <h1 class="title-serif">LỊCH SỬ THI ĐẤU & KỲ PHỔ</h1>
          <p class="history-subtitle">Xem lại các ván đấu đã qua và nghiên cứu các nước đi then chốt</p>
        </div>
        <button class="btn btn-secondary" @click="loadHistory" :disabled="isLoading">
          🔄 Làm Mới
        </button>
      </div>

      <!-- Bảng Danh Sách Trận Đấu -->
      <div class="history-card glass-panel">
        <table class="history-table">
          <thead>
            <tr>
              <th>Mã Ván</th>
              <th>Tiên Cơ (Đỏ)</th>
              <th>Hậu Cơ (Đen)</th>
              <th>Số Nước Đi</th>
              <th>Kết Quả</th>
              <th>Thời Gian</th>
              <th style="text-align: right;">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in history" :key="item.matchId">
              <td class="match-code">#{{ item.matchId }}</td>
              <td><strong>{{ item.redPlayer }}</strong></td>
              <td><strong>{{ item.blackPlayer }}</strong></td>
              <td>{{ item.moveCount }} nước</td>
              <td>
                <span class="badge" :class="getResultBadge(item.result).class">
                  {{ getResultBadge(item.result).text }}
                </span>
              </td>
              <td class="time-col">{{ item.createdAt }}</td>
              <td style="text-align: right;">
                <button class="btn btn-secondary btn-sm" @click="viewMatch(item.matchId)">
                  👁️ Xem Lại
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.history-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px 20px 60px 20px;
}

.history-header {
  padding: 24px 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  border-left: 4px solid var(--gold-primary);
}

.history-header h1 {
  font-size: 1.7rem;
}

.history-subtitle {
  color: var(--text-muted);
  margin-top: 4px;
}

.history-card {
  padding: 16px;
  overflow-x: auto;
}

.history-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 0.95rem;
}

.history-table th {
  padding: 14px 16px;
  color: var(--text-muted);
  font-weight: 600;
  border-bottom: 1px solid var(--border-subtle);
  font-size: 0.85rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.history-table td {
  padding: 16px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
}

.history-table tr:hover td {
  background: rgba(255, 255, 255, 0.02);
}

.match-code {
  font-family: monospace;
  color: var(--gold-primary);
  font-weight: 600;
}

.time-col {
  color: var(--text-dim);
  font-size: 0.85rem;
}
</style>
