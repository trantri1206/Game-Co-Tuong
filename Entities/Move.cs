namespace GameCoTuong.Entities;

public class Move
{
    public int MoveId { get; set; }
    public int MatchId { get; set; }
    public int UserId { get; set; }
    public int MoveOrder { get; set; }
    public string MoveNotation { get; set; } = string.Empty;
    public DateTime? PlayedAt { get; set; }

    // Navigation properties
    public Match Match { get; set; } = null!;
    public User User { get; set; } = null!;
}
