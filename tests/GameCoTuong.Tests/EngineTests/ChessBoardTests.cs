using GameCoTuong.Engine;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử độc lập cho mô hình bàn cờ và quân cờ Cờ Tướng (GCT-42).
/// Hoàn toàn tách biệt khỏi mạng (SignalR) và cơ sở dữ liệu (EF Core).
/// </summary>
public class ChessBoardTests
{
    [Fact]
    public void Board_HasExactly90Intersections()
    {
        // Bàn cờ Cờ Tướng quy định: 9 cột (0..8) và 10 hàng (0..9) = 90 giao điểm
        Assert.Equal(9, ChessBoard.Cols);
        Assert.Equal(10, ChessBoard.Rows);
        Assert.Equal(90, ChessBoard.TotalIntersections);

        int validCount = 0;
        for (int r = 0; r < ChessBoard.Rows; r++)
        {
            for (int c = 0; c < ChessBoard.Cols; c++)
            {
                Assert.True(Position.IsValid(c, r));
                var pos = new Position(c, r);
                Assert.Equal(c, pos.Col);
                Assert.Equal(r, pos.Row);
                Assert.Equal(r * 9 + c, pos.ToIndex());
                validCount++;
            }
        }

        Assert.Equal(90, validCount);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(9, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 10)]
    [InlineData(-5, -5)]
    [InlineData(10, 10)]
    [InlineData(8, 10)]
    [InlineData(9, 9)]
    public void Position_RejectsOutOfBoundsCoordinates_ThrowsException(int col, int row)
    {
        // Tọa độ ngoài bàn cờ phải bị chặn
        Assert.False(Position.IsValid(col, row));
        Assert.False(Position.TryCreate(col, row, out _));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Position(col, row));
        Assert.Contains("ngoài phạm vi bàn cờ 90 giao điểm", ex.Message);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(9, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 10)]
    public void Board_GetAndSetPiece_RejectsOutOfBoundsCoordinates(int col, int row)
    {
        var board = new ChessBoard();

        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetPiece(col, row));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.SetPiece(col, row, new Piece(PieceType.General, PieceColor.Red)));
    }

    [Fact]
    public void Piece_AllSevenTypesRepresented_WithCorrectAttributes()
    {
        // 7 loại quân cờ cơ bản
        var expectedTypes = new[]
        {
            PieceType.General,
            PieceType.Advisor,
            PieceType.Elephant,
            PieceType.Horse,
            PieceType.Chariot,
            PieceType.Cannon,
            PieceType.Soldier
        };

        foreach (var type in expectedTypes)
        {
            var redPiece = new Piece(type, PieceColor.Red);
            var blackPiece = new Piece(type, PieceColor.Black);

            // Đỏ phải là chữ hoa
            Assert.True(char.IsUpper(redPiece.ToFenChar()));
            // Đen phải là chữ thường
            Assert.True(char.IsLower(blackPiece.ToFenChar()));

            // Chữ Hán khác nhau hoặc biểu thị rõ ràng
            Assert.False(string.IsNullOrEmpty(redPiece.Hanzi));
            Assert.False(string.IsNullOrEmpty(blackPiece.Hanzi));

            // Roundtrip từ ký tự FEN
            Assert.Equal(redPiece, Piece.FromFenChar(redPiece.ToFenChar()));
            Assert.Equal(blackPiece, Piece.FromFenChar(blackPiece.ToFenChar()));
        }
    }

    [Fact]
    public void Board_InitStandardBoard_SetsExactly32PiecesInStandardPositions()
    {
        var board = ChessBoard.CreateInitial();

        // Tổng cộng 32 quân
        Assert.Equal(32, board.CountPieces());
        Assert.Equal(16, board.CountPieces(PieceColor.Red));
        Assert.Equal(16, board.CountPieces(PieceColor.Black));

        // 1. Kiểm tra hàng đáy phe Đen (Row 0): Xe, Mã, Tượng, Sĩ, Tướng, Sĩ, Tượng, Mã, Xe
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Black), board.GetPiece(0, 0));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Black), board.GetPiece(1, 0));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Black), board.GetPiece(2, 0));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Black), board.GetPiece(3, 0));
        Assert.Equal(new Piece(PieceType.General, PieceColor.Black), board.GetPiece(4, 0));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Black), board.GetPiece(5, 0));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Black), board.GetPiece(6, 0));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Black), board.GetPiece(7, 0));
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Black), board.GetPiece(8, 0));

        // 2. Pháo Đen (Row 2, Col 1 & 7)
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Black), board.GetPiece(1, 2));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Black), board.GetPiece(7, 2));

        // 3. Tốt Đen (Row 3, Col 0, 2, 4, 6, 8)
        for (int c = 0; c <= 8; c += 2)
        {
            Assert.Equal(new Piece(PieceType.Soldier, PieceColor.Black), board.GetPiece(c, 3));
        }

        // 4. Binh Đỏ (Row 6, Col 0, 2, 4, 6, 8)
        for (int c = 0; c <= 8; c += 2)
        {
            Assert.Equal(new Piece(PieceType.Soldier, PieceColor.Red), board.GetPiece(c, 6));
        }

        // 5. Pháo Đỏ (Row 7, Col 1 & 7)
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), board.GetPiece(1, 7));
        Assert.Equal(new Piece(PieceType.Cannon, PieceColor.Red), board.GetPiece(7, 7));

        // 6. Hàng đáy phe Đỏ (Row 9): Xe, Mã, Tượng, Sĩ, Soái, Sĩ, Tượng, Mã, Xe
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Red), board.GetPiece(0, 9));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Red), board.GetPiece(1, 9));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Red), board.GetPiece(2, 9));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Red), board.GetPiece(3, 9));
        Assert.Equal(new Piece(PieceType.General, PieceColor.Red), board.GetPiece(4, 9));
        Assert.Equal(new Piece(PieceType.Advisor, PieceColor.Red), board.GetPiece(5, 9));
        Assert.Equal(new Piece(PieceType.Elephant, PieceColor.Red), board.GetPiece(6, 9));
        Assert.Equal(new Piece(PieceType.Horse, PieceColor.Red), board.GetPiece(7, 9));
        Assert.Equal(new Piece(PieceType.Chariot, PieceColor.Red), board.GetPiece(8, 9));

        // 7. Các ô trung tâm và sông ban đầu đều trống
        Assert.Null(board.GetPiece(4, 4));
        Assert.Null(board.GetPiece(4, 5));
        Assert.Null(board.GetPiece(0, 1));
    }

    [Fact]
    public void Position_PalaceAndRiverBoundaries_Correct()
    {
        // Cung Đen: Col 3..5, Row 0..2
        var blackGeneralPos = new Position(4, 0);
        Assert.True(blackGeneralPos.IsInBlackPalace());
        Assert.False(blackGeneralPos.IsInRedPalace());
        Assert.True(blackGeneralPos.IsBlackSide());
        Assert.False(blackGeneralPos.IsRedSide());

        // Cung Đỏ: Col 3..5, Row 7..9
        var redGeneralPos = new Position(4, 9);
        Assert.True(redGeneralPos.IsInRedPalace());
        Assert.False(redGeneralPos.IsInBlackPalace());
        Assert.True(redGeneralPos.IsRedSide());
        Assert.False(redGeneralPos.IsBlackSide());

        // Ô ngoài cung
        var outsidePalace = new Position(2, 8);
        Assert.False(outsidePalace.IsInRedPalace());
        Assert.False(outsidePalace.IsInBlackPalace());
    }

    [Fact]
    public void Board_TurnAndStateVersion_IncrementsCorrectly()
    {
        var board = ChessBoard.CreateInitial();

        // Mặc định ban đầu Đỏ đi trước
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.Equal(1, board.StateVersion);
        Assert.Equal(1, board.FullMoveNumber);

        // Đổi lượt sang Đen
        board.SwitchTurn();
        Assert.Equal(PieceColor.Black, board.ActiveColor);
        Assert.Equal(2, board.StateVersion);
        Assert.Equal(1, board.FullMoveNumber);

        // Đổi lượt lại sang Đỏ (hoàn thành 1 hiệp)
        board.SwitchTurn();
        Assert.Equal(PieceColor.Red, board.ActiveColor);
        Assert.Equal(3, board.StateVersion);
        Assert.Equal(2, board.FullMoveNumber);
    }

    [Fact]
    public void Board_FenSerialization_RoundTrip()
    {
        var board = ChessBoard.CreateInitial();
        var fen = board.ToFen();

        Assert.Equal(ChessBoard.InitialFen, fen);

        // Khôi phục từ FEN
        var parsedBoard = ChessBoard.FromFen(fen);
        Assert.Equal(32, parsedBoard.CountPieces());
        Assert.Equal(PieceColor.Red, parsedBoard.ActiveColor);
        Assert.Equal(new Position(4, 0), parsedBoard.FindGeneral(PieceColor.Black));
        Assert.Equal(new Position(4, 9), parsedBoard.FindGeneral(PieceColor.Red));
    }

    [Fact]
    public void Board_Clone_CreatesIndependentDeepCopy()
    {
        var board = ChessBoard.CreateInitial();
        var clone = board.Clone();

        Assert.Equal(board.CountPieces(), clone.CountPieces());
        Assert.Equal(board.ActiveColor, clone.ActiveColor);

        // Thay đổi clone không ảnh hưởng tới board gốc
        clone.SetPiece(4, 0, null);
        Assert.Null(clone.GetPiece(4, 0));
        Assert.NotNull(board.GetPiece(4, 0));
    }
}
