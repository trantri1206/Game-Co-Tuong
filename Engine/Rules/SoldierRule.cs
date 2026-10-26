namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển và ăn quân chuẩn của quân Tốt / Binh (Soldier Rule - GCT-50).
/// </summary>
public static class SoldierRule
{
    /// <summary>
    /// Kiểm tra xem quân Tốt của một phe đã vượt qua sông hay chưa.
    /// - Phe Đen: Nửa trên (Row 0..4), qua sông khi Row >= 5.
    /// - Phe Đỏ: Nửa dưới (Row 5..9), qua sông khi Row <= 4.
    /// </summary>
    public static bool HasCrossedRiver(PieceColor color, int row)
    {
        return color switch
        {
            PieceColor.Red => row <= 4,
            PieceColor.Black => row >= 5,
            _ => false
        };
    }

    /// <summary>
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Tốt có hợp lệ theo luật hay không.
    /// </summary>
    public static bool IsValidMove(ChessBoard board, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(board);

        // 1. Tọa độ phải nằm trong bàn cờ
        if (!Position.IsValid(from.Col, from.Row) || !Position.IsValid(to.Col, to.Row))
        {
            return false;
        }

        // 2. Ô xuất phát và ô đích không được trùng nhau
        if (from == to)
        {
            return false;
        }

        // 3. Quân cờ tại ô xuất phát phải là quân Tốt/Binh
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Soldier)
        {
            return false;
        }

        // 4. Không được ăn quân đồng minh
        var targetPiece = board.GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false;
        }

        int dCol = to.Col - from.Col;
        int dRow = to.Row - from.Row;
        int absCol = Math.Abs(dCol);
        int absRow = Math.Abs(dRow);

        // 5. Khoảng cách di chuyển: mỗi nước chỉ được đi đúng 1 bước (chặn đi chéo và đi > 1 ô)
        // Manhattan distance = 1 đồng nghĩa: hoặc (absCol == 1 && absRow == 0) hoặc (absCol == 0 && absRow == 1)
        if (absCol + absRow != 1)
        {
            return false;
        }

        bool crossedRiver = HasCrossedRiver(piece.Color, from.Row);

        if (piece.Color == PieceColor.Red)
        {
            // Binh Đỏ: hướng tiến là ĐI LÊN (Row giảm dần -> dRow == -1)
            // Tuyệt đối không được đi lùi (dRow > 0)
            if (dRow > 0)
            {
                return false;
            }

            // Trước khi qua sông: chỉ được tiến thẳng (dRow == -1, dCol == 0)
            if (!crossedRiver)
            {
                return dRow == -1 && dCol == 0;
            }

            // Đã qua sông: được tiến thẳng (dRow == -1, dCol == 0) HOẶC đi ngang (dRow == 0, absCol == 1)
            return (dRow == -1 && dCol == 0) || (dRow == 0 && absCol == 1);
        }
        else // PieceColor.Black
        {
            // Tốt Đen: hướng tiến là ĐI XUỐNG (Row tăng dần -> dRow == +1)
            // Tuyệt đối không được đi lùi (dRow < 0)
            if (dRow < 0)
            {
                return false;
            }

            // Trước khi qua sông: chỉ được tiến thẳng (dRow == 1, dCol == 0)
            if (!crossedRiver)
            {
                return dRow == 1 && dCol == 0;
            }

            // Đã qua sông: được tiến thẳng (dRow == 1, dCol == 0) HOẶC đi ngang (dRow == 0, absCol == 1)
            return (dRow == 1 && dCol == 0) || (dRow == 0 && absCol == 1);
        }
    }

    /// <summary>
    /// Sinh ra tất cả các nước đi hợp lệ cho quân Tốt tại vị trí 'from'.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Soldier)
        {
            return legalMoves;
        }

        // 4 hướng di chuyển tiềm năng (tiến, lùi, trái, phải)
        var candidates = new (int dc, int dr)[]
        {
            (0, -1), // Lên (tiến với Đỏ, lùi với Đen)
            (0, 1),  // Xuống (tiến với Đen, lùi với Đỏ)
            (-1, 0), // Trái (ngang)
            (1, 0)   // Phải (ngang)
        };

        foreach (var (dc, dr) in candidates)
        {
            int nextCol = from.Col + dc;
            int nextRow = from.Row + dr;

            if (Position.IsValid(nextCol, nextRow))
            {
                var targetPos = new Position(nextCol, nextRow);
                if (IsValidMove(board, from, targetPos))
                {
                    legalMoves.Add(targetPos);
                }
            }
        }

        return legalMoves;
    }
}
