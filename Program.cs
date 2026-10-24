using System.Security.Claims;
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
var jwtService = new JwtService(configuration);

// Helper tạo DbContext mới cho mỗi test
CoTuongDbContext CreateContext() => new(optionsBuilder.Options);

try
{
    var arg = args.FirstOrDefault()?.ToLowerInvariant();

    if (arg == "--test-login")
    {
        Console.WriteLine("\n================ KIỂM THỬ TÍNH NĂNG ĐĂNG NHẬP & CẤP JWT (GCT-28) ================");

        // Đảm bảo có sẵn 1 tài khoản test chuẩn trong DB
        var testUsername = "player_login_test";
        var testEmail = "player_login_test@example.com";
        var testPassword = "ValidPassword@2026";

        using (var setupCtx = CreateContext())
        {
            var existing = await setupCtx.Users.FirstOrDefaultAsync(u => u.Username == testUsername);
            if (existing == null)
            {
                var auth = new AuthService(setupCtx, passwordHasher, jwtService);
                await auth.RegisterAsync(new RegisterRequest
                {
                    Username = testUsername,
                    Email = testEmail,
                    Password = testPassword,
                    ConfirmPassword = testPassword,
                    DisplayName = "Kỳ Thủ Đăng Nhập Test"
                });
            }
        }

        string validJwtToken = string.Empty;

        // -------------------------------------------------------------
        // TEST 1: Đăng nhập đúng bằng Username -> Nhận JWT hợp lệ
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 1] Đăng nhập đúng bằng Tên đăng nhập (Username)...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher, jwtService);
            var result = await auth.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = testUsername,
                Password = testPassword
            });

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Token) && result.User != null)
            {
                validJwtToken = result.Token;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đăng nhập thành công và cấp JWT token hợp lệ!");
                Console.WriteLine($"   * Token (Cắt ngắn): {result.Token[..35]}...");
                Console.WriteLine($"   * Thời hạn hết hạn: {result.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"   * Kỳ thủ: {result.User.DisplayName} ({result.User.Username})");

                // Giải mã và kiểm tra Claims trong Token
                if (jwtService.ValidateToken(result.Token, out var principal, out var valError) && principal != null)
                {
                    var nameId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var name = principal.FindFirst(ClaimTypes.Name)?.Value;
                    var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                    Console.WriteLine($"   * Kiểm tra Claims trong Token: UserId={nameId}, Name={name}, Email={email}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"-> THẤT BẠI: Đăng nhập không thành công ({result.Message})");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 2: Đăng nhập đúng bằng Email -> Nhận JWT hợp lệ
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 2] Đăng nhập đúng bằng Email...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher, jwtService);
            var result = await auth.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = testEmail.ToUpperInvariant(), // Test không phân biệt hoa thường
                Password = testPassword
            });

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Token))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đăng nhập bằng Email (kể cả viết hoa) thành công!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"-> THẤT BẠI: Đăng nhập bằng Email thất bại ({result.Message})");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 3: Đăng nhập sai mật khẩu -> Trả lỗi thống nhất
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 3] Đăng nhập sai mật khẩu...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher, jwtService);
            var result = await auth.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = testUsername,
                Password = "WrongPassword@123"
            });

            if (!result.IsSuccess && result.Token == null && result.Message == "Tên đăng nhập hoặc mật khẩu không chính xác.")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Đã từ chối và trả thông báo lỗi thống nhất: '{result.Message}'");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Không chặn mật khẩu sai hoặc thông báo không chuẩn!");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 4: Đăng nhập tài khoản không tồn tại -> Trả cùng lỗi thống nhất
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 4] Đăng nhập với tài khoản không tồn tại...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher, jwtService);
            var result = await auth.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = "non_existent_user_99999",
                Password = testPassword
            });

            if (!result.IsSuccess && result.Token == null && result.Message == "Tên đăng nhập hoặc mật khẩu không chính xác.")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Chống User Enumeration - Trả cùng thông báo lỗi thống nhất: '{result.Message}'");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Lộ thông tin tài khoản tồn tại hay không!");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 5: Kiểm tra Token bị chỉnh sửa (Tampered Token)
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 5] Kiểm tra Token bị sửa đổi (Tampered Token)...");
        if (!string.IsNullOrEmpty(validJwtToken))
        {
            // Sửa đổi một vài ký tự trong token
            var tamperedToken = validJwtToken[..^4] + "X9Y8";
            var isValid = jwtService.ValidateToken(tamperedToken, out _, out var tamperedError);

            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Đã từ chối Token bị sửa đổi: {tamperedError}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đã chấp nhận Token bị sửa đổi!");
                Console.ResetColor();
            }
        }

        // -------------------------------------------------------------
        // TEST 6: Kiểm tra Token hết hạn (Expired Token)
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 6] Kiểm tra Token đã hết hạn (Expired Token)...");
        using (var ctx = CreateContext())
        {
            var dummyUser = new User
            {
                UserId = 999,
                Username = "dummy",
                Email = "dummy@example.com"
            };

            // Sinh token đã hết hạn 10 giây trước
            var (expiredToken, _) = jwtService.GenerateToken(dummyUser, customLifetime: TimeSpan.FromSeconds(-10));
            var isValid = jwtService.ValidateToken(expiredToken, out _, out var expiredError);

            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Đã từ chối Token hết hạn: {expiredError}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đã chấp nhận Token hết hạn!");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n================ HOÀN TẤT TẤT CẢ KIỂM THỬ (GCT-28) ================\n");
        return;
    }

    if (arg == "--test-register")
    {
        Console.WriteLine("\n================ KIỂM THỬ TÍNH NĂNG ĐĂNG KÝ (GCT-27) ================");

        // -------------------------------------------------------------
        // TEST 1: Kiểm tra dữ liệu không hợp lệ (Validation)
        // -------------------------------------------------------------
        Console.WriteLine("\n[TEST 1] Kiểm tra dữ liệu sai có thông báo rõ ràng...");
        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher, jwtService);
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
        var validEmail = $"Player_{uniqueSuffix}@Example.COM";
        var rawPassword = "SecurePassword@123";

        using (var ctx = CreateContext())
        {
            var auth = new AuthService(ctx, passwordHasher, jwtService);
            var validRequest = new RegisterRequest
            {
                Username = $"  {validUsername}  ",
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

                var responseProps = typeof(UserResponse).GetProperties().Select(p => p.Name).ToList();
                if (!responseProps.Contains("Password") && !responseProps.Contains("PasswordHash"))
                {
                    Console.WriteLine("   * Bảo mật phản hồi: TUYỆT ĐỐI KHÔNG chứa Password hoặc PasswordHash trong DTO phản hồi.");
                }

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
            var auth = new AuthService(ctx, passwordHasher, jwtService);

            var dupUsernameRequest = new RegisterRequest
            {
                Username = validUsername.ToUpperInvariant(),
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
            using var threadCtx = CreateContext();
            var auth = new AuthService(threadCtx, passwordHasher, jwtService);
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
    Console.WriteLine("  dotnet run -- --test-login        : Kiểm thử toàn diện Đăng nhập & JWT (GCT-28)");
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
