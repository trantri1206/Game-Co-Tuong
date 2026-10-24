namespace GameCoTuong.DTOs;

public class LogoutResult
{
    public bool IsSuccess { get; set; }
    public bool RequiresSurrenderConfirmation { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MatchResult { get; set; }
    public bool DeleteClientToken { get; set; }
    public bool RedirectToLogin { get; set; }

    /// <summary>
    /// Ghi chú chính sách JWT: Do bản chất Stateless của JWT, token cũ về mặt kỹ thuật vẫn còn hiệu lực
    /// cho đến thời điểm hết hạn (ExpiresAt) trừ khi có cơ chế thu hồi (Blacklist).
    /// </summary>
    public string JwtPolicyNotice { get; set; } =
        "JWT cũ về mặt toán học vẫn còn hiệu lực cho đến thời điểm hết hạn (ExpiresAt) trừ khi được thu hồi qua Blacklist. Phía Client bắt buộc phải xóa sạch token khỏi bộ nhớ để kết thúc phiên.";

    public static LogoutResult RequireConfirmation(string message = "Bạn đang trong ván đấu dở dang. Rời phòng hoặc đăng xuất sẽ tính là đầu hàng và xử thua đối thủ. Bạn có chắc chắn muốn đầu hàng không?")
    {
        return new LogoutResult
        {
            IsSuccess = false,
            RequiresSurrenderConfirmation = true,
            Message = message,
            DeleteClientToken = false,
            RedirectToLogin = false
        };
    }

    public static LogoutResult Success(string message = "Đăng xuất thành công. Đã kết thúc phiên làm việc.", string? matchResult = null)
    {
        return new LogoutResult
        {
            IsSuccess = true,
            RequiresSurrenderConfirmation = false,
            Message = message,
            MatchResult = matchResult,
            DeleteClientToken = true,
            RedirectToLogin = true
        };
    }

    public static LogoutResult Failure(string message)
    {
        return new LogoutResult
        {
            IsSuccess = false,
            RequiresSurrenderConfirmation = false,
            Message = message,
            DeleteClientToken = false,
            RedirectToLogin = false
        };
    }
}
