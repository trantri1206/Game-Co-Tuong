using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển và cản chân của quân Mã (Horse Rule - GCT-52).
/// </summary>
public class HorseRuleTests
{
    [Fact]
    public void Horse_CenterEmptyBoard_CanMoveAllEightDirections()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Mã Đỏ tại trung tâm (4, 4)
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        var legalMoves = board.GetLegalMoves(from);

        // Đầy đủ 8 hướng chữ L khi không bị cản chân
        Assert.Equal(8, legalMoves.Count);

        var expectedMoves = new[]
        {
            new Position(3, 2), new Position(5, 2), // Nhảy lên trên
            new Position(3, 6), new Position(5, 6), // Nhảy xuống dưới
            new Position(2, 3), new Position(2, 5), // Nhảy sang trái
            new Position(6, 3), new Position(6, 5)  // Nhảy sang phải
        };

        foreach (var dest in expectedMoves)
        {
            Assert.Contains(dest, legalMoves);
            Assert.True(HorseRule.IsValidMove(board, from, dest));
            Assert.True(board.IsValidMove(from, dest));
        }
    }

    [Fact]
    public void Horse_BlackPiece_CanAlsoMoveAllEightDirections()
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        // Đặt Mã Đen tại (4, 4)
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Black));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(8, legalMoves.Count);

        foreach (var dest in new[] { new Position(3, 2), new Position(5, 2), new Position(3, 6), new Position(5, 6) })
        {
            Assert.Contains(dest, legalMoves);
            Assert.True(HorseRule.IsValidMove(board, from, dest));
            Assert.True(board.IsValidMove(from, dest));
        }
    }

    [Theory]
    [InlineData(PieceColor.Red)]   // Quân cản là quân đồng minh
    [InlineData(PieceColor.Black)] // Quân cản là quân địch
    public void Horse_ObstacleUp_BlocksBothUpwardMoves(PieceColor obstacleColor)
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đặt vật cản tại chân trên (4, 3)
        board.SetPiece(new Position(4, 3), new Piece(PieceType.Soldier, obstacleColor));

        // Hai hướng nhảy lên phải bị chặn hoàn toàn
        Assert.True(HorseRule.IsHobbled(board, from, new Position(3, 2)));
        Assert.True(HorseRule.IsHobbled(board, from, new Position(5, 2)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(3, 2)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(5, 2)));

        // 6 hướng còn lại vẫn đi bình thường
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(6, legalMoves.Count);
        Assert.DoesNotContain(new Position(3, 2), legalMoves);
        Assert.DoesNotContain(new Position(5, 2), legalMoves);
        Assert.Contains(new Position(3, 6), legalMoves);
        Assert.Contains(new Position(5, 6), legalMoves);
        Assert.Contains(new Position(2, 3), legalMoves);
        Assert.Contains(new Position(2, 5), legalMoves);
        Assert.Contains(new Position(6, 3), legalMoves);
        Assert.Contains(new Position(6, 5), legalMoves);
    }

    [Theory]
    [InlineData(PieceColor.Red)]
    [InlineData(PieceColor.Black)]
    public void Horse_ObstacleDown_BlocksBothDownwardMoves(PieceColor obstacleColor)
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đặt vật cản tại chân dưới (4, 5)
        board.SetPiece(new Position(4, 5), new Piece(PieceType.Soldier, obstacleColor));

        Assert.True(HorseRule.IsHobbled(board, from, new Position(3, 6)));
        Assert.True(HorseRule.IsHobbled(board, from, new Position(5, 6)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(3, 6)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(5, 6)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(6, legalMoves.Count);
        Assert.DoesNotContain(new Position(3, 6), legalMoves);
        Assert.DoesNotContain(new Position(5, 6), legalMoves);
    }

    [Theory]
    [InlineData(PieceColor.Red)]
    [InlineData(PieceColor.Black)]
    public void Horse_ObstacleLeft_BlocksBothLeftwardMoves(PieceColor obstacleColor)
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đặt vật cản tại chân trái (3, 4)
        board.SetPiece(new Position(3, 4), new Piece(PieceType.Soldier, obstacleColor));

        Assert.True(HorseRule.IsHobbled(board, from, new Position(2, 3)));
        Assert.True(HorseRule.IsHobbled(board, from, new Position(2, 5)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(2, 3)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(2, 5)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(6, legalMoves.Count);
        Assert.DoesNotContain(new Position(2, 3), legalMoves);
        Assert.DoesNotContain(new Position(2, 5), legalMoves);
    }

    [Theory]
    [InlineData(PieceColor.Red)]
    [InlineData(PieceColor.Black)]
    public void Horse_ObstacleRight_BlocksBothRightwardMoves(PieceColor obstacleColor)
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đặt vật cản tại chân phải (5, 4)
        board.SetPiece(new Position(5, 4), new Piece(PieceType.Soldier, obstacleColor));

        Assert.True(HorseRule.IsHobbled(board, from, new Position(6, 3)));
        Assert.True(HorseRule.IsHobbled(board, from, new Position(6, 5)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(6, 3)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(6, 5)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(6, legalMoves.Count);
        Assert.DoesNotContain(new Position(6, 3), legalMoves);
        Assert.DoesNotContain(new Position(6, 5), legalMoves);
    }

    [Fact]
    public void Horse_AllFourLegsBlocked_HasZeroLegalMoves()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Chặn cả 4 chân: Trên (4, 3), Dưới (4, 5), Trái (3, 4), Phải (5, 4)
        board.SetPiece(new Position(4, 3), new Piece(PieceType.Soldier, PieceColor.Red));
        board.SetPiece(new Position(4, 5), new Piece(PieceType.Cannon, PieceColor.Black));
        board.SetPiece(new Position(3, 4), new Piece(PieceType.Advisor, PieceColor.Red));
        board.SetPiece(new Position(5, 4), new Piece(PieceType.Chariot, PieceColor.Black));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Empty(legalMoves);
    }

    [Fact]
    public void Horse_DestinationBlockedByFriendlyPiece_CannotMove()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đặt quân Đỏ tại đích đến (3, 2)
        board.SetPiece(new Position(3, 2), new Piece(PieceType.Soldier, PieceColor.Red));

        // Chân trên (4, 3) vẫn trống, nhưng đích đến có quân ta
        Assert.False(HorseRule.IsHobbled(board, from, new Position(3, 2)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(3, 2)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(7, legalMoves.Count);
        Assert.DoesNotContain(new Position(3, 2), legalMoves);
    }

    [Fact]
    public void Horse_DestinationOccupiedByEnemyPiece_CanCapture()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đặt quân Đen tại đích đến (3, 2)
        var targetPos = new Position(3, 2);
        board.SetPiece(targetPos, new Piece(PieceType.Chariot, PieceColor.Black));

        Assert.True(HorseRule.IsValidMove(board, from, targetPos));
        var legalMoves = board.GetLegalMoves(from);
        Assert.Contains(targetPos, legalMoves);

        // Thực hiện ăn quân
        var undo = board.MakeMove(from, targetPos);
        Assert.NotNull(undo.CapturedPiece);
        Assert.Equal(PieceType.Chariot, undo.CapturedPiece.Type);
        Assert.Equal(PieceColor.Black, undo.CapturedPiece.Color);
        Assert.Equal(PieceType.Horse, board.GetPiece(targetPos)?.Type);
    }

    [Fact]
    public void Horse_AtCorner_FiltersOutOfBoundsMoves()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Mã tại góc bàn cờ (0, 0)
        var from = new Position(0, 0);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        var legalMoves = board.GetLegalMoves(from);

        // Tại góc (0, 0): chân lên (-1, 0) và chân trái (0, -1) ngoài bàn cờ
        // Chỉ có 2 nước hợp lệ: chân phải (1, 0) -> (2, 1) và chân dưới (0, 1) -> (1, 2)
        Assert.Equal(2, legalMoves.Count);
        Assert.Contains(new Position(2, 1), legalMoves);
        Assert.Contains(new Position(1, 2), legalMoves);
    }

    [Fact]
    public void Horse_RejectsInvalidMovementShapes()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Horse, PieceColor.Red));

        // Đứng yên
        Assert.False(HorseRule.IsValidMove(board, from, from));

        // Đi thẳng 1 ô hoặc nhiều ô (như Xe/Tốt)
        Assert.False(HorseRule.IsValidMove(board, from, new Position(4, 3)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(4, 5)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(4, 1)));

        // Đi chéo 1 ô (như Sĩ) hoặc chéo 2 ô (như Tượng)
        Assert.False(HorseRule.IsValidMove(board, from, new Position(5, 5)));
        Assert.False(HorseRule.IsValidMove(board, from, new Position(6, 6)));

        // Tọa độ ngoài bàn cờ không thể tạo Position hợp lệ
        Assert.False(Position.IsValid(-1, -2));
        Assert.False(Position.IsValid(9, 10));
    }

    [Fact]
    public void Horse_InitialStandardPosition_HasTwoLegalMovesEach()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition(); // Khởi tạo bàn cờ ban đầu 32 quân

        // Mã Đỏ cánh phải tại (1, 9):
        // - Chân xuống (1, 10) ngoài bàn
        // - Chân trái (0, 9) có Xe đỏ cản
        // - Chân phải (2, 9) có Tượng đỏ cản
        // - Chân lên (1, 8) trống!
        // -> Có đúng 2 nước đi: (2, 7) (Mã 2 tiến 3) và (0, 7) (Mã 2 tiến 1)
        var redHorse1 = new Position(1, 9);
        var movesRed1 = board.GetLegalMoves(redHorse1);
        Assert.Equal(2, movesRed1.Count);
        Assert.Contains(new Position(2, 7), movesRed1);
        Assert.Contains(new Position(0, 7), movesRed1);

        // Mã Đỏ cánh trái tại (7, 9):
        // -> Có đúng 2 nước đi: (6, 7) (Mã 8 tiến 7) và (8, 7) (Mã 8 tiến 9)
        var redHorse2 = new Position(7, 9);
        var movesRed2 = board.GetLegalMoves(redHorse2);
        Assert.Equal(2, movesRed2.Count);
        Assert.Contains(new Position(6, 7), movesRed2);
        Assert.Contains(new Position(8, 7), movesRed2);

        // Đổi lượt sang Đen để kiểm tra Mã Đen tại (1, 0) và (7, 0)
        board.ActiveColor = PieceColor.Black;

        var blackHorse1 = new Position(1, 0);
        var movesBlack1 = board.GetLegalMoves(blackHorse1);
        Assert.Equal(2, movesBlack1.Count);
        Assert.Contains(new Position(2, 2), movesBlack1);
        Assert.Contains(new Position(0, 2), movesBlack1);

        var blackHorse2 = new Position(7, 0);
        var movesBlack2 = board.GetLegalMoves(blackHorse2);
        Assert.Equal(2, movesBlack2.Count);
        Assert.Contains(new Position(6, 2), movesBlack2);
        Assert.Contains(new Position(8, 2), movesBlack2);
    }
}
