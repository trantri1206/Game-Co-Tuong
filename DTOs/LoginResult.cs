namespace GameCoTuong.DTOs;

public class LoginResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserResponse? User { get; set; }

    public static LoginResult Success(string token, DateTime expiresAt, UserResponse user)
    {
        return new LoginResult
        {
            IsSuccess = true,
            Message = "Đăng nhập thành công.",
            Token = token,
            ExpiresAt = expiresAt,
            User = user
        };
    }

    /// <summary>
    /// Trả thông báo lỗi thống nhất khi sai username hoặc sai password để chống tấn công dò quét (User Enumeration).
    /// </summary>
    public static LoginResult InvalidCredentials()
    {
        return new LoginResult
        {
            IsSuccess = false,
            Message = "Tên đăng nhập hoặc mật khẩu không chính xác."
        };
    }

    public static LoginResult Failure(string message)
    {
        return new LoginResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}
