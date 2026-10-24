namespace GameCoTuong.Entities;

public class RoomMember
{
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public DateTime? JoinedAt { get; set; }

    // Navigation properties
    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;
}
