namespace GameCoTuong.Engine;

/// <summary>
/// Tọa độ một giao điểm trên bàn cờ tướng 9x10 (90 giao điểm).
/// Quy định: Cột 0–8 (trái qua phải), Hàng 0–9 (trên xuống dưới).
/// Phe Đen ở trên (hàng 0–4), Phe Đỏ ở dưới (hàng 5–9).
/// </summary>
public readonly record struct Position
{
    public const int MinCol = 0;
    public const int MaxCol = 8;
    public const int MinRow = 0;
    public const int MaxRow = 9;
    public const int TotalIntersections = 90;

    public int Col { get; }
    public int Row { get; }

    public Position(int col, int row)
    {
        if (!IsValid(col, row))
        {
            throw new ArgumentOutOfRangeException(
                $"Tọa độ ({col}, {row}) nằm ngoài phạm vi bàn cờ 90 giao điểm (Cột 0–8, Hàng 0–9).");
        }

        Col = col;
        Row = row;
    }

    /// <summary>
    /// Kiểm tra xem một tọa độ (cột, hàng) có nằm trên bàn cờ hay không.
    /// </summary>
    public static bool IsValid(int col, int row)
    {
        return col >= MinCol && col <= MaxCol && row >= MinRow && row <= MaxRow;
    }

    /// <summary>
    /// Thử tạo tọa độ an toàn, trả về false nếu tọa độ ngoài bàn cờ.
    /// </summary>
    public static bool TryCreate(int col, int row, out Position position)
    {
        if (IsValid(col, row))
        {
            position = new Position(col, row);
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>
    /// Chuyển đổi tọa độ sang chỉ số mảng 1 chiều 0..89 (90 giao điểm).
    /// </summary>
    public int ToIndex() => Row * 9 + Col;

    /// <summary>
    /// Tạo tọa độ từ chỉ số mảng 1 chiều 0..89.
    /// </summary>
    public static Position FromIndex(int index)
    {
        if (index < 0 || index >= TotalIntersections)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Chỉ số giao điểm {index} không hợp lệ (phải từ 0 đến 89).");
        }

        return new Position(index % 9, index / 9);
    }

    /// <summary>
    /// Kiểm tra ô này có nằm trong Cung Tướng phe Đỏ (Cột 3..5, Hàng 7..9) hay không.
    /// </summary>
    public bool IsInRedPalace() => Col >= 3 && Col <= 5 && Row >= 7 && Row <= 9;

    /// <summary>
    /// Kiểm tra ô này có nằm trong Cung Tướng phe Đen (Cột 3..5, Hàng 0..2) hay không.
    /// </summary>
    public bool IsInBlackPalace() => Col >= 3 && Col <= 5 && Row >= 0 && Row <= 2;

    /// <summary>
    /// Kiểm tra ô này có nằm trong Cung Tướng của phe tương ứng hay không.
    /// </summary>
    public bool IsInPalace(PieceColor color) =>
        color == PieceColor.Red ? IsInRedPalace() : IsInBlackPalace();

    /// <summary>
    /// Kiểm tra ô này có nằm ở nửa bàn cờ của phe Đỏ (Hàng 5..9) hay không.
    /// </summary>
    public bool IsRedSide() => Row >= 5 && Row <= 9;

    /// <summary>
    /// Kiểm tra ô này có nằm ở nửa bàn cờ của phe Đen (Hàng 0..4) hay không.
    /// </summary>
    public bool IsBlackSide() => Row >= 0 && Row <= 4;

    /// <summary>
    /// Kiểm tra ô này có cùng phía lãnh thổ của phe hay không.
    /// </summary>
    public bool IsSameSide(PieceColor color) =>
        color == PieceColor.Red ? IsRedSide() : IsBlackSide();

    public override string ToString() => $"({Col}, {Row})";
}
