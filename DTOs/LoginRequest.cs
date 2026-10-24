namespace GameCoTuong.DTOs;

public class LoginRequest
{
    /// <summary>
    /// Cho phép người dùng đăng nhập bằng Username hoặc Email
    /// </summary>
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
