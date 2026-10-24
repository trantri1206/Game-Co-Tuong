using System.Security.Claims;
using GameCoTuong.Common;
using GameCoTuong.Data;
using GameCoTuong.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GameCoTuong.Services;

public class RoomAuthorizationService : IRoomAuthorizationService
{
    private readonly CoTuongDbContext _context;

    public RoomAuthorizationService(CoTuongDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorizationResult> AuthorizeMembershipAsync(int roomId, int userId)
    {
        var isMember = await _context.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (!isMember)
        {
            return AuthorizationResult.Forbidden("Từ chối thao tác: Bạn không phải là thành viên của phòng này.");
        }

        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> AuthorizeStartMatchAsync(int roomId, int userId)
    {
        // 1. Phải là thành viên trong phòng
        var memberCheck = await AuthorizeMembershipAsync(roomId, userId);
        if (!memberCheck.IsAuthorized)
        {
            return memberCheck;
        }

        // 2. Phòng phải đang hoạt động
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (room == null || room.IsActive == false)
        {
            return AuthorizationResult.Forbidden("Phòng cờ không tồn tại hoặc đã bị đóng.");
        }

        return AuthorizationResult.Success("Đủ điều kiện bắt đầu ván đấu.");
    }

    public async Task<AuthorizationResult> AuthorizeMakeMoveAsync(int roomId, int matchId, int userId)
    {
        // 1. Phải là thành viên trong phòng
        var memberCheck = await AuthorizeMembershipAsync(roomId, userId);
        if (!memberCheck.IsAuthorized)
        {
            return memberCheck;
        }

        // 2. Ván cờ phải thuộc phòng này
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId && m.RoomId == roomId);
        if (match == null)
        {
            return AuthorizationResult.Forbidden("Ván cờ không tồn tại hoặc không thuộc về phòng này.");
        }

        // 3. Người dùng phải là đấu thủ (Player) trong ván cờ, khán giả không được đi quân
        var isPlayer = await _context.MatchPlayers
            .AnyAsync(mp => mp.MatchId == matchId && mp.UserId == userId);

        if (!isPlayer)
        {
            return AuthorizationResult.Forbidden("Từ chối đi quân: Bạn là khán giả xem cờ, không phải kỳ thủ trong ván đấu này.");
        }

        return AuthorizationResult.Success("Đủ quyền thực hiện nước đi.");
    }

    public async Task<AuthorizationResult> AuthorizeCloseRoomAsync(int roomId, int userId)
    {
        // Phải là thành viên trong phòng
        var memberCheck = await AuthorizeMembershipAsync(roomId, userId);
        if (!memberCheck.IsAuthorized)
        {
            return memberCheck;
        }

        return AuthorizationResult.Success("Đủ quyền đóng phòng.");
    }

    public async Task<AuthorizationResult> AuthorizeVideoCallAsync(int roomId, int userId)
    {
        // Người ngoài phòng tuyệt đối không được tham gia gọi video hoặc nghe lén tín hiệu WebRTC
        var memberCheck = await AuthorizeMembershipAsync(roomId, userId);
        if (!memberCheck.IsAuthorized)
        {
            return AuthorizationResult.Forbidden("Từ chối gọi video: Người ngoài phòng không thể tham gia hoặc gửi tín hiệu video.");
        }

        return AuthorizationResult.Success("Đủ quyền tham gia gọi video.");
    }

    public AuthorizationResult VerifyNoImpersonation(ClaimsPrincipal? principal, int? claimedUserId)
    {
        if (principal == null)
        {
            return AuthorizationResult.Unauthorized("Chưa xác thực danh tính phiên làm việc.");
        }

        var actualUserId = principal.GetUserId();
        if (!actualUserId.HasValue)
        {
            return AuthorizationResult.Unauthorized("Token không chứa định danh người dùng hợp lệ.");
        }

        // Nếu client cố tình gửi kèm một ID khác với ID trong Token -> Chặn ngay hành vi giả mạo
        if (claimedUserId.HasValue && claimedUserId.Value != actualUserId.Value)
        {
            return AuthorizationResult.Forbidden(
                $"Phát hiện hành vi giả mạo: Mã người dùng gửi lên ({claimedUserId.Value}) không khớp với danh tính thực trong Token ({actualUserId.Value}).");
        }

        return AuthorizationResult.Success();
    }
}
