using System.Security.Claims;
using GameCoTuong.Entities;

namespace GameCoTuong.Services;

public interface IJwtService
{
    /// <summary>
    /// Sinh token JWT cho người dùng với danh tính (Claims), chữ ký số (HmacSha256) và thời hạn.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateToken(User user, TimeSpan? customLifetime = null);

    /// <summary>
    /// Kiểm tra tính toàn vẹn của token: chữ ký, thời hạn, tính nguyên vẹn (chống giả mạo / sửa đổi).
    /// </summary>
    bool ValidateToken(string token, out ClaimsPrincipal? principal, out string? errorMessage);
}
