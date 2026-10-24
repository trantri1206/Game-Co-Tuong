import * as signalR from '@microsoft/signalr';
import { apiConfig } from '@/api/config';
import { useAuthStore } from '@/stores/auth';

/**
 * Dịch Vụ Kết Nối SignalR Realtime (GCT-34):
 * - Đọc Access Token trực tiếp từ bộ nhớ RAM (In-Memory Auth Store).
 * - Tự động đính kèm token vào SignalR qua accessTokenFactory và query string.
 * - Đóng kết nối lập tức khi hết phiên hoặc khi người dùng đăng xuất.
 */
class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnecting = false;

  private getHubUrl(): string {
    const rootUrl = apiConfig.baseUrl.replace(/\/api$/, '');
    return `${rootUrl}/hubs/game`;
  }

  public async connect(): Promise<signalR.HubConnection | null> {
    const authStore = useAuthStore();
    if (!authStore.token) {
      console.warn('[SignalR] Không thể kết nối: Thiếu access token trong bộ nhớ.');
      return null;
    }

    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    if (this.isConnecting) return null;
    this.isConnecting = true;

    try {
      const hubUrl = this.getHubUrl();

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => {
            // Lấy token trực tiếp từ in-memory state
            return authStore.token || '';
          },
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      // Lắng nghe sự kiện ngắt kết nối
      this.connection.onclose((error) => {
        if (error) {
          console.warn('[SignalR] Kết nối đã đóng do lỗi:', error.message);
        }
      });

      await this.connection.start();
      return this.connection;
    } catch (err: any) {
      // Bỏ qua lỗi kết nối nhẹ nhàng khi backend SignalR chưa bật trong môi trường dev
      console.warn('[SignalR] Chưa thể thiết lập kết nối tới Hub:', err.message || err);
      return null;
    } finally {
      this.isConnecting = false;
    }
  }

  public async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        if (this.connection.state === signalR.HubConnectionState.Connected) {
          await this.connection.stop();
        }
      } catch (err) {
        console.warn('[SignalR] Lỗi khi đóng kết nối:', err);
      } finally {
        this.connection = null;
      }
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  public getConnection(): signalR.HubConnection | null {
    return this.connection;
  }
}

export const signalrService = new SignalRService();
