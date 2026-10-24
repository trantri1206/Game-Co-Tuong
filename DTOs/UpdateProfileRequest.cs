namespace GameCoTuong.DTOs;

public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Trường kiểm tra: Nếu client cố tình gửi ID người dùng khác nhằm đổi hồ sơ người khác, hệ thống sẽ phát hiện và chặn.
    /// </summary>
    public int? ClaimedUserId { get; set; }
}
