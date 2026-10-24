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
var tokenBlacklistService = new TokenBlacklistService();
var jwtService = new JwtService(configuration, tokenBlacklistService);

// Helper tạo DbContext mới cho mỗi test
CoTuongDbContext CreateContext() => new(optionsBuilder.Options);

try
{
    var arg = args.FirstOrDefault()?.ToLowerInvariant();

    if (arg == "--test-session")
    {
        Console.WriteLine("\n================ KIỂM THỬ HỒ SƠ & CHÍNH SÁCH PHIÊN / ĐĂNG XUẤT (GCT-30) ================");

        // Đảm bảo dữ liệu demo sẵn sàng trong DB
        using (var setupCtx = CreateContext())
        {
            await DbInitializer.SeedAsync(setupCtx);

            var userRed = await setupCtx.Users.FirstAsync(u => u.Username == "player_red_demo");
            var userBlack = await setupCtx.Users.FirstAsync(u => u.Username == "player_black_demo");
            var room = await setupCtx.Rooms.FirstAsync();

            // Đảm bảo cả 2 là thành viên phòng
            if (!await setupCtx.RoomMembers.AnyAsync(rm => rm.RoomId == room.RoomId && rm.UserId == userRed.UserId))
            {
                setupCtx.RoomMembers.Add(new RoomMember { RoomId = room.RoomId, UserId = userRed.UserId, JoinedAt = DateTime.UtcNow });
            }
            if (!await setupCtx.RoomMembers.AnyAsync(rm => rm.RoomId == room.RoomId && rm.UserId == userBlack.UserId))
            {
                setupCtx.RoomMembers.Add(new RoomMember { RoomId = room.RoomId, UserId = userBlack.UserId, JoinedAt = DateTime.UtcNow });
            }

            // Đảm bảo có ván cờ InProgress
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

            // TEST 1: Lấy thông tin hồ sơ theo danh tính phiên
            Console.WriteLine("\n[TEST 1] Lấy hồ sơ người dùng theo danh tính phiên xác thực...");
            var profile = await userService.GetProfileAsync(principalRed!);
            if (profile != null && profile.Username == "player_red_demo")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Lấy hồ sơ cá nhân thành công:");
                Console.WriteLine($"   * UserId: {profile.UserId}");
                Console.WriteLine($"   * Tên đăng nhập: {profile.Username}");
                Console.WriteLine($"   * Email: {profile.Email}");
                Console.WriteLine($"   * Tên hiển thị: {profile.DisplayName}");
                Console.WriteLine($"   * Đang trong trận cờ: {(profile.IsInActiveMatch ? "Có" : "Không")}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Không lấy được hồ sơ cá nhân!");
                Console.ResetColor();
            }

            // TEST 2: Chặn đổi hồ sơ bằng mã người dùng tùy ý (Impersonation Attack)
            Console.WriteLine("\n[TEST 2] Chặn đổi hồ sơ bằng mã người dùng tùy ý...");
            var attackRequest = new UpdateProfileRequest
            {
                DisplayName = "Hacker Thay Đổi Tên",
                ClaimedUserId = userBlack.UserId // Cố tình truyền ID của user khác
            };
            var attackResult = await userService.UpdateProfileAsync(principalRed!, attackRequest);
            if (!attackResult.IsAuthorized && attackResult.StatusCode == 403)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Đã chặn hành vi đổi hồ sơ người khác: {attackResult.Message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Cho phép đổi hồ sơ bằng mã người dùng tùy ý!");
                Console.ResetColor();
            }

            // TEST 3: Đang chơi cờ yêu cầu xác nhận đầu hàng khi đăng xuất
            Console.WriteLine("\n[TEST 3] Đang chơi cờ: Yêu cầu xác nhận đầu hàng khi đăng xuất...");
            var logoutAttemptWithoutConfirm = await userService.LogoutAsync(principalRed!, new LogoutRequest { ConfirmSurrender = false }, tokenRed);
            if (!logoutAttemptWithoutConfirm.IsSuccess && logoutAttemptWithoutConfirm.RequiresSurrenderConfirmation)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đã chặn đăng xuất và yêu cầu xác nhận đầu hàng:");
                Console.WriteLine($"   * Cảnh báo: {logoutAttemptWithoutConfirm.Message}");
                Console.WriteLine($"   * DeleteClientToken = {logoutAttemptWithoutConfirm.DeleteClientToken}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đã cho phép đăng xuất khi chưa xác nhận đầu hàng!");
                Console.ResetColor();
            }

            // TEST 4: Hủy đầu hàng giữ trận
            Console.WriteLine("\n[TEST 4] Hủy đầu hàng giữ trận...");
            var cancelResult = await userService.CancelSurrenderAsync(principalRed!);
            var checkMatchStillActive = await ctx.Matches.AnyAsync(m => m.Result == "InProgress" && m.EndTime == null);
            if (cancelResult.IsAuthorized && checkMatchStillActive)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Hủy đầu hàng giữ nguyên trận đấu:");
                Console.WriteLine($"   * Thông báo: {cancelResult.Message}");
                Console.WriteLine($"   * Trạng thái trận cờ trong DB: InProgress");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Trận đấu bị hủy hoặc không giữ nguyên!");
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
                Console.WriteLine("-> THÀNH CÔNG: Đăng xuất và xử lý đầu hàng đúng luồng:");
                Console.WriteLine($"   * Thông báo: {logoutConfirmed.Message}");
                Console.WriteLine($"   * Kết quả trận cờ: {matchUpdated.Result} ({matchUpdated.Reason})");
                Console.WriteLine($"   * Rời phòng: User đã được xóa khỏi RoomMembers");
                Console.WriteLine($"   * Chỉ dẫn Client: DeleteClientToken = {logoutConfirmed.DeleteClientToken}, RedirectToLogin = {logoutConfirmed.RedirectToLogin}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đăng xuất hoặc xử lý đầu hàng không đúng!");
                Console.ResetColor();
            }

            // TEST 6: Kiểm tra chính sách JWT và thu hồi phiên
            Console.WriteLine("\n[TEST 6] Kiểm tra chính sách JWT và thu hồi phiên đăng nhập...");
            Console.WriteLine($"   * Ghi chú chính sách: {logoutConfirmed.JwtPolicyNotice}");
            var isTokenStillValid = jwtService.ValidateToken(tokenRed, out _, out var tokenRevokeErr);
            if (!isTokenStillValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Token cũ đã bị vô hiệu hóa ngay sau khi đăng xuất: {tokenRevokeErr}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Token cũ vẫn còn hợp lệ!");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n================ HOÀN TẤT TẤT CẢ KIỂM THỬ (GCT-30) ================\n");
        return;
    }

    if (arg == "--test-authz")
    {
        Console.WriteLine("\n================ KIỂM THỬ PHÂN QUYỀN API & PHÒNG CỜ (GCT-29) ================");

        // Đảm bảo dữ liệu demo sẵn sàng trong DB
        using (var setupCtx = CreateContext())
        {
            await DbInitializer.SeedAsync(setupCtx);

            // Tạo thêm 1 người dùng ngoài phòng (Outsider) và 1 khán giả (Spectator) nếu chưa có
            var outsider = await setupCtx.Users.FirstOrDefaultAsync(u => u.Username == "outsider_test_user");
            if (outsider == null)
            {
                outsider = new User
                {
                    Username = "outsider_test_user",
                    Email = "outsider@example.com",
                    PasswordHash = passwordHasher.HashPassword("TestPass@123"),
                    DisplayName = "Người Ngoài Phòng"
                };
                setupCtx.Users.Add(outsider);
                await setupCtx.SaveChangesAsync();
            }

            var spectator = await setupCtx.Users.FirstOrDefaultAsync(u => u.Username == "spectator_test_user");
            if (spectator == null)
            {
                spectator = new User
                {
                    Username = "spectator_test_user",
                    Email = "spectator@example.com",
                    PasswordHash = passwordHasher.HashPassword("TestPass@123"),
                    DisplayName = "Khán Giả Xem Cờ"
                };
                setupCtx.Users.Add(spectator);
                await setupCtx.SaveChangesAsync();
            }

            // Gán spectator vào phòng 1 nhưng KHÔNG gán vào MatchPlayers
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
            var (tokenOutsider, _) = jwtService.GenerateToken(outsider);

            jwtService.ValidateToken(tokenPlayer, out var principalPlayer, out _);
            jwtService.ValidateToken(tokenOutsider, out var principalOutsider, out _);

            // -------------------------------------------------------------
            // TEST 1: Thiếu hoặc sai Token trên API và SignalR bị chặn
            // -------------------------------------------------------------
            Console.WriteLine("\n[TEST 1] Kiểm tra thiếu hoặc sai Token...");
            var noTokenResult = jwtService.ValidateToken("", out _, out var noTokenErr);
            var badTokenResult = jwtService.ValidateToken("invalid.bearer.token", out _, out var badTokenErr);

            if (!noTokenResult && !badTokenResult)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Chặn thiếu hoặc sai Token:");
                Console.WriteLine($"   * Khi thiếu Token: {noTokenErr}");
                Console.WriteLine($"   * Khi Token sai chữ ký: {badTokenErr}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đã chấp nhận Token sai hoặc thiếu!");
                Console.ResetColor();
            }

            // -------------------------------------------------------------
            // TEST 2: Lấy danh tính từ phiên & Chặn giả mạo người dùng (Impersonation Attack)
            // -------------------------------------------------------------
            Console.WriteLine("\n[TEST 2] Lấy danh tính từ phiên & Chặn giả mạo người dùng...");
            var impersonationAttempt = authzService.VerifyNoImpersonation(principalPlayer, claimedUserId: outsider.UserId);

            if (!impersonationAttempt.IsAuthorized && impersonationAttempt.StatusCode == 403)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đã chặn hành vi giả mạo ID người dùng:");
                Console.WriteLine($"   * Thông báo: {impersonationAttempt.Message}");
                Console.WriteLine($"   * Danh tính trích xuất từ Token: UserId = {principalPlayer?.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Không phát hiện được hành vi giả mạo!");
                Console.ResetColor();
            }

            // -------------------------------------------------------------
            // TEST 3: Người ngoài phòng (Outsider) bị từ chối mọi thao tác
            // -------------------------------------------------------------
            Console.WriteLine("\n[TEST 3] Người ngoài phòng cố gắng thao tác (Đi quân, bắt đầu, đóng phòng, gọi video)...");
            var outsiderStart = await authzService.AuthorizeStartMatchAsync(room.RoomId, outsider.UserId);
            var outsiderMove = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, outsider.UserId);
            var outsiderClose = await authzService.AuthorizeCloseRoomAsync(room.RoomId, outsider.UserId);
            var outsiderVideo = await authzService.AuthorizeVideoCallAsync(room.RoomId, outsider.UserId);

            bool allOutsiderBlocked = !outsiderStart.IsAuthorized && !outsiderMove.IsAuthorized
                                   && !outsiderClose.IsAuthorized && !outsiderVideo.IsAuthorized;

            if (allOutsiderBlocked)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Toàn bộ thao tác của người ngoài phòng đều bị chặn 403 Forbidden:");
                Console.WriteLine($"   * Bắt đầu phòng: {outsiderStart.Message}");
                Console.WriteLine($"   * Đi quân: {outsiderMove.Message}");
                Console.WriteLine($"   * Đóng phòng: {outsiderClose.Message}");
                Console.WriteLine($"   * Gọi video: {outsiderVideo.Message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Người ngoài phòng đã lọt qua kiểm tra quyền!");
                Console.ResetColor();
            }

            // -------------------------------------------------------------
            // TEST 4: Khán giả trong phòng (Spectator) cố gắng đi quân
            // -------------------------------------------------------------
            Console.WriteLine("\n[TEST 4] Khán giả trong phòng cố gắng đi quân...");
            var spectatorMove = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, spectator.UserId);

            if (!spectatorMove.IsAuthorized)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"-> THÀNH CÔNG: Chặn khán giả đi quân: {spectatorMove.Message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Khán giả được phép đi quân cờ!");
                Console.ResetColor();
            }

            // -------------------------------------------------------------
            // TEST 5: Đấu thủ hợp lệ thực hiện thao tác đúng quyền
            // -------------------------------------------------------------
            Console.WriteLine("\n[TEST 5] Đấu thủ hợp lệ thực hiện thao tác đúng quyền...");
            var playerMove = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, playerRed.UserId);
            var playerVideo = await authzService.AuthorizeVideoCallAsync(room.RoomId, playerRed.UserId);

            if (playerMove.IsAuthorized && playerVideo.IsAuthorized)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Đấu thủ hợp lệ được phép đi quân và gọi video trong phòng.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Đấu thủ hợp lệ bị từ chối quyền!");
                Console.ResetColor();
            }

            // -------------------------------------------------------------
            // TEST 6: Bảo mật nhật ký (Không ghi Token vào log)
            // -------------------------------------------------------------
            Console.WriteLine("\n[TEST 6] Kiểm tra bảo mật nhật ký (Log Sanitization)...");
            var sampleLog = $"GET /hubs/game?access_token={tokenPlayer} HTTP/1.1 | Authorization: Bearer {tokenPlayer}";
            var sanitizedLog = GameCoTuong.Middleware.LogSanitizer.Sanitize(sampleLog);

            bool containsSecretToken = sanitizedLog.Contains(tokenPlayer);
            bool containsRedacted = sanitizedLog.Contains("[REDACTED]");

            if (!containsSecretToken && containsRedacted)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-> THÀNH CÔNG: Token đã được che mờ hoàn toàn trong nhật ký hệ thống:");
                Console.WriteLine($"   * Nhật ký gốc: {sampleLog[..60]}...");
                Console.WriteLine($"   * Nhật ký an toàn: {sanitizedLog}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-> THẤT BẠI: Token bị lộ vào nhật ký!");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n================ HOÀN TẤT TẤT CẢ KIỂM THỬ (GCT-29) ================\n");
        return;
    }

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
