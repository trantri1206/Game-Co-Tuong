using System.Security.Claims;
using GameCoTuong.DTOs;

namespace GameCoTuong.Services;

public interface IRoomAuthorizationService
{
    /// <summary>
    /// Kiểm tra người dùng có phải là thành viên hợp lệ trong phòng cờ không.
    /// </summary>
    Task<AuthorizationResult> AuthorizeMembershipAsync(int roomId, int userId);

    /// <summary>
    /// Kiểm tra quyền bắt đầu ván đấu trong phòng.
    /// </summary>
    Task<AuthorizationResult> AuthorizeStartMatchAsync(int roomId, int userId);

    /// <summary>
    /// Kiểm tra quyền đi quân trong ván cờ (Phải là thành viên và là đấu thủ trong trận).
    /// </summary>
    Task<AuthorizationResult> AuthorizeMakeMoveAsync(int roomId, int matchId, int userId);

    /// <summary>
    /// Kiểm tra quyền đóng phòng cờ.
    /// </summary>
    Task<AuthorizationResult> AuthorizeCloseRoomAsync(int roomId, int userId);

    /// <summary>
    /// Kiểm tra quyền tham gia / gửi tín hiệu gọi video (WebRTC).
    /// </summary>
    Task<AuthorizationResult> AuthorizeVideoCallAsync(int roomId, int userId);

    /// <summary>
    /// Phát hiện và chặn hành vi giả mạo ID người dùng (Client gửi ClaimedUserId khác với Token).
    /// </summary>
    AuthorizationResult VerifyNoImpersonation(ClaimsPrincipal? principal, int? claimedUserId);
}
