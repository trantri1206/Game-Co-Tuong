using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển và ăn quân của quân Pháo (Cannon Rule - GCT-53).
/// </summary>
public class CannonRuleTests
{
    [Fact]
    public void Cannon_EmptyBoard_MovesLikeRookToEmptySquares()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Pháo Đỏ tại trung tâm (4, 4)
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // 1. Kiểm tra di chuyển theo hàng ngang (Row 4, Col 0..8 ngoại trừ 4)
        for (int c = 0; c < ChessBoard.Cols; c++)
        {
            if (c == 4) continue;
            Assert.True(CannonRule.IsValidMove(board, from, new Position(c, 4)));
            Assert.True(board.IsValidMove(from, new Position(c, 4)));
        }

        // 2. Kiểm tra di chuyển theo cột dọc (Col 4, Row 0..9 ngoại trừ 4)
        for (int r = 0; r < ChessBoard.Rows; r++)
        {
            if (r == 4) continue;
            Assert.True(CannonRule.IsValidMove(board, from, new Position(4, r)));
            Assert.True(board.IsValidMove(from, new Position(4, r)));
        }

        // 3. Tổng số nước đi hợp lệ trên bàn cờ trống: 8 ô ngang + 9 ô dọc = 17 nước
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(17, legalMoves.Count);
    }

    [Fact]
    public void Cannon_CannotCaptureWithoutPlatform_ZeroPiecesBetween()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // Đặt quân Đen ngay trước mặt (4, 3) theo trục dọc - 0 ô ở giữa
        var enemyAdjacent = new Position(4, 3);
        board.SetPiece(enemyAdjacent, new Piece(PieceType.Soldier, PieceColor.Black));

        // Đặt quân Đen ở xa (0, 4) theo trục ngang - 0 quân cản ở giữa trên hàng 4
        var enemyDistant = new Position(0, 4);
        board.SetPiece(enemyDistant, new Piece(PieceType.Chariot, PieceColor.Black));

        // Không có ngòi (piecesBetween == 0) -> Không thể ăn quân
        Assert.False(CannonRule.IsValidMove(board, from, enemyAdjacent));
        Assert.False(CannonRule.IsValidMove(board, from, enemyDistant));
        Assert.False(board.IsValidMove(from, enemyAdjacent));
        Assert.False(board.IsValidMove(from, enemyDistant));

        var legalMoves = board.GetLegalMoves(from);
        Assert.DoesNotContain(enemyAdjacent, legalMoves);
        Assert.DoesNotContain(enemyDistant, legalMoves);
    }

    [Theory]
    [InlineData(PieceColor.Red)]   // Ngòi là quân ta
    [InlineData(PieceColor.Black)] // Ngòi là quân địch
    public void Cannon_CanCaptureWithExactlyOnePlatform(PieceColor platformColor)
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // Đặt 1 quân cờ làm ngòi tại (4, 2)
        var platformPos = new Position(4, 2);
        board.SetPiece(platformPos, new Piece(PieceType.Soldier, platformColor));

        // Đặt quân địch cần ăn tại (4, 0)
        var targetEnemy = new Position(4, 0);
        board.SetPiece(targetEnemy, new Piece(PieceType.Chariot, PieceColor.Black));

        // Số quân ở giữa từ (4, 4) đến (4, 0) đúng bằng 1
        Assert.Equal(1, CannonRule.CountPiecesBetween(board, from, targetEnemy));

        // Ăn quân hợp lệ
        Assert.True(CannonRule.IsValidMove(board, from, targetEnemy));
        Assert.True(board.IsValidMove(from, targetEnemy));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Contains(targetEnemy, legalMoves);

        // Nước đi trước ngòi (4, 3) là ô trống -> đi được
        Assert.Contains(new Position(4, 3), legalMoves);

        // Bản thân ô ngòi (4, 2) -> không thể đi tới (vì không thể ăn ngòi khi không có ngòi khác)
        Assert.False(board.IsValidMove(from, platformPos));

        // Thực hiện nước ăn quân
        var undo = board.MakeMove(from, targetEnemy);
        Assert.NotNull(undo.CapturedPiece);
        Assert.Equal(PieceType.Chariot, undo.CapturedPiece.Type);
        Assert.Equal(PieceColor.Black, undo.CapturedPiece.Color);
    }

    [Fact]
    public void Cannon_CannotCaptureWithTwoOrMorePlatforms()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // Đặt 2 quân làm ngòi trên đường đi: (4, 3) và (4, 2)
        board.SetPiece(new Position(4, 3), new Piece(PieceType.Soldier, PieceColor.Red));
        board.SetPiece(new Position(4, 2), new Piece(PieceType.Horse, PieceColor.Black));

        // Đặt quân địch ở sau 2 ngòi tại (4, 0)
        var targetEnemy = new Position(4, 0);
        board.SetPiece(targetEnemy, new Piece(PieceType.General, PieceColor.Black));

        // Số quân ở giữa là 2
        Assert.Equal(2, CannonRule.CountPiecesBetween(board, from, targetEnemy));

        // Bị chặn vì có từ 2 ngòi trở lên
        Assert.False(CannonRule.IsValidMove(board, from, targetEnemy));
        Assert.False(board.IsValidMove(from, targetEnemy));

        var legalMoves = board.GetLegalMoves(from);
        Assert.DoesNotContain(targetEnemy, legalMoves);
    }

    [Fact]
    public void Cannon_CannotJumpOverPlatformToEmptySquare()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // Đặt ngòi tại (4, 2)
        board.SetPiece(new Position(4, 2), new Piece(PieceType.Soldier, PieceColor.Black));

        // Ô (4, 1) và (4, 0) hoàn toàn TRỐNG ở phía sau ngòi
        var emptyBehindPlatform1 = new Position(4, 1);
        var emptyBehindPlatform2 = new Position(4, 0);

        // Tuyệt đối không được nhảy qua ngòi tới ô trống
        Assert.False(CannonRule.IsValidMove(board, from, emptyBehindPlatform1));
        Assert.False(CannonRule.IsValidMove(board, from, emptyBehindPlatform2));
        Assert.False(board.IsValidMove(from, emptyBehindPlatform1));
        Assert.False(board.IsValidMove(from, emptyBehindPlatform2));

        var legalMoves = board.GetLegalMoves(from);
        Assert.DoesNotContain(emptyBehindPlatform1, legalMoves);
        Assert.DoesNotContain(emptyBehindPlatform2, legalMoves);
    }

    [Fact]
    public void Cannon_CannotCaptureFriendlyPieceEvenWithPlatform()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // Đặt ngòi tại (4, 2)
        board.SetPiece(new Position(4, 2), new Piece(PieceType.Soldier, PieceColor.Black));

        // Đặt quân ĐỒNG MINH (Đỏ) tại (4, 0)
        var friendlyTarget = new Position(4, 0);
        board.SetPiece(friendlyTarget, new Piece(PieceType.Chariot, PieceColor.Red));

        // Dù có đúng 1 ngòi nhưng đích đến là quân cùng màu -> bị chặn
        Assert.False(CannonRule.IsValidMove(board, from, friendlyTarget));
        Assert.False(board.IsValidMove(from, friendlyTarget));

        var legalMoves = board.GetLegalMoves(from);
        Assert.DoesNotContain(friendlyTarget, legalMoves);
    }

    [Fact]
    public void Cannon_RejectsDiagonalAndZeroDisplacement()
    {
        var board = new ChessBoard();
        board.Clear();

        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Cannon, PieceColor.Red));

        // Đứng yên
        Assert.False(CannonRule.IsValidMove(board, from, from));

        // Đi chéo
        Assert.False(CannonRule.IsValidMove(board, from, new Position(5, 5)));
        Assert.False(CannonRule.IsValidMove(board, from, new Position(3, 3)));
        Assert.False(CannonRule.IsValidMove(board, from, new Position(6, 2)));

        // Tọa độ ngoài bàn cờ không hợp lệ
        Assert.False(Position.IsValid(-1, 4));
        Assert.False(Position.IsValid(4, 10));
    }

    [Fact]
    public void Cannon_InitialPosition_CanMoveHorizontallyAndCaptureEnemyHorseOverCannon()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // Pháo Đỏ cánh phải tại (1, 7):
        var redCannon1 = new Position(1, 7);
        var moves = board.GetLegalMoves(redCannon1);

        // 1. Pháo đi ngang tự do trên hàng 7:
        // Cột 0 (trống), Cột 2, 3, 4, 5, 6 (trống). Cột 7 có Pháo Đỏ cánh trái cản.
        Assert.Contains(new Position(0, 7), moves);
        Assert.Contains(new Position(2, 7), moves);
        Assert.Contains(new Position(3, 7), moves);
        Assert.Contains(new Position(4, 7), moves); // Nước mở màn kinh điển: Pháo 2 bình 5 (Pháo đầu)!
        Assert.Contains(new Position(5, 7), moves);
        Assert.Contains(new Position(6, 7), moves);
        Assert.DoesNotContain(new Position(7, 7), moves); // Chặn ăn Pháo mình

        // 2. Đi dọc:
        // Hàng 8 trống -> Pháo (1, 7) lùi 1 ô (1, 8) được
        Assert.Contains(new Position(1, 8), moves);
        // Hàng 9 có Mã Đỏ (1, 9) -> Chặn ăn Mã mình
        Assert.DoesNotContain(new Position(1, 9), moves);

        // Cột 1 phía trước mặt:
        // Các ô (1, 6), (1, 5), (1, 4), (1, 3) hoàn toàn trống (Binh/Tốt nằm ở cột 0, 2, 4, 6, 8)
        // -> Pháo có thể tiến tự do vào các ô trống này
        Assert.Contains(new Position(1, 6), moves);
        Assert.Contains(new Position(1, 5), moves);
        Assert.Contains(new Position(1, 4), moves);
        Assert.Contains(new Position(1, 3), moves);

        // Ô (1, 2) có Pháo Đen: là quân đầu tiên trên cột (0 quân cản ở giữa)
        // -> Không có ngòi nên không thể ăn Pháo Đen tại (1, 2)
        Assert.DoesNotContain(new Position(1, 2), moves);
        Assert.False(board.IsValidMove(redCannon1, new Position(1, 2)));

        // Ô (1, 1) trống nằm sau ngòi Pháo Đen -> Pháo không được nhảy tới ô trống
        Assert.DoesNotContain(new Position(1, 1), moves);

        // Ô (1, 0) có Mã Đen: đoạn giữa (1, 7) và (1, 0) có ĐÚNG 1 quân ngòi là Pháo Đen (1, 2)
        // -> Pháo Đỏ (1, 7) ăn được Mã Đen (1, 0) qua ngòi Pháo Đen (nước pháo ăn mã kinh điển)!
        Assert.Contains(new Position(1, 0), moves);
        Assert.True(board.IsValidMove(redCannon1, new Position(1, 0)));
    }
}
