namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển và ăn quân chuẩn của quân Sĩ / Sĩ quan (Advisor Rule - GCT-55).
/// Luật cờ tướng:
/// - Sĩ di chuyển theo đường chéo đúng 1 giao điểm: |dCol| == 1 && |dRow| == 1 (4 hướng chéo).
/// - Giữ Sĩ di chuyển nghiêm ngặt trong phạm vi Cửu cung của phe mình:
///   + Cột cửu cung: Col từ 3 đến 5 (Col 3, 4, 5).
///   + Hàng cửu cung phe Đen: Row từ 0 đến 2 (Row 0, 1, 2).
///   + Hàng cửu cung phe Đỏ: Row từ 7 đến 9 (Row 7, 8, 9).
/// - Điểm đến: Nằm trong Cửu cung, không chứa quân cùng màu (có thể ăn quân đối phương hoặc đi vào ô trống).
/// - Chặn đi ngang, đi dọc (như Xe/Tốt), đi chéo 2 ô (như Tượng), đi chữ L (như Mã) hoặc đứng yên tại chỗ.
/// - Mọi điểm ngoài cung đều bị từ chối tuyệt đối cho cả hai bên.
/// </summary>
public static class AdvisorRule
{
    /// <summary>
    /// 4 hướng đi chéo đúng 1 giao điểm của quân Sĩ: Lên-Trái, Lên-Phải, Xuống-Trái, Xuống-Phải.
    /// </summary>
    public static readonly (int dCol, int dRow)[] DiagonalDirections =
    [
        (-1, -1),
        ( 1, -1),
        (-1,  1),
        ( 1,  1)
    ];

    /// <summary>
    /// Kiểm tra xem một tọa độ (col, row) có nằm trong phạm vi Cửu cung của phe chỉ định hay không.
    /// - Cột Cửu cung chung: Col trong khoảng [3, 5].
    /// - Cung Đen: Row trong khoảng [0, 2].
    /// - Cung Đỏ: Row trong khoảng [7, 9].
    /// </summary>
    public static bool IsInPalace(PieceColor color, int col, int row)
    {
        if (col < 3 || col > 5)
        {
            return false;
        }

        return color switch
        {
            PieceColor.Black => row >= 0 && row <= 2,
            PieceColor.Red => row >= 7 && row <= 9,
            _ => false
        };
    }

    /// <summary>
    /// Kiểm tra xem một vị trí struct Position có nằm trong Cửu cung của phe chỉ định hay không.
    /// </summary>
    public static bool IsInPalace(PieceColor color, Position pos) => IsInPalace(color, pos.Col, pos.Row);

    /// <summary>
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Sĩ có hợp lệ theo luật hay không.
    /// </summary>
    public static bool IsValidMove(ChessBoard board, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(board);

        // 1. Tọa độ phải nằm trong bàn cờ
        if (!Position.IsValid(from.Col, from.Row) || !Position.IsValid(to.Col, to.Row))
        {
            return false;
        }

        // 2. Chặn đứng yên tại chỗ
        if (from == to)
        {
            return false;
        }

        // 3. Quân cờ tại ô xuất phát phải là quân Sĩ
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Advisor)
        {
            return false;
        }

        // 4. Phải đi chéo đúng 1 giao điểm: |dCol| == 1 && |dRow| == 1
        int dCol = to.Col - from.Col;
        int dRow = to.Row - from.Row;
        if (Math.Abs(dCol) != 1 || Math.Abs(dRow) != 1)
        {
            return false;
        }

        // 5. Cả ô xuất phát và ô đích đều phải nằm trong Cửu cung của phe mình
        if (!IsInPalace(piece.Color, from.Col, from.Row) || !IsInPalace(piece.Color, to.Col, to.Row))
        {
            return false;
        }

        // 6. Điểm đến không được chứa quân đồng minh
        var targetPiece = board.GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sinh danh sách tất cả các nước đi hợp lệ cho quân Sĩ tại vị trí 'from'.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Advisor)
        {
            return legalMoves;
        }

        // Nếu ô xuất phát nằm ngoài Cửu cung (thế cờ bất thường) thì không có nước đi hợp lệ
        if (!IsInPalace(piece.Color, from.Col, from.Row))
        {
            return legalMoves;
        }

        foreach (var (dc, dr) in DiagonalDirections)
        {
            int targetCol = from.Col + dc;
            int targetRow = from.Row + dr;

            // Kiểm tra điểm đến phải nằm trong Cửu cung
            if (!IsInPalace(piece.Color, targetCol, targetRow))
            {
                continue;
            }

            // Kiểm tra điểm đến không chứa quân đồng minh
            var targetPiece = board.GetPiece(targetCol, targetRow);
            if (targetPiece != null && targetPiece.Color == piece.Color)
            {
                continue;
            }

            legalMoves.Add(new Position(targetCol, targetRow));
        }

        return legalMoves;
    }
}
