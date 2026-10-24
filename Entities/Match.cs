namespace GameCoTuong.Entities;

public class Match
{
    public int MatchId { get; set; }
    public int? RoomId { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public string InitialBoardState { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Reason { get; set; }
    public string? Version { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    // Navigation properties
    public Room? Room { get; set; }
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<Move> Moves { get; set; } = new List<Move>();
}
