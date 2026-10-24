namespace GameCoTuong.DTOs;

/// <summary>
/// DTO phản hồi thông tin tài khoản người dùng sau khi đăng ký hoặc truy vấn.
/// TUYỆT ĐỐI KHÔNG chứa Password hoặc PasswordHash vì lý do bảo mật.
/// </summary>
public class UserResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime? CreatedAt { get; set; }
}
