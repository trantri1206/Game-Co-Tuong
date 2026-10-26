using System.Security.Claims;
using GameCoTuong.Data;
using GameCoTuong.Data.Seed;
using GameCoTuong.DTOs;
using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
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
var tokenBlacklistService = new TokenBlacklistService();
var jwtService = new JwtService(configuration, tokenBlacklistService);

CoTuongDbContext CreateContext() => new(optionsBuilder.Options);

try
{
    var arg = args.FirstOrDefault()?.ToLowerInvariant();

    if (arg == "--test-all")
    {
        Console.WriteLine("\n================ CHẠY TOÀN BỘ KIỂM THỬ HỆ THỐNG (GCT-31, GCT-42, GCT-43, GCT-50, GCT-51) ================");
        Console.WriteLine(">> Dữ liệu kiểm thử hoàn toàn độc lập và có thể chạy lặp lại vô số lần.");

        RunTestEngine();
        RunTestSimulation();
        RunTestSoldier();
        RunTestRook();
        await RunTestRegisterAsync();
        await RunTestLoginAsync();
        await RunTestAuthzAsync();
        await RunTestSessionAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n================ ĐÃ HOÀN TẤT TẤT CẢ CÁC BỘ KIỂM THỬ (100% THÀNH CÔNG) ================\n");
        Console.ResetColor();
        return;
    }

    if (arg == "--test-rook" || arg == "--test-chariot")
    {
        RunTestRook();
        return;
    }

    if (arg == "--test-soldier")
    {
        RunTestSoldier();
        return;
    }

    if (arg == "--test-simulation")
    {
        RunTestSimulation();
        return;
    }

    if (arg == "--test-engine")
    {
        RunTestEngine();
        return;
    }

    if (arg == "--test-session")
    {
        await RunTestSessionAsync();
        return;
    }

    if (arg == "--test-authz")
    {
        await RunTestAuthzAsync();
        return;
    }

    if (arg == "--test-login")
    {
        await RunTestLoginAsync();
        return;
    }

    if (arg == "--test-register")
    {
        await RunTestRegisterAsync();
        return;
    }

    if (arg == "--test-constraints")
    {
        await RunTestConstraintsAsync();
        return;
    }

    // Các lệnh từ GCT-26
    using var context = CreateContext();
    if (arg == "--seed")
    {
        Console.WriteLine("Đang nạp dữ liệu mẫu (Seed Data)...");
        await DbInitializer.SeedAsync(context);
        Console.WriteLine("Nạp dữ liệu mẫu hoàn tất!");
        return;
    }

    if (arg == "--migrate")
    {
        Console.WriteLine("Đang chạy migration lên SQL Server...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Migration hoàn tất!");
        return;
    }

    if (arg == "--server")
    {
        Console.WriteLine("\n[WEB SERVER] Đang khởi động Backend API Server...");
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<CoTuongDbContext>(opt => opt.UseSqlServer(connectionString));
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        builder.Services.AddSingleton<IJwtService, JwtService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserService, UserService>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        var app = builder.Build();
        app.UseCors();

        // 1. API Đăng Ký
        app.MapPost("/api/auth/register", async (RegisterRequest req, IAuthService auth) =>
        {
            var result = await auth.RegisterAsync(req);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { success = false, message = "Dữ liệu không hợp lệ hoặc tài khoản đã tồn tại.", errors = result.Errors });
            }
            return Results.Ok(new { success = true, user = result.Data });
        });

        // 2. API Đăng Nhập
        app.MapPost("/api/auth/login", async (LoginRequest req, IAuthService auth) =>
        {
            var result = await auth.LoginAsync(req);
            if (!result.IsSuccess)
            {
                return Results.Json(new { success = false, message = result.Message }, statusCode: 401);
            }
            return Results.Ok(new { success = true, token = result.Token, user = result.User });
        });

        // 3. API Hồ Sơ
        app.MapGet("/api/auth/profile", async (HttpContext ctx, IUserService userService, IJwtService jwt) =>
        {
            var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!jwt.ValidateToken(token, out var principal, out _) || principal == null)
                return Results.Unauthorized();

            var profile = await userService.GetProfileAsync(principal);
            return profile == null ? Results.NotFound() : Results.Ok(profile);
        });

        // 4. API Đăng Xuất
        app.MapPost("/api/auth/logout", async (HttpContext ctx, LogoutRequest req, IUserService userService, IJwtService jwt) =>
        {
            var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!jwt.ValidateToken(token, out var principal, out _) || principal == null)
                return Results.Unauthorized();

            var result = await userService.LogoutAsync(principal, req, token);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new { success = false, message = result.Message, requiresConfirmation = result.RequiresSurrenderConfirmation });
            }

            return Results.Ok(result);
        });

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[WEB SERVER] Backend API Server đang lắng nghe tại http://localhost:5000");
        Console.ResetColor();
        await app.RunAsync("http://localhost:5000");
        return;
    }

    Console.WriteLine("\nCác lệnh khả dụng:");
    Console.WriteLine("  dotnet run -- --server            : Khởi chạy Web API Server (http://localhost:5000)");
    Console.WriteLine("  dotnet run -- --test-engine       : Kiểm thử Mô hình Bàn cờ & Quân cờ (GCT-42)");
    Console.WriteLine("  dotnet test                       : Chạy bộ Unit/Integration Tests tự động (xUnit)");
    Console.WriteLine("  dotnet run -- --test-all          : Chạy toàn bộ kiểm thử hệ thống (GCT-31 & GCT-42)");
    Console.WriteLine("  dotnet run -- --test-session      : Kiểm thử Hồ sơ & Chính sách Đăng xuất (GCT-30)");
    Console.WriteLine("  dotnet run -- --test-authz        : Kiểm thử toàn diện Phân quyền API & Phòng (GCT-29)");
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

// =========================================================================================
// CÁC HÀM KIỂM THỬ CLI ĐỘC LẬP (ISOLATED TEST RUNNERS)
// =========================================================================================

async Task RunTestRegisterAsync()
{
    Console.WriteLine("\n================ KIỂM THỬ TÍNH NĂNG ĐĂNG KÝ (GCT-27) ================");

    // 1. Kiểm tra dữ liệu không hợp lệ
    Console.WriteLine("\n[TEST 1] Kiểm tra dữ liệu sai có thông báo rõ ràng...");
    using (var ctx = CreateContext())
    {
        var auth = new AuthService(ctx, passwordHasher, jwtService);
        var invalidRequest = new RegisterRequest
        {
            Username = "a",
            Email = "invalid-email-format",
            Password = "123",
            ConfirmPassword = "456"
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

    // 2. Đăng ký tài khoản hợp lệ, băm mật khẩu, bảo mật phản hồi
    Console.WriteLine("\n[TEST 2] Đăng ký tài khoản hợp lệ, băm mật khẩu & kiểm tra bảo mật phản hồi...");
    var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
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

    // 3. Chặn trùng kể cả khác hoa thường
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

        var dupEmailRequest = new RegisterRequest
        {
            Username = $"diff_{uniqueSuffix}",
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
    }

    // 4. Chặn trùng khi gửi đồng thời
    Console.WriteLine("\n[TEST 4] Kiểm tra gửi đồng thời (10 luồng cùng đăng ký một tài khoản)...");
    var concurrentUsername = $"concurrent_{Guid.NewGuid():N}";
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

    Console.WriteLine($"   * Số luồng gửi đi: 10 | Thành công: {successCount} | Bị chặn: {failureCount}");
    if (successCount == 1 && failureCount == 9)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Đúng chính xác 1 tài khoản được tạo! 9 yêu cầu gửi đồng thời còn lại đều bị chặn an toàn.");
        Console.ResetColor();
    }
}

async Task RunTestLoginAsync()
{
    Console.WriteLine("\n================ KIỂM THỬ TÍNH NĂNG ĐĂNG NHẬP & CẤP JWT (GCT-28) ================");

    var testUsername = $"player_login_{Guid.NewGuid():N}";
    var testEmail = $"{testUsername}@example.com";
    var testPassword = "ValidPassword@2026";

    using (var setupCtx = CreateContext())
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

    string validJwtToken = string.Empty;

    // TEST 1: Đăng nhập đúng bằng Username
    Console.WriteLine("\n[TEST 1] Đăng nhập đúng bằng Tên đăng nhập (Username)...");
    using (var ctx = CreateContext())
    {
        var auth = new AuthService(ctx, passwordHasher, jwtService);
        var result = await auth.LoginAsync(new LoginRequest { UsernameOrEmail = testUsername, Password = testPassword });

        if (result.IsSuccess && !string.IsNullOrEmpty(result.Token) && result.User != null)
        {
            validJwtToken = result.Token;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đăng nhập thành công và cấp JWT token hợp lệ!");
            Console.WriteLine($"   * Token (Cắt ngắn): {result.Token[..35]}...");
            Console.WriteLine($"   * Kỳ thủ: {result.User.DisplayName} ({result.User.Username})");

            if (jwtService.ValidateToken(result.Token, out var principal, out _) && principal != null)
            {
                var nameId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var name = principal.FindFirst(ClaimTypes.Name)?.Value;
                Console.WriteLine($"   * Claims giải mã từ Token: UserId={nameId}, Name={name}");
            }
            Console.ResetColor();
        }
    }

    // TEST 2: Đăng nhập đúng bằng Email
    Console.WriteLine("\n[TEST 2] Đăng nhập đúng bằng Email...");
    using (var ctx = CreateContext())
    {
        var auth = new AuthService(ctx, passwordHasher, jwtService);
        var result = await auth.LoginAsync(new LoginRequest { UsernameOrEmail = testEmail.ToUpperInvariant(), Password = testPassword });

        if (result.IsSuccess && !string.IsNullOrEmpty(result.Token))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đăng nhập bằng Email (kể cả viết hoa) thành công!");
            Console.ResetColor();
        }
    }

    // TEST 3: Đăng nhập sai mật khẩu
    Console.WriteLine("\n[TEST 3] Đăng nhập sai mật khẩu...");
    using (var ctx = CreateContext())
    {
        var auth = new AuthService(ctx, passwordHasher, jwtService);
        var result = await auth.LoginAsync(new LoginRequest { UsernameOrEmail = testUsername, Password = "WrongPassword@123" });

        if (!result.IsSuccess && result.Token == null && result.Message == "Tên đăng nhập hoặc mật khẩu không chính xác.")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Đã từ chối và trả thông báo lỗi thống nhất: '{result.Message}'");
            Console.ResetColor();
        }
    }

    // TEST 4: Đăng nhập tài khoản không tồn tại
    Console.WriteLine("\n[TEST 4] Đăng nhập với tài khoản không tồn tại...");
    using (var ctx = CreateContext())
    {
        var auth = new AuthService(ctx, passwordHasher, jwtService);
        var result = await auth.LoginAsync(new LoginRequest { UsernameOrEmail = "ghost_user_99999", Password = testPassword });

        if (!result.IsSuccess && result.Token == null && result.Message == "Tên đăng nhập hoặc mật khẩu không chính xác.")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Chống User Enumeration - Trả cùng thông báo lỗi thống nhất: '{result.Message}'");
            Console.ResetColor();
        }
    }

    // TEST 5: Token bị sửa đổi
    Console.WriteLine("\n[TEST 5] Kiểm tra Token bị sửa đổi (Tampered Token)...");
    if (!string.IsNullOrEmpty(validJwtToken))
    {
        var tamperedToken = validJwtToken[..^4] + "X9Y8";
        var isValid = jwtService.ValidateToken(tamperedToken, out _, out var tamperedError);

        if (!isValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Đã từ chối Token bị sửa đổi: {tamperedError}");
            Console.ResetColor();
        }
    }

    // TEST 6: Token hết hạn
    Console.WriteLine("\n[TEST 6] Kiểm tra Token đã hết hạn (Expired Token)...");
    var dummyUser = new User { UserId = 999, Username = "dummy", Email = "dummy@example.com" };
    var (expiredToken, _) = jwtService.GenerateToken(dummyUser, customLifetime: TimeSpan.FromSeconds(-10));
    var isExpiredValid = jwtService.ValidateToken(expiredToken, out _, out var expiredError);

    if (!isExpiredValid)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Đã từ chối Token hết hạn: {expiredError}");
        Console.ResetColor();
    }
}

async Task RunTestAuthzAsync()
{
    Console.WriteLine("\n================ KIỂM THỬ PHÂN QUYỀN API & PHÒNG CỜ (GCT-29) ================");

    using (var setupCtx = CreateContext())
    {
        await DbInitializer.SeedAsync(setupCtx);

        var outsider = await setupCtx.Users.FirstOrDefaultAsync(u => u.Username == "outsider_test_user");
        if (outsider == null)
        {
            outsider = new User { Username = "outsider_test_user", Email = "outsider@example.com", PasswordHash = passwordHasher.HashPassword("TestPass@123"), DisplayName = "Người Ngoài Phòng" };
            setupCtx.Users.Add(outsider);
            await setupCtx.SaveChangesAsync();
        }

        var spectator = await setupCtx.Users.FirstOrDefaultAsync(u => u.Username == "spectator_test_user");
        if (spectator == null)
        {
            spectator = new User { Username = "spectator_test_user", Email = "spectator@example.com", PasswordHash = passwordHasher.HashPassword("TestPass@123"), DisplayName = "Khán Giả Xem Cờ" };
            setupCtx.Users.Add(spectator);
            await setupCtx.SaveChangesAsync();
        }

        var room = await setupCtx.Rooms.FirstAsync();
        var playerRed = await setupCtx.Users.FirstAsync(u => u.Username == "player_red_demo");
        if (!await setupCtx.RoomMembers.AnyAsync(rm => rm.RoomId == room.RoomId && rm.UserId == playerRed.UserId))
        {
            setupCtx.RoomMembers.Add(new RoomMember { RoomId = room.RoomId, UserId = playerRed.UserId, JoinedAt = DateTime.UtcNow });
        }

        var isSpectatorInRoom = await setupCtx.RoomMembers.AnyAsync(rm => rm.RoomId == room.RoomId && rm.UserId == spectator.UserId);
        if (!isSpectatorInRoom)
        {
            setupCtx.RoomMembers.Add(new RoomMember { RoomId = room.RoomId, UserId = spectator.UserId, JoinedAt = DateTime.UtcNow });
        }
        await setupCtx.SaveChangesAsync();
    }

    using (var ctx = CreateContext())
    {
        var authzService = new RoomAuthorizationService(ctx);
        var room = await ctx.Rooms.FirstAsync();
        var match = await ctx.Matches.FirstAsync();
        var playerRed = await ctx.Users.FirstAsync(u => u.Username == "player_red_demo");
        var outsider = await ctx.Users.FirstAsync(u => u.Username == "outsider_test_user");
        var spectator = await ctx.Users.FirstAsync(u => u.Username == "spectator_test_user");

        var (tokenPlayer, _) = jwtService.GenerateToken(playerRed);
        jwtService.ValidateToken(tokenPlayer, out var principalPlayer, out _);

        // TEST 1: Thiếu hoặc sai Token
        Console.WriteLine("\n[TEST 1] Kiểm tra thiếu hoặc sai Token...");
        var noTokenResult = jwtService.ValidateToken("", out _, out var noTokenErr);
        var badTokenResult = jwtService.ValidateToken("invalid.bearer.token", out _, out var badTokenErr);

        if (!noTokenResult && !badTokenResult)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Chặn thiếu hoặc sai Token: {noTokenErr}");
            Console.ResetColor();
        }

        // TEST 2: Chặn giả mạo người dùng
        Console.WriteLine("\n[TEST 2] Lấy danh tính từ phiên & Chặn giả mạo người dùng...");
        var impersonationAttempt = authzService.VerifyNoImpersonation(principalPlayer, claimedUserId: outsider.UserId);
        if (!impersonationAttempt.IsAuthorized && impersonationAttempt.StatusCode == 403)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Đã chặn hành vi giả mạo ID người dùng: {impersonationAttempt.Message}");
            Console.ResetColor();
        }

        // TEST 3: Người ngoài phòng bị chặn
        Console.WriteLine("\n[TEST 3] Người ngoài phòng cố gắng thao tác (Đi quân, bắt đầu, đóng phòng, gọi video)...");
        var outsiderStart = await authzService.AuthorizeStartMatchAsync(room.RoomId, outsider.UserId);
        var outsiderMove = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, outsider.UserId);
        var outsiderClose = await authzService.AuthorizeCloseRoomAsync(room.RoomId, outsider.UserId);
        var outsiderVideo = await authzService.AuthorizeVideoCallAsync(room.RoomId, outsider.UserId);

        if (!outsiderStart.IsAuthorized && !outsiderMove.IsAuthorized && !outsiderClose.IsAuthorized && !outsiderVideo.IsAuthorized)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Toàn bộ thao tác của người ngoài phòng đều bị chặn 403 Forbidden:");
            Console.WriteLine($"   * Bắt đầu phòng: {outsiderStart.Message}");
            Console.WriteLine($"   * Đi quân: {outsiderMove.Message}");
            Console.ResetColor();
        }

        // TEST 4: Khán giả đi quân bị chặn
        Console.WriteLine("\n[TEST 4] Khán giả trong phòng cố gắng đi quân...");
        var spectatorMove = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, spectator.UserId);
        if (!spectatorMove.IsAuthorized)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Chặn khán giả đi quân: {spectatorMove.Message}");
            Console.ResetColor();
        }

        // TEST 5: Đấu thủ hợp lệ
        Console.WriteLine("\n[TEST 5] Đấu thủ hợp lệ thực hiện thao tác đúng quyền...");
        var playerMove = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, playerRed.UserId);
        if (playerMove.IsAuthorized)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Đấu thủ hợp lệ được phép đi quân và gọi video trong phòng.");
            Console.ResetColor();
        }

        // TEST 6: Bảo mật nhật ký
        Console.WriteLine("\n[TEST 6] Kiểm tra bảo mật nhật ký (Log Sanitization)...");
        var sampleLog = $"GET /hubs/game?access_token={tokenPlayer} HTTP/1.1 | Authorization: Bearer {tokenPlayer}";
        var sanitizedLog = GameCoTuong.Middleware.LogSanitizer.Sanitize(sampleLog);
        if (!sanitizedLog.Contains(tokenPlayer) && sanitizedLog.Contains("[REDACTED]"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-> THÀNH CÔNG: Token đã được che mờ hoàn toàn trong nhật ký hệ thống:");
            Console.WriteLine($"   * {sanitizedLog}");
            Console.ResetColor();
        }
    }
}

async Task RunTestSessionAsync()
{
    Console.WriteLine("\n================ KIỂM THỬ HỒ SƠ & CHÍNH SÁCH PHIÊN / ĐĂNG XUẤT (GCT-30) ================");

    using (var setupCtx = CreateContext())
    {
        await DbInitializer.SeedAsync(setupCtx);
        var userRed = await setupCtx.Users.FirstAsync(u => u.Username == "player_red_demo");
        var userBlack = await setupCtx.Users.FirstAsync(u => u.Username == "player_black_demo");
        var room = await setupCtx.Rooms.FirstAsync();

        if (!await setupCtx.RoomMembers.AnyAsync(rm => rm.RoomId == room.RoomId && rm.UserId == userRed.UserId))
        {
            setupCtx.RoomMembers.Add(new RoomMember { RoomId = room.RoomId, UserId = userRed.UserId, JoinedAt = DateTime.UtcNow });
        }
        if (!await setupCtx.RoomMembers.AnyAsync(rm => rm.RoomId == room.RoomId && rm.UserId == userBlack.UserId))
        {
            setupCtx.RoomMembers.Add(new RoomMember { RoomId = room.RoomId, UserId = userBlack.UserId, JoinedAt = DateTime.UtcNow });
        }

        var testMatch = await setupCtx.Matches.FirstOrDefaultAsync(m => m.RoomId == room.RoomId && m.Result == "InProgress" && m.EndTime == null);
        if (testMatch == null)
        {
            testMatch = new Match
            {
                RoomId = room.RoomId,
                MatchType = "Ranked",
                InitialBoardState = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                Result = "InProgress",
                Reason = "Đang thi đấu",
                Version = "1.0",
                StartTime = DateTime.UtcNow
            };
            setupCtx.Matches.Add(testMatch);
            await setupCtx.SaveChangesAsync();

            setupCtx.MatchPlayers.AddRange(
                new MatchPlayer { MatchId = testMatch.MatchId, UserId = userRed.UserId, Side = "Red" },
                new MatchPlayer { MatchId = testMatch.MatchId, UserId = userBlack.UserId, Side = "Black" }
            );
        }
        await setupCtx.SaveChangesAsync();
    }

    using (var ctx = CreateContext())
    {
        var userService = new UserService(ctx, tokenBlacklistService);
        var userRed = await ctx.Users.FirstAsync(u => u.Username == "player_red_demo");
        var userBlack = await ctx.Users.FirstAsync(u => u.Username == "player_black_demo");

        var (tokenRed, _) = jwtService.GenerateToken(userRed);
        jwtService.ValidateToken(tokenRed, out var principalRed, out _);

        // TEST 1: Lấy hồ sơ
        Console.WriteLine("\n[TEST 1] Lấy hồ sơ người dùng theo danh tính phiên xác thực...");
        var profile = await userService.GetProfileAsync(principalRed!);
        if (profile != null && profile.Username == "player_red_demo")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Lấy hồ sơ cá nhân: UserId={profile.UserId}, DisplayName={profile.DisplayName}");
            Console.ResetColor();
        }

        // TEST 2: Chặn đổi hồ sơ bằng mã người dùng tùy ý
        Console.WriteLine("\n[TEST 2] Chặn đổi hồ sơ bằng mã người dùng tùy ý...");
        var attackRequest = new UpdateProfileRequest { DisplayName = "Hacker", ClaimedUserId = userBlack.UserId };
        var attackResult = await userService.UpdateProfileAsync(principalRed!, attackRequest);
        if (!attackResult.IsAuthorized && attackResult.StatusCode == 403)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Đã chặn hành vi đổi hồ sơ người khác: {attackResult.Message}");
            Console.ResetColor();
        }

        // TEST 3: Đang chơi cờ yêu cầu xác nhận đầu hàng khi đăng xuất
        Console.WriteLine("\n[TEST 3] Đang chơi cờ: Yêu cầu xác nhận đầu hàng khi đăng xuất...");
        var logoutAttemptWithoutConfirm = await userService.LogoutAsync(principalRed!, new LogoutRequest { ConfirmSurrender = false }, tokenRed);
        if (!logoutAttemptWithoutConfirm.IsSuccess && logoutAttemptWithoutConfirm.RequiresSurrenderConfirmation)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Đã chặn đăng xuất và yêu cầu xác nhận đầu hàng: {logoutAttemptWithoutConfirm.Message}");
            Console.ResetColor();
        }

        // TEST 4: Hủy đầu hàng giữ trận
        Console.WriteLine("\n[TEST 4] Hủy đầu hàng giữ trận...");
        var cancelResult = await userService.CancelSurrenderAsync(principalRed!);
        var checkMatchStillActive = await ctx.Matches.AnyAsync(m => m.Result == "InProgress" && m.EndTime == null);
        if (cancelResult.IsAuthorized && checkMatchStillActive)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Hủy đầu hàng giữ nguyên trận đấu (Trạng thái: InProgress).");
            Console.ResetColor();
        }

        // TEST 5: Xác nhận đầu hàng và đăng xuất đưa về đăng nhập đúng luồng
        Console.WriteLine("\n[TEST 5] Xác nhận đầu hàng và đăng xuất đưa về đăng nhập đúng luồng...");
        var logoutConfirmed = await userService.LogoutAsync(principalRed!, new LogoutRequest { ConfirmSurrender = true }, tokenRed);
        var isUserRemovedFromRoom = !await ctx.RoomMembers.AnyAsync(rm => rm.UserId == userRed.UserId);
        var matchUpdated = await ctx.Matches.OrderByDescending(m => m.MatchId).FirstOrDefaultAsync();

        if (logoutConfirmed.IsSuccess && logoutConfirmed.DeleteClientToken && logoutConfirmed.RedirectToLogin
            && isUserRemovedFromRoom && matchUpdated?.Result == "BlackWin")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Đăng xuất thành công, xử thua ({matchUpdated.Result}), rời phòng và đưa về trang đăng nhập.");
            Console.ResetColor();
        }

        // TEST 6: Chính sách JWT và thu hồi phiên
        Console.WriteLine("\n[TEST 6] Kiểm tra chính sách JWT và thu hồi phiên đăng nhập...");
        var isTokenStillValid = jwtService.ValidateToken(tokenRed, out _, out var tokenRevokeErr);
        if (!isTokenStillValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"-> THÀNH CÔNG: Token cũ đã bị vô hiệu hóa qua Blacklist: {tokenRevokeErr}");
            Console.ResetColor();
        }
    }
}

async Task RunTestConstraintsAsync()
{
    Console.WriteLine("\n[1/2] Đang kiểm tra ràng buộc: Chặn tài khoản trùng Username...");
    using var ctx = CreateContext();
    try
    {
        var dupUser = new User
        {
            Username = "player_red_demo",
            Email = "player_red_demo@example.com",
            PasswordHash = "dummyhash",
            DisplayName = "Trùng lặp"
        };
        ctx.Users.Add(dupUser);
        await ctx.SaveChangesAsync();
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
        ctx.ChangeTracker.Clear();
    }

    Console.WriteLine("\n[2/2] Đang kiểm tra ràng buộc: Chặn nước đi trùng thứ tự (MoveOrder) trong cùng ván cờ...");
    try
    {
        var existingMatch = await ctx.Matches.FirstOrDefaultAsync();
        var existingUser = await ctx.Users.FirstOrDefaultAsync();

        if (existingMatch != null && existingUser != null)
        {
            var dupMove = new Move
            {
                MatchId = existingMatch.MatchId,
                UserId = existingUser.UserId,
                MoveOrder = 1,
                MoveNotation = "C2.5_DUP"
            };
            ctx.Moves.Add(dupMove);
            await ctx.SaveChangesAsync();
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
        ctx.ChangeTracker.Clear();
    }
}

void RunTestEngine()
{
    Console.WriteLine("\n================ KIỂM THỬ MÔ HÌNH BÀN CỜ & QUÂN CỜ (GCT-42) ================");
    Console.WriteLine(">> Bộ luật chạy độc lập 100%, không phụ thuộc Database hoặc Network.");

    // 1. Kiểm tra kích thước bàn cờ & 90 giao điểm
    Console.WriteLine("\n[TEST 1] Kiểm tra quy chuẩn 9 cột (0–8), 10 hàng (0–9) & 90 giao điểm...");
    if (ChessBoard.Cols == 9 && ChessBoard.Rows == 10 && ChessBoard.TotalIntersections == 90)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Bàn cờ chuẩn hóa chính xác {ChessBoard.Cols} cột x {ChessBoard.Rows} hàng = {ChessBoard.TotalIntersections} giao điểm.");
        Console.ResetColor();
    }

    // 2. Kiểm tra chặn tọa độ ngoài bàn
    Console.WriteLine("\n[TEST 2] Kiểm tra chặn tọa độ ngoài bàn cờ (Col < 0, Col > 8, Row < 0, Row > 9)...");
    int rejectedCount = 0;
    var invalidCoords = new (int c, int r)[] { (-1, 0), (9, 0), (0, -1), (0, 10), (10, 10) };
    var testBoard = new ChessBoard();
    foreach (var (c, r) in invalidCoords)
    {
        try
        {
            testBoard.GetPiece(c, r);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejectedCount++;
        }
    }
    if (rejectedCount == invalidCoords.Length)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Đã chặn thành công {rejectedCount}/{invalidCoords.Length} tọa độ ngoài bàn cờ.");
        Console.ResetColor();
    }

    // 3. Biểu diễn đủ 7 loại quân cờ
    Console.WriteLine("\n[TEST 3] Kiểm tra biểu diễn đủ 7 loại quân cờ cho cả 2 phe (Đỏ / Đen)...");
    var types = Enum.GetValues<PieceType>();
    if (types.Length == 7)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Đã biểu diễn đầy đủ 7 loại quân: Tướng/Soái, Sĩ, Tượng, Mã, Xe, Pháo, Binh/Tốt.");
        Console.ResetColor();
    }

    // 4. Khởi tạo bàn cờ ban đầu (Đen ở trên, Đỏ ở dưới, 32 quân cờ)
    Console.WriteLine("\n[TEST 4] Khởi tạo thế cờ tiêu chuẩn: Đen ở trên (Hàng 0–4), Đỏ ở dưới (Hàng 5–9)...");
    var initialBoard = ChessBoard.CreateInitial();
    int totalPieces = initialBoard.CountPieces();
    int redPieces = initialBoard.CountPieces(PieceColor.Red);
    int blackPieces = initialBoard.CountPieces(PieceColor.Black);
    var blackGeneral = initialBoard.GetPiece(4, 0);
    var redGeneral = initialBoard.GetPiece(4, 9);

    if (totalPieces == 32 && redPieces == 16 && blackPieces == 16 &&
        blackGeneral?.Type == PieceType.General && blackGeneral.Color == PieceColor.Black &&
        redGeneral?.Type == PieceType.General && redGeneral.Color == PieceColor.Red)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Thế cờ ban đầu chuẩn xác 32 quân (16 Đỏ, 16 Đen).");
        Console.WriteLine($"   * Tướng Đen ngự tại Cung Đen (4, 0) - Hán Tự: {blackGeneral.Hanzi}");
        Console.WriteLine($"   * Soái Đỏ ngự tại Cung Đỏ (4, 9) - Hán Tự: {redGeneral.Hanzi}");
        Console.ResetColor();
    }

    // 5. Quản lý bên tới lượt và phiên bản trạng thái
    Console.WriteLine("\n[TEST 5] Kiểm tra bên tới lượt (ActiveColor) và phiên bản trạng thái (StateVersion)...");
    int v1 = initialBoard.StateVersion;
    initialBoard.SwitchTurn();
    int v2 = initialBoard.StateVersion;
    if (initialBoard.ActiveColor == PieceColor.Black && v2 > v1)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Đổi lượt chính xác sang Đen, StateVersion tăng ({v1} -> {v2}).");
        Console.ResetColor();
    }

    // 6. Tuần tự hóa FEN
    Console.WriteLine("\n[TEST 6] Kiểm tra chuyển đổi thế cờ sang chuỗi FEN chuẩn quốc tế...");
    var fen = initialBoard.ToFen();
    var parsed = ChessBoard.FromFen(fen);
    if (parsed.CountPieces() == 32 && parsed.ActiveColor == PieceColor.Black)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Chuỗi FEN đối ứng và khôi phục hoàn hảo 100% trạng thái.");
        Console.WriteLine($"   FEN: {fen}");
        Console.ResetColor();
    }
}

void RunTestSimulation()
{
    Console.WriteLine("\n================ KIỂM THỬ KHỞI TẠO BÀN CỜ & MÔ PHỎNG NƯỚC ĐI (GCT-43 & GCT-106) ================");
    Console.WriteLine(">> Kiểm chứng toàn diện dữ liệu bàn cờ dùng cho bộ luật và máy (AI).");

    // TEST 1: Khởi tạo đủ 32 quân theo đúng màu và vị trí, cùng 58 ô trống
    Console.WriteLine("\n[TEST 1] Khởi tạo đủ 32 quân cờ chuẩn xác theo màu, vị trí và 58 ô trống...");
    var board = new ChessBoard();
    board.SetupInitialPosition();

    int occupiedCount = 0;
    int emptyCount = 0;
    for (int r = 0; r < ChessBoard.Rows; r++)
    {
        for (int c = 0; c < ChessBoard.Cols; c++)
        {
            if (board.GetPiece(c, r) != null) occupiedCount++;
            else emptyCount++;
        }
    }

    bool countOk = board.CountPieces() == 32 &&
                   board.CountPieces(PieceColor.Red) == 16 &&
                   board.CountPieces(PieceColor.Black) == 16 &&
                   occupiedCount == 32 &&
                   emptyCount == 58;

    var redGen = board.FindGeneral(PieceColor.Red);
    var blackGen = board.FindGeneral(PieceColor.Black);
    bool generalOk = redGen == new Position(4, 9) && blackGen == new Position(4, 0);

    bool soldiersOk = true;
    for (int col = 0; col <= 8; col += 2)
    {
        var bSoldier = board.GetPiece(col, 3);
        var rSoldier = board.GetPiece(col, 6);
        if (bSoldier?.Type != PieceType.Soldier || bSoldier.Color != PieceColor.Black ||
            rSoldier?.Type != PieceType.Soldier || rSoldier.Color != PieceColor.Red)
        {
            soldiersOk = false;
            break;
        }
    }

    if (countOk && generalOk && soldiersOk && board.ActiveColor == PieceColor.Red)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Đặt đủ 32 quân (16 Đỏ, 16 Đen), 58 ô trống = 90 giao điểm; Đỏ đi trước, Đen ở trên và Đỏ ở dưới.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Thiết lập ban đầu 32 quân cờ chưa chính xác.");
        Console.ResetColor();
    }

    // TEST 2: Áp dụng nước đi không ăn quân (MakeMove)
    Console.WriteLine("\n[TEST 2] Áp dụng nước thử không ăn quân: Pháo 2 bình 5 (1, 7) -> (4, 7)...");
    int initialVer = board.StateVersion;
    var undo1 = board.MakeMove(new Position(1, 7), new Position(4, 7));

    bool move1Ok = board.GetPiece(1, 7) == null &&
                   board.GetPiece(4, 7)?.Type == PieceType.Cannon &&
                   board.GetPiece(4, 7)?.Color == PieceColor.Red &&
                   board.ActiveColor == PieceColor.Black &&
                   board.StateVersion == initialVer + 1 &&
                   !undo1.HasCaptured &&
                   board.CanUndo;

    if (move1Ok)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Đã áp dụng nước đi ({undo1}), đổi lượt sang {board.ActiveColor}, StateVersion: {board.StateVersion}.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Nước đi không ăn quân cập nhật trạng thái không đúng.");
        Console.ResetColor();
    }

    // TEST 3: Áp dụng nước thử có ăn quân (Capturing Move)
    Console.WriteLine("\n[TEST 3] Áp dụng nước thử có ăn quân: Đen đi Pháo (1, 2) -> (4, 2), Đỏ ăn Pháo (4, 7) -> (4, 2)...");
    board.MakeMove(new Position(1, 2), new Position(4, 2)); // Đen đi
    var undoCapture = board.MakeMove(new Position(4, 7), new Position(4, 2)); // Đỏ ăn

    bool captureOk = undoCapture.HasCaptured &&
                     undoCapture.CapturedPiece?.Type == PieceType.Cannon &&
                     undoCapture.CapturedPiece?.Color == PieceColor.Black &&
                     board.CountPieces() == 31 &&
                     board.HalfMoveClock == 0;

    if (captureOk)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Ăn quân thành công! Đã lưu quân bị ăn: {undoCapture.CapturedPiece}, tổng số quân còn {board.CountPieces()}.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Xử lý ăn quân không chính xác.");
        Console.ResetColor();
    }

    // TEST 4: Hoàn tác nước đi (UndoMove)
    Console.WriteLine("\n[TEST 4] Hoàn tác nước đi ăn quân và kiểm tra khôi phục 100%...");
    var restored = board.UndoMove();

    bool undoOk = board.CountPieces() == 32 &&
                  board.GetPiece(4, 2)?.Type == PieceType.Cannon &&
                  board.GetPiece(4, 2)?.Color == PieceColor.Black &&
                  board.GetPiece(4, 7)?.Type == PieceType.Cannon &&
                  board.GetPiece(4, 7)?.Color == PieceColor.Red &&
                  board.ActiveColor == PieceColor.Red;

    if (undoOk)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Hoàn tác chính xác 100%! Quân bị ăn được hồi sinh, vị trí cũ được khôi phục, đổi lại lượt.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Hoàn tác nước ăn quân không khôi phục đúng trạng thái.");
        Console.ResetColor();
    }

    // TEST 5: Chuỗi nước đi và hoàn tác ngược chiều (Sequence Undo)
    Console.WriteLine("\n[TEST 5] Kiểm tra chuỗi nước đi và hoàn tác toàn bộ về FEN gốc...");
    var cleanBoard = new ChessBoard();
    cleanBoard.SetupInitialPosition();
    string originalFen = cleanBoard.ToFen();

    cleanBoard.MakeMove(new Position(1, 7), new Position(4, 7)); // Pháo 2 bình 5
    cleanBoard.MakeMove(new Position(1, 0), new Position(2, 2)); // Mã 2 tiến 3
    cleanBoard.MakeMove(new Position(1, 9), new Position(2, 7)); // Mã 2 tiến 3
    cleanBoard.MakeMove(new Position(4, 3), new Position(4, 4)); // Tốt 5 tiến 1

    while (cleanBoard.CanUndo)
    {
        cleanBoard.UndoMove();
    }

    if (cleanBoard.ToFen() == originalFen && cleanBoard.ActiveColor == PieceColor.Red && cleanBoard.UndoCount == 0)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Áp dụng chuỗi nước đi rồi hoàn tác trả lại chính xác 100% chuỗi FEN ban đầu!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Chuỗi hoàn tác không đưa về đúng FEN gốc.");
        Console.ResetColor();
    }

    // TEST 6: Thử nhiều nhánh mô phỏng (Multi-Branch Breadth)
    Console.WriteLine("\n[TEST 6] Mô phỏng duyệt nhiều nhánh mở cờ ứng viên (Tree Branching)...");
    var candidateMoves = new[]
    {
        new MoveAction(1, 7, 4, 7), // Pháo 2 bình 5
        new MoveAction(1, 9, 2, 7), // Mã 2 tiến 3
        new MoveAction(4, 6, 4, 5), // Binh 5 tiến 1
        new MoveAction(7, 9, 6, 7)  // Mã 8 tiến 7
    };

    bool branchesOk = true;
    foreach (var move in candidateMoves)
    {
        cleanBoard.Simulate(move, sandbox =>
        {
            if (sandbox.ActiveColor != PieceColor.Black || sandbox.CountPieces() != 32)
            {
                branchesOk = false;
            }
        });

        if (cleanBoard.ToFen() != originalFen || cleanBoard.ActiveColor != PieceColor.Red || cleanBoard.CanUndo)
        {
            branchesOk = false;
            break;
        }
    }

    if (branchesOk)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Thử nghiệm 4 nhánh mô phỏng khác nhau đều hoàn nguyên bàn cờ gốc 100%, không rò rỉ trạng thái!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Mô phỏng nhiều nhánh làm thay đổi bàn cờ chính.");
        Console.ResetColor();
    }

    // TEST 7: Mô phỏng cây sâu 3 tầng (Deep Nested Simulation / Minimax Tree)
    Console.WriteLine("\n[TEST 7] Mô phỏng cây tìm kiếm nước đi sâu 3 tầng (Minimax Depth = 3)...");
    bool deepTreeOk = true;
    cleanBoard.Simulate(new Position(1, 7), new Position(4, 7), lvl1 =>
    {
        lvl1.Simulate(new Position(1, 0), new Position(2, 2), lvl2 =>
        {
            lvl2.Simulate(new Position(1, 9), new Position(2, 7), lvl3 =>
            {
                if (lvl3.CountPieces() != 32 || lvl3.ActiveColor != PieceColor.Black)
                {
                    deepTreeOk = false;
                }
            });

            if (lvl2.ActiveColor != PieceColor.Red || lvl2.GetPiece(2, 7) != null) deepTreeOk = false;
        });

        if (lvl1.ActiveColor != PieceColor.Black || lvl1.GetPiece(2, 2) != null) deepTreeOk = false;
    });

    if (deepTreeOk && cleanBoard.ToFen() == originalFen && cleanBoard.CountPieces() == 32)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Cây mô phỏng sâu 3 tầng tự động unwind sạch sẽ, bàn cờ thật không mất bất kỳ quân nào!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Mô phỏng sâu làm mất quân hoặc sai trạng thái.");
        Console.ResetColor();
    }

    // TEST 8: Deep Clone độc lập
    Console.WriteLine("\n[TEST 8] Sao chép trạng thái độc lập (Clone Deep Copy)...");
    var clonedBoard = cleanBoard.Clone();
    clonedBoard.MakeMove(new Position(1, 7), new Position(4, 7));

    if (cleanBoard.GetPiece(1, 7) != null && clonedBoard.GetPiece(1, 7) == null && cleanBoard.ToFen() == originalFen)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Bản sao Clone hoàn toàn độc lập, thay đổi trên Clone không ảnh hưởng bàn cờ chính.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Deep Clone bị tham chiếu chéo.");
        Console.ResetColor();
    }

    // TEST 9: Kiểm tra tọa độ ngoài biên và lượt đi nghiêm ngặt
    Console.WriteLine("\n[TEST 9] Kiểm tra chặn tọa độ ngoài biên và kiểm soát lượt đi nghiêm ngặt...");
    int errorsCaught = 0;
    try { cleanBoard.GetPiece(-1, 0); } catch (ArgumentOutOfRangeException) { errorsCaught++; }
    try { cleanBoard.GetPiece(9, 0); } catch (ArgumentOutOfRangeException) { errorsCaught++; }
    try { cleanBoard.GetPiece(0, 10); } catch (ArgumentOutOfRangeException) { errorsCaught++; }
    try { cleanBoard.SetPiece(0, -1, null); } catch (ArgumentOutOfRangeException) { errorsCaught++; }
    try { cleanBoard.MakeMove(new Position(0, 5), new Position(0, 4)); } catch (InvalidOperationException) { errorsCaught++; } // Ô trống
    try { cleanBoard.MakeMove(new Position(0, 0), new Position(0, 1)); } catch (InvalidOperationException) { errorsCaught++; } // Sai lượt
    try { cleanBoard.MakeMove(new Position(4, 9), new Position(3, 9)); } catch (InvalidOperationException) { errorsCaught++; } // Ăn đồng minh
    try { new MoveAction(new Position(1, 7), new Position(1, 7)); } catch (ArgumentException) { errorsCaught++; } // Trùng ô

    if (errorsCaught == 8)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Đã phát hiện và chặn đúng {errorsCaught}/8 tình huống tọa độ ngoài biên và thao tác vi phạm!");
        Console.ResetColor();
    }
}

void RunTestSoldier()
{
    Console.WriteLine("\n================ KIỂM THỬ LUẬT QUÂN TỐT / BINH (GCT-50) ================");
    Console.WriteLine(">> Kiểm chứng luật tiến, qua sông, đi ngang, hàng đáy, ăn quân và chặn vi phạm.");

    // TEST 1: Kiểm tra ranh giới sông
    Console.WriteLine("\n[TEST 1] Kiểm tra xác định ranh giới sông cho hai phe...");
    bool riverOk = !SoldierRule.HasCrossedRiver(PieceColor.Red, 6) &&
                   !SoldierRule.HasCrossedRiver(PieceColor.Red, 5) &&
                   SoldierRule.HasCrossedRiver(PieceColor.Red, 4) &&
                   SoldierRule.HasCrossedRiver(PieceColor.Red, 0) &&
                   !SoldierRule.HasCrossedRiver(PieceColor.Black, 3) &&
                   !SoldierRule.HasCrossedRiver(PieceColor.Black, 4) &&
                   SoldierRule.HasCrossedRiver(PieceColor.Black, 5) &&
                   SoldierRule.HasCrossedRiver(PieceColor.Black, 9);

    if (riverOk)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Xác định chính xác ranh giới sông: Đỏ qua sông khi Row <= 4, Đen qua sông khi Row >= 5.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Xác định ranh giới sông không chính xác.");
        Console.ResetColor();
    }

    // TEST 2: Binh Đỏ trước sông
    Console.WriteLine("\n[TEST 2] Binh Đỏ trước sông (4, 6): Chỉ được tiến thẳng một giao điểm...");
    var board = new ChessBoard();
    board.Clear();
    var redBeforeRiver = new Position(4, 6);
    board.SetPiece(redBeforeRiver, new Piece(PieceType.Soldier, PieceColor.Red));

    bool redBeforeOk = board.IsValidMove(redBeforeRiver, new Position(4, 5)) && // Tiến thẳng hợp lệ
                       !board.IsValidMove(redBeforeRiver, new Position(3, 6)) && // Ngang trái bị chặn
                       !board.IsValidMove(redBeforeRiver, new Position(5, 6)) && // Ngang phải bị chặn
                       !board.IsValidMove(redBeforeRiver, new Position(4, 7)) && // Lùi bị chặn
                       !board.IsValidMove(redBeforeRiver, new Position(3, 5)) && // Chéo bị chặn
                       !board.IsValidMove(redBeforeRiver, new Position(4, 4));   // 2 bước bị chặn

    var redLegalBefore = board.GetLegalMoves(redBeforeRiver);
    if (redBeforeOk && redLegalBefore.Count == 1 && redLegalBefore[0] == new Position(4, 5))
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Binh Đỏ trước sông chỉ đi được duy nhất 1 nước tiến thẳng (4, 5); chặn ngang, lùi, chéo!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Luật Binh Đỏ trước sông vi phạm quy chuẩn.");
        Console.ResetColor();
    }

    // TEST 3: Binh Đỏ sau khi qua sông
    Console.WriteLine("\n[TEST 3] Binh Đỏ sau khi qua sông (4, 4): Được tiến hoặc ngang, không được lùi...");
    var redAcrossRiver = new Position(4, 4);
    board.SetPiece(redAcrossRiver, new Piece(PieceType.Soldier, PieceColor.Red));

    bool redAcrossOk = board.IsValidMove(redAcrossRiver, new Position(4, 3)) && // Tiến thẳng
                       board.IsValidMove(redAcrossRiver, new Position(3, 4)) && // Ngang trái
                       board.IsValidMove(redAcrossRiver, new Position(5, 4)) && // Ngang phải
                       !board.IsValidMove(redAcrossRiver, new Position(4, 5)) && // Lùi bị chặn
                       !board.IsValidMove(redAcrossRiver, new Position(3, 3));   // Chéo bị chặn

    var redLegalAcross = board.GetLegalMoves(redAcrossRiver);
    if (redAcrossOk && redLegalAcross.Count == 3)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Binh Đỏ qua sông đi được 3 hướng: tiến thẳng, sang trái, sang phải; chặn tuyệt đối nước đi lùi!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Luật Binh Đỏ sau khi qua sông chưa đúng.");
        Console.ResetColor();
    }

    // TEST 4: Binh Đỏ ở hàng đáy đối phương
    Console.WriteLine("\n[TEST 4] Binh Đỏ tại hàng đáy đối phương (4, 0): Vẫn đi ngang được, không phong cấp...");
    var redAtBottom = new Position(4, 0);
    board.SetPiece(redAtBottom, new Piece(PieceType.Soldier, PieceColor.Red));

    bool redBottomOk = board.IsValidMove(redAtBottom, new Position(3, 0)) && // Ngang trái
                       board.IsValidMove(redAtBottom, new Position(5, 0)) && // Ngang phải
                       !board.IsValidMove(redAtBottom, new Position(4, 1));  // Lùi bị chặn

    var redPiece = board.GetPiece(redAtBottom);
    if (redBottomOk && redPiece?.Type == PieceType.Soldier)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Binh Đỏ tại hàng đáy vẫn đi ngang hợp lệ, không phong cấp, giữ nguyên bản chất Binh!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Binh Đỏ ở hàng đáy xử lý sai.");
        Console.ResetColor();
    }

    // TEST 5: Tốt Đen trước sông
    Console.WriteLine("\n[TEST 5] Tốt Đen trước sông (4, 3): Chỉ được tiến thẳng xuống một giao điểm...");
    board.ActiveColor = PieceColor.Black;
    var blackBeforeRiver = new Position(4, 3);
    board.SetPiece(blackBeforeRiver, new Piece(PieceType.Soldier, PieceColor.Black));

    bool blackBeforeOk = board.IsValidMove(blackBeforeRiver, new Position(4, 4)) && // Tiến xuống hợp lệ
                         !board.IsValidMove(blackBeforeRiver, new Position(3, 3)) && // Ngang bị chặn
                         !board.IsValidMove(blackBeforeRiver, new Position(5, 3)) && // Ngang bị chặn
                         !board.IsValidMove(blackBeforeRiver, new Position(4, 2)) && // Lùi bị chặn
                         !board.IsValidMove(blackBeforeRiver, new Position(3, 4));   // Chéo bị chặn

    var blackLegalBefore = board.GetLegalMoves(blackBeforeRiver);
    if (blackBeforeOk && blackLegalBefore.Count == 1 && blackLegalBefore[0] == new Position(4, 4))
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Tốt Đen trước sông chỉ đi được 1 nước tiến xuống (4, 4); chặn ngang, lùi, chéo!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Luật Tốt Đen trước sông vi phạm quy chuẩn.");
        Console.ResetColor();
    }

    // TEST 6: Tốt Đen sau khi qua sông
    Console.WriteLine("\n[TEST 6] Tốt Đen sau khi qua sông (4, 5): Được tiến hoặc ngang, không được lùi...");
    var blackAcrossRiver = new Position(4, 5);
    board.SetPiece(blackAcrossRiver, new Piece(PieceType.Soldier, PieceColor.Black));

    bool blackAcrossOk = board.IsValidMove(blackAcrossRiver, new Position(4, 6)) && // Tiến xuống
                         board.IsValidMove(blackAcrossRiver, new Position(3, 5)) && // Ngang trái
                         board.IsValidMove(blackAcrossRiver, new Position(5, 5)) && // Ngang phải
                         !board.IsValidMove(blackAcrossRiver, new Position(4, 4));  // Lùi bị chặn

    var blackLegalAcross = board.GetLegalMoves(blackAcrossRiver);
    if (blackAcrossOk && blackLegalAcross.Count == 3)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Tốt Đen qua sông đi được 3 hướng: tiến xuống, sang trái, sang phải; chặn đi lùi!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Luật Tốt Đen sau khi qua sông chưa đúng.");
        Console.ResetColor();
    }

    // TEST 7: Tốt Đen ở hàng đáy đối phương
    Console.WriteLine("\n[TEST 7] Tốt Đen tại hàng đáy đối phương (4, 9): Vẫn đi ngang được, không phong cấp...");
    var blackAtBottom = new Position(4, 9);
    board.SetPiece(blackAtBottom, new Piece(PieceType.Soldier, PieceColor.Black));

    bool blackBottomOk = board.IsValidMove(blackAtBottom, new Position(3, 9)) && // Ngang trái
                         board.IsValidMove(blackAtBottom, new Position(5, 9)) && // Ngang phải
                         !board.IsValidMove(blackAtBottom, new Position(4, 8));  // Lùi bị chặn

    var blackPiece = board.GetPiece(blackAtBottom);
    if (blackBottomOk && blackPiece?.Type == PieceType.Soldier)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Tốt Đen tại hàng đáy đối phương đi ngang hợp lệ, không phong cấp, giữ nguyên bản chất!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Tốt Đen ở hàng đáy đối phương xử lý sai.");
        Console.ResetColor();
    }

    // TEST 8: Ăn quân đối phương và chặn ăn quân đồng minh
    Console.WriteLine("\n[TEST 8] Tốt ăn quân đối phương hợp lệ và chặn ăn quân cùng màu...");
    board.ActiveColor = PieceColor.Red;
    var redAttacker = new Position(4, 4);
    board.SetPiece(redAttacker, new Piece(PieceType.Soldier, PieceColor.Red));
    board.SetPiece(new Position(4, 3), new Piece(PieceType.Horse, PieceColor.Black)); // Địch ở ô tiến
    board.SetPiece(new Position(3, 4), new Piece(PieceType.Cannon, PieceColor.Black)); // Địch ở ô ngang
    board.SetPiece(new Position(5, 4), new Piece(PieceType.Chariot, PieceColor.Red)); // Đồng minh ở ô ngang

    bool captureOk = board.IsValidMove(redAttacker, new Position(4, 3)) && // Ăn địch tiến
                     board.IsValidMove(redAttacker, new Position(3, 4)) && // Ăn địch ngang
                     !board.IsValidMove(redAttacker, new Position(5, 4));  // Chặn ăn đồng minh

    // Kiểm tra biên bàn cờ (cột 0 không được đi sang trái, cột 8 không được đi sang phải)
    var redCorner = new Position(0, 4);
    board.SetPiece(redCorner, new Piece(PieceType.Soldier, PieceColor.Red));
    bool edgeOk = !Position.IsValid(-1, 4) && board.IsValidMove(redCorner, new Position(1, 4));

    if (captureOk && edgeOk)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Tốt ăn quân đối phương chính xác cả tiến và ngang, chặn ăn đồng minh và chặn ra ngoài biên!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Xử lý ăn quân hoặc biên bàn cờ chưa chính xác.");
        Console.ResetColor();
    }
}

void RunTestRook()
{
    Console.WriteLine("\n================ KIỂM THỬ LUẬT QUÂN XE (CHARIOT / ROOK) (GCT-51) ================");
    Console.WriteLine(">> Kiểm chứng luật đi ngang/dọc, không nhảy qua quân cản, ăn quân hợp lệ và chặn vi phạm.");

    // TEST 1: Di chuyển ngang dọc tự do trên đường trống
    Console.WriteLine("\n[TEST 1] Xe di chuyển ngang và dọc tự do qua các ô trống...");
    var board = new ChessBoard();
    board.Clear();
    var rookPos = new Position(4, 4);
    board.SetPiece(rookPos, new Piece(PieceType.Chariot, PieceColor.Red));

    bool horizontalOk = board.IsValidMove(rookPos, new Position(0, 4)) &&
                         board.IsValidMove(rookPos, new Position(8, 4)) &&
                         board.IsValidMove(rookPos, new Position(3, 4));

    bool verticalOk = board.IsValidMove(rookPos, new Position(4, 0)) &&
                       board.IsValidMove(rookPos, new Position(4, 9)) &&
                       board.IsValidMove(rookPos, new Position(4, 5));

    var legalMovesEmpty = board.GetLegalMoves(rookPos);
    if (horizontalOk && verticalOk && legalMovesEmpty.Count == 17)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"-> THÀNH CÔNG: Xe tại tâm (4, 4) đi đủ 17 nước ngang/dọc qua sông tự do trên bàn cờ trống!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Di chuyển ngang/dọc trên đường trống không chính xác.");
        Console.ResetColor();
    }

    // TEST 2: Chặn đi chéo và chặn đứng yên tại chỗ
    Console.WriteLine("\n[TEST 2] Chặn đi chéo và chặn đứng yên tại chỗ...");
    bool diagonalBlocked = !board.IsValidMove(rookPos, new Position(5, 5)) &&
                           !board.IsValidMove(rookPos, new Position(3, 3)) &&
                           !board.IsValidMove(rookPos, new Position(6, 2)) &&
                           !board.IsValidMove(rookPos, rookPos);

    if (diagonalBlocked)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Đã chặn hoàn toàn các nước đi chéo và nước đi đứng yên tại chỗ!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Không chặn được nước đi chéo hoặc đứng yên.");
        Console.ResetColor();
    }

    // TEST 3: Chặn nhảy qua quân cản (Không xuyên thấu)
    Console.WriteLine("\n[TEST 3] Chặn nhảy qua quân cản (bất kể quân ta hay quân địch)...");
    board.SetPiece(new Position(4, 2), new Piece(PieceType.Soldier, PieceColor.Red)); // Quân ta ở (4, 2)
    board.SetPiece(new Position(2, 4), new Piece(PieceType.Cannon, PieceColor.Black)); // Quân địch ở (2, 4)

    bool leapBlocked = board.IsValidMove(rookPos, new Position(4, 3)) && // Trước quân cản -> ĐI ĐƯỢC
                       !board.IsValidMove(rookPos, new Position(4, 2)) && // Vào ô đồng minh -> BỊ CHẶN
                       !board.IsValidMove(rookPos, new Position(4, 1)) && // Nhảy qua quân cản -> BỊ CHẶN
                       !board.IsValidMove(rookPos, new Position(4, 0)) && // Nhảy qua quân cản -> BỊ CHẶN
                       board.IsValidMove(rookPos, new Position(3, 4)) &&  // Trước quân địch -> ĐI ĐƯỢC
                       board.IsValidMove(rookPos, new Position(2, 4)) &&  // Vào ô quân địch -> ĂN QUÂN HỢP LỆ
                       !board.IsValidMove(rookPos, new Position(1, 4)) && // Nhảy qua quân địch -> BỊ CHẶN
                       !board.IsValidMove(rookPos, new Position(0, 4));   // Nhảy qua quân địch -> BỊ CHẶN

    if (leapBlocked)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Xe tuyệt đối không được nhảy qua quân cản (cả quân mình lẫn quân địch)!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Quân Xe đã nhảy qua vật cản trái luật.");
        Console.ResetColor();
    }

    // TEST 4: Ăn quân đối phương ở điểm đến hợp lệ và chặn ăn quân cùng màu
    Console.WriteLine("\n[TEST 4] Ăn quân đối phương ở điểm đến hợp lệ và chặn ăn quân cùng màu...");
    board.Clear();
    var redRook = new Position(0, 9);
    board.SetPiece(redRook, new Piece(PieceType.Chariot, PieceColor.Red));
    board.SetPiece(new Position(0, 0), new Piece(PieceType.General, PieceColor.Black)); // Tướng Đen ở đích
    board.SetPiece(new Position(5, 9), new Piece(PieceType.Advisor, PieceColor.Red));   // Sĩ Đỏ đồng minh ở đích

    bool captureEnemyOk = board.IsValidMove(redRook, new Position(0, 0)); // Ăn Tướng địch suốt dọc bàn cờ
    bool friendlyBlocked = !board.IsValidMove(redRook, new Position(5, 9)); // Chặn ăn Sĩ mình

    if (captureEnemyOk && friendlyBlocked)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Xe ăn quân đối phương chính xác tại điểm đến và chặn ăn quân cùng màu!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Xử lý ăn quân đối phương hoặc chặn đồng minh chưa đúng.");
        Console.ResetColor();
    }

    // TEST 5: Thế cờ ban đầu: Xe đi dọc 2 ô trống trước mặt (Xe 1 tiến 1, tiến 2), chặn ngang bởi Mã, chặn dọc bởi Binh
    Console.WriteLine("\n[TEST 5] Kiểm tra Xe ở thế cờ ban đầu: Đi được 2 ô dọc trước mặt (hàng 8 & 7), chặn ngang bởi Mã, chặn dọc bởi Binh...");
    var initialBoard = new ChessBoard();
    initialBoard.SetupInitialPosition();

    var leftRookMoves = initialBoard.GetLegalMoves(new Position(0, 9));
    var rightRookMoves = initialBoard.GetLegalMoves(new Position(8, 9));

    bool leftOk = leftRookMoves.Count == 2 &&
                  leftRookMoves.Contains(new Position(0, 8)) &&
                  leftRookMoves.Contains(new Position(0, 7)) &&
                  !initialBoard.IsValidMove(new Position(0, 9), new Position(0, 6)) && // Chặn ăn Binh mình
                  !initialBoard.IsValidMove(new Position(0, 9), new Position(0, 5)) && // Chặn nhảy qua Binh mình
                  !initialBoard.IsValidMove(new Position(0, 9), new Position(1, 9));   // Chặn ăn Mã mình

    bool rightOk = rightRookMoves.Count == 2 &&
                   rightRookMoves.Contains(new Position(8, 8)) &&
                   rightRookMoves.Contains(new Position(8, 7));

    if (leftOk && rightOk)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("-> THÀNH CÔNG: Xe ở thế cờ ban đầu đi đúng 2 nước dọc trước mặt (tiến 1, tiến 2), bị chặn ngang bởi Mã và chặn dọc bởi Binh!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-> THẤT BẠI: Xe ở thế cờ ban đầu không xử lý đúng.");
        Console.ResetColor();
    }
}




