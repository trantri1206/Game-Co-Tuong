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

## 7. Kiểm Thử Hệ Thống

### A. Kiểm thử toàn diện tính năng Đăng Ký (GCT-27)
```bash
dotnet run -- --test-register
# Hoặc nếu môi trường chặn thực thi file exe trực tiếp:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-register
```
Kịch bản kiểm thử tự động gồm 4 phần:
1. **Kiểm tra dữ liệu sai**: Báo lỗi chi tiết với từng trường (tên ngắn, email sai định dạng, mật khẩu yếu...).
2. **Đăng ký hợp lệ**: Băm mật khẩu bằng BCrypt, phản hồi an toàn không lộ Password/PasswordHash.
3. **Chặn trùng khác hoa thường**: Thử tạo lại với chữ in hoa `PLAYER_...` và email `...@EXAMPLE.COM`.
4. **Kiểm thử gửi đồng thời**: 10 luồng gửi đăng ký cùng 1 lúc $\rightarrow$ Đúng 1 tài khoản thành công, 9 luồng bị chặn an toàn.

### B. Kiểm thử ràng buộc Unique Database (GCT-26)
```bash
dotnet run -- --test-constraints
# Hoặc:
dotnet .\bin\Debug\net10.0\GameCoTuong.dll --test-constraints
```