using GameCoTuong.Data;
using GameCoTuong.Data.Seed;
using GameCoTuong.DTOs;
using GameCoTuong.Entities;
using GameCoTuong.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== HỆ THỐNG QUẢN LÝ CỜ TƯỚNG (EF CORE & AUTHENTICATION) ===");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.\\SQLEXPRESS;Database=CoTuongDB;Trusted_Connection=True;TrustServerCertificate=True;";

var optionsBuilder = new DbContextOptionsBuilder<CoTuongDbContext>();
optionsBuilder.UseSqlServer(connectionString);

var passwordHasher = new BCryptPasswordHasher();

try
{
    var arg = args.FirstOrDefault()?.ToLowerInvariant();

    if (arg == "--test-register")
    {
        Console.WriteLine("\n================ KIỂM THỬ TÍNH NĂNG ĐĂNG KÝ (GCT-27) ================");

        // Helper tạo DbContext mới cho mỗi test
        CoTuongDbContext CreateContext() => new(optionsBuilder.Options);

        // -------------------------------------------------------------
        // TEST 1: Kiểm tra dữ liệu không hợp lệ (Validation)
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 1] Kiểm tra dữ liệu sai có thông báo rõ ràng...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher);
            var invalidRequest = new RegisterRequest
            {
                Username = "a",                      // Quá ngắn (< 3)
                Email = "invalid-email-format",      // Sai định dạng
                Password = "123",                    // Quá ngắn (< 8), thiếu hoa, thường, ký tự đặc biệt
                ConfirmPassword = "456"              // Không khớp
            };

            var result = await auth.RegisterAsync(invalidRequest);

            if (!result.IsSuccess && result.Errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đã phát hiện và thông báo đầy đủ các lỗi:");
                foreach (var err in result.Errors)
                {
                    Console.WriteLine($"   * {err}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Hệ thống không chặn dữ liệu sai!");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 2: Đăng ký tài khoản hợp lệ, băm mật khẩu, bảo mật phản hồi
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 2] Đăng ký tài khoản hợp lệ, băm mật khẩu & kiểm tra bảo mật phản hồi...");
        var uniqueSuffix = DateTime.UtcNow.Ticks.ToString()[^6..];
        var validUsername = $"Player_{uniqueSuffix}";
        var validEmail = $"Player_{uniqueSuffix}@Example.COM"; // Viết hoa để test chuẩn hóa
        var rawPassword = "SecurePassword@123";

        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher);
            var validRequest = new RegisterRequest
            {
                Username = $"  {validUsername}  ", // Kèm khoảng trắng để test Trim
                Email = $"  {validEmail}  ",
                Password = rawPassword,
                ConfirmPassword = rawPassword,
                DisplayName = "Kỳ Thủ Mới"
            };

            var result = await auth.RegisterAsync(validRequest);

            if (result.IsSuccess && result.Data != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đăng ký tài khoản hợp lệ thành công!");
                Console.WriteLine($"   * UserId: {result.Data.UserId}");
                Console.WriteLine($"   * Username (Đã chuẩn hóa thường): '{result.Data.Username}'");
                Console.WriteLine($"   * Email (Đã chuẩn hóa thường): '{result.Data.Email}'");
                Console.WriteLine($"   * DisplayName: '{result.Data.DisplayName}'");

                // Kiểm tra UserResponse không có trường Password hay PasswordHash
                var responseProps = typeof(UserResponse).GetProperties().Select(p => p.Name).ToList();
                if (!responseProps.Contains("Password") && !responseProps.Contains("PasswordHash"))
                {
                    Console.WriteLine("   * Bảo mật phản hồi: TUYỆT ĐỐI KHÔNG chứa Password hoặc PasswordHash trong DTO phản hồi.");
                }

                // Kiểm tra trong Database
                var dbUser = await ctx.Users.FirstOrDefaultAsync(u => u.UserId == result.Data.UserId);
                if (dbUser != null)
                {
                    bool isHashed = dbUser.PasswordHash.StartsWith("$2") && dbUser.PasswordHash != rawPassword;
                    bool canVerify = passwordHasher.VerifyPassword(rawPassword, dbUser.PasswordHash);
                    Console.WriteLine($"   * Mật khẩu đã được băm an toàn bằng BCrypt: {dbUser.PasswordHash[..20]}...");
                    Console.WriteLine($"   * Kiểm tra khớp mật khẩu gốc: {(canVerify ? "HỢP LỆ" : "KHÔNG HỢP LỆ")}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"-> THẤT BẠI: {result.Message}");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 3: Chặn trùng kể cả khác hoa thường (Case-insensitive)
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 3] Kiểm tra chặn trùng tên / email kể cả khác hoa thường...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher);

            // Test trùng username khác hoa thường (ví dụ: PLAYER_123456)
            var dupUsernameRequest = new RegisterRequest
            {
                Username = validUsername.ToUpperInvariant(), // Viết hoa toàn bộ
                Email = $"different_{uniqueSuffix}@example.com",
                Password = rawPassword,
                ConfirmPassword = rawPassword
            };

            var resultUsername = await auth.RegisterAsync(dupUsernameRequest);
            if (!resultUsername.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Đã chặn trùng Username (khác hoa thường): {resultUsername.Errors.FirstOrDefault()}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Cho phép trùng Username khác hoa thường!");
                Console.ResetColor();
            }

            // Test trùng email khác hoa thường
            var dupEmailRequest = new RegisterRequest
            {
                Username = $"different_{uniqueSuffix}",
                Email = validEmail.ToLowerInvariant(),
                Password = rawPassword,
                ConfirmPassword = rawPassword
            };

            var resultEmail = await auth.RegisterAsync(dupEmailRequest);
            if (!resultEmail.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Đã chặn trùng Email (khác hoa thường): {resultEmail.Errors.FirstOrDefault()}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Cho phép trùng Email khác hoa thường!");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 4: Chặn trùng khi gửi đồng thời (Concurrency Test)
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 4] Kiểm tra gửi đồng thời (10 luồng cùng đăng ký một tài khoản)...");
        var concurrentUsername = $"concurrent_{DateTime.UtcNow.Ticks.ToString()[^5..]}";
        var concurrentEmail = $"{concurrentUsername}@example.com";

        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            // Mỗi thread dùng 1 DbContext riêng biệt
            using var threadCtx = CreateContext();
            var auth = new AuthService(threadCtx, passwordHasher);
            return await auth.RegisterAsync(new RegisterRequest
            {
                Username = concurrentUsername,
                Email = concurrentEmail,
                Password = rawPassword,
                ConfirmPassword = rawPassword,
                DisplayName = $"Concurrent Thread {i}"
            });
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        Console.WriteLine($"   * Số luồng gửi đi: 10");
        Console.WriteLine($"   * Số luồng thành công: {successCount}");
        Console.WriteLine($"   * Số luồng bị chặn: {failureCount}");

        if (successCount == 1 && failureCount == 9)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đúng chính xác 1 tài khoản được tạo! 9 yêu cầu gửi đồng thời còn lại đều bị chặn an toàn.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"-> THẤT BẠI: Kết quả không mong đợi (Thành công: {successCount}, Thất bại: {failureCount})");
            Console.ResetColor();
        }

        Console.WriteLine("\n================ HOÀN TẤT TẤT CẢ KIỂM THỬ (GCT-27) ================\n");
        return;
    }

    // Các lệnh từ GCT-26
    using var context = new CoTuongDbContext(optionsBuilder.Options);
    var defaultArg = args.FirstOrDefault()?.ToLowerInvariant();

    if (defaultArg == "--seed")
    {
        Console.WriteLine("Đang nạp dữ liệu mẫu (Seed Data)...");
        await DbInitializer.SeedAsync(context);
        Console.WriteLine("Nạp dữ liệu mẫu hoàn tất!");
        return;
    }

    if (defaultArg == "--migrate")
    {
        Console.WriteLine("Đang chạy migration lên SQL Server...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Migration hoàn tất!");
        return;
    }

    if (defaultArg == "--test-constraints")
    {
        Console.WriteLine("\n[1/2] Đang kiểm tra ràng buộc: Chặn tài khoản trùng Username...");
        try
        {
            var dupUser = new User
            {
                Username = "player_red_demo",
                Email = "player_red_demo@example.com",
                PasswordHash = "dummyhash",
                DisplayName = "Trùng lặp"
            };
            context.Users.Add(dupUser);
            await context.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("-> THẤT BẠI: Đã không chặn được tài khoản trùng!");
            Console.ResetColor();
        }
        catch (DbUpdateException ex)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đã chặn tài khoản trùng Username đúng theo thiết kế!");
            Console.WriteLine($"   Chi tiết lỗi chặn: {ex.InnerException?.Message ?? ex.Message}");
            Console.ResetColor();
            context.ChangeTracker.Clear();
        }

        Console.WriteLine("\n[2/2] Đang kiểm tra ràng buộc: Chặn nước đi trùng thứ tự (MoveOrder) trong cùng ván cờ...");
        try
        {
            var existingMatch = await context.Matches.FirstOrDefaultAsync();
            var existingUser = await context.Users.FirstOrDefaultAsync();

            if (existingMatch != null && existingUser != null)
            {
                var dupMove = new Move
                {
                    MatchId = existingMatch.MatchId,
                    UserId = existingUser.UserId,
                    MoveOrder = 1,
                    MoveNotation = "C2.5_DUP"
                };
                context.Moves.Add(dupMove);
                await context.SaveChangesAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đã không chặn được nước đi trùng!");
                Console.ResetColor();
            }
        }
        catch (DbUpdateException ex)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đã chặn nước đi trùng thứ tự trong cùng ván cờ (UQ_Moves_Order)!");
            Console.WriteLine($"   Chi tiết lỗi chặn: {ex.InnerException?.Message ?? ex.Message}");
            Console.ResetColor();
            context.ChangeTracker.Clear();
        }

        return;
    }

    Console.WriteLine("\nCác lệnh khả dụng:");
    Console.WriteLine("  dotnet run -- --test-register     : Kiểm thử toàn diện Đăng ký tài khoản (GCT-27)");
    Console.WriteLine("  dotnet run -- --test-constraints  : Kiểm thử các ràng buộc Unique DB (GCT-26)");
    Console.WriteLine("  dotnet run -- --seed              : Nạp dữ liệu mẫu demo");
    Console.WriteLine("  dotnet run -- --migrate           : Áp dụng migration lên database");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[Lỗi]: {ex.Message}");
    Console.ResetColor();
}
