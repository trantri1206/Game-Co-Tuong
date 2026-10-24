namespace GameCoTuong.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Navigation properties
    public ICollection<RoomMember> RoomMembers { get; set; } = new List<RoomMember>();
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<Move> Moves { get; set; } = new List<Move>();
}
