using GameCoTuong.Engine;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho chức năng kiểm tra bàn cờ ban đầu và mô phỏng nước đi (GCT-43 & GCT-106).
/// Đảm bảo kiểm chứng dữ liệu bàn cờ dùng cho luật và máy (AI):
/// - Đủ 32 quân, 58 ô trống, hai màu và vị trí ban đầu.
/// - Áp dụng rồi hoàn tác nước thường và ăn quân trả lại 100% dữ liệu gốc.
/// - Thử nhiều nhánh mô phỏng (rộng và sâu), tọa độ ngoài biên và kiểm soát lượt nghiêm ngặt.
/// - Mô phỏng tuyệt đối không sửa trận thật hoặc làm mất quân cờ.
/// </summary>
public class MoveSimulationTests
{
    [Fact]
    public void InitialSetup_PlacesAll32PiecesCorrectly()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // 1. Tổng số quân và số lượng từng bên
        Assert.Equal(32, board.CountPieces());
        Assert.Equal(16, board.CountPieces(PieceColor.Red));
        Assert.Equal(16, board.CountPieces(PieceColor.Black));

        // 2. Tướng (General / Soái)
        var redGeneral = board.GetPiece(4, 9);
        Assert.NotNull(redGeneral);
        Assert.Equal(PieceType.General, redGeneral.Type);
        Assert.Equal(PieceColor.Red, redGeneral.Color);
        Assert.Equal(new Position(4, 9), board.FindGeneral(PieceColor.Red));

        var blackGeneral = board.GetPiece(4, 0);
        Assert.NotNull(blackGeneral);
        Assert.Equal(PieceType.General, blackGeneral.Type);
        Assert.Equal(PieceColor.Black, blackGeneral.Color);
        Assert.Equal(new Position(4, 0), board.FindGeneral(PieceColor.Black));

        // 3. Sĩ (Advisor)
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Black), board.GetPiece(3, 0));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Black), board.GetPiece(5, 0));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Red), board.GetPiece(3, 9));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Red), board.GetPiece(5, 9));

        // 4. Tượng / Tịnh (Elephant)
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Black), board.GetPiece(2, 0));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Black), board.GetPiece(6, 0));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Red), board.GetPiece(2, 9));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Red), board.GetPiece(6, 9));

        // 5. Mã (Horse)
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Black), board.GetPiece(1, 0));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Black), board.GetPiece(7, 0));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Red), board.GetPiece(1, 9));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Red), board.GetPiece(7, 9));

        // 6. Xe (Chariot)
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Black), board.GetPiece(0, 0));
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Black), board.GetPiece(8, 0));
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Red), board.GetPiece(0, 9));
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Red), board.GetPiece(8, 9));

        // 7. Pháo (Cannon)
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Black), board.GetPiece(1, 2));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Black), board.GetPiece(7, 2));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), board.GetPiece(1, 7));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), board.GetPiece(7, 7));

        // 8. Tốt / Binh (Soldier)
        for (int col = 0; col <= 8; col += 2)
        {
            Assert.Equal(new Piece(PieceType.Soldier, PieceColor.Black), board.GetPiece(col, 3));
            Assert.Equal(new Piece(PieceType.Soldier, PieceColor.Red), board.GetPiece(col, 6));
        }

        // 9. Lượt đi khởi đầu là phe Đỏ
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.Equal(0, board.HalfMoveClock);
        Assert.Equal(1, board.FullMoveNumber);
        Assert.Equal(1, board.StateVersion);
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void InitialSetup_Exact58EmptySquares_AndTotal90Squares()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        int occupiedCount = 0;
        int emptyCount = 0;

        for (int r = 0; r < ChessBoard.Rows; r++)
        {
            for (int c = 0; c < ChessBoard.Cols; c++)
            {
                var piece = board.GetPiece(c, r);
                if (piece != null)
                {
                    occupiedCount++;
                }
                else
                {
                    emptyCount++;
                }
            }
        }

        Assert.Equal(32, occupiedCount);
        Assert.Equal(58, emptyCount);
        Assert.Equal(90, occupiedCount + emptyCount);
    }

    [Fact]
    public void MakeMove_NonCapturingMove_UpdatesBoardAndTogglesTurn()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // Đỏ đi Pháo 2 bình 5: (1, 7) -> (4, 7)
        var from = new Position(1, 7);
        var to = new Position(4, 7);
        var undo = board.MakeMove(from, to);

        Assert.Equal(from, undo.From);
        Assert.Equal(to, undo.To);
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), undo.MovedPiece);
        Assert.Null(undo.CapturedPiece);
        Assert.False(undo.HasCaptured);
        Assert.Equal(PieceColor.Red, undo.PreviousActiveColor);

        // Kiểm tra bàn cờ sau nước đi
        Assert.Null(board.GetPiece(1, 7));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), board.GetPiece(4, 7));

        // Lượt chuyển sang Đen, phiên bản tăng
        Assert.Equal(PieceColor.Black, board.ActiveColor);
        Assert.Equal(2, board.StateVersion);
        Assert.Equal(1, board.HalfMoveClock);
        Assert.Equal(1, board.FullMoveNumber);
        Assert.True(board.CanUndo);
        Assert.Equal(1, board.UndoCount);
    }

    [Fact]
    public void MakeMove_CapturingMove_RecordsCapturedPieceCorrectly()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // Nước 1: Đỏ đi Pháo (1, 7) -> (4, 7)
        board.MakeMove(new Position(1, 7), new Position(4, 7));

        // Nước 2: Đen đi Pháo (1, 2) -> (4, 2)
        board.MakeMove(new Position(1, 2), new Position(4, 2));
        Assert.Equal(2, board.FullMoveNumber); // Sau khi Đen hoàn thành, FullMoveNumber tăng lên 2

        // Nước 3: Đỏ ăn Pháo Đen: (4, 7) -> (4, 2)
        var undoCapture = board.MakeMove(new Position(4, 7), new Position(4, 2));

        Assert.True(undoCapture.HasCaptured);
        Assert.NotNull(undoCapture.CapturedPiece);
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Black), undoCapture.CapturedPiece);
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), undoCapture.MovedPiece);

        // Tổng quân số giảm xuống 31
        Assert.Equal(31, board.CountPieces());
        Assert.Equal(15, board.CountPieces(PieceColor.Black));
        Assert.Equal(16, board.CountPieces(PieceColor.Red));

        // Ăn quân thì HalfMoveClock được reset về 0
        Assert.Equal(0, board.HalfMoveClock);
    }

    [Fact]
    public void UndoMove_RestoresOriginalState100Percent()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string initialFen = board.ToFen();

        // 1. Thử nước đi không ăn quân
        var move1 = new MoveAction(1, 7, 4, 7);
        board.MakeMove(move1);

        // Hoàn tác
        var undone1 = board.UndoMove();
        Assert.Equal(move1.From, undone1.From);
        Assert.Equal(move1.To, undone1.To);
        Assert.Equal(initialFen, board.ToFen());
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.Equal(32, board.CountPieces());
        Assert.False(board.CanUndo);

        // 2. Thử chuỗi nước đi có ăn quân rồi hoàn tác
        board.MakeMove(new Position(1, 7), new Position(4, 7)); // Đỏ
        board.MakeMove(new Position(1, 2), new Position(4, 2)); // Đen
        board.MakeMove(new Position(4, 7), new Position(4, 2)); // Đỏ ăn Pháo Đen

        Assert.Equal(31, board.CountPieces());
        Assert.Equal(3, board.UndoCount);

        // Hoàn tác nước 3 (ăn quân)
        var undoneCapture = board.UndoMove();
        Assert.True(undoneCapture.HasCaptured);
        Assert.Equal(32, board.CountPieces()); // Quân Đen được phục hồi
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Black), board.GetPiece(4, 2));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), board.GetPiece(4, 7));
        Assert.Equal(PieceColor.Red, board.ActiveColor);

        // Hoàn tác tiếp nước 2 và 1
        board.UndoMove();
        board.UndoMove();

        Assert.Equal(initialFen, board.ToFen());
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void SequenceOfMoves_UndoInReverse_MatchesOriginalFenExactly()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string originalFen = board.ToFen();

        // Thực hiện 4 nước đi chuẩn khai cuộc
        board.MakeMove(new Position(1, 7), new Position(4, 7)); // Pháo 2 bình 5
        board.MakeMove(new Position(1, 0), new Position(2, 2)); // Mã 2 tiến 3
        board.MakeMove(new Position(1, 9), new Position(2, 7)); // Mã 2 tiến 3
        board.MakeMove(new Position(4, 3), new Position(4, 4)); // Tốt 5 tiến 1

        Assert.Equal(4, board.UndoCount);
        Assert.NotEqual(originalFen, board.ToFen());

        // Hoàn tác từng nước
        while (board.CanUndo)
        {
            board.UndoMove();
        }

        Assert.Equal(0, board.UndoCount);
        Assert.Equal(originalFen, board.ToFen());
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.Equal(1, board.FullMoveNumber);
        Assert.Equal(0, board.HalfMoveClock);
    }

    [Fact]
    public void Simulation_MultiBranchBreadth_DoesNotMutateRealGame()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string initialFen = board.ToFen();
        int initialVersion = board.StateVersion;

        // Mô phỏng 4 nhánh mở nước khác nhau của Đỏ từ trạng thái gốc
        var candidateMoves = new[]
        {
            new MoveAction(1, 7, 4, 7), // Pháo 2 bình 5
            new MoveAction(1, 9, 2, 7), // Mã 2 tiến 3
            new MoveAction(4, 6, 4, 5), // Binh 5 tiến 1
            new MoveAction(7, 9, 6, 7)  // Mã 8 tiến 7
        };

        foreach (var move in candidateMoves)
        {
            board.Simulate(move, sandbox =>
            {
                // Trong sandbox: đã đổi lượt sang Đen, ô xuất phát trống
                Assert.Equal(PieceColor.Black, sandbox.ActiveColor);
                Assert.Null(sandbox.GetPiece(move.From));
                Assert.NotNull(sandbox.GetPiece(move.To));
                Assert.Equal(32, sandbox.CountPieces());
            });

            // Sau mỗi nhánh, bàn cờ thật phải ngay lập tức hoàn nguyên
            Assert.Equal(initialFen, board.ToFen());
            Assert.Equal(PieceColor.Red, board.ActiveColor);
            Assert.Equal(initialVersion, board.StateVersion);
            Assert.Equal(32, board.CountPieces());
            Assert.False(board.CanUndo);
        }

        // Sau toàn bộ các nhánh mô phỏng, bàn cờ thật tuyệt đối không mất quân
        Assert.Equal(initialFen, board.ToFen());
        Assert.Equal(32, board.CountPieces());
    }

    [Fact]
    public void Simulation_DeepNestedBranches_UnwindsCleanlyWithoutPieceLoss()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string rootFen = board.ToFen();

        // Cây tìm kiếm nước đi sâu 3 tầng (Minimax simulation depth = 3)
        // Level 1 (Đỏ): Pháo (1, 7) -> (4, 7)
        board.Simulate(new Position(1, 7), new Position(4, 7), lvl1Board =>
        {
            Assert.Equal(PieceColor.Black, lvl1Board.ActiveColor);

            // Level 2 (Đen): Mã (1, 0) -> (2, 2)
            lvl1Board.Simulate(new Position(1, 0), new Position(2, 2), lvl2Board =>
            {
                Assert.Equal(PieceColor.Red, lvl2Board.ActiveColor);

                // Level 3 (Đỏ): Mã (1, 9) -> (2, 7)
                lvl2Board.Simulate(new Position(1, 9), new Position(2, 7), lvl3Board =>
                {
                    Assert.Equal(PieceColor.Black, lvl3Board.ActiveColor);
                    Assert.Equal(32, lvl3Board.CountPieces());
                    Assert.NotNull(lvl3Board.GetPiece(2, 7));
                    Assert.NotNull(lvl3Board.GetPiece(2, 2));
                    Assert.NotNull(lvl3Board.GetPiece(4, 7));
                });

                // Sau Level 3 unwound: Level 2 vẫn giữ nguyên trạng thái của nó
                Assert.Equal(PieceColor.Red, lvl2Board.ActiveColor);
                Assert.Null(lvl2Board.GetPiece(2, 7)); // Nước đi level 3 đã được hoàn tác
            });

            // Sau Level 2 unwound: Level 1 vẫn giữ nguyên
            Assert.Equal(PieceColor.Black, lvl1Board.ActiveColor);
            Assert.Null(lvl1Board.GetPiece(2, 2));
        });

        // Sau toàn bộ cây tìm kiếm: Bàn cờ gốc nguyên vẹn 100%, không mất quân
        Assert.Equal(rootFen, board.ToFen());
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.Equal(32, board.CountPieces());
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void Simulate_Action_DoesNotModifyRealBoard()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string originalFen = board.ToFen();
        int originalVersion = board.StateVersion;

        // Mô phỏng nước đi trong sandbox
        board.Simulate(new Position(1, 7), new Position(4, 7), sandboxBoard =>
        {
            // Trong sandbox, nước đi đã được áp dụng
            Assert.Null(sandboxBoard.GetPiece(1, 7));
            Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), sandboxBoard.GetPiece(4, 7));
            Assert.Equal(PieceColor.Black, sandboxBoard.ActiveColor);
        });

        // Sau khi ra khỏi sandbox, bàn cờ thật được khôi phục nguyên vẹn
        Assert.Equal(originalFen, board.ToFen());
        Assert.Equal(originalVersion, board.StateVersion);
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void Simulate_Func_ReturnsComputedValueAndRestoresState()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string originalFen = board.ToFen();

        int pieceCountDuringMove = board.Simulate(new Position(1, 7), new Position(4, 7), b =>
        {
            return b.CountPieces();
        });

        Assert.Equal(32, pieceCountDuringMove);
        Assert.Equal(originalFen, board.ToFen());
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void Simulate_WhenExceptionThrown_StillGuaranteesUndo()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();
        string originalFen = board.ToFen();

        Assert.Throws<InvalidOperationException>(() =>
        {
            board.Simulate(new Position(1, 7), new Position(4, 7), b =>
            {
                throw new InvalidOperationException("Lỗi giả lập trong tính toán AI");
            });
        });

        // Bàn cờ vẫn phải được khôi phục 100% nhờ khối finally
        Assert.Equal(originalFen, board.ToFen());
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void Clone_CreatesIndependentDeepCopy()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        var clone = board.Clone();

        // Thao tác trên bản clone
        clone.MakeMove(new Position(1, 7), new Position(4, 7));
        Assert.Equal(PieceColor.Black, clone.ActiveColor);
        Assert.Null(clone.GetPiece(1, 7));

        // Bàn cờ gốc không hề bị ảnh hưởng
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.NotNull(board.GetPiece(1, 7));
        Assert.Null(board.GetPiece(4, 7));
        Assert.False(board.CanUndo);
    }

    [Fact]
    public void Coordinates_OutOfBounds_RejectsAllOperations()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        var outOfBounds = new (int c, int r)[]
        {
            (-1, 0), (9, 0), (0, -1), (0, 10), (-5, -5), (10, 10), (100, 100)
        };

        foreach (var (c, r) in outOfBounds)
        {
            Assert.False(Position.IsValid(c, r));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetPiece(c, r));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.SetPiece(c, r, new Piece(PieceType.General, PieceColor.Red)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Position(c, r));
        }

        // Bàn cờ không hề bị biến đổi sau các thao tác lỗi
        Assert.Equal(32, board.CountPieces());
        Assert.Equal(PieceColor.Red, board.ActiveColor);
    }

    [Fact]
    public void TurnOrder_StrictEnforcement_CannotMoveOutOfTurn()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // Lượt của Đỏ: Cố tình đi quân Đen ở (0, 0) -> lỗi
        var ex = Assert.Throws<InvalidOperationException>(() => board.MakeMove(new Position(0, 0), new Position(0, 1)));
        Assert.Contains("Chưa đến lượt đi của phe Black", ex.Message);

        // Đỏ đi hợp lệ: Pháo (1, 7) -> (4, 7)
        board.MakeMove(new Position(1, 7), new Position(4, 7));

        // Bây giờ lượt của Đen: Cố tình đi quân Đỏ ở (0, 9) -> lỗi
        var ex2 = Assert.Throws<InvalidOperationException>(() => board.MakeMove(new Position(0, 9), new Position(0, 8)));
        Assert.Contains("Chưa đến lượt đi của phe Red", ex2.Message);

        // Sau khi hoàn tác: Đỏ lại được đi
        board.UndoMove();
        Assert.Equal(PieceColor.Red, board.ActiveColor);
    }

    [Fact]
    public void MakeMove_RejectsInvalidOperations()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // 1. Di chuyển ô trống
        Assert.Throws<InvalidOperationException>(() => board.MakeMove(new Position(0, 5), new Position(0, 4)));

        // 2. Đi quân đối phương khi chưa tới lượt (lượt Đỏ nhưng đi quân Đen ở (0, 0))
        Assert.Throws<InvalidOperationException>(() => board.MakeMove(new Position(0, 0), new Position(0, 1)));

        // 3. Ăn quân đồng minh (Tướng ăn Sĩ của mình)
        Assert.Throws<InvalidOperationException>(() => board.MakeMove(new Position(4, 9), new Position(3, 9)));

        // 4. Di chuyển trùng ô xuất phát và ô đích
        Assert.Throws<ArgumentException>(() => new MoveAction(new Position(1, 7), new Position(1, 7)));
    }

    [Fact]
    public void UndoMove_WhenStackEmpty_ThrowsInvalidOperationException()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        Assert.False(board.CanUndo);
        Assert.Throws<InvalidOperationException>(() => board.UndoMove());
    }
}
