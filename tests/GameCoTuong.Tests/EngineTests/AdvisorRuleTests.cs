using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển và giới hạn cửu cung của quân Sĩ (Advisor Rule - GCT-55).
/// </summary>
public class AdvisorRuleTests
{
    [Fact]
    public void Advisor_AtCenterOfPalace_CanMoveToAllFourCorners()
    {
        var board = new ChessBoard();
        board.Clear();

        // 1. Sĩ Đỏ tại tâm Cửu cung (4, 8)
        var centerRed = new Position(4, 8);
        board.SetPiece(centerRed, new Piece(PieceType.Advisor, PieceColor.Red));

        var legalMovesRed = board.GetLegalMoves(centerRed);
        Assert.Equal(4, legalMovesRed.Count);

        var expectedCornersRed = new[]
        {
            new Position(3, 7), // Góc trên - trái
            new Position(5, 7), // Góc trên - phải
            new Position(3, 9), // Góc dưới - trái
            new Position(5, 9)  // Góc dưới - phải
        };

        foreach (var dest in expectedCornersRed)
        {
            Assert.Contains(dest, legalMovesRed);
            Assert.True(AdvisorRule.IsValidMove(board, centerRed, dest));
            Assert.True(board.IsValidMove(centerRed, dest));
        }

        // 2. Sĩ Đen tại tâm Cửu cung (4, 1)
        board.Clear();
        board.ActiveColor = PieceColor.Black;
        var centerBlack = new Position(4, 1);
        board.SetPiece(centerBlack, new Piece(PieceType.Advisor, PieceColor.Black));

        var legalMovesBlack = board.GetLegalMoves(centerBlack);
        Assert.Equal(4, legalMovesBlack.Count);

        var expectedCornersBlack = new[]
        {
            new Position(3, 0), // Góc trên - trái
            new Position(5, 0), // Góc trên - phải
            new Position(3, 2), // Góc dưới - trái
            new Position(5, 2)  // Góc dưới - phải
        };

        foreach (var dest in expectedCornersBlack)
        {
            Assert.Contains(dest, legalMovesBlack);
            Assert.True(AdvisorRule.IsValidMove(board, centerBlack, dest));
            Assert.True(board.IsValidMove(centerBlack, dest));
        }
    }

    [Theory]
    [InlineData(3, 9, 4, 8)] // Góc dưới - trái -> Tâm
    [InlineData(5, 9, 4, 8)] // Góc dưới - phải -> Tâm
    [InlineData(3, 7, 4, 8)] // Góc trên - trái -> Tâm
    [InlineData(5, 7, 4, 8)] // Góc trên - phải -> Tâm
    public void RedAdvisor_AtAnyCorner_HasExactlyOneMoveToCenter(int cornerCol, int cornerRow, int centerCol, int centerRow)
    {
        var board = new ChessBoard();
        board.Clear();

        var cornerPos = new Position(cornerCol, cornerRow);
        var centerPos = new Position(centerCol, centerRow);
        board.SetPiece(cornerPos, new Piece(PieceType.Advisor, PieceColor.Red));

        var legalMoves = board.GetLegalMoves(cornerPos);
        Assert.Single(legalMoves);
        Assert.Contains(centerPos, legalMoves);
        Assert.True(AdvisorRule.IsValidMove(board, cornerPos, centerPos));
        Assert.True(board.IsValidMove(cornerPos, centerPos));
    }

    [Theory]
    [InlineData(3, 0, 4, 1)] // Góc trên - trái -> Tâm
    [InlineData(5, 0, 4, 1)] // Góc trên - phải -> Tâm
    [InlineData(3, 2, 4, 1)] // Góc dưới - trái -> Tâm
    [InlineData(5, 2, 4, 1)] // Góc dưới - phải -> Tâm
    public void BlackAdvisor_AtAnyCorner_HasExactlyOneMoveToCenter(int cornerCol, int cornerRow, int centerCol, int centerRow)
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        var cornerPos = new Position(cornerCol, cornerRow);
        var centerPos = new Position(centerCol, centerRow);
        board.SetPiece(cornerPos, new Piece(PieceType.Advisor, PieceColor.Black));

        var legalMoves = board.GetLegalMoves(cornerPos);
        Assert.Single(legalMoves);
        Assert.Contains(centerPos, legalMoves);
        Assert.True(AdvisorRule.IsValidMove(board, cornerPos, centerPos));
        Assert.True(board.IsValidMove(cornerPos, centerPos));
    }

    [Fact]
    public void Advisor_CannotMoveOutsidePalace()
    {
        var board = new ChessBoard();
        board.Clear();

        // 1. Sĩ Đỏ tại góc trên - trái (3, 7):
        // Nước đi chéo hướng ra ngoài cung: (2, 6) [về phía sông]
        var redAdvisor = new Position(3, 7);
        board.SetPiece(redAdvisor, new Piece(PieceType.Advisor, PieceColor.Red));

        var outsideDestRed = new Position(2, 6);
        Assert.False(AdvisorRule.IsInPalace(PieceColor.Red, outsideDestRed));
        Assert.False(AdvisorRule.IsValidMove(board, redAdvisor, outsideDestRed));
        Assert.False(board.IsValidMove(redAdvisor, outsideDestRed));

        // 2. Sĩ Đen tại góc dưới - phải (5, 2):
        // Nước đi chéo hướng ra ngoài cung: (6, 3) [về phía sông]
        board.Clear();
        board.ActiveColor = PieceColor.Black;
        var blackAdvisor = new Position(5, 2);
        board.SetPiece(blackAdvisor, new Piece(PieceType.Advisor, PieceColor.Black));

        var outsideDestBlack = new Position(6, 3);
        Assert.False(AdvisorRule.IsInPalace(PieceColor.Black, outsideDestBlack));
        Assert.False(AdvisorRule.IsValidMove(board, blackAdvisor, outsideDestBlack));
        Assert.False(board.IsValidMove(blackAdvisor, outsideDestBlack));
    }

    [Fact]
    public void Advisor_RejectsOrthogonalAndMultiStepMoves()
    {
        var board = new ChessBoard();
        board.Clear();

        var centerRed = new Position(4, 8);
        board.SetPiece(centerRed, new Piece(PieceType.Advisor, PieceColor.Red));

        // 1. Đứng yên tại chỗ
        Assert.False(AdvisorRule.IsValidMove(board, centerRed, centerRed));

        // 2. Đi ngang hoặc dọc bên trong cung (như Tướng/Xe)
        Assert.False(AdvisorRule.IsValidMove(board, centerRed, new Position(4, 7))); // Tiến thẳng
        Assert.False(AdvisorRule.IsValidMove(board, centerRed, new Position(4, 9))); // Thoái thẳng
        Assert.False(AdvisorRule.IsValidMove(board, centerRed, new Position(3, 8))); // Đi ngang trái
        Assert.False(AdvisorRule.IsValidMove(board, centerRed, new Position(5, 8))); // Đi ngang phải

        // 3. Đi chéo 2 ô (như Tượng)
        Assert.False(AdvisorRule.IsValidMove(board, new Position(3, 9), new Position(5, 7)));

        // 4. Tọa độ ngoài bàn cờ
        Assert.False(Position.IsValid(2, 10));
        Assert.False(Position.IsValid(4, 11));
    }

    [Fact]
    public void Advisor_CanCaptureEnemyPiece_AndBlockedByFriendlyPiece()
    {
        var board = new ChessBoard();
        board.Clear();

        var centerRed = new Position(4, 8);
        board.SetPiece(centerRed, new Piece(PieceType.Advisor, PieceColor.Red));

        // Đặt quân Địch tại góc cung (3, 7)
        var enemyPos = new Position(3, 7);
        board.SetPiece(enemyPos, new Piece(PieceType.Horse, PieceColor.Black));

        // Đặt quân Ta tại góc cung (5, 7)
        var friendlyPos = new Position(5, 7);
        board.SetPiece(friendlyPos, new Piece(PieceType.General, PieceColor.Red));

        // Ăn quân địch trong cung hợp lệ
        Assert.True(AdvisorRule.IsValidMove(board, centerRed, enemyPos));
        Assert.True(board.IsValidMove(centerRed, enemyPos));

        // Bị chặn ăn quân đồng minh
        Assert.False(AdvisorRule.IsValidMove(board, centerRed, friendlyPos));
        Assert.False(board.IsValidMove(centerRed, friendlyPos));

        var legalMoves = board.GetLegalMoves(centerRed);
        Assert.Contains(enemyPos, legalMoves);
        Assert.DoesNotContain(friendlyPos, legalMoves);

        // Thực hiện nước đi ăn quân
        var undo = board.MakeMove(centerRed, enemyPos);
        Assert.NotNull(undo.CapturedPiece);
        Assert.Equal(PieceType.Horse, undo.CapturedPiece.Type);
        Assert.Equal(PieceColor.Black, undo.CapturedPiece.Color);
    }

    [Fact]
    public void Advisor_InitialStandardPosition_HasOneMoveEachToCenter()
    {
        var board = new ChessBoard();
        board.SetupInitialPosition();

        // 1. Sĩ Đỏ cánh trái tại (3, 9) và cánh phải tại (5, 9):
        // Đều có đúng 1 nước đi duy nhất vào tâm cung (4, 8)
        var redAdvisor1 = new Position(3, 9);
        var movesRed1 = board.GetLegalMoves(redAdvisor1);
        Assert.Single(movesRed1);
        Assert.Contains(new Position(4, 8), movesRed1);

        var redAdvisor2 = new Position(5, 9);
        var movesRed2 = board.GetLegalMoves(redAdvisor2);
        Assert.Single(movesRed2);
        Assert.Contains(new Position(4, 8), movesRed2);

        // 2. Sĩ Đen cánh trái tại (3, 0) và cánh phải tại (5, 0):
        board.ActiveColor = PieceColor.Black;

        var blackAdvisor1 = new Position(3, 0);
        var movesBlack1 = board.GetLegalMoves(blackAdvisor1);
        Assert.Single(movesBlack1);
        Assert.Contains(new Position(4, 1), movesBlack1);

        var blackAdvisor2 = new Position(5, 0);
        var movesBlack2 = board.GetLegalMoves(blackAdvisor2);
        Assert.Single(movesBlack2);
        Assert.Contains(new Position(4, 1), movesBlack2);
    }
}
