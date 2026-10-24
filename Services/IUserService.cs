using System.Security.Claims;
using GameCoTuong.DTOs;

namespace GameCoTuong.Services;

public interface IUserService
{
    /// <summary>
    /// Lấy thông tin hồ sơ theo danh tính phiên đã xác thực (Token).
    /// </summary>
    Task<UserProfileResponse?> GetProfileAsync(ClaimsPrincipal user);

    /// <summary>
    /// Cập nhật thông tin hồ sơ cá nhân. Chặn tuyệt đối việc truyền mã người dùng khác để sửa đổi.
    /// </summary>
    Task<AuthorizationResult> UpdateProfileAsync(ClaimsPrincipal user, UpdateProfileRequest request);

    /// <summary>
    /// Đăng xuất: Yêu cầu xác nhận đầu hàng nếu đang trong ván cờ dở dang, xóa kết nối, rời phòng và thu hồi token.
    /// </summary>
    Task<LogoutResult> LogoutAsync(ClaimsPrincipal user, LogoutRequest request, string? rawToken = null);

    /// <summary>
    /// Hủy yêu cầu đầu hàng: Giữ nguyên trạng thái ván đấu đang diễn ra.
    /// </summary>
    Task<AuthorizationResult> CancelSurrenderAsync(ClaimsPrincipal user);
}
