using System.Security.Claims;

namespace GameCoTuong.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Trích xuất định danh UserId duy nhất từ Claims của phiên đã xác thực (JWT).
    /// Tuyệt đối KHÔNG tin tưởng UserId gửi từ Client Payload để chống mạo danh.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Trích xuất Username từ Claims.
    /// </summary>
    public static string? GetUsername(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Kiểm tra hành vi giả mạo: nếu Client truyền kèm một claimedUserId khác với UserId thực sự trong Token.
    /// </summary>
    public static bool IsImpersonating(this ClaimsPrincipal? principal, int? claimedUserId)
    {
        if (!claimedUserId.HasValue)
        {
            return false;
        }

        var actualUserId = principal.GetUserId();
        return actualUserId.HasValue && actualUserId.Value != claimedUserId.Value;
    }
}
