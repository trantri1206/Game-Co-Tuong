namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển, lộ mặt tướng và an toàn điểm đến của quân Tướng / Soái (General Rule - GCT-56).
/// Luật cờ tướng:
/// - Tướng di chuyển theo hàng ngang hoặc cột dọc đúng 1 giao điểm (|dCol| + |dRow| == 1).
/// - Giữ Tướng di chuyển nghiêm ngặt trong Cửu cung của phe mình:
///   + Cột cửu cung: Col từ 3 đến 5 (Col 3, 4, 5).
///   + Hàng cửu cung phe Đen: Row từ 0 đến 2 (Row 0, 1, 2).
///   + Hàng cửu cung phe Đỏ: Row từ 7 đến 9 (Row 7, 8, 9).
/// - Chặn tuyệt đối đi chéo, đi quá 1 ô hoặc đứng yên tại chỗ.
/// - Luật Lộ mặt Tướng (Flying General): Hai Tướng không được nhìn thẳng mặt nhau trên cùng 1 cột mà không có quân cản ở giữa.
/// - Kiểm tra an toàn điểm đến: Tướng tuyệt đối không được đi vào vị trí bị quân đối phương khống chế (tự đưa vào thế chiếu).
/// </summary>
public static class GeneralRule
{
    public static readonly (int dCol, int dRow)[] OrthogonalDirections =
    [
        ( 0, -1), // Lên
        ( 0,  1), // Xuống
        (-1,  0), // Trái
        ( 1,  0)  // Phải
    ];

    /// <summary>
    /// Kiểm tra xem một tọa độ có nằm trong phạm vi Cửu cung của phe chỉ định hay không.
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

    public static bool IsInPalace(PieceColor color, Position pos) => IsInPalace(color, pos.Col, pos.Row);

    /// <summary>
    /// Tìm vị trí của quân Tướng thuộc phe chỉ định trên bàn cờ.
    /// </summary>
    public static Position? FindGeneral(ChessBoard board, PieceColor color)
    {
        ArgumentNullException.ThrowIfNull(board);

        int minRow = color == PieceColor.Black ? 0 : 7;
        int maxRow = color == PieceColor.Black ? 2 : 9;

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = 3; c <= 5; c++)
            {
                var piece = board.GetPiece(c, r);
                if (piece != null && piece.Type == PieceType.General && piece.Color == color)
                {
                    return new Position(c, r);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Kiểm tra hai vị trí Tướng có đối mặt nhau trực tiếp trên cùng một cột mà không có quân cản ở giữa hay không.
    /// </summary>
    public static bool AreGeneralsFacing(ChessBoard board, Position pos1, Position pos2)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (pos1.Col != pos2.Col)
        {
            return false;
        }

        int minRow = Math.Min(pos1.Row, pos2.Row) + 1;
        int maxRow = Math.Max(pos1.Row, pos2.Row) - 1;

        for (int r = minRow; r <= maxRow; r++)
        {
            if (board.GetPiece(pos1.Col, r) != null)
            {
                return false; // Có ít nhất 1 quân cản ở giữa -> không đối mặt
            }
        }

        return true; // Hoàn toàn trống giữa 2 Tướng -> đối mặt (phạm luật)
    }

    /// <summary>
    /// Kiểm tra xem một ô cờ 'target' có đang bị phe 'attackerColor' tấn công / khống chế hay không.
    /// </summary>
    public static bool IsSquareAttacked(ChessBoard board, Position target, PieceColor attackerColor)
    {
        ArgumentNullException.ThrowIfNull(board);

        // 1. Kiểm tra tấn công bởi Xe hoặc Tướng đối phương theo cột/hàng trống, hoặc Pháo qua 1 ngòi
        var directions = new (int dc, int dr)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };
        foreach (var (dc, dr) in directions)
        {
            int c = target.Col + dc;
            int r = target.Row + dr;
            int piecesBetween = 0;

            while (Position.IsValid(c, r))
            {
                var piece = board.GetPiece(c, r);
                if (piece != null)
                {
                    if (piecesBetween == 0)
                    {
                        // Quân cờ đầu tiên nhìn thấy:
                        if (piece.Color == attackerColor)
                        {
                            // Xe tấn công trực tiếp
                            if (piece.Type == PieceType.Chariot)
                            {
                                return true;
                            }

                            // Tướng đối phương nhìn trực tiếp trên cùng cột
                            if (piece.Type == PieceType.General && target.Col == c)
                            {
                                return true;
                            }
                        }

                        piecesBetween = 1; // Quân này đóng vai trò là ngòi cho Pháo phía sau
                    }
                    else if (piecesBetween == 1)
                    {
                        // Quân thứ hai nhìn thấy (sau 1 ngòi):
                        if (piece.Color == attackerColor && piece.Type == PieceType.Cannon)
                        {
                            return true; // Pháo đối phương chiếu qua 1 ngòi
                        }

                        break; // Đã gặp quân thứ 2, dừng quét hướng này
                    }
                }

                c += dc;
                r += dr;
            }
        }

        // 2. Kiểm tra tấn công bởi Mã đối phương (8 hướng nhảy tới target)
        // Nếu ô target bị Mã đối phương ở ô 'horsePos' nhảy tới mà không bị cản chân
        var horseSources = new (int dc, int dr, int footDc, int footDr)[]
        {
            (-1, -2, 0, -1), ( 1, -2, 0, -1),
            (-1,  2, 0,  1), ( 1,  2, 0,  1),
            (-2, -1, -1, 0), (-2,  1, -1, 0),
            ( 2, -1,  1, 0), ( 2,  1,  1, 0)
        };

        foreach (var (dc, dr, footDc, footDr) in horseSources)
        {
            int horseCol = target.Col + dc;
            int horseRow = target.Row + dr;

            if (Position.IsValid(horseCol, horseRow))
            {
                var piece = board.GetPiece(horseCol, horseRow);
                if (piece != null && piece.Color == attackerColor && piece.Type == PieceType.Horse)
                {
                    // Chân cản của con Mã này khi nhảy tới target nằm ở:
                    // Từ horsePos nhảy (-dc, -dr) tới target
                    // Chân cản nằm ở horsePos + (foot theo hướng nhảy tới target)
                    // Vì target = horsePos - (dc, dr), chân cản nằm ở target + footDc, target + footDr
                    int footCol = target.Col + footDc;
                    int footRow = target.Row + footDr;

                    if (Position.IsValid(footCol, footRow) && board.GetPiece(footCol, footRow) == null)
                    {
                        return true; // Mã không bị cản chân -> đang khống chế ô target
                    }
                }
            }
        }

        // 3. Kiểm tra tấn công bởi Tốt / Binh đối phương
        if (attackerColor == PieceColor.Black)
        {
            // Tốt Đen đi xuống: tấn công ô target từ trên xuống (target.Col, target.Row - 1)
            var fromAbove = new Position(target.Col, target.Row - 1);
            if (Position.IsValid(fromAbove.Col, fromAbove.Row))
            {
                var p = board.GetPiece(fromAbove);
                if (p != null && p.Color == PieceColor.Black && p.Type == PieceType.Soldier)
                {
                    return true;
                }
            }

            // Hoặc tấn công ngang nếu Tốt Đen đã qua sông (Row >= 5)
            if (target.Row >= 5)
            {
                foreach (int sideCol in new[] { target.Col - 1, target.Col + 1 })
                {
                    if (Position.IsValid(sideCol, target.Row))
                    {
                        var p = board.GetPiece(sideCol, target.Row);
                        if (p != null && p.Color == PieceColor.Black && p.Type == PieceType.Soldier)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        else // attackerColor == PieceColor.Red
        {
            // Binh Đỏ đi lên: tấn công ô target từ dưới lên (target.Col, target.Row + 1)
            var fromBelow = new Position(target.Col, target.Row + 1);
            if (Position.IsValid(fromBelow.Col, fromBelow.Row))
            {
                var p = board.GetPiece(fromBelow);
                if (p != null && p.Color == PieceColor.Red && p.Type == PieceType.Soldier)
                {
                    return true;
                }
            }

            // Hoặc tấn công ngang nếu Binh Đỏ đã qua sông (Row <= 4)
            if (target.Row <= 4)
            {
                foreach (int sideCol in new[] { target.Col - 1, target.Col + 1 })
                {
                    if (Position.IsValid(sideCol, target.Row))
                    {
                        var p = board.GetPiece(sideCol, target.Row);
                        if (p != null && p.Color == PieceColor.Red && p.Type == PieceType.Soldier)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Tướng có hợp lệ theo luật hay không:
    /// - Đi ngang hoặc dọc đúng 1 ô trong Cửu cung.
    /// - Không ăn quân đồng minh.
    /// - Không làm hai Tướng đối mặt nhau (Lộ mặt Tướng).
    /// - Không đi vào vị trí bị quân đối phương khống chế (an toàn điểm đến).
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

        // 3. Quân cờ tại ô xuất phát phải là quân Tướng
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.General)
        {
            return false;
        }

        // 4. Phải đi ngang hoặc dọc đúng 1 giao điểm: |dCol| + |dRow| == 1
        int dCol = to.Col - from.Col;
        int dRow = to.Row - from.Row;
        int absCol = Math.Abs(dCol);
        int absRow = Math.Abs(dRow);
        if (absCol + absRow != 1)
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

        // 7. Mô phỏng nước đi để kiểm tra an toàn và Lộ mặt Tướng
        PieceColor opponentColor = piece.Color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

        // Tạm thời thay đổi bàn cờ để kiểm tra
        board.SetPiece(from, null);
        board.SetPiece(to, piece);

        bool isValid = true;

        try
        {
            // 7a. Kiểm tra lộ mặt Tướng
            var enemyGenPos = FindGeneral(board, opponentColor);
            if (enemyGenPos.HasValue && AreGeneralsFacing(board, to, enemyGenPos.Value))
            {
                isValid = false; // Hai Tướng đối mặt nhau trực tiếp trên cùng 1 cột trống
            }

            // 7b. Kiểm tra Tướng có đi vào ô bị đối phương khống chế hay không
            if (isValid && IsSquareAttacked(board, to, opponentColor))
            {
                isValid = false; // Tướng đi vào miệng cọp (bị chiếu)
            }
        }
        finally
        {
            // Khôi phục chính xác trạng thái bàn cờ ban đầu
            board.SetPiece(to, targetPiece);
            board.SetPiece(from, piece);
        }

        return isValid;
    }

    /// <summary>
    /// Sinh danh sách tất cả các nước đi hợp lệ cho quân Tướng tại vị trí 'from',
    /// đã qua bộ lọc kiểm tra an toàn điểm đến và chống lộ mặt Tướng.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.General)
        {
            return legalMoves;
        }

        foreach (var (dc, dr) in OrthogonalDirections)
        {
            int targetCol = from.Col + dc;
            int targetRow = from.Row + dr;

            if (Position.IsValid(targetCol, targetRow))
            {
                var targetPos = new Position(targetCol, targetRow);
                if (IsValidMove(board, from, targetPos))
                {
                    legalMoves.Add(targetPos);
                }
            }
        }

        return legalMoves;
    }
}
