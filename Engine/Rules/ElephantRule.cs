namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển và ăn quân chuẩn của quân Tượng / Tịnh (Elephant Rule - GCT-54).
/// Luật cờ tướng:
/// - Tượng đi chéo đúng 2 giao điểm (hình chữ Điền 田): |dCol| == 2 && |dRow| == 2 (4 hướng chéo).
/// - Chẹn mắt Tượng (塞象眼): Giao điểm chính giữa đường chéo (from.Col + dCol/2, from.Row + dRow/2)
///   được gọi là mắt Tượng. Nếu tại mắt Tượng có bất kỳ quân cờ nào (quân ta hoặc quân địch)
///   thì hướng đi đó bị cản hoàn toàn, không được nhảy qua.
/// - Không được qua sông (Tượng bất quá hà):
///   + Tượng Đen (象): Chỉ được di chuyển ở nửa trên bàn cờ (Row 0..4). Tuyệt đối cấm sang Row >= 5.
///   + Tượng Đỏ (相): Chỉ được di chuyển ở nửa dưới bàn cờ (Row 5..9). Tuyệt đối cấm sang Row <= 4.
/// - Điểm đến: Nằm trong bàn cờ, không chứa quân cùng màu (có thể ăn quân đối phương hoặc đi vào ô trống).
/// - Chặn đi chéo 1 ô (như Sĩ), đi thẳng/ngang (như Xe), chữ L (như Mã) hoặc đứng yên tại chỗ.
/// </summary>
public static class ElephantRule
{
    /// <summary>
    /// Định nghĩa 4 bước nhảy chéo hình chữ Điền và độ dời của mắt Tượng tương ứng.
    /// (dCol, dRow, eyeDCol, eyeDRow)
    /// </summary>
    public static readonly (int dCol, int dRow, int eyeDCol, int eyeDRow)[] ElephantMoves =
    [
        (-2, -2, -1, -1), // Lên - Trái
        ( 2, -2,  1, -1), // Lên - Phải
        (-2,  2, -1,  1), // Xuống - Trái
        ( 2,  2,  1,  1)  // Xuống - Phải
    ];

    /// <summary>
    /// Xác định tọa độ mắt Tượng (giao điểm chính giữa) giữa ô xuất phát và ô đích.
    /// Trả về null nếu nước đi không phải đi chéo 2 giao điểm.
    /// </summary>
    public static Position? GetEyePosition(Position from, Position to)
    {
        int dCol = to.Col - from.Col;
        int dRow = to.Row - from.Row;

        if (Math.Abs(dCol) == 2 && Math.Abs(dRow) == 2)
        {
            return new Position(from.Col + (dCol / 2), from.Row + (dRow / 2));
        }

        return null;
    }

    /// <summary>
    /// Kiểm tra xem ô mắt Tượng có bị cản bởi bất kỳ quân cờ nào (quân mình hoặc quân đối phương) hay không.
    /// </summary>
    public static bool IsEyeBlocked(ChessBoard board, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(board);

        var eyePos = GetEyePosition(from, to);
        if (eyePos == null)
        {
            return false;
        }

        return board.GetPiece(eyePos.Value) != null;
    }

    /// <summary>
    /// Kiểm tra xem hàng đích có vi phạm quy tắc "Tượng không qua sông" hay không:
    /// - Phe Đỏ: Nửa dưới (Row 5..9), vi phạm qua sông nếu Row <= 4.
    /// - Phe Đen: Nửa trên (Row 0..4), vi phạm qua sông nếu Row >= 5.
    /// </summary>
    public static bool IsCrossingRiver(PieceColor color, int targetRow)
    {
        return color switch
        {
            PieceColor.Red => targetRow <= 4,
            PieceColor.Black => targetRow >= 5,
            _ => false
        };
    }

    /// <summary>
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Tượng có hợp lệ theo luật hay không.
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

        // 3. Quân cờ tại ô xuất phát phải là quân Tượng
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Elephant)
        {
            return false;
        }

        // 4. Phải đi chéo đúng 2 giao điểm: |dCol| == 2 && |dRow| == 2
        var eyePos = GetEyePosition(from, to);
        if (eyePos == null)
        {
            return false;
        }

        // 5. Tuyệt đối không được qua sông (Tượng bất quá hà)
        if (IsCrossingRiver(piece.Color, to.Row))
        {
            return false;
        }

        // 6. Kiểm tra mắt Tượng: ô mắt Tượng ở giữa bắt buộc phải trống (chặn chẹn mắt Tượng)
        if (board.GetPiece(eyePos.Value) != null)
        {
            return false; // Bị chẹn mắt Tượng (bất kể màu quân cản)
        }

        // 7. Điểm đến không được chứa quân đồng minh
        var targetPiece = board.GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sinh danh sách tất cả các nước đi hợp lệ cho quân Tượng tại vị trí 'from'.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Elephant)
        {
            return legalMoves;
        }

        foreach (var (dCol, dRow, eyeDCol, eyeDRow) in ElephantMoves)
        {
            int targetCol = from.Col + dCol;
            int targetRow = from.Row + dRow;

            // 1. Kiểm tra điểm đến nằm trong bàn cờ
            if (!Position.IsValid(targetCol, targetRow))
            {
                continue;
            }

            // 2. Kiểm tra không được qua sông
            if (IsCrossingRiver(piece.Color, targetRow))
            {
                continue;
            }

            // 3. Kiểm tra ô mắt Tượng phải nằm trong bàn cờ và không có quân cản
            int eyeCol = from.Col + eyeDCol;
            int eyeRow = from.Row + eyeDRow;
            if (!Position.IsValid(eyeCol, eyeRow) || board.GetPiece(eyeCol, eyeRow) != null)
            {
                continue; // Bị chẹn mắt Tượng
            }

            // 4. Kiểm tra ô đích không chứa quân đồng minh
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
