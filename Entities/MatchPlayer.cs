namespace GameCoTuong.Entities;

public class MatchPlayer
{
    public int MatchId { get; set; }
    public int UserId { get; set; }
    public string Side { get; set; } = string.Empty;

    // Navigation properties
    public Match Match { get; set; } = null!;
    public User User { get; set; } = null!;
}
