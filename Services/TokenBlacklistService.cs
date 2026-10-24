using System.Collections.Concurrent;

namespace GameCoTuong.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    // Lưu các token bị thu hồi kèm theo thời điểm hết hạn
    private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

    public void RevokeToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        // Tự động dọn dẹp các token đã quá hạn tự nhiên
        CleanupExpiredTokens();

        _revokedTokens.TryAdd(token.Trim(), expiresAt);
    }

    public bool IsTokenRevoked(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (_revokedTokens.TryGetValue(token.Trim(), out var expiresAt))
        {
            if (DateTime.UtcNow < expiresAt)
            {
                return true; // Token vẫn còn trong thời gian hiệu lực nhưng đã bị đưa vào danh sách đen
            }

            // Đã hết hạn tự nhiên, xóa khỏi blacklist
            _revokedTokens.TryRemove(token.Trim(), out _);
        }

        return false;
    }

    private void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        foreach (var (t, exp) in _revokedTokens)
        {
            if (exp <= now)
            {
                _revokedTokens.TryRemove(t, out _);
            }
        }
    }
}
