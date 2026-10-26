# Game-Co-Tuong: Quản Lý Cơ Sở Dữ Liệu Bằng EF Core

Dự án quản lý cơ sở dữ liệu hệ thống Cờ Tướng trực tuyến sử dụng **Entity Framework Core (Code-First Migration)** tương thích 100% với SQL Server.

---

## 1. Cấu Trúc Thực Thể & Ánh Xạ ERD

Hệ thống gồm 6 bảng thực thể chính với các cấu hình Fluent API chi tiết:

| Bảng | Mô tả | Khóa chính | Ràng buộc & Chỉ mục quan trọng |
| :--- | :--- | :--- | :--- |
| **`Users`** | Quản lý tài khoản người chơi | `UserId` (Identity) | **Unique Index** `UQ_Users_Username` (Chặn trùng username), **Unique Index** `UQ_Users_Email` (Chặn trùng email) |
| **`Rooms`** | Quản lý các phòng chơi | `RoomId` (Identity) | Mặc định `IsActive = 1`, `CreatedAt = getdate()` |
| **`RoomMembers`** | Thành viên trong phòng | `(RoomId, UserId)` | Composite PK, FK cascade tới `Rooms` và `Users` |
| **`Matches`** | Thông tin ván đấu | `MatchId` (Identity) | FK tới `Rooms` (On Delete Set Null), `Version = '1.0'` |
| **`MatchPlayers`** | Người chơi trong ván | `(MatchId, UserId)` | Composite PK, FK cascade tới `Matches`, FK tới `Users`, `Side` (Red / Black) |
| **`Moves`** | Lịch sử các nước đi | `MoveId` (Identity) | **Unique Index** `UQ_Moves_Order` `(MatchId, MoveOrder)` (Chặn trùng thứ tự nước đi trong cùng ván đấu) |

---

## 2. Yêu Cầu Môi Trường
* **.NET SDK** 10.0+ (hoặc .NET 8.0/9.0)
* **SQL Server** (SQL Server Express, Developer Edition hoặc LocalDB)
* Công cụ CLI: `dotnet-ef` (Cài đặt: `dotnet tool install --global dotnet-ef`)

---

## 3. Cấu Hình Chuỗi Kết Nối
Cấu hình chuỗi kết nối trong file [`appsettings.json`](file:///appsettings.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CoTuongDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
*(Hoặc dùng tài khoản SQL Server Authentication: `Server=localhost;Database=CoTuongDB;User Id=sa;Password=your_password;TrustServerCertificate=True;`)*

---

## 4. Hướng Dẫn Tạo Mới Hoặc Nâng Cấp Cơ Sở Dữ Liệu

### Cách 1: Sử dụng EF Core CLI (Khuyên dùng)
Áp dụng migration trực tiếp lên cơ sở dữ liệu trống hoặc nâng cấp cơ sở dữ liệu hiện có:
```bash
dotnet ef database update
```

### Cách 2: Chạy trực tiếp qua Code
```bash
dotnet run -- --migrate
```

### Cách 3: Thực thi bằng file SQL Script
Nếu máy chủ không có sẵn .NET CLI, có thể dùng file script SQL Migration độc lập (Idempotent Script):
* Đường dẫn: [`Migrations/InitialCreate.sql`](file:///Migrations/InitialCreate.sql)
* Chạy trực tiếp trong **SSMS (SQL Server Management Studio)** hoặc tiện ích dòng lệnh `sqlcmd`.

---

## 5. Nạp Dữ Liệu Demo (Seed Data)
Dữ liệu demo được tách biệt hoàn toàn tại [`Data/Seed/DbInitializer.cs`](file:///Data/Seed/DbInitializer.cs), sử dụng 100% tài khoản ảo (`player_red_demo`, `player_black_demo`, `bot_easy_demo`), mật khẩu băm mẫu và không chứa bất kỳ thông tin thật nào.

Để nạp dữ liệu mẫu:
```bash
dotnet run -- --seed
```

---

## 6. Tính Năng Đăng Ký Tài Khoản (GCT-27)

### Quy tắc kiểm tra tính hợp lệ (Validation):
* **Username**: Độ dài từ 3 đến 30 ký tự, chỉ chứa chữ cái, chữ số và dấu gạch dưới (`_`), không chứa khoảng trắng hay ký tự đặc biệt.
* **Email**: Không được để trống, đúng định dạng email chuẩn RFC (tối đa 100 ký tự).
* **Password**: Tối thiểu 8 ký tự, phải bao gồm ít nhất: 1 chữ cái in hoa, 1 chữ cái in thường, 1 chữ số và 1 ký tự đặc biệt.

### Chuẩn hóa & An toàn dữ liệu:
* **Chuẩn hóa hoa/thường (Case-Insensitive)**: Tên đăng nhập và email được `Trim()` và chuyển thành chữ thường khi lưu/so sánh, đảm bảo `User123` và `user123` bị chặn trùng lặp.
* **Băm mật khẩu**: Sử dụng thư viện chuẩn công nghiệp **`BCrypt.Net-Next`** với Salt ngẫu nhiên và WorkFactor = 12.
* **Bảo mật phản hồi**: Đối tượng phản hồi (`UserResponse`) tuyệt đối **không chứa mật khẩu hoặc mã băm**.
* **Chặn trùng khi gửi đồng thời (Concurrency)**: Sử dụng Unique Index ở Database (`UQ_Users_Username`, `UQ_Users_Email`) kết hợp xử lý `DbUpdateException` để đảm bảo khi nhiều request gửi đến cùng lúc (Race condition), chỉ có duy nhất 1 tài khoản được tạo.

---

## 7. Tính Năng Đăng Nhập & Cấp Phiên Truy Cập JWT (GCT-28)

### Cơ chế hoạt động:
* **Hỗ trợ linh hoạt**: Đăng nhập bằng **Tên đăng nhập** hoặc **Email**.
* **Xác thực an toàn**: Sử dụng `BCrypt.Verify` đối chiếu mật khẩu với mã băm trong Database.
* **Chống tấn công dò quét (User Enumeration)**: Dù sai tên đăng nhập hay sai mật khẩu, hệ thống đều trả về **một thông báo lỗi thống nhất duy nhất**: `"Tên đăng nhập hoặc mật khẩu không chính xác."`.
* **Cấp phát JWT (JSON Web Token)**:
  * **Payload (Claims)**: Chứa định danh `UserId`, `Username`, `Email`, `DisplayName`, và mã `jti`.
  * **Chữ ký số**: Thuật toán `HmacSha256` với khóa bí mật cấu hình trong `appsettings.json`.
  * **Thời hạn (Expiry)**: Thời gian hết hạn cụ thể (mặc định 60 phút).
* **Kiểm tra tính toàn vẹn (Token Validation)**:
  * Từ chối token nếu chữ ký không khớp hoặc token bị sửa đổi bất kỳ ký tự nào (Tampered token).
  * Từ chối token nếu đã quá hạn sử dụng (Lifetime verification, không có độ trễ clock skew).

## 8. Kiểm Tra Quyền Truy Cập API & Phòng (GCT-29)

### Cơ chế bảo mật và phân quyền:
* **Kiểm tra JWT trên API và SignalR**:
  * API kiểm tra token từ header `Authorization: Bearer <token>`.
  * SignalR kiểm tra token từ query parameter `?access_token=<token>` khi thiết lập kết nối WebSocket tới `/hubs/game`.
* **Trích xuất danh tính an toàn (Chống giả mạo - Impersonation Attack)**:
  * Tuyệt đối không tin tưởng `userId` gửi từ phía Client.
  * Danh tính bắt buộc phải được giải mã trực tiếp từ Claims của Token (`ClaimsPrincipal.GetUserId()`).
  * Mọi hành vi cố tình gửi `claimedUserId` khác với `UserId` trong Token đều bị chặn lập tức với mã `403 Forbidden`.
* **Phân quyền thao tác trong phòng cờ (`RoomAuthorizationService`)**:
  * **Thành viên phòng**: Người ngoài phòng bị chặn `403 Forbidden` đối với toàn bộ các thao tác: bắt đầu ván đấu, đi quân, đóng phòng hoặc gọi video.
  * **Đi quân cờ**: Chỉ có kỳ thủ (phe Đỏ / Đen) trong ván đấu mới được đi quân; khán giả xem cờ trong phòng bị từ chối đi quân.
  * **Gọi video (WebRTC Signaling)**: Chỉ thành viên có mặt trong phòng mới được phép gửi/nhận tín hiệu video.
* **Bảo mật nhật ký (Log Sanitization)**:
  * Tự động làm mờ toàn bộ token bí mật trong log: header `Bearer [REDACTED]` và query parameter `access_token=[REDACTED]`. Không bao giờ ghi token vào nhật ký hệ thống.

## 9. Lấy Hồ Sơ & Chính Sách Phiên / Đăng Xuất (GCT-30)

### Cơ chế hoạt động:
* **Lấy hồ sơ theo danh tính phiên**: Trích xuất `UserId` an toàn từ Token. Không bao giờ nhận `userId` từ Client để quyết định xem hồ sơ ai.
* **Chặn đổi hồ sơ bằng mã người dùng tùy ý**: Mọi yêu cầu cập nhật tên hiển thị (`DisplayName`) kèm mã người dùng khác (`ClaimedUserId != TokenUserId`) đều bị từ chối `403 Forbidden`.
* **Chính sách đầu hàng khi đang trong ván cờ (Surrender Policy)**:
  * Nếu người chơi đang trong ván cờ `InProgress` mà yêu cầu đăng xuất hoặc rời phòng:
    * Khi `ConfirmSurrender = false`: Hệ thống **chặn đăng xuất** và yêu cầu người chơi xác nhận đầu hàng.
    * **Hủy đầu hàng**: Ván đấu giữ nguyên trạng thái `InProgress`, người chơi tiếp tục trận đấu ("Hủy đầu hàng giữ trận").
    * **Xác nhận đầu hàng**: Xử thua người chơi, đối thủ thắng cuộc (`Result = OpponentWin`), kết thúc ván đấu, xóa người chơi khỏi phòng và điều hướng về trang đăng nhập ("Xác nhận đăng xuất đưa về đăng nhập đúng luồng").
* **Chính sách hiệu lực JWT**:
  * Ghi chú rõ: *"Do bản chất phi trạng thái (Stateless) của JWT, token cũ về mặt kỹ thuật vẫn còn hiệu lực cho đến thời điểm hết hạn (ExpiresAt) trừ khi được hệ thống thu hồi qua Blacklist. Phía Client bắt buộc phải xóa sạch token khỏi bộ nhớ (localStorage/cookies) khi đăng xuất."*
  * Hệ thống tích hợp sẵn `TokenBlacklistService` vô hiệu hóa tức thì token cũ ngay sau khi người chơi đăng xuất.

---

## 10. Kiểm Thử Hệ Thống

### A. Kiểm thử Hồ Sơ & Chính Sách Đăng Xuất (GCT-30)
```bash
dotnet run -- --test-session
# Hoặc nếu môi trường chặn thực thi file exe trực tiếp:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-session
```
Kịch bản kiểm thử tự động gồm 6 phần:
1. **Lấy hồ sơ cá nhân**: Trả đúng thông tin từ Token.
2. **Chặn đổi hồ sơ tùy ý**: Chặn `403 Forbidden` khi cố tình truyền mã người dùng khác.
3. **Yêu cầu xác nhận đầu hàng**: Đang chơi cờ mà đăng xuất $\rightarrow$ Yêu cầu xác nhận đầu hàng.
4. **Hủy đầu hàng giữ trận**: Hủy đầu hàng $\rightarrow$ Giữ nguyên trận đấu `InProgress`.
5. **Xác nhận đầu hàng & Đăng xuất**: Cập nhật xử thua, rời phòng, trả chỉ dẫn xóa token phía client (`DeleteClientToken = true`, `RedirectToLogin = true`).
6. **Thu hồi phiên & Chính sách JWT**: Ghi chú rõ thời hạn JWT và vô hiệu hóa token cũ qua Blacklist.

### B. Kiểm thử toàn diện Phân Quyền API & Phòng Cờ (GCT-29)
```bash
dotnet run -- --test-authz
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-authz
```

### C. Kiểm thử toàn diện Đăng Nhập & Cấp JWT (GCT-28)
```bash
dotnet run -- --test-login
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-login
```

### D. Kiểm thử toàn diện Đăng Ký Tài Khoản (GCT-27)
```bash
dotnet run -- --test-register
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-register
```

### E. Kiểm thử ràng buộc Unique Database (GCT-26)
```bash
dotnet run -- --test-constraints
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-constraints
```

---

## 11. Bộ Kiểm Thử Tự Động & Độc Lập Dữ Liệu (GCT-31)

Dự án cung cấp bộ kiểm thử tự động toàn diện đáp ứng trọn vẹn yêu cầu **GCT-31**:
* **Dữ liệu độc lập và lặp lại được (Idempotent / Isolated Data)**: Sử dụng In-Memory Database với database name ngẫu nhiên (`Guid.NewGuid()`) cho mỗi test runner và test case, đảm bảo các ca kiểm thử chạy song song hoặc chạy lặp lại vô hạn lần mà không bao giờ bị phụ thuộc trạng thái hay xung đột dữ liệu.
* **Bộ kiểm thử xUnit chuẩn công nghiệp**: Nằm trong project `tests/GameCoTuong.Tests/` bao gồm **32 unit test**:
  * **`RegisterTests.cs` (13 tests)**: Thử thông tin đúng, 10 trường hợp sai định dạng (tên ngắn, email sai cú pháp, thiếu ký tự hoa/thường/số/đặc biệt, mật khẩu không khớp), chặn trùng tên/email bất kể chữ hoa thường, và giả lập 2 yêu cầu đăng ký đồng thời (concurrency test).
  * **`LoginAndTokenTests.cs` (7 tests)**: Đăng nhập bằng username, email, mật khẩu sai, tài khoản không tồn tại (chống User Enumeration), kiểm tra chữ ký token bị sửa đổi (Tampered Token), token hết hạn (Expired Token) và token bị thu hồi (Revoked/Blacklisted Token).
  * **`AuthorizationTests.cs` (4 tests)**: Người ngoài phòng bị chặn mọi hành vi (403 Forbidden), khán giả trong phòng bị chặn đi quân, kỳ thủ hợp lệ được phép thao tác, và phát hiện chặn đứng hành vi giả mạo ID người dùng.
  * **`SecurityAndPrivacyTests.cs` (8 tests)**: Xác nhận đối tượng phản hồi (`UserResponse`, `LoginResponse`) tuyệt đối không chứa `password` hay `passwordHash`, kiểm tra bộ lọc nhật ký (`LogSanitizer`) che giấu hoàn toàn token bí mật (`[REDACTED]`), và kiểm thử logic đầu hàng khi đang chơi.

### Cách 1: Chạy toàn bộ 32 test bằng xUnit Runner
```bash
dotnet test GameCoTuong.slnx
```

### Cách 2: Chạy trực quan toàn bộ kịch bản kiểm thử qua CLI
```bash
dotnet run -- --test-all
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-all
```

---

## 12. Khung Giao Diện & Kết Nối API (GCT-32)

Dự án tích hợp khung giao diện **Vue 3 + Vite + TypeScript** hiện đại, kết nối tầng API backend với các đặc tính:

### A. Cấu trúc định tuyến (Vue Router)
* **`/login`** ([`LoginView.vue`](file:///src/views/LoginView.vue)): Màn hình đăng nhập & đăng ký tài khoản, hỗ trợ điền nhanh tài khoản demo.
* **`/lobby`** ([`LobbyView.vue`](file:///src/views/LobbyView.vue)): Sảnh cờ, danh sách phòng thi đấu, bộ lọc trạng thái, tạo phòng mới và các nút kiểm thử lỗi kết nối API.
* **`/room/:id`** ([`RoomView.vue`](file:///src/views/RoomView.vue)): Phòng cờ, vị trí Tiên cơ (Đỏ) và Hậu cơ (Đen), danh sách khán giả, trạng thái sẵn sàng và nút bắt đầu trận đấu.
* **`/match/:id`** ([`MatchView.vue`](file:///src/views/MatchView.vue)): Ván đấu cờ tướng trực tiếp, bàn cờ, đồng hồ đếm ngược hai bên, biên bản nước đi, xin hòa và đầu hàng.
* **`/history`** ([`HistoryView.vue`](file:///src/views/HistoryView.vue)): Lịch sử thi đấu, kết quả (Đỏ thắng / Đen thắng / Hòa), số nước đi và nút xem lại kỳ phổ.
* **Navigation Guard**: Tự động chặn và chuyển hướng về `/login` nếu chưa có phiên hợp lệ.

### B. Cơ chế API Client (Chống kẹt, Timeout & Tự động thử lại)
* **Cấu hình tập trung**: Đọc từ biến môi trường [`.env`](file:///src/api/config.ts) (`VITE_API_BASE_URL`, `VITE_API_TIMEOUT_MS`, `VITE_API_MAX_RETRIES`).
* **Header Interceptor**: Tự động trích xuất token từ [`useAuthStore`](file:///src/stores/auth.ts) và gắn `Authorization: Bearer <token>`.
* **Cơ chế Retry (Exponential Backoff)**: Tự động thử lại tối đa 3 lần khi gặp sự cố mạng hoặc lỗi máy chủ tạm thời (1s $\rightarrow$ 2s $\rightarrow$ 4s).
* **Nút Thử Lại Thủ Công**: Hiển thị thanh trạng thái kết nối [`ApiStatusBar.vue`](file:///src/components/ApiStatusBar.vue) với nút **"Thử lại ngay"** khi mất kết nối, đảm bảo ứng dụng không bao giờ bị treo hay mắc kẹt.

### C. Lệnh Build & Phát Triển Frontend
```bash
# Cài đặt thư viện
npm install

# Kiểm tra kiểu TypeScript & Build bundle
npm run build

# Khởi chạy máy chủ phát triển frontend
npm run dev
```

---

## 13. Quản Lý Phiên Trên Giao Diện (GCT-34)

Hệ thống quản lý phiên đăng nhập và bảo mật phiên theo chuẩn an toàn tuyệt đối:

### A. Quản lý Token In-Memory (Zero LocalStorage)
* **Tuyệt đối không lưu Token trong LocalStorage**: Toàn bộ Access Token JWT và User Profile chỉ được lưu giữ trong bộ nhớ tạm (RAM) thông qua Pinia Store [`useAuthStore`](file:///src/stores/auth.ts).
* **Bảo mật khi tải lại trang**: Khi người dùng tải lại trang (F5/Reload) hoặc đóng tab, bộ nhớ RAM được reset về `null`, hệ thống lập tức yêu cầu đăng nhập lại để đảm bảo an toàn, không lưu vết token trên ổ đĩa máy khách.
* **Xóa sạch dấu vết cũ**: Tự động dọn dẹp các khóa token cũ trong storage nếu có.

### B. Gắn Token Vào API & Tích Hợp SignalR Realtime
* **REST API**: Header Interceptor trong [`src/api/client.ts`](file:///src/api/client.ts) đọc token từ RAM và tự động gắn `Authorization: Bearer <token>`.
* **SignalR Realtime**: Module [`src/services/signalrService.ts`](file:///src/services/signalrService.ts) kết nối tới `/hubs/game` thông qua thư viện `@microsoft/signalr`, cung cấp token qua `accessTokenFactory` và query string `?access_token=<token>`.
* **Đóng kết nối khi kết thúc phiên**: Khi người dùng bấm đăng xuất hoặc phiên hết hạn (401), hệ thống lập tức ngắt kết nối WebSocket/SignalR (`connection.stop()`) để giải phóng tài nguyên.

### C. Bảo Vệ Màn Hình Riêng & Chống Lặp Chuyển Trang (Navigation Guards)
* **Bảo vệ màn hình**: Mọi route có `meta.requiresAuth` ([`/lobby`](file:///src/views/LobbyView.vue), [`/room/:id`](file:///src/views/RoomView.vue), [`/match/:id`](file:///src/views/MatchView.vue), [`/history`](file:///src/views/HistoryView.vue)) đều được kiểm tra trước khi chuyển trang.
* **Chuyển hướng chính xác**: Nếu chưa đăng nhập, router lưu đường dẫn đích vào tham số query `?redirect=${encodeURIComponent(to.fullPath)}` và đưa về `/login`. Sau khi đăng nhập thành công, người dùng được đưa về đúng màn hình đã yêu cầu.
* **Chống lặp vô tận (No Infinite Redirect Loop)**: Kiểm tra điều kiện `to.path !== '/login'` trước khi chuyển hướng để ngăn chặn hoàn toàn lỗi lặp chuyển trang.

---

## 14. Mô Hình Bàn Cờ & Quân Cờ Cờ Tướng (GCT-42)

Mô hình trạng thái và dữ liệu cờ tướng được thiết kế theo kiến trúc **Pure Domain**: tách biệt 100% khỏi cơ sở dữ liệu (EF Core) và kết nối mạng (SignalR / WebSockets).

### A. Quy Chuẩn Hệ Tọa Độ & Bàn Cờ 90 Giao Điểm
* **Hệ trục tọa độ**:
  * `Col` (Cột): `0` đến `8` (9 cột ngang).
  * `Row` (Hàng): `0` đến `9` (10 hàng dọc).
  * Tổng cộng: $9 \times 10 = 90$ giao điểm ([`Engine/Position.cs`](file:///Engine/Position.cs)).
* **Quy ước vị trí hai phe**:
  * **Phe Đen (Black)**: Nằm ở nửa trên bàn cờ (`Row` từ `0` đến `4`). Hàng đáy là `Row 0`. Cung tướng Đen gồm các ô `Col` $3..5$ và `Row` $0..2$.
  * **Sông (River)**: Ranh giới tự nhiên ngăn cách giữa `Row 4` và `Row 5`.
  * **Phe Đỏ (Red)**: Nằm ở nửa dưới bàn cờ (`Row` từ `5` đến `9`). Hàng đáy là `Row 9`. Cung tướng Đỏ gồm các ô `Col` $3..5$ và `Row` $7..9$.
* **Chặn tọa độ ngoài bàn**:
  * Mọi vị trí ngoài phạm vi $0 \le Col \le 8$ hoặc $0 \le Row \le 9$ đều bị chặn lập tức và ném ngoại lệ `ArgumentOutOfRangeException`.

### B. Biểu Diễn 7 Loại Quân Cờ ([`Engine/Piece.cs`](file:///Engine/Piece.cs))
* **Bảy loại quân cờ**:
  1. `General`: Tướng / Soái (帅 / 将)
  2. `Advisor`: Sĩ (仕 / 士)
  3. `Elephant`: Tượng (相 / 象)
  4. `Horse`: Mã (傌 / 馬)
  5. `Chariot`: Xe (俥 / 車)
  6. `Cannon`: Pháo (炮 / 砲)
  7. `Soldier`: Binh / Tốt (兵 / 卒)
* **Ký tự FEN quốc tế**:
  * Phe Đỏ (viết hoa): `R, N, B, A, K, C, P`
  * Phe Đen (viết thường): `r, n, b, a, k, c, p`

### C. Quản Lý Trạng Thái & Phiên Bản Bàn Cờ ([`Engine/ChessBoard.cs`](file:///Engine/ChessBoard.cs))
* **Bàn cờ ban đầu chuẩn (32 quân cờ)**: Đen ở trên, Đỏ ở dưới, đầy đủ 32 quân tại các tọa độ tiêu chuẩn.
* **Bên tới lượt (`ActiveColor`)**: Mặc định Đỏ đi trước (`PieceColor.Red`), đổi lượt qua hàm `SwitchTurn()`.
* **Phiên bản trạng thái (`StateVersion`)**: Tự động tăng sau mỗi biến đổi trạng thái để phục vụ đồng bộ dữ liệu.
* **Tuần tự hóa FEN**:
  * Chuỗi FEN ban đầu: `rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1`
  * Chuyển đổi và khôi phục 100% qua `ToFen()` và `FromFen()`.

### D. Kiểm Thử Độc Lập
```bash
# Kiểm thử trực quan mô hình cờ tướng qua CLI
dotnet run -- --test-engine

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 15. Khởi Tạo Bàn Cờ & Mô Phỏng Nước Đi (GCT-43 & GCT-106)

Được thiết kế nhằm chuẩn bị trạng thái ban đầu của bàn cờ và hỗ trợ thử nước, hoàn tác, mô phỏng tính toán chiến thuật (cho AI và thẩm định nước đi) mà **tuyệt đối không làm thay đổi trạng thái trận đấu thật đang diễn ra**.

### A. Khởi Tạo Chuẩn Đầy Đủ 32 Quân Cờ & 58 Ô Trống ([`Engine/ChessBoard.cs`](file:///Engine/ChessBoard.cs))
* **Bố trí 32 quân**:
  * **Phe Đen (Black - Hàng 0–4)**: Hàng 0 (Xe, Mã, Tượng, Sĩ, Tướng tại (4,0), Sĩ, Tượng, Mã, Xe); Hàng 2 (Pháo tại (1,2) và (7,2)); Hàng 3 (5 Tốt tại các cột chẵn 0, 2, 4, 6, 8).
  * **Phe Đỏ (Red - Hàng 5–9)**: Hàng 9 (Xe, Mã, Tượng, Sĩ, Soái tại (4,9), Sĩ, Tượng, Mã, Xe); Hàng 7 (Pháo tại (1,7) và (7,7)); Hàng 6 (5 Binh tại các cột chẵn 0, 2, 4, 6, 8).
  * **58 giao điểm trống**: Đảm bảo $32 + 58 = 90$ giao điểm chuẩn xác.
* **Quy chuẩn ban đầu**: Đỏ đi trước (`ActiveColor = Red`), `HalfMoveClock = 0`, `FullMoveNumber = 1`, `StateVersion = 1`.

### B. Áp Dụng Nước Thử (`MakeMove`) & Bản Ghi Hoàn Tác ([`Engine/UndoMoveRecord.cs`](file:///Engine/UndoMoveRecord.cs))
* **Thực hiện nước đi**:
  * Kiểm tra tính hợp lệ cơ bản: ô xuất phát có quân, quân thuộc phe tới lượt, không ăn quân đồng minh, ô xuất phát khác ô đích (`MoveAction`).
  * Di chuyển quân đến ô đích, làm trống ô xuất phát.
  * Nếu ăn quân: lưu lại `CapturedPiece` và reset `HalfMoveClock = 0`.
  * Nếu đi Binh/Tốt: reset `HalfMoveClock = 0`.
  * Đổi lượt đi: Đỏ $\leftrightarrow$ Đen. Tăng `FullMoveNumber` khi Đen hoàn thành lượt.
  * Tăng `StateVersion` và đẩy `UndoMoveRecord` vào ngăn xếp `_undoStack`.

### C. Hoàn Tác Nước Đi (`UndoMove`) Khôi Phục Chính Xác 100%
* Lấy bản ghi `UndoMoveRecord` gần nhất từ ngăn xếp.
* Đưa quân đã di chuyển (`MovedPiece`) trở lại ô xuất phát (`From`).
* Khôi phục quân bị ăn (`CapturedPiece`) trở lại ô đích (`To`), hồi sinh nguyên vẹn số lượng quân cờ trên bàn.
* Phục hồi nguyên bản các biến đếm: `ActiveColor`, `HalfMoveClock`, `FullMoveNumber`, `StateVersion`.
* Chuỗi nhiều nước đi được hoàn tác ngược thứ tự sẽ đưa chuỗi FEN của bàn cờ về trùng khớp 100% với thế cờ ban đầu.

### D. Mô Phỏng An Toàn Trong Sandbox (`Simulate` & `Clone`) & Cây Nước Đi (Minimax Tree)
* **Hàm `Simulate(from, to, action)`**:
  * Tự động áp dụng nước đi thử trong khối `try`, thực thi khối lệnh tính toán hoặc đánh giá thế cờ, và luôn đảm bảo gọi `UndoMove` trong khối `finally`.
  * Đảm bảo an toàn tuyệt đối kể cả khi logic bên trong phát sinh ngoại lệ: bàn cờ thật của trận đấu không bị ảnh hưởng.
* **Duyệt nhiều nhánh (Tree Branching)**: Thử nghiệm nhiều nhánh ứng viên từ một thế cờ mà không gây rò rỉ trạng thái.
* **Mô phỏng sâu nhiều tầng (Deep Nested Simulation)**: Thử nghiệm cây tìm kiếm minimax sâu 3 tầng, tự động unwind sạch sẽ về trạng thái gốc không làm mất bất kỳ quân nào.
* **Deep Copy (`Clone`)**: Tạo bản sao sâu độc lập của bàn cờ để cho máy (AI) hoặc các luồng phân tích chạy thử hàng triệu nước đi mà không chạm vào trận đấu chính.

### E. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chức năng mô phỏng và hoàn tác nước đi cờ tướng
dotnet run -- --test-simulation

# Kiểm thử toàn diện tất cả các phân hệ từ xác thực tới cờ tướng
dotnet run -- --test-all
```

---

## 16. Luật Di Chuyển & Ăn Quân: Quân Tốt / Binh (GCT-50)

Quy tắc di chuyển và ăn quân của quân Tốt (卒 - Black Soldier) và Binh (兵 - Red Soldier) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/SoldierRule.cs`](file:///Engine/Rules/SoldierRule.cs) và [`src/engine/rules/soldier.ts`](file:///src/engine/rules/soldier.ts).

### A. Quy Tắc Trước & Sau Khi Qua Sông (River Crossing Rule)
* **Xác định ranh giới sông**:
  * **Binh Đỏ**: Nửa dưới bàn cờ (`Row 5–9`). **Chưa qua sông** khi $Row \ge 5$; **Đã qua sông** khi $Row \le 4$.
  * **Tốt Đen**: Nửa trên bàn cờ (`Row 0–4`). **Chưa qua sông** khi $Row \le 4$; **Đã qua sông** khi $Row \ge 5$.
* **Quy tắc di chuyển**:
  * **Trước khi qua sông**: Chỉ được phép **tiến thẳng 1 ô** ($Col$ giữ nguyên, $dRow = -1$ với Đỏ, $dRow = +1$ với Đen). Tuyệt đối cấm đi ngang và cấm đi lùi.
  * **Sau khi qua sông**: Được phép **tiến thẳng 1 ô** HOẶC **đi ngang 1 ô** sang trái/phải ($|dCol| = 1 \land dRow = 0$). Tuyệt đối cấm đi lùi.

### B. Hàng Đáy Đối Phương & Không Phong Cấp (No Promotion)
* Khi quân Tốt/Binh tiến tới hàng đáy cuối cùng của đối phương (Binh Đỏ tại `Row 0`, Tốt Đen tại `Row 9`):
  * Không thể tiến thêm do giới hạn biên của bàn cờ ($Row < 0$ hoặc $Row > 9$).
  * Tuyệt đối không được đi lùi.
  * **Vẫn được phép đi ngang** sang trái hoặc sang phải 1 bước nếu ô đích hợp lệ (nằm trong bàn cờ và không có quân cùng phe).
  * **Không phong cấp**: Khác với Cờ Vua, quân Tốt trong Cờ Tướng khi xuống hàng đáy đối phương không biến thành Xe/Pháo/Mã, mà vẫn giữ nguyên là Tốt.

### C. Ăn Quân & Chặn Các Hành Vi Vi Phạm
* **Quy tắc ăn quân**: Cờ Tướng đi thế nào thì ăn thế ấy. Khi ô đích hợp lệ có quân đối phương, Tốt di chuyển tới và ăn quân đó.
* **Chặn ăn quân đồng minh**: Tuyệt đối không được đi vào ô đã có quân cùng phe.
* **Chặn đi chéo & đi quá 1 bước**: Tổng khoảng cách Manhattan $|dCol| + |dRow|$ luôn bắt buộc phải bằng đúng 1.
* **Chặn đi ra ngoài bàn cờ**: Chặn mọi nước đi vượt ra ngoài phạm vi 9 cột và 10 hàng.

### D. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Tốt / Binh qua CLI
dotnet run -- --test-soldier

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 17. Luật Di Chuyển & Ăn Quân: Quân Xe (Chariot / Rook) (GCT-51)

Quy tắc di chuyển và ăn quân của quân Xe (俥 / 車 - Chariot / Rook) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/ChariotRule.cs`](file:///Engine/Rules/ChariotRule.cs) và [`src/engine/rules/chariot.ts`](file:///src/engine/rules/chariot.ts).

### A. Quy Tắc Di Chuyển Ngang & Dọc Trên Đường Trống
* **Hướng đi**: Chỉ đi thẳng theo hàng ngang (`from.Row == to.Row`) hoặc cột dọc (`from.Col == to.Col`).
* **Cự ly di chuyển**: Không giới hạn số bước đi (tối đa 8 ô ngang hoặc 9 ô dọc), qua sông tự do.
* **Chặn đi chéo và đứng yên**: Tuyệt đối chặn đi chéo ($from.Col \neq to.Col \land from.Row \neq to.Row$) và đứng yên tại chỗ ($from == to$).

### B. Chặn Nhảy Qua Quân Cản (Không Xuyên Thấu)
* Toàn bộ các giao điểm nằm **ở giữa** ô xuất phát và ô đích bắt buộc phải là ô trống (`CountPiecesBetween == 0`).
* Nếu có bất kỳ quân cờ nào (cả quân mình lẫn quân đối phương) nằm giữa đường đi $\implies$ Bị chặn lập tức, Xe tuyệt đối không được nhảy qua hoặc đi xuyên thấu.

### C. Ăn Quân Đối Phương & Chặn Ăn Đồng Minh
* **Ăn quân đối phương**: Xe đi tới ô có quân đối phương (sau đoạn đường trống) thì ăn quân đó. Quân đối phương chính là điểm dừng của nước đi.
* **Chặn ăn quân đồng minh**: Nếu ô đích có quân cùng màu, nước đi bị chặn.

### D. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Xe qua CLI
dotnet run -- --test-rook

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 18. Luật Di Chuyển & Cản Chân: Quân Mã (Horse / Knight) (GCT-52)

Quy tắc di chuyển và cản chân của quân Mã (傌 / 馬 - Horse / Knight) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/HorseRule.cs`](file:///Engine/Rules/HorseRule.cs) và [`src/engine/rules/horse.ts`](file:///src/engine/rules/horse.ts).

### A. Quy Tắc Di Chuyển Hình Chữ L (8 Hướng Đi)
* **Hình dạng nước đi**: Mã di chuyển theo hình chữ L gồm **2 giao điểm theo một trục** (dọc hoặc ngang) và **1 giao điểm theo trục còn lại**.
* **8 hướng đi tiềm năng**:
  * Nhảy lên trên ($dRow = -2$): Ô đích $(Col \pm 1, Row - 2)$.
  * Nhảy xuống dưới ($dRow = +2$): Ô đích $(Col \pm 1, Row + 2)$.
  * Nhảy sang trái ($dCol = -2$): Ô đích $(Col - 2, Row \pm 1)$.
  * Nhảy sang phải ($dCol = +2$): Ô đích $(Col + 2, Row \pm 1)$.
* **Phạm vi hoạt động**: Mã không bị giới hạn qua sông hay cung tướng (đi tự do khắp 90 giao điểm của bàn cờ).

### B. Quy Tắc Cản Chân Mã (Bán Mã Cước / Hobbling Rule)
* **Xác định vị trí ô chân cản (Obstacle Square)**:
  * Khi nhảy 2 dọc, 1 ngang ($|dRow| = 2 \land |dCol| = 1$): Chân cản nằm ở ô liền kề theo trục dọc: $(from.Col, from.Row + dRow / 2)$.
  * Khi nhảy 2 ngang, 1 dọc ($|dCol| = 2 \land |dRow| = 1$): Chân cản nằm ở ô liền kề theo trục ngang: $(from.Col + dCol / 2, from.Row)$.
* **Cơ chế cản**:
  * Nếu ô chân cản có bất kỳ quân cờ nào (quân mình hoặc quân đối phương), hướng đi đó bị cản chân hoàn toàn (`IsHobbled == true`). Cả 2 hướng nhảy qua phía chân đó đều không thể di chuyển.
  * Khi bị vây hãm cả 4 chân, quân Mã có đúng 0 nước đi hợp lệ.

### C. Ăn Quân Đối Phương & Chặn Nước Đi Sai
* **Ăn quân đối phương**: Khi ô đích hợp lệ có quân đối phương, Mã nhảy tới và ăn quân đó.
* **Chặn ăn quân đồng minh**: Nếu ô đích có quân cùng màu, nước đi bị chặn.
* **Chặn các hình dạng di chuyển sai**: Chặn đứng yên tại chỗ, đi thẳng/ngang (như Xe/Tốt), đi chéo (như Sĩ/Tượng).
* **Lọc bỏ nước đi ngoài biên**: Các nước nhảy vượt ra ngoài phạm vi 9 cột và 10 hàng đều bị loại bỏ tự động.

### D. Thế Cờ Ban Đầu Tiêu Chuẩn
* Tại vị trí ban đầu của ván đấu (Mã Đỏ tại `(1, 9)` và `(7, 9)`; Mã Đen tại `(1, 0)` và `(7, 0)`):
  * Chân hướng biên/đáy nằm ngoài bàn cờ hoặc bị cản.
  * Chân hướng ngang bị cản bởi Xe và Tượng đồng minh.
  * Chân hướng tiến vào trung tâm hoàn toàn trống $\implies$ Mỗi quân Mã ban đầu có đúng **2 nước tiến hợp lệ** (ví dụ Mã 2 tiến 3 hoặc Mã 2 tiến 1).

### E. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Mã qua CLI
dotnet run -- --test-horse

# Chạy kiểm thử tự động xUnit
dotnet test tests/GameCoTuong.Tests/GameCoTuong.Tests.csproj

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 19. Luật Di Chuyển & Ăn Quân: Quân Pháo (Cannon) (GCT-53)

Quy tắc di chuyển và ăn quân của quân Pháo (砲 / 炮 - Cannon) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/CannonRule.cs`](file:///Engine/Rules/CannonRule.cs) và [`src/engine/rules/cannon.ts`](file:///src/engine/rules/cannon.ts).

### A. Cơ Chế Di Chuyển Khi Không Ăn Quân (Move to Empty Square)
* **Hướng đi**: Đi theo hàng ngang (`from.Row == to.Row`) hoặc cột dọc (`from.Col == to.Col`), không giới hạn số ô di chuyển.
* **Đoạn đường giữa**: Toàn bộ các giao điểm nằm giữa ô xuất phát và ô đích bắt buộc phải là ô trống (`CountPiecesBetween == 0`).
* **Chặn nhảy qua ngòi tới ô trống**: Pháo tuyệt đối không được nhảy qua bất kỳ quân cờ nào để rơi vào một ô trống (chặn hành vi *"xuyên ngòi tới ô trống"*).

### B. Cơ Chế Ăn Quân Qua Ngòi (Capture with Platform)
* **Quy tắc ăn quân**: Pháo chỉ được phép ăn quân đối phương khi trên đường đi giữa ô xuất phát và ô đích có **ĐÚNG 1 quân cờ làm ngòi** (`CountPiecesBetween == 1`).
* **Tính chất của quân ngòi (pháo giá)**:
  * Quân làm ngòi có thể là **quân ta hoặc quân đối phương**, thuộc bất kỳ loại quân nào.
  * Bản thân ô ngòi không phải là điểm đến của nước đi.
* **Chặn khi không có ngòi (0 ngòi)**: Khi đoạn giữa hoàn toàn trống (`CountPiecesBetween == 0`), Pháo không thể ăn quân (không được ăn quân như Xe).
* **Chặn khi có từ 2 ngòi trở lên ($\ge 2$ ngòi)**: Pháo tuyệt đối không được nhảy qua 2 quân trở lên để ăn quân thứ 3.

### C. Chặn Các Nước Đi Sai Quy Tắc
* **Chặn ăn quân đồng minh**: Dù có ngòi hay không, ô đích có quân cùng màu đều bị chặn.
* **Chặn đi chéo và đứng yên**: Tuyệt đối từ chối các nước đi chéo hoặc đứng yên tại chỗ.
* **Chặn đi ra ngoài bàn cờ**: Lọc sạch mọi tọa độ vượt ra ngoài phạm vi 9 cột và 10 hàng.

### D. Thế Cờ Ban Đầu Tiêu Chuẩn
* Tại vị trí ban đầu (Pháo Đỏ tại `(1, 7)` và `(7, 7)`; Pháo Đen tại `(1, 2)` và `(7, 2)`):
  * **Nước đi ngang**: Hàng 7 trống từ cột 2 đến cột 6 $\implies$ Nước mở màn kinh điển **Pháo 2 bình 5** (Pháo đầu `(1, 7) -> (4, 7)`).
  * **Nước đi dọc**: Cột 1 trước mặt trống từ hàng 6 đến hàng 3 $\implies$ Pháo có thể tiến tự do vào các ô trống này.
  * **Ăn quân qua ngòi**: Pháo Đỏ `(1, 7)` có đúng 1 ngòi là Pháo Đen `(1, 2)` trên cột 1 $\implies$ Ăn được Mã Đen tại `(1, 0)` (*Pháo ăn Mã* khai cuộc)!

### E. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Pháo qua CLI
dotnet run -- --test-cannon

# Chạy kiểm thử tự động xUnit
dotnet test tests/GameCoTuong.Tests/GameCoTuong.Tests.csproj

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 20. Luật Di Chuyển & Cản Mắt: Quân Tượng / Tịnh (Elephant) (GCT-54)

Quy tắc di chuyển, kiểm tra mắt Tượng và giới hạn qua sông của quân Tượng (相 / 象 - Elephant / Minister) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/ElephantRule.cs`](file:///Engine/Rules/ElephantRule.cs) và [`src/engine/rules/elephant.ts`](file:///src/engine/rules/elephant.ts).

### A. Quy Tắc Di Chuyển Chéo 2 Ô (Hình Chữ Điền 田)
* **Hình dạng nước đi**: Tượng di chuyển theo đường chéo đúng **2 giao điểm** ở cả hai trục:
  $$|dCol| == 2 \land |dRow| == 2$$
* **4 hướng chéo tiềm năng**:
  * Lên - Trái: $(dCol = -2, dRow = -2)$
  * Lên - Phải: $(dCol = +2, dRow = -2)$
  * Xuống - Trái: $(dCol = -2, dRow = +2)$
  * Xuống - Phải: $(dCol = +2, dRow = +2)$
* **Phạm vi 7 giao điểm cố định**: Do quy tắc đi chéo 2 ô và không qua sông, mỗi bên Tượng chỉ hoạt động trên đúng 7 giao điểm trên sân nhà.

### B. Quy Tắc Chẹn Mắt Tượng (塞象眼 - Elephant's Eye Obstacle)
* **Xác định Mắt Tượng**: Giao điểm chính giữa đường chéo nối ô xuất phát và ô đích:
  $$eyeCol = from.Col + \frac{dCol}{2}, \quad eyeRow = from.Row + \frac{dRow}{2}$$
* **Cơ chế chẹn mắt**:
  * Ô mắt Tượng bắt buộc phải hoàn toàn trống (`board.GetPiece(eye) == null`).
  * Nếu có bất kỳ quân cờ nào (quân mình hoặc quân đối phương) đứng tại mắt Tượng $\implies$ Nước đi bị chẹn hoàn toàn (`IsEyeBlocked == true`), Tượng tuyệt đối không được nhảy qua.
  * Khi bị chẹn cả 4 mắt Tượng xung quanh, Tượng có đúng 0 nước đi hợp lệ.

### C. Quy Tắc Không Qua Sông (Tượng Bất Quá Hà)
* **Tượng Đỏ (相)**: Chỉ hoạt động ở nửa dưới bàn cờ ($Row \ge 5$). Tuyệt đối cấm nhảy sang $Row \le 4$ kể cả khi mắt Tượng và đường đi hoàn toàn trống.
* **Tượng Đen (象)**: Chỉ hoạt động ở nửa trên bàn cờ ($Row \le 4$). Tuyệt đối cấm nhảy sang $Row \ge 5$ kể cả khi mắt Tượng và đường đi hoàn toàn trống.

### D. Ăn Quân Đối Phương & Chặn Nước Đi Sai
* **Ăn quân đối phương**: Khi ô đích hợp lệ có quân đối phương (mắt Tượng trống, không qua sông), Tượng nhảy tới và ăn quân đó.
* **Chặn ăn quân đồng minh**: Tuyệt đối không được nhảy vào ô có quân cùng phe.
* **Chặn các hình dạng di chuyển sai**: Chặn đi chéo 1 ô (như Sĩ), đi thẳng/ngang (như Xe), chữ L (như Mã), đứng yên tại chỗ hoặc vượt ra ngoài biên bàn cờ.

### E. Thế Cờ Ban Đầu Tiêu Chuẩn
* Tại vị trí ban đầu của ván đấu (Tượng Đỏ tại `(2, 9)` và `(6, 9)`; Tượng Đen tại `(2, 0)` và `(6, 0)`):
  * Hướng xuống đáy nằm ngoài bàn cờ.
  * Hướng tiến vào tâm `(4, 7)` và tiến ra biên `(0, 7)` / `(8, 7)` đều có mắt Tượng trống $\implies$ Mỗi quân Tượng ban đầu có đúng **2 nước tiến hợp lệ** (ví dụ Tượng 3 tiến 5 hoặc Tượng 7 tiến 5).

### F. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Tượng qua CLI
dotnet run -- --test-elephant

# Chạy kiểm thử tự động xUnit
dotnet test tests/GameCoTuong.Tests/GameCoTuong.Tests.csproj

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 21. Luật Di Chuyển & Giới Hạn Cung: Quân Sĩ / Sĩ Quan (Advisor) (GCT-55)

Quy tắc di chuyển và phạm vi hoạt động trong Cửu cung của quân Sĩ (仕 / 士 - Advisor / Guard) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/AdvisorRule.cs`](file:///Engine/Rules/AdvisorRule.cs) và [`src/engine/rules/advisor.ts`](file:///src/engine/rules/advisor.ts).

### A. Quy Tắc Di Chuyển Chéo 1 Giao Điểm
* **Hình dạng nước đi**: Sĩ di chuyển theo đường chéo đúng **1 giao điểm** mỗi nước đi:
  $$|dCol| == 1 \land |dRow| == 1$$
* **4 hướng chéo tiềm năng**:
  * Lên - Trái: $(dCol = -1, dRow = -1)$
  * Lên - Phải: $(dCol = +1, dRow = -1)$
  * Xuống - Trái: $(dCol = -1, dRow = +1)$
  * Xuống - Phải: $(dCol = +1, dRow = +1)$
* **Chặn các hình dạng di chuyển sai**: Tuyệt đối chặn đi ngang, đi dọc (như Xe/Tốt), đi chéo 2 ô (như Tượng), đi chữ L (như Mã) hoặc đứng yên tại chỗ.

### B. Giới Hạn Nghiêm Ngặt Trong Cửu Cung (Palace Constraint)
* **Phạm vi Cửu cung**:
  * Cột chung: $Col \in [3, 5]$ (cột 3, 4, 5).
  * Cung Đen: $Row \in [0, 2]$ (hàng 0, 1, 2).
  * Cung Đỏ: $Row \in [7, 9]$ (hàng 7, 8, 9).
* **5 vị trí hợp lệ trên sân nhà**:
  * **Tại 4 góc cung**: Sĩ chỉ có duy nhất **1 nước đi hợp lệ** vào tâm Cửu cung.
    * Đỏ: `(3, 9)`, `(5, 9)`, `(3, 7)`, `(5, 7)` $\longrightarrow$ đi vào `(4, 8)`.
    * Đen: `(3, 0)`, `(5, 0)`, `(3, 2)`, `(5, 2)` $\longrightarrow$ đi vào `(4, 1)`.
  * **Tại tâm cung**: Sĩ có tối đa **4 nước đi hợp lệ** tỏa ra 4 góc cung.
    * Đỏ: `(4, 8)` $\longrightarrow$ đi ra `(3, 7)`, `(5, 7)`, `(3, 9)`, `(5, 9)`.
    * Đen: `(4, 1)` $\longrightarrow$ đi ra `(3, 0)`, `(5, 0)`, `(3, 2)`, `(5, 2)`.
* **Từ chối mọi điểm ngoài cung**: Bất kỳ nước đi nào có điểm đến nằm ngoài Cửu cung bên mình (`!IsInPalace(color, to)`) đều bị từ chối tuyệt đối.

### C. Ăn Quân Đối Phương & Chặn Ăn Quân Đồng Minh
* **Ăn quân đối phương**: Khi ô đích hợp lệ trong Cửu cung có quân đối phương, Sĩ di chuyển tới và ăn quân đó.
* **Chặn ăn quân đồng minh**: Tuyệt đối không được di chuyển vào ô đã có quân cùng phe.

### D. Thế Cờ Ban Đầu Tiêu Chuẩn
* Tại vị trí ban đầu của ván đấu (Sĩ Đỏ tại `(3, 9)` và `(5, 9)`; Sĩ Đen tại `(3, 0)` và `(5, 0)`):
  * Mỗi quân Sĩ có đúng **1 nước đi hợp lệ duy nhất** tiến vào tâm Cửu cung (ví dụ Sĩ 4 tiến 5 hoặc Sĩ 6 tiến 5).

### E. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Sĩ qua CLI
dotnet run -- --test-advisor

# Chạy kiểm thử tự động xUnit
dotnet test tests/GameCoTuong.Tests/GameCoTuong.Tests.csproj

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```

---

## 22. Luật Di Chuyển, Lộ Mặt Tướng & An Toàn: Quân Tướng / Soái (General) (GCT-56)

Quy tắc di chuyển, bộ lọc nước đi an toàn và luật Lộ mặt Tướng (Flying Generals) của quân Tướng (帅 / 将 - General / King) được cài đặt độc lập theo chuẩn Cờ Tướng quốc tế tại [`Engine/Rules/GeneralRule.cs`](file:///Engine/Rules/GeneralRule.cs) và [`src/engine/rules/general.ts`](file:///src/engine/rules/general.ts).

### A. Quy Tắc Di Chuyển Ngang / Dọc 1 Giao Điểm
* **Hình dạng nước đi**: Tướng chỉ di chuyển theo hướng ngang hoặc dọc đúng **1 giao điểm** mỗi nước đi:
  $$(|dCol| == 1 \land dRow == 0) \lor (dCol == 0 \land |dRow| == 1)$$
* **4 hướng ngang/dọc tiềm năng**:
  * Tiến thẳng: $(dCol = 0, dRow = -1 \text{ với Đỏ}, +1 \text{ với Đen})$
  * Thoái thẳng: $(dCol = 0, dRow = +1 \text{ với Đỏ}, -1 \text{ với Đen})$
  * Sang trái: $(dCol = -1, dRow = 0)$
  * Sang phải: $(dCol = +1, dRow = 0)$
* **Chặn các hình dạng di chuyển sai**: Tuyệt đối chặn đi chéo (như Sĩ/Tượng), nhảy chữ L (như Mã), nhảy nhiều hơn 1 ô hoặc đứng yên tại chỗ.

### B. Giới Hạn Nghiêm Ngặt Trong Cửu Cung (Palace Constraint)
* **Phạm vi Cửu cung**:
  * Cột chung: $Col \in [3, 5]$ (cột 3, 4, 5).
  * Cung Đen: $Row \in [0, 2]$ (hàng 0, 1, 2).
  * Cung Đỏ: $Row \in [7, 9]$ (hàng 7, 8, 9).
* **Từ chối mọi điểm ngoài cung**: Mọi nước đi có điểm đến vượt ra ngoài Cửu cung bên mình đều bị từ chối tuyệt đối (`!IsInPalace(color, to)`).

### C. Quy Tắc Lộ Mặt Tướng (Flying Generals / Đối Diện Trực Tiếp)
* **Khái niệm**: Hai quân Tướng của hai bên tuyệt đối không được phép nhìn thẳng mặt nhau trên cùng một cột dọc mà không có bất kỳ quân cờ nào che chắn ở giữa.
* **Cơ chế kiểm tra**:
  * Giả lập nước đi của Tướng sang ô mới `to`.
  * Xác định cột của Tướng đối phương.
  * Nếu 2 Tướng ở cùng cột dọc: đếm số quân nằm giữa hai Tướng trên cột đó:
    $$\text{countPiecesBetween} == 0 \implies \text{Vi phạm luật Lộ mặt Tướng (Bị Cấm Tuyệt Đối)}.$$
  * Nếu có ít nhất 1 quân cờ bất kỳ (của ta hoặc đối thủ) chắn giữa hai Tướng $\implies$ Nước đi hợp lệ.

### D. Bộ Lọc An Toàn Điểm Đến (Safe Destination Filter / Anti-Suicide)
* **Khái niệm**: Quân Tướng là nguyên thủ tối cao; luật Cờ Tướng cấm Tướng tự ý đi vào bất kỳ ô cờ nào đang bị quân đối phương khống chế hoặc chiếu xạ (không được tự sát).
* **Cơ chế kiểm tra ô bị khống chế (`IsSquareAttacked`)**:
  * **Xe / Tướng địch**: Kiểm tra 4 hướng ngang/dọc xuất phát từ ô `to`; nếu gặp Xe địch (hoặc Tướng địch) trên đường thẳng trống $\implies$ Bị khống chế.
  * **Pháo địch**: Kiểm tra 4 hướng ngang/dọc; nếu xuất hiện đúng 1 quân làm ngòi và phía sau là Pháo địch $\implies$ Bị chiếu, cấm đi vào.
  * **Mã địch**: Quét 8 ô Mã tiềm năng có thể nhảy tới `to`; nếu Mã địch không bị cản chân $\implies$ Bị khống chế.
  * **Tốt địch**: Kiểm tra các ô Tốt địch có thể kiểm soát (tiến nếu chưa qua sông; tiến hoặc ngang nếu đã qua sông).
* **Ăn quân đối phương có bảo kê**:
  * Nếu quân địch tại `to` không có quân bảo kê $\implies$ Tướng được phép ăn quân đó.
  * Nếu quân địch tại `to` được quân địch khác bảo vệ (ví dụ Xe, Pháo, Mã hoặc Tốt bảo kê) $\implies$ Ô đó vẫn bị khống chế, Tướng bị cấm ăn quân (chống tự sát).

### E. Thế Cờ Ban Đầu Tiêu Chuẩn
* Tại vị trí ban đầu của ván đấu (Tướng Đỏ tại `(4, 9)`; Tướng Đen tại `(4, 0)`):
  * Hai bên mạn sườn `(3, 9)` và `(5, 9)` đã có Sĩ Đỏ che chắn.
  * Điểm đến phía trước `(4, 8)` là ô an toàn và được Binh tại `(4, 6)` chắn giữa, không làm lộ mặt Tướng Đen `(4, 0)`.
  * Tướng mỗi bên có đúng **1 nước đi tiến lên duy nhất** trong Cửu cung.

### F. Kiểm Thử CLI & xUnit
```bash
# Kiểm thử chuyên biệt luật quân Tướng qua CLI
dotnet run -- --test-general

# Chạy kiểm thử tự động xUnit
dotnet test tests/GameCoTuong.Tests/GameCoTuong.Tests.csproj

# Hoặc chạy kiểm thử toàn bộ hệ thống
dotnet run -- --test-all
```