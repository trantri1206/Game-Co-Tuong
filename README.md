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

---

## 8. Kiểm Thử Hệ Thống

### A. Kiểm thử toàn diện Đăng Nhập & Cấp JWT (GCT-28)
```bash
dotnet run -- --test-login
# Hoặc nếu môi trường chặn thực thi file exe trực tiếp:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-login
```
Kịch bản kiểm thử tự động gồm 6 phần:
1. **Đăng nhập đúng bằng Username**: Nhận JWT hợp lệ, giải mã đúng danh tính kỳ thủ (Claims).
2. **Đăng nhập đúng bằng Email**: Không phân biệt chữ hoa/thường, cấp token thành công.
3. **Đăng nhập sai mật khẩu**: Bị từ chối với thông báo lỗi thống nhất.
4. **Đăng nhập tài khoản không tồn tại**: Bị từ chối với cùng thông báo lỗi thống nhất (chống dò tài khoản).
5. **Token bị sửa đổi (Tampered)**: Sửa 1 vài ký tự trong token $\rightarrow$ Bị từ chối do sai chữ ký số.
6. **Token hết hạn (Expired)**: Kiểm tra token tạo trong quá khứ $\rightarrow$ Bị từ chối do quá hạn phiên.

### B. Kiểm thử toàn diện Đăng Ký Tài Khoản (GCT-27)
```bash
dotnet run -- --test-register
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-register
```

### C. Kiểm thử ràng buộc Unique Database (GCT-26)
```bash
dotnet run -- --test-constraints
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-constraints
```