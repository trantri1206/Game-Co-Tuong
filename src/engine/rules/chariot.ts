import { ChessBoardState, Position, isValidPosition } from '../model';

/**
 * Đếm số lượng quân cờ nằm giữa hai ô trên cùng một hàng hoặc cùng một cột (exclusive).
 */
export function countPiecesBetween(
  board: ChessBoardState,
  from: Position,
  to: Position
): number {
  if (from.col === to.col) {
    const minRow = Math.min(from.row, to.row) + 1;
    const maxRow = Math.max(from.row, to.row) - 1;
    let count = 0;

    for (let r = minRow; r <= maxRow; r++) {
      if (board.grid[from.col][r] !== null) {
        count++;
      }
    }

    return count;
  } else if (from.row === to.row) {
    const minCol = Math.min(from.col, to.col) + 1;
    const maxCol = Math.max(from.col, to.col) - 1;
    let count = 0;

    for (let c = minCol; c <= maxCol; c++) {
      if (board.grid[c][from.row] !== null) {
        count++;
      }
    }

    return count;
  }

  return -1; // Không cùng hàng hoặc cột
}

/**
 * Kiểm tra tính hợp lệ của nước đi quân Xe (GCT-51).
 */
export function isValidChariotMove(
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
  if (!piece || piece.type !== 'Chariot') {
    return false;
  }

  const isVertical = from.col === to.col;
  const isHorizontal = from.row === to.row;
  if (!isVertical && !isHorizontal) {
    return false; // Chặn đi chéo
  }

  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    return false; // Chặn ăn quân đồng minh
  }

  // Đoạn đường giữa from và to phải hoàn toàn trống (không nhảy qua bất kỳ quân nào)
  return countPiecesBetween(board, from, to) === 0;
}

/**
 * Sinh danh sách tất cả các nước đi hợp lệ cho quân Xe tại vị trí 'from'.
 */
export function getChariotLegalMoves(
  board: ChessBoardState,
  from: Position
): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Chariot') {
    return [];
  }

  const legalMoves: Position[] = [];
  const directions = [
    { dc: 0, dr: -1 }, // Lên
    { dc: 0, dr: 1 },  // Xuống
    { dc: -1, dr: 0 }, // Trái
    { dc: 1, dr: 0 }   // Phải
  ];

  for (const { dc, dr } of directions) {
    let c = from.col + dc;
    let r = from.row + dr;

    while (isValidPosition(c, r)) {
      const target = board.grid[c][r];

      if (!target) {
        legalMoves.push({ col: c, row: r });
      } else {
        if (target.color !== piece.color) {
          // Ăn quân đối phương và dừng tia
          legalMoves.push({ col: c, row: r });
        }
        break; // Dừng tia khi gặp bất kỳ vật cản nào
      }

      c += dc;
      r += dr;
    }
  }

  return legalMoves;
}
