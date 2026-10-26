using System.Text;
using GameCoTuong.Engine.Rules;

namespace GameCoTuong.Engine;

/// <summary>
/// Mô hình trạng thái bàn cờ Cờ Tướng 90 giao điểm (9 cột x 10 hàng).
/// Độc lập hoàn toàn khỏi kết nối mạng và cơ sở dữ liệu.
/// </summary>
public class ChessBoard
{
    public const int Cols = 9; // Cột 0..8
    public const int Rows = 10; // Hàng 0..9
    public const int TotalIntersections = 90;

    // Chuỗi FEN khởi đầu thế cờ Cờ Tướng tiêu chuẩn
    public const string InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1";

    private readonly Piece?[,] _grid = new Piece?[Cols, Rows];
    private readonly Stack<UndoMoveRecord> _undoStack = new();

    /// <summary>
    /// Cho biết hiện tại có thể hoàn tác (Undo) nước đi vừa thực hiện hay không.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Số lượng nước đi trong ngăn xếp hoàn tác hiện tại.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Bên tới lượt đi hiện tại (Mặc định Đỏ đi trước).
    /// </summary>
    public PieceColor ActiveColor { get; set; } = PieceColor.Red;

    /// <summary>
    /// Phiên bản trạng thái của bàn cờ (Tăng lên sau mỗi biến đổi trạng thái).
    /// </summary>
    public int StateVersion { get; set; } = 1;

    /// <summary>
    /// Số nửa nước đi kể từ lần ăn quân hoặc đi tốt gần nhất (phục vụ luật hòa 60 nước).
    /// </summary>
    public int HalfMoveClock { get; set; } = 0;

    /// <summary>
    /// Số thứ tự lượt đấu (bắt đầu từ 1, tăng sau mỗi nước đi của bên Đen).
    /// </summary>
    public int FullMoveNumber { get; set; } = 1;

    public ChessBoard()
    {
    }

    /// <summary>
    /// Tạo một bàn cờ với thế cờ ban đầu chuẩn (32 quân cờ).
    /// </summary>
    public static ChessBoard CreateInitial()
    {
        var board = new ChessBoard();
        board.InitStandardBoard();
        return board;
    }

    /// <summary>
    /// Lấy quân cờ tại một vị trí an toàn. Ném ArgumentOutOfRangeException nếu ngoài bàn cờ.
    /// </summary>
    public Piece? GetPiece(int col, int row)
    {
        if (!Position.IsValid(col, row))
        {
            throw new ArgumentOutOfRangeException(
                $"Tọa độ ({col}, {row}) nằm ngoài phạm vi bàn cờ (Cột 0–8, Hàng 0–9).");
        }

        return _grid[col, row];
    }

    /// <summary>
    /// Lấy quân cờ tại một vị trí struct Position.
    /// </summary>
    public Piece? GetPiece(Position pos) => _grid[pos.Col, pos.Row];

    /// <summary>
    /// Đặt quân cờ tại một vị trí (hoặc null để xóa quân).
    /// </summary>
    public void SetPiece(int col, int row, Piece? piece)
    {
        if (!Position.IsValid(col, row))
        {
            throw new ArgumentOutOfRangeException(
                $"Tọa độ ({col}, {row}) nằm ngoài phạm vi bàn cờ (Cột 0–8, Hàng 0–9).");
        }

        _grid[col, row] = piece;
        StateVersion++;
    }

    /// <summary>
    /// Đặt quân cờ tại một vị trí struct Position.
    /// </summary>
    public void SetPiece(Position pos, Piece? piece)
    {
        _grid[pos.Col, pos.Row] = piece;
        StateVersion++;
    }

    /// <summary>
    /// Xóa toàn bộ quân cờ trên 90 giao điểm.
    /// </summary>
    public void Clear()
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                _grid[c, r] = null;
            }
        }

        ActiveColor = PieceColor.Red;
        HalfMoveClock = 0;
        FullMoveNumber = 1;
        _undoStack.Clear();
        StateVersion++;
    }

    /// <summary>
    /// Khởi tạo thế cờ tiêu chuẩn của Cờ Tướng:
    /// Đen ở trên (Hàng 0–4), Đỏ ở dưới (Hàng 5–9). Tổng cộng 32 quân.
    /// </summary>
    public void SetupInitialPosition() => InitStandardBoard();

    public void InitStandardBoard()
    {
        Clear();

        // 1. Phe Đen (Hàng 0–4)
        // Hàng đáy 0: Xe, Mã, Tượng, Sĩ, Tướng, Sĩ, Tượng, Mã, Xe
        SetPieceInternal(0, 0, new Piece(PieceType.Chariot, PieceColor.Black));
        SetPieceInternal(1, 0, new Piece(PieceType.Horse, PieceColor.Black));
        SetPieceInternal(2, 0, new Piece(PieceType.Elephant, PieceColor.Black));
        SetPieceInternal(3, 0, new Piece(PieceType.Advisor, PieceColor.Black));
        SetPieceInternal(4, 0, new Piece(PieceType.General, PieceColor.Black));
        SetPieceInternal(5, 0, new Piece(PieceType.Advisor, PieceColor.Black));
        SetPieceInternal(6, 0, new Piece(PieceType.Elephant, PieceColor.Black));
        SetPieceInternal(7, 0, new Piece(PieceType.Horse, PieceColor.Black));
        SetPieceInternal(8, 0, new Piece(PieceType.Chariot, PieceColor.Black));

        // Hàng 2: 2 Pháo Đen
        SetPieceInternal(1, 2, new Piece(PieceType.Cannon, PieceColor.Black));
        SetPieceInternal(7, 2, new Piece(PieceType.Cannon, PieceColor.Black));

        // Hàng 3: 5 Tốt Đen
        SetPieceInternal(0, 3, new Piece(PieceType.Soldier, PieceColor.Black));
        SetPieceInternal(2, 3, new Piece(PieceType.Soldier, PieceColor.Black));
        SetPieceInternal(4, 3, new Piece(PieceType.Soldier, PieceColor.Black));
        SetPieceInternal(6, 3, new Piece(PieceType.Soldier, PieceColor.Black));
        SetPieceInternal(8, 3, new Piece(PieceType.Soldier, PieceColor.Black));

        // 2. Phe Đỏ (Hàng 5–9)
        // Hàng 6: 5 Binh Đỏ
        SetPieceInternal(0, 6, new Piece(PieceType.Soldier, PieceColor.Red));
        SetPieceInternal(2, 6, new Piece(PieceType.Soldier, PieceColor.Red));
        SetPieceInternal(4, 6, new Piece(PieceType.Soldier, PieceColor.Red));
        SetPieceInternal(6, 6, new Piece(PieceType.Soldier, PieceColor.Red));
        SetPieceInternal(8, 6, new Piece(PieceType.Soldier, PieceColor.Red));

        // Hàng 7: 2 Pháo Đỏ
        SetPieceInternal(1, 7, new Piece(PieceType.Cannon, PieceColor.Red));
        SetPieceInternal(7, 7, new Piece(PieceType.Cannon, PieceColor.Red));

        // Hàng đáy 9: Xe, Mã, Tượng, Sĩ, Soái, Sĩ, Tượng, Mã, Xe
        SetPieceInternal(0, 9, new Piece(PieceType.Chariot, PieceColor.Red));
        SetPieceInternal(1, 9, new Piece(PieceType.Horse, PieceColor.Red));
        SetPieceInternal(2, 9, new Piece(PieceType.Elephant, PieceColor.Red));
        SetPieceInternal(3, 9, new Piece(PieceType.Advisor, PieceColor.Red));
        SetPieceInternal(4, 9, new Piece(PieceType.General, PieceColor.Red));
        SetPieceInternal(5, 9, new Piece(PieceType.Advisor, PieceColor.Red));
        SetPieceInternal(6, 9, new Piece(PieceType.Elephant, PieceColor.Red));
        SetPieceInternal(7, 9, new Piece(PieceType.Horse, PieceColor.Red));
        SetPieceInternal(8, 9, new Piece(PieceType.Chariot, PieceColor.Red));

        ActiveColor = PieceColor.Red;
        StateVersion = 1;
        HalfMoveClock = 0;
        FullMoveNumber = 1;
    }

    private void SetPieceInternal(int col, int row, Piece piece)
    {
        _grid[col, row] = piece;
    }

    /// <summary>
    /// Đổi lượt đi giữa Đỏ và Đen, cập nhật số hiệp và phiên bản trạng thái.
    /// </summary>
    public void SwitchTurn()
    {
        if (ActiveColor == PieceColor.Black)
        {
            FullMoveNumber++;
            ActiveColor = PieceColor.Red;
        }
        else
        {
            ActiveColor = PieceColor.Black;
        }

        StateVersion++;
    }

    /// <summary>
    /// Thực hiện nước đi trên bàn cờ, ghi lại quân bị ăn (nếu có), đổi lượt và lưu vào lịch sử hoàn tác.
    /// </summary>
    public UndoMoveRecord MakeMove(MoveAction move) => MakeMove(move.From, move.To);

    /// <summary>
    /// Thực hiện nước đi từ ô xuất phát (from) tới ô đích (to).
    /// </summary>
    public UndoMoveRecord MakeMove(Position from, Position to)
    {
        var piece = GetPiece(from);
        if (piece == null)
        {
            throw new InvalidOperationException($"Không có quân cờ nào tại ô xuất phát {from}.");
        }

        if (piece.Color != ActiveColor)
        {
            throw new InvalidOperationException(
                $"Chưa đến lượt đi của phe {piece.Color}. Hiện tại đang là lượt của phe {ActiveColor}.");
        }

        var targetPiece = GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            throw new InvalidOperationException(
                $"Không thể di chuyển quân {piece} vào ô {to} vì đã có quân cùng phe {targetPiece}.");
        }

        // Tạo bản ghi hoàn tác trước khi thay đổi trạng thái
        var undoRecord = new UndoMoveRecord(
            from: from,
            to: to,
            movedPiece: piece,
            capturedPiece: targetPiece,
            previousActiveColor: ActiveColor,
            previousHalfMoveClock: HalfMoveClock,
            previousFullMoveNumber: FullMoveNumber,
            previousStateVersion: StateVersion
        );

        // Di chuyển quân
        _grid[to.Col, to.Row] = piece;
        _grid[from.Col, from.Row] = null;

        // Cập nhật bộ đếm nửa nước (HalfMoveClock): reset nếu ăn quân hoặc đi Binh/Tốt
        if (targetPiece != null || piece.Type == PieceType.Soldier)
        {
            HalfMoveClock = 0;
        }
        else
        {
            HalfMoveClock++;
        }

        // Đổi lượt đi
        if (ActiveColor == PieceColor.Black)
        {
            FullMoveNumber++;
            ActiveColor = PieceColor.Red;
        }
        else
        {
            ActiveColor = PieceColor.Black;
        }

        StateVersion++;
        _undoStack.Push(undoRecord);

        return undoRecord;
    }

    /// <summary>
    /// Kiểm tra xem một nước đi có hợp lệ theo luật cờ của quân cờ tương ứng hay không.
    /// </summary>
    public bool IsValidMove(MoveAction move) => IsValidMove(move.From, move.To);

    public bool IsValidMove(Position from, Position to)
    {
        if (!Position.IsValid(from.Col, from.Row) || !Position.IsValid(to.Col, to.Row))
        {
            return false;
        }

        var piece = GetPiece(from);
        if (piece == null || piece.Color != ActiveColor)
        {
            return false;
        }

        var targetPiece = GetPiece(to);
        if (targetPiece != null && targetPiece.Color == piece.Color)
        {
            return false;
        }

        return piece.Type switch
        {
            PieceType.Soldier => SoldierRule.IsValidMove(this, from, to),
            PieceType.Chariot => ChariotRule.IsValidMove(this, from, to),
            PieceType.Horse => HorseRule.IsValidMove(this, from, to),
            PieceType.Cannon => CannonRule.IsValidMove(this, from, to),
            _ => true
        };
    }

    /// <summary>
    /// Lấy danh sách các nước đi hợp lệ cho quân cờ tại vị trí 'from'.
    /// </summary>
    public List<Position> GetLegalMoves(Position from)
    {
        if (!Position.IsValid(from.Col, from.Row))
        {
            return new List<Position>();
        }

        var piece = GetPiece(from);
        if (piece == null)
        {
            return new List<Position>();
        }

        return piece.Type switch
        {
            PieceType.Soldier => SoldierRule.GetLegalMoves(this, from),
            PieceType.Chariot => ChariotRule.GetLegalMoves(this, from),
            PieceType.Horse => HorseRule.GetLegalMoves(this, from),
            PieceType.Cannon => CannonRule.GetLegalMoves(this, from),
            _ => new List<Position>()
        };
    }

    /// <summary>
    /// Hoàn tác nước đi gần nhất từ ngăn xếp hoàn tác.
    /// Khôi phục chính xác 100% quân cờ, vị trí, quân bị ăn, bên tới lượt và phiên bản trạng thái.
    /// </summary>
    public UndoMoveRecord UndoMove()
    {
        if (_undoStack.Count == 0)
        {
            throw new InvalidOperationException("Không có nước đi nào trong lịch sử để hoàn tác.");
        }

        var undoRecord = _undoStack.Pop();
        UndoMove(undoRecord);
        return undoRecord;
    }

    /// <summary>
    /// Hoàn tác một nước đi cụ thể theo bản ghi UndoMoveRecord.
    /// </summary>
    public void UndoMove(UndoMoveRecord undoRecord)
    {
        ArgumentNullException.ThrowIfNull(undoRecord);

        // Khôi phục quân cờ về ô xuất phát
        _grid[undoRecord.From.Col, undoRecord.From.Row] = undoRecord.MovedPiece;

        // Khôi phục quân bị ăn (hoặc null) về ô đích
        _grid[undoRecord.To.Col, undoRecord.To.Row] = undoRecord.CapturedPiece;

        // Khôi phục các biến trạng thái
        ActiveColor = undoRecord.PreviousActiveColor;
        HalfMoveClock = undoRecord.PreviousHalfMoveClock;
        FullMoveNumber = undoRecord.PreviousFullMoveNumber;
        StateVersion = undoRecord.PreviousStateVersion;
    }

    /// <summary>
    /// Mô phỏng an toàn một nước đi: áp dụng nước đi, chạy logic thử nghiệm, và luôn hoàn tác sau khi hoàn thành.
    /// Đảm bảo trạng thái ván cờ thật đang chơi tuyệt đối không bị thay đổi.
    /// </summary>
    public void Simulate(MoveAction move, Action<ChessBoard> action) => Simulate(move.From, move.To, action);

    public void Simulate(Position from, Position to, Action<ChessBoard> action)
    {
        var undo = MakeMove(from, to);
        try
        {
            action(this);
        }
        finally
        {
            if (_undoStack.Count > 0 && ReferenceEquals(_undoStack.Peek(), undo))
            {
                _undoStack.Pop();
            }
            UndoMove(undo);
        }
    }

    /// <summary>
    /// Mô phỏng an toàn một nước đi và trả về kết quả tính toán.
    /// </summary>
    public T Simulate<T>(MoveAction move, Func<ChessBoard, T> func) => Simulate(move.From, move.To, func);

    public T Simulate<T>(Position from, Position to, Func<ChessBoard, T> func)
    {
        var undo = MakeMove(from, to);
        try
        {
            return func(this);
        }
        finally
        {
            if (_undoStack.Count > 0 && ReferenceEquals(_undoStack.Peek(), undo))
            {
                _undoStack.Pop();
            }
            UndoMove(undo);
        }
    }

    /// <summary>
    /// Tìm vị trí Tướng / Soái của phe tương ứng.
    /// </summary>
    public Position? FindGeneral(PieceColor color)
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var piece = _grid[c, r];
                if (piece != null && piece.Color == color && piece.Type == PieceType.General)
                {
                    return new Position(c, r);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Đếm tổng số quân cờ hiện có trên bàn.
    /// </summary>
    public int CountPieces()
    {
        int count = 0;
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                if (_grid[c, r] != null) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Đếm số quân của một phe cụ thể (tùy chọn theo loại quân).
    /// </summary>
    public int CountPieces(PieceColor color, PieceType? type = null)
    {
        int count = 0;
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var p = _grid[c, r];
                if (p != null && p.Color == color)
                {
                    if (!type.HasValue || p.Type == type.Value)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ quân cờ đang có mặt trên bàn kèm tọa độ.
    /// </summary>
    public IEnumerable<(Position Position, Piece Piece)> GetAllPieces()
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var piece = _grid[c, r];
                if (piece != null)
                {
                    yield return (new Position(c, r), piece);
                }
            }
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái bàn cờ sang chuỗi FEN chuẩn Cờ Tướng.
    /// </summary>
    public string ToFen()
    {
        var sb = new StringBuilder();

        // 1. Bàn cờ từ hàng 0 (trên) xuống hàng 9 (dưới)
        for (int r = 0; r < Rows; r++)
        {
            int emptyCount = 0;
            for (int c = 0; c < Cols; c++)
            {
                var piece = _grid[c, r];
                if (piece == null)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        sb.Append(emptyCount);
                        emptyCount = 0;
                    }
                    sb.Append(piece.ToFenChar());
                }
            }

            if (emptyCount > 0)
            {
                sb.Append(emptyCount);
            }

            if (r < Rows - 1)
            {
                sb.Append('/');
            }
        }

        // 2. Bên tới lượt (w = Red/White, b = Black)
        sb.Append(ActiveColor == PieceColor.Red ? " w" : " b");

        // 3. Castling & En Passant (Cờ Tướng không có, dùng '-' '-')
        sb.Append(" - - ");

        // 4. Halfmove & Fullmove
        sb.Append(HalfMoveClock);
        sb.Append(' ');
        sb.Append(FullMoveNumber);

        return sb.ToString();
    }

    /// <summary>
    /// Khôi phục trạng thái bàn cờ từ chuỗi FEN.
    /// </summary>
    public static ChessBoard FromFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen))
        {
            throw new ArgumentException("Chuỗi FEN không được để trống.", nameof(fen));
        }

        var parts = fen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var boardParts = parts[0].Split('/');

        if (boardParts.Length != Rows)
        {
            throw new FormatException($"Chuỗi FEN phải có đúng 10 hàng, nhận được {boardParts.Length} hàng.");
        }

        var board = new ChessBoard();
        board.Clear();

        for (int r = 0; r < Rows; r++)
        {
            string rowStr = boardParts[r];
            int col = 0;

            foreach (char ch in rowStr)
            {
                if (char.IsDigit(ch))
                {
                    col += ch - '0';
                }
                else
                {
                    if (col >= Cols)
                    {
                        throw new FormatException($"Hàng {r} trong FEN vượt quá 9 cột.");
                    }

                    var piece = Piece.FromFenChar(ch);
                    board.SetPieceInternal(col, r, piece);
                    col++;
                }
            }

            if (col != Cols)
            {
                throw new FormatException($"Hàng {r} trong FEN không đủ 9 cột (nhận được {col}).");
            }
        }

        // Lượt đi
        if (parts.Length > 1)
        {
            board.ActiveColor = parts[1].ToLowerInvariant() == "b" ? PieceColor.Black : PieceColor.Red;
        }

        // Halfmove
        if (parts.Length > 4 && int.TryParse(parts[4], out int halfMove))
        {
            board.HalfMoveClock = halfMove;
        }

        // Fullmove
        if (parts.Length > 5 && int.TryParse(parts[5], out int fullMove))
        {
            board.FullMoveNumber = fullMove;
        }

        board.StateVersion = 1;
        return board;
    }

    /// <summary>
    /// Tạo bản sao độc lập (Deep Copy) của bàn cờ.
    /// </summary>
    public ChessBoard Clone()
    {
        var clone = new ChessBoard
        {
            ActiveColor = this.ActiveColor,
            StateVersion = this.StateVersion,
            HalfMoveClock = this.HalfMoveClock,
            FullMoveNumber = this.FullMoveNumber
        };

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var p = _grid[c, r];
                clone._grid[c, r] = p != null ? new Piece(p.Type, p.Color) : null;
            }
        }

        return clone;
    }
}
