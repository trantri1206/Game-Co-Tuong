namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển và ăn quân chuẩn của quân Xe (Chariot / Rook Rule - GCT-51).
/// </summary>
public static class ChariotRule
{
    /// <summary>
    /// Đếm số lượng quân cờ nằm giữa hai ô trên cùng một hàng hoặc cùng một cột (không tính hai ô đầu/cuối).
    /// </summary>
    public static int CountPiecesBetween(ChessBoard board, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (from.Col == to.Col)
        {
            int minRow = Math.Min(from.Row, to.Row) + 1;
            int maxRow = Math.Max(from.Row, to.Row) - 1;
            int count = 0;

            for (int r = minRow; r <= maxRow; r++)
            {
                if (board.GetPiece(from.Col, r) != null)
                {
                    count++;
                }
            }

            return count;
        }
        else if (from.Row == to.Row)
        {
            int minCol = Math.Min(from.Col, to.Col) + 1;
            int maxCol = Math.Max(from.Col, to.Col) - 1;
            int count = 0;

            for (int c = minCol; c <= maxCol; c++)
            {
                if (board.GetPiece(c, from.Row) != null)
                {
                    count++;
                }
            }

            return count;
        }

        return -1; // Không cùng hàng hoặc cột
    }

    /// <summary>
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Xe có hợp lệ theo luật hay không.
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

        // 3. Quân cờ tại ô xuất phát phải là quân Xe
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Chariot)
        {
            return false;
        }

        // 4. Phải đi theo hàng ngang hoặc cột dọc (chặn đi chéo)
        bool isVertical = from.Col == to.Col;
        bool isHorizontal = from.Row == to.Row;
        if (!isVertical && !isHorizontal)
        {
            return false;
        }

        // 5. Không được ăn quân đồng minh
        var targetPiece = board.GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false;
        }

        // 6. Không được nhảy qua quân cản: đoạn đường giữa 'from' và 'to' phải hoàn toàn trống (0 vật cản)
        int piecesBetween = CountPiecesBetween(board, from, to);
        return piecesBetween == 0;
    }

    /// <summary>
    /// Sinh danh sách tất cả các nước đi hợp lệ cho quân Xe tại vị trí 'from'.
    /// Quét 4 tia: Lên, Xuống, Trái, Phải cho tới biên bàn cờ hoặc khi gặp vật cản.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Chariot)
        {
            return legalMoves;
        }

        // 4 hướng quét: Lên (dr = -1), Xuống (dr = 1), Trái (dc = -1), Phải (dc = 1)
        var directions = new (int dc, int dr)[]
        {
            (0, -1),
            (0, 1),
            (-1, 0),
            (1, 0)
        };

        foreach (var (dc, dr) in directions)
        {
            int c = from.Col + dc;
            int r = from.Row + dr;

            while (Position.IsValid(c, r))
            {
                var targetPiece = board.GetPiece(c, r);

                if (targetPiece == null)
                {
                    // Ô trống: nước đi hợp lệ, tiếp tục quét tia
                    legalMoves.Add(new Position(c, r));
                }
                else
                {
                    if (targetPiece.Color != piece.Color)
                    {
                        // Gặp quân đối phương: ăn được quân này, sau đó dừng tia (không nhảy qua)
                        legalMoves.Add(new Position(c, r));
                    }
                    // Gặp quân cản (dù là ta hay địch): dừng quét hướng này ngay lập tức
                    break;
                }

                c += dc;
                r += dr;
            }
        }

        return legalMoves;
    }
}
