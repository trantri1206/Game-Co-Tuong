namespace GameCoTuong.Engine;

/// <summary>
/// Bản ghi lưu trữ đầy đủ ngữ cảnh của một nước đi để phục vụ việc hoàn tác (Undo) chính xác 100%.
/// </summary>
public sealed record UndoMoveRecord
{
    public Position From { get; }
    public Position To { get; }
    public Piece MovedPiece { get; }
    public Piece? CapturedPiece { get; }
    public PieceColor PreviousActiveColor { get; }
    public int PreviousHalfMoveClock { get; }
    public int PreviousFullMoveNumber { get; }
    public int PreviousStateVersion { get; }

    public UndoMoveRecord(
        Position from,
        Position to,
        Piece movedPiece,
        Piece? capturedPiece,
        PieceColor previousActiveColor,
        int previousHalfMoveClock,
        int previousFullMoveNumber,
        int previousStateVersion)
    {
        From = from;
        To = to;
        MovedPiece = movedPiece;
        CapturedPiece = capturedPiece;
        PreviousActiveColor = previousActiveColor;
        PreviousHalfMoveClock = previousHalfMoveClock;
        PreviousFullMoveNumber = previousFullMoveNumber;
        PreviousStateVersion = previousStateVersion;
    }

    /// <summary>
    /// Cho biết nước đi này có ăn quân của đối phương hay không.
    /// </summary>
    public bool HasCaptured => CapturedPiece != null;

    public override string ToString()
    {
        var captureStr = HasCaptured ? $" (Ăn {CapturedPiece})" : "";
        return $"{MovedPiece}: {From} -> {To}{captureStr}";
    }
}
