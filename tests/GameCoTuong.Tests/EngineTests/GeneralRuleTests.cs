using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển, lộ mặt tướng và an toàn điểm đến của quân Tướng (General Rule - GCT-56).
/// </summary>
public class GeneralRuleTests
{
    [Fact]
    public void General_AtCenterOfPalace_CanMoveAllFourOrthogonalDirectionsWhenSafe()
    {
        var board = new ChessBoard();
        board.Clear();

        // 1. Tướng Đỏ tại tâm Cửu cung (4, 8)
        var centerRed = new Position(4, 8);
        board.SetPiece(centerRed, new Piece(PieceType.General, PieceColor.Red));

        // Đặt Tướng Đen ở (3, 0), và đặt quân chắn ở (3, 4) để khi Tướng Đỏ sang (3, 8) không bị Lộ mặt Tướng
        board.SetPiece(new Position(3, 0), new Piece(PieceType.General, PieceColor.Black));
        board.SetPiece(new Position(3, 4), new Piece(PieceType.Soldier, PieceColor.Red));

        var legalMovesRed = board.GetLegalMoves(centerRed);
        Assert.Equal(4, legalMovesRed.Count);

        var expectedOrthogonal = new[]
        {
            new Position(4, 7), // Tiến thẳng
            new Position(4, 9), // Thoái thẳng
            new Position(3, 8), // Sang trái
            new Position(5, 8)  // Sang phải
        };

        foreach (var dest in expectedOrthogonal)
        {
            Assert.Contains(dest, legalMovesRed);
            Assert.True(GeneralRule.IsValidMove(board, centerRed, dest));
            Assert.True(board.IsValidMove(centerRed, dest));
        }

        // 2. Tướng Đen tại tâm Cửu cung (4, 1)
        board.Clear();
        board.ActiveColor = PieceColor.Black;
        var centerBlack = new Position(4, 1);
        board.SetPiece(centerBlack, new Piece(PieceType.General, PieceColor.Black));
        board.SetPiece(new Position(5, 9), new Piece(PieceType.General, PieceColor.Red)); // Lệch cột
        board.SetPiece(new Position(5, 5), new Piece(PieceType.Soldier, PieceColor.Black)); // Chắn cột 5 không lộ mặt Tướng

        var legalMovesBlack = board.GetLegalMoves(centerBlack);
        Assert.Equal(4, legalMovesBlack.Count);

        foreach (var dest in new[] { new Position(4, 0), new Position(4, 2), new Position(3, 1), new Position(5, 1) })
        {
            Assert.Contains(dest, legalMovesBlack);
            Assert.True(GeneralRule.IsValidMove(board, centerBlack, dest));
            Assert.True(board.IsValidMove(centerBlack, dest));
        }
    }

    [Fact]
    public void General_RejectsDiagonalAndOutOfPalaceMoves()
    {
        var board = new ChessBoard();
        board.Clear();

        var centerRed = new Position(4, 8);
        board.SetPiece(centerRed, new Piece(PieceType.General, PieceColor.Red));
        board.SetPiece(new Position(3, 0), new Piece(PieceType.General, PieceColor.Black));

        // 1. Đứng yên tại chỗ
        Assert.False(GeneralRule.IsValidMove(board, centerRed, centerRed));

        // 2. Chặn đi chéo trong cung (như Sĩ)
        Assert.False(GeneralRule.IsValidMove(board, centerRed, new Position(3, 7)));
        Assert.False(GeneralRule.IsValidMove(board, centerRed, new Position(5, 7)));
        Assert.False(GeneralRule.IsValidMove(board, centerRed, new Position(3, 9)));
        Assert.False(GeneralRule.IsValidMove(board, centerRed, new Position(5, 9)));

        // 3. Chặn đi ra ngoài Cửu cung:
        // Đặt Tướng ở mép trái Cửu cung (3, 8) -> đi ngang ra ngoài cung (2, 8)
        var edgeRed = new Position(3, 8);
        board.SetPiece(edgeRed, new Piece(PieceType.General, PieceColor.Red));
        Assert.False(GeneralRule.IsValidMove(board, edgeRed, new Position(2, 8)));

        // Đặt Tướng ở mép trên Cửu cung (4, 7) -> đi tiến ra ngoài cung (4, 6)
        var topRed = new Position(4, 7);
        board.SetPiece(topRed, new Piece(PieceType.General, PieceColor.Red));
        Assert.False(GeneralRule.IsValidMove(board, topRed, new Position(4, 6)));
    }

    [Fact]
    public void General_CannotMoveToFaceOpponentGeneralDirectly()
    {
        var board = new ChessBoard();
        board.Clear();

        // Tướng Đen tại (5, 1)
        board.SetPiece(new Position(5, 1), new Piece(PieceType.General, PieceColor.Black));

        // Tướng Đỏ tại (4, 8)
        var redGen = new Position(4, 8);
        board.SetPiece(redGen, new Piece(PieceType.General, PieceColor.Red));

        // 1. Cột 5 đang hoàn toàn trống: Tướng Đỏ đi ngang sang (5, 8) sẽ đối mặt trực tiếp với Tướng Đen (5, 1)
        var facingDest = new Position(5, 8);
        Assert.False(GeneralRule.IsValidMove(board, redGen, facingDest));
        Assert.False(board.IsValidMove(redGen, facingDest));

        var legalMoves = board.GetLegalMoves(redGen);
        Assert.DoesNotContain(facingDest, legalMoves);

        // 2. Đặt một quân cản trên cột 5 tại (5, 5) -> Không còn lộ mặt Tướng -> Nước đi trở nên hợp lệ!
        board.SetPiece(new Position(5, 5), new Piece(PieceType.Soldier, PieceColor.Red));
        Assert.True(GeneralRule.IsValidMove(board, redGen, facingDest));
        Assert.True(board.IsValidMove(redGen, facingDest));
    }

    [Fact]
    public void General_CannotMoveIntoCheckByChariot()
    {
        var board = new ChessBoard();
        board.Clear();

        var redGen = new Position(4, 8);
        board.SetPiece(redGen, new Piece(PieceType.General, PieceColor.Red));
        board.SetPiece(new Position(3, 0), new Piece(PieceType.General, PieceColor.Black));
        board.SetPiece(new Position(3, 4), new Piece(PieceType.Soldier, PieceColor.Red)); // Chắn cột 3 không lộ mặt Tướng

        // Đặt Xe Đen tại (0, 7) đang kiểm soát toàn bộ hàng 7
        board.SetPiece(new Position(0, 7), new Piece(PieceType.Chariot, PieceColor.Black));

        // Ô (4, 7) bị Xe Đen khống chế -> Tướng Đỏ tuyệt đối không được tiến lên (4, 7)
        var unsafeDest = new Position(4, 7);
        Assert.False(GeneralRule.IsValidMove(board, redGen, unsafeDest));
        Assert.False(board.IsValidMove(redGen, unsafeDest));

        var legalMoves = board.GetLegalMoves(redGen);
        Assert.DoesNotContain(unsafeDest, legalMoves);
        Assert.Contains(new Position(4, 9), legalMoves);
        Assert.Contains(new Position(3, 8), legalMoves);
        Assert.Contains(new Position(5, 8), legalMoves);
    }

    [Fact]
    public void General_CannotMoveIntoCheckByCannon()
    {
        var board = new ChessBoard();
        board.Clear();

        var redGen = new Position(4, 8);
        board.SetPiece(redGen, new Piece(PieceType.General, PieceColor.Red));
        board.SetPiece(new Position(3, 0), new Piece(PieceType.General, PieceColor.Black));

        // Đặt ngòi tại (4, 5) và Pháo Đen tại (4, 2)
        board.SetPiece(new Position(4, 5), new Piece(PieceType.Soldier, PieceColor.Red));
        board.SetPiece(new Position(4, 2), new Piece(PieceType.Cannon, PieceColor.Black));

        // Nếu Tướng Đỏ tiến lên (4, 7): Cột 4 có đúng 1 ngòi (4, 5) nối tới Pháo Đen (4, 2)
        // -> Ô (4, 7) bị Pháo Đen chiếu -> BỊ CHẶN!
        var unsafeDest = new Position(4, 7);
        Assert.False(GeneralRule.IsValidMove(board, redGen, unsafeDest));
        Assert.False(board.IsValidMove(redGen, unsafeDest));

        var legalMoves = board.GetLegalMoves(redGen);
        Assert.DoesNotContain(unsafeDest, legalMoves);
    }

    [Fact]
    public void General_CannotMoveIntoCheckByHorse()
    {
        var board = new ChessBoard();
        board.Clear();

        var redGen = new Position(4, 8);
        board.SetPiece(redGen, new Piece(PieceType.General, PieceColor.Red));
        board.SetPiece(new Position(3, 0), new Piece(PieceType.General, PieceColor.Black));

        // Đặt Mã Đen tại (3, 5): Chân Mã (3, 6) trống -> khống chế ô (4, 7)
        board.SetPiece(new Position(3, 5), new Piece(PieceType.Horse, PieceColor.Black));

        var unsafeDest = new Position(4, 7);
        Assert.False(GeneralRule.IsValidMove(board, redGen, unsafeDest));
        Assert.False(board.IsValidMove(redGen, unsafeDest));

        var legalMoves = board.GetLegalMoves(redGen);
        Assert.DoesNotContain(unsafeDest, legalMoves);
    }

    [Fact]
    public void General_CanCaptureEnemyPiece_IfDestinationIsSafe()
    {
        var board = new ChessBoard();
        board.Clear();

        var redGen = new Position(4, 8);
        board.SetPiece(redGen, new Piece(PieceType.General, PieceColor.Red));
        board.SetPiece(new Position(3, 0), new Piece(PieceType.General, PieceColor.Black));

        // 1. Đặt quân địch KHÔNG ĐƯỢC BẢO KÊ tại (4, 7)
        var unprotectedEnemy = new Position(4, 7);
        board.SetPiece(unprotectedEnemy, new Piece(PieceType.Soldier, PieceColor.Black));

        // Tướng ăn được quân địch này
        Assert.True(GeneralRule.IsValidMove(board, redGen, unprotectedEnemy));
        Assert.True(board.IsValidMove(redGen, unprotectedEnemy));

        // 2. Bây giờ đặt Xe Đen tại (0, 7) BẢO KÊ quân này
        // Nếu Tướng ăn quân tại (4, 7), ô đó sẽ bị Xe Đen ở (0, 7) chiếu
        // -> Nước ăn quân trở nên không hợp lệ vì bị bảo kê!
        board.SetPiece(new Position(0, 7), new Piece(PieceType.Chariot, PieceColor.Black));
        Assert.False(GeneralRule.IsValidMove(board, redGen, unprotectedEnemy));
        Assert.False(board.IsValidMove(redGen, unprotectedEnemy));
    }

    [Fact]
    public void General_InitialStandardPosition_CanMoveUpwardBehindSoldier()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // Tướng Đỏ ban đầu tại (4, 9):
        // - Cánh trái (3, 9) và cánh phải (5, 9) có Sĩ Đỏ cản
        // - Phía sau (4, 10) ngoài bàn cờ
        // - Phía trước (4, 8) trống, và được Binh Đỏ tại (4, 6) che chắn không bị lộ mặt Tướng Đen (4, 0)
        // -> Tướng Đỏ có đúng 1 nước đi duy nhất là tiến lên (4, 8)
        var redGen = new Position(4, 9);
        var movesRed = board.GetLegalMoves(redGen);
        Assert.Single(movesRed);
        Assert.Contains(new Position(4, 8), movesRed);

        // Tương tự với Tướng Đen tại (4, 0):
        board.ActiveColor = PieceColor.Black;
        var blackGen = new Position(4, 0);
        var movesBlack = board.GetLegalMoves(blackGen);
        Assert.Single(movesBlack);
        Assert.Contains(new Position(4, 1), movesBlack);
    }
}
