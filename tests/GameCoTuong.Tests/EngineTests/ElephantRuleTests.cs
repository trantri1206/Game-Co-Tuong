using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển và cản mắt của quân Tượng / Tịnh (Elephant Rule - GCT-54).
/// </summary>
public class ElephantRuleTests
{
    [Fact]
    public void Elephant_CenterOfOwnSide_CanMoveAllFourDiagonalDirections()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Tượng Đỏ tại tâm sân nhà (4, 7)
        var from = new Position(4, 7);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Red));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(4, legalMoves.Count);

        var expectedMoves = new[]
        {
            new Position(2, 5), // Lên - Trái
            new Position(6, 5), // Lên - Phải
            new Position(2, 9), // Xuống - Trái
            new Position(6, 9)  // Xuống - Phải
        };

        foreach (var dest in expectedMoves)
        {
            Assert.Contains(dest, legalMoves);
            Assert.True(ElephantRule.IsValidMove(board, from, dest));
            Assert.True(board.IsValidMove(from, dest));
        }
    }

    [Fact]
    public void Elephant_BlackSide_CanMoveAllFourDiagonalDirections()
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        // Đặt Tượng Đen tại tâm sân nhà (4, 2)
        var from = new Position(4, 2);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Black));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(4, legalMoves.Count);

        var expectedMoves = new[]
        {
            new Position(2, 0), // Lên - Trái
            new Position(6, 0), // Lên - Phải
            new Position(2, 4), // Xuống - Trái
            new Position(6, 4)  // Xuống - Phải
        };

        foreach (var dest in expectedMoves)
        {
            Assert.Contains(dest, legalMoves);
            Assert.True(ElephantRule.IsValidMove(board, from, dest));
            Assert.True(board.IsValidMove(from, dest));
        }
    }

    [Theory]
    [InlineData(PieceColor.Red)]   // Mắt Tượng bị chẹn bởi quân đồng minh
    [InlineData(PieceColor.Black)] // Mắt Tượng bị chẹn bởi quân đối phương
    public void Elephant_BlockedEye_CannotMoveThroughBlockedDiagonal(PieceColor blockerColor)
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 7);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Red));

        // Chẹn mắt Tượng hướng Lên - Trái tại (3, 6)
        var eyeUpLeft = new Position(3, 6);
        board.SetPiece(eyeUpLeft, new Piece(PieceType.Soldier, blockerColor));

        var targetUpLeft = new Position(2, 5);

        // Mắt Tượng bị chẹn -> Nước đi bị từ chối
        Assert.True(ElephantRule.IsEyeBlocked(board, from, targetUpLeft));
        Assert.False(ElephantRule.IsValidMove(board, from, targetUpLeft));
        Assert.False(board.IsValidMove(from, targetUpLeft));

        // 3 hướng chéo còn lại vẫn đi được bình thường
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(3, legalMoves.Count);
        Assert.DoesNotContain(targetUpLeft, legalMoves);
        Assert.Contains(new Position(6, 5), legalMoves);
        Assert.Contains(new Position(2, 9), legalMoves);
        Assert.Contains(new Position(6, 9), legalMoves);
    }

    [Fact]
    public void Elephant_AllFourEyesBlocked_HasZeroLegalMoves()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 7);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Red));

        // Chẹn cả 4 mắt Tượng xung quanh:
        board.SetPiece(new Position(3, 6), new Piece(PieceType.Soldier, PieceColor.Red));
        board.SetPiece(new Position(5, 6), new Piece(PieceType.Soldier, PieceColor.Black));
        board.SetPiece(new Position(3, 8), new Piece(PieceType.Horse, PieceColor.Red));
        board.SetPiece(new Position(5, 8), new Piece(PieceType.Chariot, PieceColor.Black));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Empty(legalMoves);
    }

    [Fact]
    public void Elephant_RedElephant_CannotCrossRiverEvenWhenPathIsEmpty()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Tượng Đỏ sát bờ sông tại (2, 5)
        var from = new Position(2, 5);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Red));

        // Hai nước nhảy sang sông: (0, 3) và (4, 3) thuộc hàng 3 của phe Đen (Row <= 4)
        var crossRiverLeft = new Position(0, 3);
        var crossRiverRight = new Position(4, 3);

        // Mắt Tượng (1, 4) và (3, 4) hoàn toàn trống
        Assert.Null(board.GetPiece(1, 4));
        Assert.Null(board.GetPiece(3, 4));

        // Tuy nhiên tuyệt đối không được qua sông
        Assert.True(ElephantRule.IsCrossingRiver(PieceColor.Red, crossRiverLeft.Row));
        Assert.True(ElephantRule.IsCrossingRiver(PieceColor.Red, crossRiverRight.Row));
        Assert.False(ElephantRule.IsValidMove(board, from, crossRiverLeft));
        Assert.False(ElephantRule.IsValidMove(board, from, crossRiverRight));
        Assert.False(board.IsValidMove(from, crossRiverLeft));
        Assert.False(board.IsValidMove(from, crossRiverRight));

        // Chỉ có thể lùi về sau trên sân nhà Đỏ: (0, 7) và (4, 7)
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(2, legalMoves.Count);
        Assert.Contains(new Position(0, 7), legalMoves);
        Assert.Contains(new Position(4, 7), legalMoves);
    }

    [Fact]
    public void Elephant_BlackElephant_CannotCrossRiverEvenWhenPathIsEmpty()
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        // Đặt Tượng Đen sát bờ sông tại (2, 4)
        var from = new Position(2, 4);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Black));

        // Hai nước nhảy sang sông: (0, 6) và (4, 6) thuộc hàng 6 của phe Đỏ (Row >= 5)
        var crossRiverLeft = new Position(0, 6);
        var crossRiverRight = new Position(4, 6);

        // Mắt Tượng (1, 5) và (3, 5) hoàn toàn trống
        Assert.Null(board.GetPiece(1, 5));
        Assert.Null(board.GetPiece(3, 5));

        // Tuy nhiên tuyệt đối không được qua sông
        Assert.True(ElephantRule.IsCrossingRiver(PieceColor.Black, crossRiverLeft.Row));
        Assert.True(ElephantRule.IsCrossingRiver(PieceColor.Black, crossRiverRight.Row));
        Assert.False(ElephantRule.IsValidMove(board, from, crossRiverLeft));
        Assert.False(ElephantRule.IsValidMove(board, from, crossRiverRight));
        Assert.False(board.IsValidMove(from, crossRiverLeft));
        Assert.False(board.IsValidMove(from, crossRiverRight));

        // Chỉ có thể lùi về sân nhà Đen: (0, 2) và (4, 2)
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(2, legalMoves.Count);
        Assert.Contains(new Position(0, 2), legalMoves);
        Assert.Contains(new Position(4, 2), legalMoves);
    }

    [Fact]
    public void Elephant_CanCaptureEnemyPiece_AndBlockedByFriendlyPiece()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 7);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Red));

        // Đặt quân Địch tại (2, 5)
        var enemyPos = new Position(2, 5);
        board.SetPiece(enemyPos, new Piece(PieceType.Chariot, PieceColor.Black));

        // Đặt quân Ta tại (6, 5)
        var friendlyPos = new Position(6, 5);
        board.SetPiece(friendlyPos, new Piece(PieceType.Soldier, PieceColor.Red));

        // Ăn quân địch hợp lệ
        Assert.True(ElephantRule.IsValidMove(board, from, enemyPos));
        Assert.True(board.IsValidMove(from, enemyPos));

        // Bị chặn ăn quân ta
        Assert.False(ElephantRule.IsValidMove(board, from, friendlyPos));
        Assert.False(board.IsValidMove(from, friendlyPos));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Contains(enemyPos, legalMoves);
        Assert.DoesNotContain(friendlyPos, legalMoves);

        // Thực hiện nước đi ăn quân
        var undo = board.MakeMove(from, enemyPos);
        Assert.NotNull(undo.CapturedPiece);
        Assert.Equal(PieceType.Chariot, undo.CapturedPiece.Type);
        Assert.Equal(PieceColor.Black, undo.CapturedPiece.Color);
    }

    [Fact]
    public void Elephant_RejectsInvalidMovementShapes()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 7);
        board.SetPiece(from, new Piece(PieceType.Elephant, PieceColor.Red));

        // Đứng yên
        Assert.False(ElephantRule.IsValidMove(board, from, from));

        // Đi chéo 1 ô (như Sĩ)
        Assert.False(ElephantRule.IsValidMove(board, from, new Position(3, 6)));
        Assert.False(ElephantRule.IsValidMove(board, from, new Position(5, 8)));

        // Đi thẳng/ngang (như Xe/Pháo)
        Assert.False(ElephantRule.IsValidMove(board, from, new Position(4, 5)));
        Assert.False(ElephantRule.IsValidMove(board, from, new Position(2, 7)));

        // Đi chữ L (như Mã)
        Assert.False(ElephantRule.IsValidMove(board, from, new Position(3, 5)));
        Assert.False(ElephantRule.IsValidMove(board, from, new Position(5, 5)));

        // Tọa độ ngoài biên
        Assert.False(Position.IsValid(-2, 7));
        Assert.False(Position.IsValid(4, 11));
    }

    [Fact]
    public void Elephant_InitialStandardPosition_HasTwoLegalMovesEach()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // 1. Tượng Đỏ cánh phải (2, 9):
        // - Chân xuống (0, 11) và (4, 11) ngoài bàn cờ
        // - Nước lên trái (0, 7) và lên phải (4, 7) [vào tâm] đều trống mắt (1, 8) và (3, 8)
        var redElephant1 = new Position(2, 9);
        var movesRed1 = board.GetLegalMoves(redElephant1);
        Assert.Equal(2, movesRed1.Count);
        Assert.Contains(new Position(0, 7), movesRed1);
        Assert.Contains(new Position(4, 7), movesRed1);

        // 2. Tượng Đỏ cánh trái (6, 9):
        // - Có đúng 2 nước: (4, 7) [vào tâm] và (8, 7)
        var redElephant2 = new Position(6, 9);
        var movesRed2 = board.GetLegalMoves(redElephant2);
        Assert.Equal(2, movesRed2.Count);
        Assert.Contains(new Position(4, 7), movesRed2);
        Assert.Contains(new Position(8, 7), movesRed2);

        // 3. Đổi lượt sang Đen để kiểm tra 2 Tượng Đen:
        board.ActiveColor = PieceColor.Black;

        var blackElephant1 = new Position(2, 0);
        var movesBlack1 = board.GetLegalMoves(blackElephant1);
        Assert.Equal(2, movesBlack1.Count);
        Assert.Contains(new Position(0, 2), movesBlack1);
        Assert.Contains(new Position(4, 2), movesBlack1);

        var blackElephant2 = new Position(6, 0);
        var movesBlack2 = board.GetLegalMoves(blackElephant2);
        Assert.Equal(2, movesBlack2.Count);
        Assert.Contains(new Position(4, 2), movesBlack2);
        Assert.Contains(new Position(8, 2), movesBlack2);
    }
}
