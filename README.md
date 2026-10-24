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

---

## 9. Kiểm Thử Hệ Thống

### A. Kiểm thử toàn diện Phân Quyền API & Phòng Cờ (GCT-29)
```bash
dotnet run -- --test-authz
# Hoặc nếu môi trường chặn thực thi file exe trực tiếp:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-authz
```
Kịch bản kiểm thử tự động gồm 6 phần:
1. **Chặn thiếu hoặc sai token**: Thiếu token hoặc token sai chữ ký đều bị chặn.
2. **Chặn giả mạo người dùng**: Phát hiện và chặn khi gửi `claimedUserId` khác với ID trong Token.
3. **Chặn người ngoài phòng**: Bị từ chối `403 Forbidden` khi cố bắt đầu, đi quân, đóng phòng hoặc gọi video.
4. **Chặn khán giả đi quân**: Khán giả trong phòng bị từ chối đi quân.
5. **Đấu thủ hợp lệ**: Thực hiện đi quân và gọi video thành công.
6. **Bảo mật nhật ký**: Token được làm mờ `[REDACTED]` khi ghi log.

### B. Kiểm thử toàn diện Đăng Nhập & Cấp JWT (GCT-28)
```bash
dotnet run -- --test-login
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-login
```

### C. Kiểm thử toàn diện Đăng Ký Tài Khoản (GCT-27)
```bash
dotnet run -- --test-register
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-register
```

### D. Kiểm thử ràng buộc Unique Database (GCT-26)
```bash
dotnet run -- --test-constraints
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-constraints
```