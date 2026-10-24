namespace GameCoTuong.DTOs;

public class LogoutRequest
{
    /// <summary>
    /// Người dùng xác nhận đầu hàng nếu đang trong ván đấu dở dang.
    /// Nếu đang chơi mà ConfirmSurrender = false, hệ thống sẽ yêu cầu xác nhận trước khi cho phép đăng xuất.
    /// </summary>
    public bool ConfirmSurrender { get; set; } = false;
}
