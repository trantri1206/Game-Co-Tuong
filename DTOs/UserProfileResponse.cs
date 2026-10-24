namespace GameCoTuong.DTOs;

public class UserProfileResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CurrentRoomId { get; set; }
    public int? CurrentMatchId { get; set; }
    public bool IsInActiveMatch { get; set; }
}
