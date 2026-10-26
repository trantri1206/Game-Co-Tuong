namespace GameCoTuong.Engine.Rules;

/// <summary>
/// Cài đặt quy tắc di chuyển và ăn quân chuẩn của quân Mã (Horse Rule - GCT-52).
/// Luật cờ tướng:
/// - Mã đi theo hình chữ L: 2 ô theo một trục (dọc hoặc ngang) và 1 ô theo trục còn lại (tổng cộng 8 hướng).
/// - Cản chân Mã (bán mã cước): Nếu tại ô liền kề theo hướng nhảy 2 bước có bất kỳ quân cờ nào
///   (quân ta hoặc quân địch) thì 2 hướng nhảy qua phía chân đó đều bị chặn hoàn toàn.
/// - Điểm đến: Nằm trong bàn cờ, không chứa quân cùng màu (có thể ăn quân đối phương hoặc đi vào ô trống).
/// - Mã không bị giới hạn qua sông hay cung tướng (đi tự do khắp bàn cờ).
/// </summary>
public static class HorseRule
{
    /// <summary>
    /// Định nghĩa 8 bước nhảy hình chữ L và vị trí chân cản tương ứng (dFootCol, dFootRow).
    /// </summary>
    public static readonly (int dCol, int dRow, int footDCol, int footDRow)[] HorseMoves =
    [
        // Nhảy lên trên (dRow = -2): chân cản tại (0, -1)
        (-1, -2, 0, -1),
        ( 1, -2, 0, -1),

        // Nhảy xuống dưới (dRow = +2): chân cản tại (0, +1)
        (-1,  2, 0,  1),
        ( 1,  2, 0,  1),

        // Nhảy sang trái (dCol = -2): chân cản tại (-1, 0)
        (-2, -1, -1, 0),
        (-2,  1, -1, 0),

        // Nhảy sang phải (dCol = +2): chân cản tại (+1, 0)
        ( 2, -1,  1, 0),
        ( 2,  1,  1, 0)
    ];

    /// <summary>
    /// Xác định tọa độ ô chân cản Mã (obstacle square) giữa ô xuất phát 'from' và ô đích 'to'.
    /// Trả về null nếu nước đi không phải hình chữ L (không phải nước đi của Mã).
    /// </summary>
    public static Position? GetObstacleSquare(Position from, Position to)
    {
        int dCol = to.Col - from.Col;
        int dRow = to.Row - from.Row;
        int absCol = Math.Abs(dCol);
        int absRow = Math.Abs(dRow);

        // Nhảy 2 dọc, 1 ngang: chân cản nằm ở ô liền kề theo trục dọc
        if (absRow == 2 && absCol == 1)
        {
            return new Position(from.Col, from.Row + (dRow / 2));
        }

        // Nhảy 2 ngang, 1 dọc: chân cản nằm ở ô liền kề theo trục ngang
        if (absCol == 2 && absRow == 1)
        {
            return new Position(from.Col + (dCol / 2), from.Row);
        }

        return null;
    }

    /// <summary>
    /// Kiểm tra xem hướng đi từ 'from' tới 'to' có bị cản chân Mã (bán mã cước) hay không.
    /// Nếu có bất kỳ quân nào (ta hoặc địch) đứng ở ô chân cản -> trả về true (bị cản).
    /// </summary>
    public static bool IsHobbled(ChessBoard board, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(board);

        var obstacle = GetObstacleSquare(from, to);
        if (obstacle == null)
        {
            return false;
        }

        return board.GetPiece(obstacle.Value) != null;
    }

    /// <summary>
    /// Kiểm tra nước đi từ ô 'from' tới ô 'to' của quân Mã có hợp lệ theo luật hay không.
    /// </summary>
    public static bool IsValidMove(ChessBoard board, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(board);

        // 1. Tọa độ phải nằm trong bàn cờ (0..8, 0..9)
        if (!Position.IsValid(from.Col, from.Row) || !Position.IsValid(to.Col, to.Row))
        {
            return false;
        }

        // 2. Chặn đứng yên tại chỗ
        if (from == to)
        {
            return false;
        }

        // 3. Quân cờ tại ô xuất phát phải là quân Mã
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Horse)
        {
            return false;
        }

        // 4. Kiểm tra đúng hình chữ L: (2 dọc, 1 ngang) hoặc (2 ngang, 1 dọc)
        var obstacle = GetObstacleSquare(from, to);
        if (obstacle == null)
        {
            return false;
        }

        // 5. Kiểm tra cản chân Mã: ô chân cản phải hoàn toàn trống
        if (board.GetPiece(obstacle.Value) != null)
        {
            return false; // Bị cản chân (bán mã cước), bất kể màu quân cản
        }

        // 6. Điểm đến không được có quân đồng minh
        var targetPiece = board.GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false; // Chặn ăn quân cùng màu
        }

        return true;
    }

    /// <summary>
    /// Sinh danh sách tất cả các nước đi hợp lệ cho quân Mã tại vị trí 'from'.
    /// Quét 8 hướng chữ L và lọc theo chân cản cũng như quân đồng minh.
    /// </summary>
    public static List<Position> GetLegalMoves(ChessBoard board, Position from)
    {
        ArgumentNullException.ThrowIfNull(board);

        var legalMoves = new List<Position>();
        var piece = board.GetPiece(from);
        if (piece == null || piece.Type != PieceType.Horse)
        {
            return legalMoves;
        }

        foreach (var (dCol, dRow, footDCol, footDRow) in HorseMoves)
        {
            int footCol = from.Col + footDCol;
            int footRow = from.Row + footDRow;

            // Kiểm tra ô chân cản có nằm trong bàn cờ và có quân cản hay không
            if (!Position.IsValid(footCol, footRow) || board.GetPiece(footCol, footRow) != null)
            {
                continue; // Bị cản chân hoặc ô chân ngoài bàn cờ
            }

            int targetCol = from.Col + dCol;
            int targetRow = from.Row + dRow;

            // Kiểm tra điểm đến nằm trong bàn cờ
            if (!Position.IsValid(targetCol, targetRow))
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
