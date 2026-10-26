namespace GameCoTuong.Engine;

/// <summary>
/// Đại diện cho một quân cờ cụ thể trên bàn cờ tướng.
/// </summary>
public sealed record Piece
{
    public PieceType Type { get; }
    public PieceColor Color { get; }

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
    }

    /// <summary>
    /// Ký tự FEN chuẩn quốc tế:
    /// Đỏ (Chữ hoa): R (Xe), N (Mã), B (Tượng), A (Sĩ), K (Tướng/Soái), C (Pháo), P (Binh).
    /// Đen (Chữ thường): r, n, b, a, k, c, p.
    /// </summary>
    public char ToFenChar()
    {
        char c = Type switch
        {
            PieceType.General => 'K',
            PieceType.Advisor => 'A',
            PieceType.Elephant => 'B',
            PieceType.Horse => 'N',
            PieceType.Chariot => 'R',
            PieceType.Cannon => 'C',
            PieceType.Soldier => 'P',
            _ => throw new InvalidOperationException($"Loại quân cờ {Type} không hợp lệ.")
        };

        return Color == PieceColor.Red ? c : char.ToLowerInvariant(c);
    }

    /// <summary>
    /// Tạo quân cờ từ ký tự FEN.
    /// </summary>
    public static Piece FromFenChar(char c)
    {
        var color = char.IsUpper(c) ? PieceColor.Red : PieceColor.Black;
        var upper = char.ToUpperInvariant(c);

        var type = upper switch
        {
            'K' => PieceType.General,
            'A' => PieceType.Advisor,
            'B' or 'E' => PieceType.Elephant,
            'N' or 'H' => PieceType.Horse,
            'R' => PieceType.Chariot,
            'C' => PieceType.Cannon,
            'P' or 'S' => PieceType.Soldier,
            _ => throw new ArgumentException($"Ký tự FEN '{c}' không hợp lệ cho quân cờ tướng.")
        };

        return new Piece(type, color);
    }

    /// <summary>
    /// Ký tự chữ Hán truyền thống của quân cờ.
    /// </summary>
    public string Hanzi => (Type, Color) switch
    {
        (PieceType.General, PieceColor.Red) => "帥",
        (PieceType.General, PieceColor.Black) => "將",
        (PieceType.Advisor, PieceColor.Red) => "仕",
        (PieceType.Advisor, PieceColor.Black) => "士",
        (PieceType.Elephant, PieceColor.Red) => "相",
        (PieceType.Elephant, PieceColor.Black) => "象",
        (PieceType.Horse, PieceColor.Red) => "傌",
        (PieceType.Horse, PieceColor.Black) => "馬",
        (PieceType.Chariot, PieceColor.Red) => "俥",
        (PieceType.Chariot, PieceColor.Black) => "車",
        (PieceType.Cannon, PieceColor.Red) => "炮",
        (PieceType.Cannon, PieceColor.Black) => "砲",
        (PieceType.Soldier, PieceColor.Red) => "兵",
        (PieceType.Soldier, PieceColor.Black) => "卒",
        _ => "?"
    };

    /// <summary>
    /// Tên gọi tiếng Việt của quân cờ.
    /// </summary>
    public string Name => (Type, Color) switch
    {
        (PieceType.General, PieceColor.Red) => "Soái Đỏ",
        (PieceType.General, PieceColor.Black) => "Tướng Đen",
        (PieceType.Advisor, PieceColor.Red) => "Sĩ Đỏ",
        (PieceType.Advisor, PieceColor.Black) => "Sĩ Đen",
        (PieceType.Elephant, PieceColor.Red) => "Tượng Đỏ",
        (PieceType.Elephant, PieceColor.Black) => "Tượng Đen",
        (PieceType.Horse, PieceColor.Red) => "Mã Đỏ",
        (PieceType.Horse, PieceColor.Black) => "Mã Đen",
        (PieceType.Chariot, PieceColor.Red) => "Xe Đỏ",
        (PieceType.Chariot, PieceColor.Black) => "Xe Đen",
        (PieceType.Cannon, PieceColor.Red) => "Pháo Đỏ",
        (PieceType.Cannon, PieceColor.Black) => "Pháo Đen",
        (PieceType.Soldier, PieceColor.Red) => "Binh Đỏ",
        (PieceType.Soldier, PieceColor.Black) => "Tốt Đen",
        _ => "Quân Cờ"
    };

    public override string ToString() => $"{Name} ({Hanzi})";
}
