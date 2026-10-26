namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển và ăn quân chuẩn của quân Pháo (Cannon Rule - GCT-53).
/// Luật cờ tướng:
/// - Khi KHÔNG ăn quân (ô đích là ô trống): Pháo di chuyển theo hàng ngang hoặc cột dọc,
///   đoạn giữa ô xuất phát và ô đích phải hoàn toàn trống (CountPiecesBetween == 0),
///   tuyệt đối không được nhảy qua quân cản tới ô trống (chặn xuyên ngòi tới ô trống).
/// - Khi ĂN QUÂN (ô đích có quân đối phương): Pháo di chuyển theo hàng ngang hoặc cột dọc,
///   đoạn giữa ô xuất phát và ô đích bắt buộc phải có ĐÚNG 1 quân cờ làm ngòi (CountPiecesBetween == 1).
///   Bất kể quân làm ngòi là quân ta hay quân địch.
/// - Không ngòi (0 ngòi) hoặc từ 2 ngòi trở lên: Không được ăn quân.
/// - Chặn đi chéo, đứng yên, ra ngoài bàn cờ, và chặn ăn quân cùng màu (kể cả có ngòi).
/// </summary>
public static class CannonRule
{
    /// <summary>
    /// Đếm số lượng quân cờ nằm giữa hai ô trên cùng một hàng hoặc cùng một cột (không tính hai ô đầu/cuối).
    /// Trả về -1 nếu không cùng hàng hoặc cùng cột.
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
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Pháo có hợp lệ theo luật hay không.
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

        // 3. Quân cờ tại ô xuất phát phải là quân Pháo
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Cannon)
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

        // 5. Kiểm tra ô đích
        var targetPiece = board.GetPiece(to);

        // 5a. Tuyệt đối chặn ăn quân đồng minh (dù có ngòi hay không)
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false;
        }

        int piecesBetween = CountPiecesBetween(board, from, to);
        if (piecesBetween < 0)
        {
            return false;
        }

        // 6. Trường hợp không ăn quân: Ô đích là ô TRỐNG
        if (targetPiece == null)
        {
            // Đoạn đường giữa phải hoàn toàn trống (0 vật cản, không được nhảy qua ngòi tới ô trống)
            return piecesBetween == 0;
        }

        // 7. Trường hợp ăn quân: Ô đích có QUÂN ĐỐI PHƯƠNG
        // Đoạn giữa bắt buộc phải có ĐÚNG 1 quân cờ làm ngòi (bất kể màu sắc quân ngòi)
        return piecesBetween == 1;
    }

    /// <summary>
    /// Sinh danh sách tất cả các nước đi hợp lệ cho quân Pháo tại vị trí 'from'.
    /// Quét 4 tia: Lên, Xuống, Trái, Phải.
    /// - Giai đoạn 1 (trước ngòi): các ô trống đều hợp lệ.
    /// - Khi gặp quân đầu tiên: quân này làm ngòi.
    /// - Giai đoạn 2 (sau ngòi): các ô trống bị bỏ qua, nếu gặp quân đối phương thì ăn được và dừng tia.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Cannon)
        {
            return legalMoves;
        }

        // 4 hướng quét tia: Lên (dr = -1), Xuống (dr = 1), Trái (dc = -1), Phải (dc = 1)
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
            bool hasPlatform = false; // Đã tìm thấy quân làm ngòi chưa

            while (Position.IsValid(c, r))
            {
                var targetPiece = board.GetPiece(c, r);

                if (!hasPlatform)
                {
                    // Giai đoạn 1: Chưa gặp ngòi
                    if (targetPiece == null)
                    {
                        // Ô trống: Pháo di chuyển bình thường như Xe
                        legalMoves.Add(new Position(c, r));
                    }
                    else
                    {
                        // Gặp quân cờ đầu tiên: Quân này trở thành NGÒI (không thêm vào nước đi)
                        hasPlatform = true;
                    }
                }
                else
                {
                    // Giai đoạn 2: Đã có ngòi
                    if (targetPiece != null)
                    {
                        // Gặp quân cờ thứ hai sau ngòi:
                        // Nếu là quân đối phương thì ĂN ĐƯỢC
                        if (targetPiece.Color != piece.Color)
                        {
                            legalMoves.Add(new Position(c, r));
                        }

                        // Sau khi gặp quân thứ hai (dù là ta hay địch), dừng quét tia hướng này ngay lập tức
                        break;
                    }
                    // Nếu là ô trống sau ngòi: Pháo không được nhảy tới ô trống -> bỏ qua, tiếp tục quét
                }

                c += dc;
                r += dr;
            }
        }

        return legalMoves;
    }
}
