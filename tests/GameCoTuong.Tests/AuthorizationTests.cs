using System.Security.Claims;
using GameCoTuong.Data;
using GameCoTuong.DTOs;
using GameCoTuong.Entities;
using GameCoTuong.Services;
using GameCoTuong.Tests.Helpers;
using Xunit;

namespace GameCoTuong.Tests;

public class AuthorizationTests
{
    private static (CoTuongDbContext context, Room room, Match match, User player1, User player2, User outsider, User spectator) SetupRoomScenario()
    {
        var context = TestDbHelper.CreateIsolatedDbContext();

        var player1 = new User { UserId = 1, Username = "player1", Email = "p1@example.com", PasswordHash = "hash" };
        var player2 = new User { UserId = 2, Username = "player2", Email = "p2@example.com", PasswordHash = "hash" };
        var outsider = new User { UserId = 3, Username = "outsider", Email = "out@example.com", PasswordHash = "hash" };
        var spectator = new User { UserId = 4, Username = "spectator", Email = "spec@example.com", PasswordHash = "hash" };

        context.Users.AddRange(player1, player2, outsider, spectator);
        context.SaveChanges();

        var room = new Room { RoomId = 1, RoomName = "Phòng Thi Đấu", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Rooms.Add(room);
        context.SaveChanges();

        // Player 1, Player 2 và Spectator nằm trong phòng
        context.RoomMembers.AddRange(
            new RoomMember { RoomId = room.RoomId, UserId = player1.UserId, JoinedAt = DateTime.UtcNow },
            new RoomMember { RoomId = room.RoomId, UserId = player2.UserId, JoinedAt = DateTime.UtcNow },
            new RoomMember { RoomId = room.RoomId, UserId = spectator.UserId, JoinedAt = DateTime.UtcNow }
        );

        var match = new Match
        {
            MatchId = 1,
            RoomId = room.RoomId,
            MatchType = "Ranked",
            InitialBoardState = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
            Result = "InProgress",
            StartTime = DateTime.UtcNow
        };
        context.Matches.Add(match);
        context.SaveChanges();

        // Chỉ Player 1 và Player 2 là đấu thủ
        context.MatchPlayers.AddRange(
            new MatchPlayer { MatchId = match.MatchId, UserId = player1.UserId, Side = "Red" },
            new MatchPlayer { MatchId = match.MatchId, UserId = player2.UserId, Side = "Black" }
        );
        context.SaveChanges();

        return (context, room, match, player1, player2, outsider, spectator);
    }

    [Fact]
    public async Task Outsider_AttemptingActions_IsForbidden()
    {
        // Arrange
        var (context, room, match, _, _, outsider, _) = SetupRoomScenario();
        using (context)
        {
            var authzService = new RoomAuthorizationService(context);

            // Act
            var startResult = await authzService.AuthorizeStartMatchAsync(room.RoomId, outsider.UserId);
            var moveResult = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, outsider.UserId);
            var closeResult = await authzService.AuthorizeCloseRoomAsync(room.RoomId, outsider.UserId);
            var videoResult = await authzService.AuthorizeVideoCallAsync(room.RoomId, outsider.UserId);

            // Assert
            Assert.False(startResult.IsAuthorized);
            Assert.Equal(403, startResult.StatusCode);

            Assert.False(moveResult.IsAuthorized);
            Assert.Equal(403, moveResult.StatusCode);

            Assert.False(closeResult.IsAuthorized);
            Assert.Equal(403, closeResult.StatusCode);

            Assert.False(videoResult.IsAuthorized);
            Assert.Equal(403, videoResult.StatusCode);
        }
    }

    [Fact]
    public async Task Spectator_AttemptingToMakeMove_IsForbidden()
    {
        // Arrange
        var (context, room, match, _, _, _, spectator) = SetupRoomScenario();
        using (context)
        {
            var authzService = new RoomAuthorizationService(context);

            // Act
            var moveResult = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, spectator.UserId);

            // Assert
            Assert.False(moveResult.IsAuthorized);
            Assert.Contains("khán giả", moveResult.Message);
        }
    }

    [Fact]
    public async Task Player_MakingMoveAndVideoCall_IsAuthorized()
    {
        // Arrange
        var (context, room, match, player1, _, _, _) = SetupRoomScenario();
        using (context)
        {
            var authzService = new RoomAuthorizationService(context);

            // Act
            var moveResult = await authzService.AuthorizeMakeMoveAsync(room.RoomId, match.MatchId, player1.UserId);
            var videoResult = await authzService.AuthorizeVideoCallAsync(room.RoomId, player1.UserId);

            // Assert
            Assert.True(moveResult.IsAuthorized);
            Assert.True(videoResult.IsAuthorized);
        }
    }

    [Fact]
    public void Impersonation_SendingDifferentClaimedUserId_IsForbidden()
    {
        // Arrange
        var (context, _, _, _, _, _, _) = SetupRoomScenario();
        using (context)
        {
            var authzService = new RoomAuthorizationService(context);

            // Giả lập Principal có UserId = 1
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            // Act: Cố tình gửi claimedUserId = 2
            var result = authzService.VerifyNoImpersonation(principal, claimedUserId: 2);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("giả mạo", result.Message);
        }
    }
}
