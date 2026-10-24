using System.Reflection;
using System.Security.Claims;
using GameCoTuong.DTOs;
using GameCoTuong.Entities;
using GameCoTuong.Middleware;
using GameCoTuong.Services;
using GameCoTuong.Tests.Helpers;
using Xunit;

namespace GameCoTuong.Tests;

public class SecurityAndPrivacyTests
{
    [Theory]
    [InlineData(typeof(UserResponse))]
    [InlineData(typeof(UserProfileResponse))]
    [InlineData(typeof(LoginResult))]
    [InlineData(typeof(RegisterResult))]
    public void ResponseDtos_DoNotExposePasswordOrPasswordHash(Type dtoType)
    {
        // Act: Lấy danh sách toàn bộ property của DTO
        var propertyNames = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        // Assert: Không được chứa bất kỳ trường nào liên quan đến mật khẩu thô hay mã băm
        Assert.DoesNotContain("password", propertyNames);
        Assert.DoesNotContain("passwordhash", propertyNames);
        Assert.DoesNotContain("rawpassword", propertyNames);
    }

    [Fact]
    public void LogSanitizer_MasksBearerAndQueryTokens_InLogs()
    {
        // Arrange
        var fakeJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIn0.secret_signature_12345";
        var rawLogMessage = $"Request URL: /hubs/game?access_token={fakeJwt} | Authorization: Bearer {fakeJwt}";

        // Act
        var sanitized = LogSanitizer.Sanitize(rawLogMessage);

        // Assert
        Assert.DoesNotContain(fakeJwt, sanitized);
        Assert.Contains("Bearer [REDACTED]", sanitized);
        Assert.Contains("access_token=[REDACTED]", sanitized);
    }

    [Fact]
    public async Task Session_InGameLogout_RequiresSurrenderConfirmation()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var userService = new UserService(context);

        var user = new User { UserId = 1, Username = "player1", Email = "p1@example.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.SaveChanges();

        var room = new Room { RoomId = 1, RoomName = "Room 1", IsActive = true };
        context.Rooms.Add(room);
        context.SaveChanges();

        var match = new Match { MatchId = 1, RoomId = 1, MatchType = "Ranked", Result = "InProgress" };
        context.Matches.Add(match);
        context.SaveChanges();

        context.MatchPlayers.Add(new MatchPlayer { MatchId = 1, UserId = 1, Side = "Red" });
        context.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "player1") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act: Đăng xuất khi chưa xác nhận đầu hàng (ConfirmSurrender = false)
        var result = await userService.LogoutAsync(principal, new LogoutRequest { ConfirmSurrender = false });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.RequiresSurrenderConfirmation);
        Assert.False(result.DeleteClientToken);
        Assert.Contains("đầu hàng", result.Message);
    }

    [Fact]
    public async Task Session_CancelSurrender_KeepsMatchInProgress()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var userService = new UserService(context);

        var user = new User { UserId = 1, Username = "player1", Email = "p1@example.com", PasswordHash = "hash" };
        context.Users.Add(user);
        context.SaveChanges();

        var match = new Match { MatchId = 1, RoomId = 1, MatchType = "Ranked", Result = "InProgress" };
        context.Matches.Add(match);
        context.SaveChanges();

        context.MatchPlayers.Add(new MatchPlayer { MatchId = 1, UserId = 1, Side = "Red" });
        context.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "player1") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act: Người chơi hủy đầu hàng
        var cancelResult = await userService.CancelSurrenderAsync(principal);

        // Assert
        Assert.True(cancelResult.IsAuthorized);

        var matchInDb = context.Matches.First(m => m.MatchId == 1);
        Assert.Equal("InProgress", matchInDb.Result);
        Assert.Null(matchInDb.EndTime);
    }

    [Fact]
    public async Task Session_ConfirmSurrender_EndsMatchAndSetsOpponentWin()
    {
        // Arrange
        using var context = TestDbHelper.CreateIsolatedDbContext();
        var userService = new UserService(context);

        var player1 = new User { UserId = 1, Username = "player1", Email = "p1@example.com", PasswordHash = "hash" };
        var player2 = new User { UserId = 2, Username = "player2", Email = "p2@example.com", PasswordHash = "hash" };
        context.Users.AddRange(player1, player2);
        context.SaveChanges();

        var room = new Room { RoomId = 1, RoomName = "Room 1", IsActive = true };
        context.Rooms.Add(room);
        context.SaveChanges();

        context.RoomMembers.Add(new RoomMember { RoomId = 1, UserId = 1, JoinedAt = DateTime.UtcNow });
        context.SaveChanges();

        var match = new Match { MatchId = 1, RoomId = 1, MatchType = "Ranked", Result = "InProgress" };
        context.Matches.Add(match);
        context.SaveChanges();

        context.MatchPlayers.AddRange(
            new MatchPlayer { MatchId = 1, UserId = 1, Side = "Red" },
            new MatchPlayer { MatchId = 1, UserId = 2, Side = "Black" }
        );
        context.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "player1") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        // Act: Người chơi xác nhận đầu hàng khi đăng xuất
        var logoutResult = await userService.LogoutAsync(principal, new LogoutRequest { ConfirmSurrender = true });

        // Assert
        Assert.True(logoutResult.IsSuccess);
        Assert.True(logoutResult.DeleteClientToken);
        Assert.True(logoutResult.RedirectToLogin);

        var matchInDb = context.Matches.First(m => m.MatchId == 1);
        Assert.Equal("BlackWin", matchInDb.Result); // Player 1 (Red) đầu hàng -> BlackWin
        Assert.NotNull(matchInDb.EndTime);

        // Player 1 bị xóa khỏi RoomMembers
        Assert.False(context.RoomMembers.Any(rm => rm.RoomId == 1 && rm.UserId == 1));
    }
}
