using GameCoTuong.Engine;
using GameCoTuong.Engine.Rules;
using Xunit;

namespace GameCoTuong.Tests.EngineTests;

/// <summary>
/// Bộ kiểm thử chuyên biệt cho luật di chuyển và ăn quân của quân Tốt / Binh (GCT-50).
/// </summary>
public class SoldierRuleTests
{
    [Fact]
    public void RiverBoundary_CrossedRiverCheck_IsAccurate()
    {
        // Phe Đỏ: hàng 5..9 là sân nhà (chưa qua sông), hàng 0..4 là qua sông
        Assert.False(SoldierRule.HasCrossedRiver(PieceColor.Red, 9));
        Assert.False(SoldierRule.HasCrossedRiver(PieceColor.Red, 6));
        Assert.False(SoldierRule.HasCrossedRiver(PieceColor.Red, 5));
        Assert.True(SoldierRule.HasCrossedRiver(PieceColor.Red, 4));
        Assert.True(SoldierRule.HasCrossedRiver(PieceColor.Red, 0));

        // Phe Đen: hàng 0..4 là sân nhà (chưa qua sông), hàng 5..9 là qua sông
        Assert.False(SoldierRule.HasCrossedRiver(PieceColor.Black, 0));
        Assert.False(SoldierRule.HasCrossedRiver(PieceColor.Black, 3));
        Assert.False(SoldierRule.HasCrossedRiver(PieceColor.Black, 4));
        Assert.True(SoldierRule.HasCrossedRiver(PieceColor.Black, 5));
        Assert.True(SoldierRule.HasCrossedRiver(PieceColor.Black, 9));
    }

    [Fact]
    public void RedSoldier_BeforeRiver_CanOnlyMoveForwardOneStep()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Binh Đỏ tại vị trí xuất phát ban đầu (4, 6)
        var from = new Position(4, 6);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Red));

        // 1. Tiến 1 bước thẳng lên (4, 5) -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(4, 5)));
        Assert.True(board.IsValidMove(from, new Position(4, 5)));

        // 2. Đi ngang trái (3, 6) hoặc phải (5, 6) khi chưa qua sông -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(3, 6)));
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(5, 6)));

        // 3. Đi lùi xuống (4, 7) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 7)));

        // 4. Đi chéo (3, 5) hoặc (5, 5) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(3, 5)));
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(5, 5)));

        // 5. Tiến 2 bước (4, 4) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 4)));

        // 6. Danh sách nước đi hợp lệ chỉ có duy nhất 1 nước (4, 5)
        var legalMoves = board.GetLegalMoves(from);
        Assert.Single(legalMoves);
        Assert.Equal(new Position(4, 5), legalMoves[0]);
    }

    [Fact]
    public void RedSoldier_AcrossRiver_CanMoveForwardAndSideways_NeverBackward()
    {
        var board = new ChessBoard();
        board.Clear();

        // Đặt Binh Đỏ đã qua sông tại (4, 4)
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Red));

        // 1. Tiến thẳng 1 bước (4, 3) -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(4, 3)));

        // 2. Đi ngang trái (3, 4) và phải (5, 4) -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(3, 4)));
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(5, 4)));

        // 3. Đi lùi (4, 5) -> BỊ CHẶN TUYỆT ĐỐI
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 5)));

        // 4. Đi chéo (3, 3) hoặc (5, 3) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(3, 3)));
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(5, 3)));

        // 5. Nước đi hợp lệ gồm đúng 3 nước: tiến, trái, phải
        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(3, legalMoves.Count);
        Assert.Contains(new Position(4, 3), legalMoves);
        Assert.Contains(new Position(3, 4), legalMoves);
        Assert.Contains(new Position(5, 4), legalMoves);
    }

    [Fact]
    public void RedSoldier_AtOpponentBackRank_CanOnlyMoveSideways_NoPromotion()
    {
        var board = new ChessBoard();
        board.Clear();

        // Binh Đỏ đã chạm đáy bàn cờ đối phương tại (4, 0)
        var from = new Position(4, 0);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Red));

        // 1. Không thể tiến (ngoài bàn cờ)
        Assert.False(Position.IsValid(4, -1));

        // 2. Không được đi lùi (4, 1)
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 1)));

        // 3. Chỉ được đi ngang trái (3, 0) hoặc phải (5, 0)
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(3, 0)));
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(5, 0)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(2, legalMoves.Count);
        Assert.Contains(new Position(3, 0), legalMoves);
        Assert.Contains(new Position(5, 0), legalMoves);

        // 4. Không phong cấp: quân cờ vẫn là Binh / Tốt
        var piece = board.GetPiece(from);
        Assert.NotNull(piece);
        Assert.Equal(PieceType.Soldier, piece.Type);
    }

    [Fact]
    public void RedSoldier_AtBackRankCorner_CanOnlyMoveOneWaySideways()
    {
        var board = new ChessBoard();
        board.Clear();

        // Binh Đỏ ở góc đáy (0, 0)
        var from = new Position(0, 0);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Red));

        // Chỉ đi được sang phải (1, 0)
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(1, 0)));
        Assert.False(Position.IsValid(-1, 0)); // Bên trái ra ngoài bàn

        var legalMoves = board.GetLegalMoves(from);
        Assert.Single(legalMoves);
        Assert.Equal(new Position(1, 0), legalMoves[0]);
    }

    [Fact]
    public void BlackSoldier_BeforeRiver_CanOnlyMoveForwardOneStep()
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        // Đặt Tốt Đen tại vị trí xuất phát ban đầu (4, 3)
        var from = new Position(4, 3);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Black));

        // 1. Tiến 1 bước thẳng xuống (4, 4) -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(4, 4)));
        Assert.True(board.IsValidMove(from, new Position(4, 4)));

        // 2. Đi ngang trái (3, 3) hoặc phải (5, 3) khi chưa qua sông -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(3, 3)));
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(5, 3)));

        // 3. Đi lùi lên (4, 2) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 2)));

        // 4. Đi chéo (3, 4) hoặc (5, 4) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(3, 4)));
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(5, 4)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Single(legalMoves);
        Assert.Equal(new Position(4, 4), legalMoves[0]);
    }

    [Fact]
    public void BlackSoldier_AcrossRiver_CanMoveForwardAndSideways_NeverBackward()
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        // Đặt Tốt Đen đã qua sông tại (4, 5)
        var from = new Position(4, 5);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Black));

        // 1. Tiến thẳng 1 bước (4, 6) -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(4, 6)));

        // 2. Đi ngang trái (3, 5) và phải (5, 5) -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(3, 5)));
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(5, 5)));

        // 3. Đi lùi (4, 4) -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 4)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(3, legalMoves.Count);
        Assert.Contains(new Position(4, 6), legalMoves);
        Assert.Contains(new Position(3, 5), legalMoves);
        Assert.Contains(new Position(5, 5), legalMoves);
    }

    [Fact]
    public void BlackSoldier_AtOpponentBackRank_CanOnlyMoveSideways_NoPromotion()
    {
        var board = new ChessBoard();
        board.Clear();
        board.ActiveColor = PieceColor.Black;

        // Tốt Đen ở hàng đáy đối phương tại (4, 9)
        var from = new Position(4, 9);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Black));

        // 1. Không thể tiến (ngoài bàn)
        Assert.False(Position.IsValid(4, 10));

        // 2. Không được đi lùi (4, 8)
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(4, 8)));

        // 3. Chỉ được đi ngang trái (3, 9) hoặc phải (5, 9)
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(3, 9)));
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(5, 9)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(2, legalMoves.Count);
        Assert.Contains(new Position(3, 9), legalMoves);
        Assert.Contains(new Position(5, 9), legalMoves);

        // 4. Không phong cấp
        var piece = board.GetPiece(from);
        Assert.NotNull(piece);
        Assert.Equal(PieceType.Soldier, piece.Type);
    }

    [Fact]
    public void Soldier_CapturesEnemyPiece_AndRejectsFriendlyFire()
    {
        var board = new ChessBoard();
        board.Clear();

        // Binh Đỏ tại (4, 4) đã qua sông
        var from = new Position(4, 4);
        board.SetPiece(from, new Piece(PieceType.Soldier, PieceColor.Red));

        // Đặt quân Đen (đối phương) ở ô tiến (4, 3) và ô ngang (3, 4)
        board.SetPiece(new Position(4, 3), new Piece(PieceType.Horse, PieceColor.Black));
        board.SetPiece(new Position(3, 4), new Piece(PieceType.Cannon, PieceColor.Black));

        // Đặt quân Đỏ (đồng minh) ở ô ngang (5, 4)
        board.SetPiece(new Position(5, 4), new Piece(PieceType.Chariot, PieceColor.Red));

        // 1. Ăn quân địch ở ô tiến -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(4, 3)));

        // 2. Ăn quân địch ở ô ngang -> HỢP LỆ
        Assert.True(SoldierRule.IsValidMove(board, from, new Position(3, 4)));

        // 3. Đi vào ô quân đồng minh -> BỊ CHẶN
        Assert.False(SoldierRule.IsValidMove(board, from, new Position(5, 4)));

        var legalMoves = board.GetLegalMoves(from);
        Assert.Equal(2, legalMoves.Count);
        Assert.Contains(new Position(4, 3), legalMoves);
        Assert.Contains(new Position(3, 4), legalMoves);
        Assert.DoesNotContain(new Position(5, 4), legalMoves);
    }
}
