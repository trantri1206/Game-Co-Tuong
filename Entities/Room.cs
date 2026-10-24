namespace GameCoTuong.Entities;

public class Room
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation properties
    public ICollection<RoomMember> RoomMembers { get; set; } = new List<RoomMember>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
