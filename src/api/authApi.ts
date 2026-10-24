import { apiClient } from './client';

export interface LoginPayload {
  usernameOrEmail: string;
  password: string;
}

export interface RegisterPayload {
  username: string;
  email: string;
  password: string;
  confirmPassword?: string;
  displayName?: string;
}

export interface RoomSummary {
  roomId: number;
  roomName: string;
  hostName: string;
  playerCount: number;
  spectatorCount: number;
  status: 'Waiting' | 'Playing' | 'Finished';
}

export interface MatchSummary {
  matchId: number;
  roomId: number;
  redPlayer: string;
  blackPlayer: string;
  status: string;
  result?: string;
  moveCount: number;
  createdAt: string;
}

export const authApi = {
  // 1. Đăng nhập
  async login(payload: LoginPayload) {
    const response = await apiClient.post('/auth/login', payload);
    return response.data;
  },

  // 2. Đăng ký
  async register(payload: RegisterPayload) {
    const response = await apiClient.post('/auth/register', payload);
    return response.data;
  },

  // 3. Lấy hồ sơ cá nhân
  async getProfile() {
    const response = await apiClient.get('/auth/profile');
    return response.data;
  },

  // 4. Lấy danh sách phòng ở Sảnh (Lobby)
  async getRooms(): Promise<RoomSummary[]> {
    try {
      const response = await apiClient.get('/rooms');
      return response.data;
    } catch {
      // Mock data dự phòng khi backend chưa mở cổng
      return [
        { roomId: 1, roomName: 'Bàn Cờ Hoàng Gia', hostName: 'Kỳ Thủ Đỏ Demo', playerCount: 2, spectatorCount: 5, status: 'Playing' },
        { roomId: 2, roomName: 'Giao Lưu Hữu Nghị', hostName: 'player_black_demo', playerCount: 1, spectatorCount: 0, status: 'Waiting' },
        { roomId: 3, roomName: 'Đấu Tập Cao Thủ', hostName: 'bot_easy_demo', playerCount: 2, spectatorCount: 1, status: 'Playing' },
        { roomId: 4, roomName: 'Thử Thách Kỳ Vương', hostName: 'master_xiangqi', playerCount: 1, spectatorCount: 2, status: 'Waiting' }
      ];
    }
  },

  // 5. Chi tiết phòng (Room)
  async getRoomDetails(roomId: number) {
    try {
      const response = await apiClient.get(`/rooms/${roomId}`);
      return response.data;
    } catch {
      return {
        roomId,
        roomName: `Phòng Thi Đấu #${roomId}`,
        redPlayer: { id: 1, name: 'Kỳ Thủ Đỏ Demo', ready: true },
        blackPlayer: { id: 2, name: 'Kỳ Thủ Đen Demo', ready: false },
        spectators: ['Khán Giả 1', 'Khán Giả 2'],
        status: 'Waiting'
      };
    }
  },

  // 6. Chi tiết trận đấu (Match)
  async getMatchDetails(matchId: number) {
    try {
      const response = await apiClient.get(`/matches/${matchId}`);
      return response.data;
    } catch {
      return {
        matchId,
        roomId: 1,
        redPlayer: 'Kỳ Thủ Đỏ Demo',
        blackPlayer: 'Kỳ Thủ Đen Demo',
        redTimeSeconds: 540,
        blackTimeSeconds: 580,
        currentTurn: 'Red',
        status: 'InProgress',
        moves: [
          { order: 1, side: 'Red', notation: 'Pháo 2 bình 5' },
          { order: 2, side: 'Black', notation: 'Mã 8 tiến 7' },
          { order: 3, side: 'Red', notation: 'Mã 2 tiến 3' },
          { order: 4, side: 'Black', notation: 'Xe 9 bình 8' }
        ]
      };
    }
  },

  // 7. Lịch sử đấu (History)
  async getMatchHistory(): Promise<MatchSummary[]> {
    try {
      const response = await apiClient.get('/matches/history');
      return response.data;
    } catch {
      return [
        { matchId: 101, roomId: 1, redPlayer: 'Kỳ Thủ Đỏ Demo', blackPlayer: 'bot_easy_demo', status: 'Finished', result: 'RedWin', moveCount: 42, createdAt: '2026-10-24 14:30' },
        { matchId: 102, roomId: 2, redPlayer: 'player_black_demo', blackPlayer: 'Kỳ Thủ Đỏ Demo', status: 'Finished', result: 'Draw', moveCount: 65, createdAt: '2026-10-24 15:15' },
        { matchId: 103, roomId: 3, redPlayer: 'master_xiangqi', blackPlayer: 'Kỳ Thủ Đỏ Demo', status: 'Finished', result: 'BlackWin', moveCount: 38, createdAt: '2026-10-24 16:00' }
      ];
    }
  },

  // 8. Đăng xuất và chính sách phiên
  async logout(confirmSurrender = false) {
    const response = await apiClient.post('/auth/logout', { confirmSurrender });
    return response.data;
  },

  // 9. Endpoint kiểm thử cơ chế Thử lại (Retry) & Lỗi (Error Handling)
  async testSimulateError(type: 'timeout' | '500' | 'network') {
    return apiClient.get(`/test/simulate-error?type=${type}`, {
      _retryCount: 0
    });
  }
};
