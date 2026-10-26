using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển và ăn quân của quân Xe (Chariot / Rook - GCT-51).
/// </summary>
public class ChariotRuleTests
{
    [Fact]
    public void Chariot_EmptyBoard_CanMoveHorizontallyAndVerticallyAnyDistance()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Xe Đỏ tại tâm bàn cờ (4, 4)
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Chariot, PieceColor.Red));

        // 1. Kiểm tra di chuyển theo hàng ngang (Row 4, Col từ 0..8)
        for (int c = 0; c < ChessBoard.Cols; c++)
        {
            if (c == 4) continue;
            Assert.True(ChariotRule.IsValidMove(board, from, new Position(c, 4)));
            Assert.True(board.IsValidMove(from, new Position(c, 4)));
        }

        // 2. Kiểm tra di chuyển theo cột dọc (Col 4, Row từ 0..9)
        for (int r = 0; r < ChessBoard.Rows; r++)
        {
            if (r == 4) continue;
            Assert.True(ChariotRule.IsValidMove(board, from, new Position(4, r)));
            Assert.True(board.IsValidMove(from, new Position(4, r)));
        }

        // 3. Tổng số nước đi hợp lệ trên bàn cờ trống: 8 ô ngang + 9 ô dọc = 17 nước
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(17, legalMoves.Count);
    }

    [Fact]
    public void Chariot_RejectsDiagonalAndZeroDisplacement()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Chariot, PieceColor.Red));

        // 1. Đứng yên tại chỗ
        Assert.False(ChariotRule.IsValidMove(board, from, from));

        // 2. Chặn đi chéo các góc
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(5, 5)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(3, 3)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(5, 3)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(3, 5)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(0, 0)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(8, 0)));
    }

    [Fact]
    public void Chariot_CannotLeapOverObstacles_BothFriendlyAndEnemy()
    {
        var board = new ChessBoard();
        board.Clear();

        // Xe Đỏ tại (4, 4)
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Chariot, PieceColor.Red));

        // Vật cản 1 (Đồng minh): Binh Đỏ tại (4, 2)
        board.SetPiece(new Position(4, 2), new Piece(PieceType.Soldier, PieceColor.Red));

        // Vật cản 2 (Đối phương): Pháo Đen tại (2, 4)
        board.SetPiece(new Position(2, 4), new Piece(PieceType.Cannon, PieceColor.Black));

        // 1. Phía trên (Col 4):
        // (4, 3) trống -> đi được
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(4, 3)));
        // (4, 2) có đồng minh -> không được đi vào
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(4, 2)));
        // (4, 1) và (4, 0) sau vật cản -> TUYỆT ĐỐI không được nhảy qua
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(4, 1)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(4, 0)));

        // 2. Phía bên trái (Row 4):
        // (3, 4) trống -> đi được
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(3, 4)));
        // (2, 4) có địch -> ăn được quân địch ở điểm dừng
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(2, 4)));
        // (1, 4) và (0, 4) sau quân địch -> TUYỆT ĐỐI không được nhảy qua
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(1, 4)));
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(0, 4)));
    }

    [Fact]
    public void Chariot_CapturesEnemyPieceAtDestination_AndRejectsFriendlyFire()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(0, 9);
        board.SetPiece(from, new Piece(PieceType.Chariot, PieceColor.Red));

        // Đặt quân Đen tại (0, 0) [đối diện hết cột dọc]
        board.SetPiece(new Position(0, 0), new Piece(PieceType.Chariot, PieceColor.Black));

        // Đặt quân Đỏ tại (5, 9) [trên hàng ngang]
        board.SetPiece(new Position(5, 9), new Piece(PieceType.Horse, PieceColor.Red));

        // 1. Đi dọc ăn quân Đen tại (0, 0) -> HỢP LỆ
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(0, 0)));

        // 2. Đi ngang tới (4, 9) [trước đồng minh] -> HỢP LỆ
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(4, 9)));

        // 3. Đi ngang vào ô (5, 9) [quân đồng minh] -> BỊ CHẶN
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(5, 9)));

        // 4. Nhảy qua ô (5, 9) tới (6, 9) -> BỊ CHẶN
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(6, 9)));
    }

    [Fact]
    public void Chariot_InitialPosition_CanAdvanceTwoSteps_BlockedHorizontallyAndBySoldier()
    {
        // Trong thế cờ ban đầu chuẩn 32 quân:
        // Xe ở góc (0, 9): Hàng 8 và 7 trống, hàng 6 có Binh Đỏ, cột 1 có Mã Đỏ
        var board = new ChessBoard();
        board.SetupInitialPosition();

        var redChariotLeft = new Position(0, 9);
        var redChariotRight = new Position(8, 9);

        // 1. Xe trái (0, 9) chỉ đi được 2 nước dọc: (0, 8) và (0, 7)
        var leftMoves = board.GetLegalMoves(redChariotLeft);
        Assert.Equal(2, leftMoves.Count);
        Assert.Contains(new Position(0, 8), leftMoves);
        Assert.Contains(new Position(0, 7), leftMoves);

        // 2. Chặn ăn Binh mình tại (0, 6) và chặn nhảy qua Binh tới (0, 5)
        Assert.False(board.IsValidMove(redChariotLeft, new Position(0, 6)));
        Assert.False(board.IsValidMove(redChariotLeft, new Position(0, 5)));

        // 3. Chặn ăn Mã mình tại (1, 9) và chặn nhảy qua Mã tới (2, 9)
        Assert.False(board.IsValidMove(redChariotLeft, new Position(1, 9)));
        Assert.False(board.IsValidMove(redChariotLeft, new Position(2, 9)));

        // 4. Xe phải (8, 9) cũng đi được đúng 2 nước dọc: (8, 8) và (8, 7)
        var rightMoves = board.GetLegalMoves(redChariotRight);
        Assert.Equal(2, rightMoves.Count);
        Assert.Contains(new Position(8, 8), rightMoves);
        Assert.Contains(new Position(8, 7), rightMoves);
    }

    [Fact]
    public void Chariot_CornerToOppositeCorner_OnEmptyBoard()
    {
        var board = new ChessBoard();
        board.Clear();

        // Xe ở góc (0, 0)
        var from = new Position(0, 0);
        board.SetPiece(from, new Piece(PieceType.Chariot, PieceColor.Black));

        // Đi ngang hết bàn cờ tới (8, 0)
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(8, 0)));

        // Đi dọc hết bàn cờ tới (0, 9)
        Assert.True(ChariotRule.IsValidMove(board, from, new Position(0, 9)));

        // Đi chéo tới (8, 9) -> BỊ CHẶN
        Assert.False(ChariotRule.IsValidMove(board, from, new Position(8, 9)));
    }
}
