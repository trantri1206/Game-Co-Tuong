using GameCoTuong.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameCoTuong.Data.Seed;

public static class DbInitializer
{
    /// <summary>
    /// Nạp dữ liệu mẫu (Demo/Seed Data) hoàn toàn là tài khoản ảo, không chứa tài khoản thật.
    /// </summary>
    public static async Task SeedAsync(CoTuongDbContext context)
    {
        // Đảm bảo các bảng đã sẵn sàng
        if (await context.Users.AnyAsync())
        {
            return; // Đã có dữ liệu, không nạp lại
        }

        // 1. Dữ liệu mẫu Users (Tài khoản ảo demo cho môi trường test/development)
        var userRed = new User
        {
            Username = "player_red_demo",
            Email = "player_red_demo@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPass123!"),
            DisplayName = "Kỳ Thủ Đỏ Demo",
            CreatedAt = DateTime.UtcNow
        };

        var userBlack = new User
        {
            Username = "player_black_demo",
            Email = "player_black_demo@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPass123!"),
            DisplayName = "Kỳ Thủ Đen Demo",
            CreatedAt = DateTime.UtcNow
        };

        var botAi = new User
        {
            Username = "bot_easy_demo",
            Email = "bot_easy_demo@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPass123!"),
            DisplayName = "Máy Dễ Demo",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(userRed, userBlack, botAi);
        await context.SaveChangesAsync();

        // 2. Dữ liệu mẫu Phòng chơi (Rooms)
        var room = new Room
        {
            RoomName = "Phòng Giao Hữu Mẫu #1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.Rooms.AddAsync(room);
        await context.SaveChangesAsync();

        // 3. Dữ liệu mẫu Thành viên phòng (RoomMembers)
        var member1 = new RoomMember
        {
            RoomId = room.RoomId,
            UserId = userRed.UserId,
            JoinedAt = DateTime.UtcNow
        };

        var member2 = new RoomMember
        {
            RoomId = room.RoomId,
            UserId = userBlack.UserId,
            JoinedAt = DateTime.UtcNow
        };

        await context.RoomMembers.AddRangeAsync(member1, member2);
        await context.SaveChangesAsync();

        // 4. Dữ liệu mẫu Ván đấu (Matches)
        var match = new Match
        {
            RoomId = room.RoomId,
            MatchType = "Ranked",
            InitialBoardState = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
            Result = "InProgress",
            Reason = "Đang thi đấu",
            Version = "1.0",
            StartTime = DateTime.UtcNow
        };

        await context.Matches.AddAsync(match);
        await context.SaveChangesAsync();

        // 5. Dữ liệu mẫu Người chơi trong ván đấu (MatchPlayers)
        var matchPlayerRed = new MatchPlayer
        {
            MatchId = match.MatchId,
            UserId = userRed.UserId,
            Side = "Red"
        };

        var matchPlayerBlack = new MatchPlayer
        {
            MatchId = match.MatchId,
            UserId = userBlack.UserId,
            Side = "Black"
        };

        await context.MatchPlayers.AddRangeAsync(matchPlayerRed, matchPlayerBlack);
        await context.SaveChangesAsync();

        // 6. Dữ liệu mẫu Nước đi (Moves)
        var move1 = new Move
        {
            MatchId = match.MatchId,
            UserId = userRed.UserId,
            MoveOrder = 1,
            MoveNotation = "C2.5", // Pháo 2 bình 5 (Trung pháo)
            PlayedAt = DateTime.UtcNow
        };

        var move2 = new Move
        {
            MatchId = match.MatchId,
            UserId = userBlack.UserId,
            MoveOrder = 2,
            MoveNotation = "H8+7", // Mã 8 tiến 7 (Bình phong mã)
            PlayedAt = DateTime.UtcNow
        };

        await context.Moves.AddRangeAsync(move1, move2);
        await context.SaveChangesAsync();
    }
}
