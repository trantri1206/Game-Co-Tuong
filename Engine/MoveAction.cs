namespace GameCoTuong.Engine;

/// <summary>
/// Đại diện cho một thao tác di chuyển quân cờ từ ô xuất phát (From) tới ô đích (To).
/// </summary>
public readonly record struct MoveAction
{
    public Position From { get; }
    public Position To { get; }

    public MoveAction(Position from, Position to)
    {
        if (from == to)
        {
            throw new ArgumentException($"Ô xuất phát và ô đích không được trùng nhau ({from}).");
        }

        From = from;
        To = to;
    }

    public MoveAction(int fromCol, int fromRow, int toCol, int toRow)
        : this(new Position(fromCol, fromRow), new Position(toCol, toRow))
    {
    }

    public override string ToString() => $"{From} -> {To}";
}
