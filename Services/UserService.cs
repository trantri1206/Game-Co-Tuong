using System.Security.Claims;
using GameCoTuong.Common;
using GameCoTuong.Data;
using GameCoTuong.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GameCoTuong.Services;

public class UserService : IUserService
{
    private readonly CoTuongDbContext _context;
    private readonly ITokenBlacklistService? _blacklistService;

    public UserService(CoTuongDbContext context, ITokenBlacklistService? blacklistService = null)
    {
        _context = context;
        _blacklistService = blacklistService;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
        if (dbUser == null)
        {
            return null;
        }

        // Lấy thông tin phòng đang tham gia (nếu có)
        var roomMember = await _context.RoomMembers
            .FirstOrDefaultAsync(rm => rm.UserId == userId.Value);

        // Lấy thông tin ván đấu dở dang (nếu có)
        var activeMatchPlayer = await _context.MatchPlayers
            .Include(mp => mp.Match)
            .FirstOrDefaultAsync(mp => mp.UserId == userId.Value
                                    && mp.Match.Result == "InProgress"
                                    && mp.Match.EndTime == null);

        return new UserProfileResponse
        {
            UserId = dbUser.UserId,
            Username = dbUser.Username,
            Email = dbUser.Email,
            DisplayName = dbUser.DisplayName,
            CreatedAt = dbUser.CreatedAt,
            CurrentRoomId = roomMember?.RoomId,
            CurrentMatchId = activeMatchPlayer?.MatchId,
            IsInActiveMatch = activeMatchPlayer != null
        };
    }

    public async Task<AuthorizationResult> UpdateProfileAsync(ClaimsPrincipal user, UpdateProfileRequest request)
    {
        var actualUserId = user.GetUserId();
        if (!actualUserId.HasValue)
        {
            return AuthorizationResult.Unauthorized("Phiên làm việc chưa được xác thực.");
        }

        // Chặn tuyệt đối hành vi truyền ClaimedUserId khác để cố tình đổi hồ sơ người khác
        if (request.ClaimedUserId.HasValue && request.ClaimedUserId.Value != actualUserId.Value)
        {
            return AuthorizationResult.Forbidden(
                $"Từ chối thao tác: Bạn không có quyền chỉnh sửa hồ sơ người dùng khác (Mã gửi lên {request.ClaimedUserId.Value} != {actualUserId.Value}).");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return AuthorizationResult.Forbidden("Tên hiển thị không được để trống.");
        }

        if (request.DisplayName.Trim().Length > 100)
        {
            return AuthorizationResult.Forbidden("Tên hiển thị không được vượt quá 100 ký tự.");
        }

        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == actualUserId.Value);
        if (dbUser == null)
        {
            return AuthorizationResult.Forbidden("Người dùng không tồn tại.");
        }

        dbUser.DisplayName = request.DisplayName.Trim();
        await _context.SaveChangesAsync();

        return AuthorizationResult.Success("Cập nhật thông tin hồ sơ thành công.");
    }

    public async Task<LogoutResult> LogoutAsync(ClaimsPrincipal user, LogoutRequest request, string? rawToken = null)
    {
        var userId = user.GetUserId();
        if (!userId.HasValue)
        {
            return LogoutResult.Failure("Phiên làm việc không hợp lệ hoặc thiếu định danh người dùng.");
        }

        // 1. Kiểm tra xem người dùng có đang tham gia ván đấu đang diễn ra không (In-Game check)
        var activeMatchPlayer = await _context.MatchPlayers
            .Include(mp => mp.Match)
            .FirstOrDefaultAsync(mp => mp.UserId == userId.Value
                                    && mp.Match.Result == "InProgress"
                                    && mp.Match.EndTime == null);

        string? matchResultMsg = null;

        if (activeMatchPlayer != null)
        {
            // Nếu đang trong trận cờ mà người dùng CHƯA xác nhận đầu hàng -> Yêu cầu xác nhận
            if (!request.ConfirmSurrender)
            {
                return LogoutResult.RequireConfirmation(
                    $"Bạn đang trong ván đấu #{activeMatchPlayer.MatchId}. Rời phòng hoặc đăng xuất sẽ bị tính là đầu hàng và xử thua đối thủ. Bạn có chắc chắn muốn đầu hàng không?");
            }

            // Nếu người dùng ĐÃ XÁC NHẬN đầu hàng -> Xử thua và kết thúc ván đấu
            var match = activeMatchPlayer.Match;
            var mySide = activeMatchPlayer.Side;
            var opponentSide = mySide.Equals("Red", StringComparison.OrdinalIgnoreCase) ? "Black" : "Red";

            match.Result = $"{opponentSide}Win";
            match.Reason = $"Kỳ thủ ({user.GetUsername() ?? userId.Value.ToString()}) đã xác nhận đầu hàng khi đăng xuất / thoát phiên.";
            match.EndTime = DateTime.UtcNow;

            matchResultMsg = $"Ván đấu #{match.MatchId} đã kết thúc với kết quả: {opponentSide} thắng (do đối thủ đầu hàng).";
        }

        // 2. Rời phòng cờ (Xóa khỏi RoomMembers để cập nhật đúng trạng thái)
        var currentRooms = await _context.RoomMembers
            .Where(rm => rm.UserId == userId.Value)
            .ToListAsync();

        if (currentRooms.Count > 0)
        {
            _context.RoomMembers.RemoveRange(currentRooms);
        }

        await _context.SaveChangesAsync();

        // 3. Thu hồi token (Blacklist) nếu được cung cấp
        if (!string.IsNullOrEmpty(rawToken) && _blacklistService != null)
        {
            var expClaim = user.FindFirst("exp")?.Value;
            var expiresAt = long.TryParse(expClaim, out var expSeconds)
                ? DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime
                : DateTime.UtcNow.AddHours(1);

            _blacklistService.RevokeToken(rawToken, expiresAt);
        }

        return LogoutResult.Success(
            "Đăng xuất thành công. Đã rời phòng và đóng phiên làm việc.",
            matchResultMsg);
    }

    public async Task<AuthorizationResult> CancelSurrenderAsync(ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        if (!userId.HasValue)
        {
            return AuthorizationResult.Unauthorized("Phiên làm việc chưa được xác thực.");
        }

        // Kiểm tra trận cờ
        var activeMatchPlayer = await _context.MatchPlayers
            .Include(mp => mp.Match)
            .FirstOrDefaultAsync(mp => mp.UserId == userId.Value
                                    && mp.Match.Result == "InProgress"
                                    && mp.Match.EndTime == null);

        if (activeMatchPlayer != null)
        {
            // Trận cờ được giữ nguyên, không thay đổi bất kỳ trạng thái nào
            return AuthorizationResult.Success(
                $"Đã hủy yêu cầu đầu hàng. Ván đấu #{activeMatchPlayer.MatchId} được giữ nguyên trạng thái InProgress.");
        }

        return AuthorizationResult.Success("Người dùng hiện không tham gia ván đấu dở dang nào.");
    }
}
