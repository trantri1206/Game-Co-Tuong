import { ChessBoardState, PieceColor, Position, isValidPosition } from '../model';

/**
 * Kiểm tra xem quân Tốt đã qua sông hay chưa.
 * - Phe Đỏ: Xuất phát ở hàng 6, qua sông khi row <= 4.
 * - Phe Đen: Xuất phát ở hàng 3, qua sông khi row >= 5.
 */
export function hasSoldierCrossedRiver(color: PieceColor, row: number): boolean {
  return color === 'Red' ? row <= 4 : row >= 5;
}

/**
 * Kiểm tra tính hợp lệ của nước đi quân Tốt / Binh (GCT-50).
 */
export function isValidSoldierMove(
  board: ChessBoardState,
  from: Position,
  to: Position
): boolean {
  if (!isValidPosition(from.col, from.row) || !isValidPosition(to.col, to.row)) {
    return false;
  }

  if (from.col === to.col && from.row === to.row) {
    return false;
  }

  const piece = board.grid[from.col][from.row];
  if (!piece || piece.type !== 'Soldier') {
    return false;
  }

  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    return false;
  }

  const dCol = to.col - from.col;
  const dRow = to.row - from.row;
  const absCol = Math.abs(dCol);
  const absRow = Math.abs(dRow);

  // Mỗi nước đi chỉ được đi đúng 1 bước (chặn đi chéo và đi > 1 ô)
  if (absCol + absRow !== 1) {
    return false;
  }

  const crossedRiver = hasSoldierCrossedRiver(piece.color, from.row);

  if (piece.color === 'Red') {
    // Binh Đỏ: tiến là row giảm (dRow = -1), cấm đi lùi (dRow > 0)
    if (dRow > 0) return false;

    // Chưa qua sông: chỉ tiến thẳng
    if (!crossedRiver) {
      return dRow === -1 && dCol === 0;
    }

    // Đã qua sông: được tiến thẳng HOẶC đi ngang
    return (dRow === -1 && dCol === 0) || (dRow === 0 && absCol === 1);
  } else {
    // Tốt Đen: tiến là row tăng (dRow = 1), cấm đi lùi (dRow < 0)
    if (dRow < 0) return false;

    // Chưa qua sông: chỉ tiến thẳng
    if (!crossedRiver) {
      return dRow === 1 && dCol === 0;
    }

    // Đã qua sông: được tiến thẳng HOẶC đi ngang
    return (dRow === 1 && dCol === 0) || (dRow === 0 && absCol === 1);
  }
}

/**
 * Lấy tất cả các nước đi hợp lệ cho quân Tốt tại vị trí 'from'.
 */
export function getSoldierLegalMoves(board: ChessBoardState, from: Position): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Soldier') {
    return [];
  }

  const legalMoves: Position[] = [];
  const candidates = [
    { col: from.col, row: from.row - 1 }, // Lên
    { col: from.col, row: from.row + 1 }, // Xuống
    { col: from.col - 1, row: from.row }, // Trái
    { col: from.col + 1, row: from.row }  // Phải
  ];

  for (const cand of candidates) {
    if (isValidPosition(cand.col, cand.row)) {
      if (isValidSoldierMove(board, from, cand)) {
        legalMoves.push(cand);
      }
    }
  }

  return legalMoves;
}
