import { ChessBoardState, Position, PieceColor, isValidPosition } from '../model';

export const GENERAL_DIRECTIONS = [
  { dc: 0, dr: -1 }, // Lên
  { dc: 0, dr: 1 },  // Xuống
  { dc: -1, dr: 0 }, // Trái
  { dc: 1, dr: 0 }   // Phải
] as const;

/**
 * Kiểm tra xem một ô (col, row) có nằm trong phạm vi Cửu cung của phe chỉ định hay không.
 */
export function isGeneralInPalace(color: PieceColor, col: number, row: number): boolean {
  if (col < 3 || col > 5) {
    return false;
  }

  if (color === 'Black') {
    return row >= 0 && row <= 2;
  } else {
    return row >= 7 && row <= 9;
  }
}

/**
 * Tìm vị trí của quân Tướng thuộc phe chỉ định.
 */
export function findGeneralPosition(board: ChessBoardState, color: PieceColor): Position | null {
  const minRow = color === 'Black' ? 0 : 7;
  const maxRow = color === 'Black' ? 2 : 9;

  for (let r = minRow; r <= maxRow; r++) {
    for (let c = 3; c <= 5; c++) {
      const piece = board.grid[c]?.[r];
      if (piece && piece.type === 'General' && piece.color === color) {
        return { col: c, row: r };
      }
    }
  }

  return null;
}

/**
 * Kiểm tra xem hai vị trí Tướng có nhìn thẳng mặt nhau trên cùng một cột trống hay không.
 */
export function areGeneralsFacing(board: ChessBoardState, pos1: Position, pos2: Position): boolean {
  if (pos1.col !== pos2.col) {
    return false;
  }

  const minRow = Math.min(pos1.row, pos2.row) + 1;
  const maxRow = Math.max(pos1.row, pos2.row) - 1;

  for (let r = minRow; r <= maxRow; r++) {
    if (board.grid[pos1.col][r] !== null) {
      return false; // Có quân cản ở giữa
    }
  }

  return true; // Hoàn toàn trống giữa 2 Tướng
}

/**
 * Kiểm tra xem ô target có đang bị phe attackerColor khống chế (tấn công) hay không.
 */
export function isSquareAttacked(board: ChessBoardState, target: Position, attackerColor: PieceColor): boolean {
  // 1. Kiểm tra Xe, Tướng (đường trống) hoặc Pháo (qua 1 ngòi)
  const directions = [
    { dc: 0, dr: -1 },
    { dc: 0, dr: 1 },
    { dc: -1, dr: 0 },
    { dc: 1, dr: 0 }
  ];

  for (const { dc, dr } of directions) {
    let c = target.col + dc;
    let r = target.row + dr;
    let piecesBetween = 0;

    while (isValidPosition(c, r)) {
      const piece = board.grid[c][r];
      if (piece !== null) {
        if (piecesBetween === 0) {
          if (piece.color === attackerColor) {
            if (piece.type === 'Chariot') {
              return true;
            }
            if (piece.type === 'General' && target.col === c) {
              return true;
            }
          }
          piecesBetween = 1;
        } else if (piecesBetween === 1) {
          if (piece.color === attackerColor && piece.type === 'Cannon') {
            return true;
          }
          break;
        }
      }
      c += dc;
      r += dr;
    }
  }

  // 2. Kiểm tra Mã đối phương
  const horseSources = [
    { dc: -1, dr: -2, footDc: 0, footDr: -1 },
    { dc: 1, dr: -2, footDc: 0, footDr: -1 },
    { dc: -1, dr: 2, footDc: 0, footDr: 1 },
    { dc: 1, dr: 2, footDc: 0, footDr: 1 },
    { dc: -2, dr: -1, footDc: -1, footDr: 0 },
    { dc: -2, dr: 1, footDc: -1, footDr: 0 },
    { dc: 2, dr: -1, footDc: 1, footDr: 0 },
    { dc: 2, dr: 1, footDc: 1, footDr: 0 }
  ];

  for (const { dc, dr, footDc, footDr } of horseSources) {
    const horseCol = target.col + dc;
    const horseRow = target.row + dr;

    if (isValidPosition(horseCol, horseRow)) {
      const piece = board.grid[horseCol][horseRow];
      if (piece && piece.color === attackerColor && piece.type === 'Horse') {
        const footCol = target.col + footDc;
        const footRow = target.row + footDr;
        if (isValidPosition(footCol, footRow) && board.grid[footCol][footRow] === null) {
          return true;
        }
      }
    }
  }

  // 3. Kiểm tra Tốt / Binh đối phương
  if (attackerColor === 'Black') {
    // Tốt Đen đi xuống (dr = +1), tấn công ô target từ trên (target.row - 1)
    const aboveRow = target.row - 1;
    if (isValidPosition(target.col, aboveRow)) {
      const p = board.grid[target.col][aboveRow];
      if (p && p.color === 'Black' && p.type === 'Soldier') {
        return true;
      }
    }
    // Tấn công ngang nếu Tốt Đen đã qua sông (target.row >= 5)
    if (target.row >= 5) {
      for (const sideCol of [target.col - 1, target.col + 1]) {
        if (isValidPosition(sideCol, target.row)) {
          const p = board.grid[sideCol][target.row];
          if (p && p.color === 'Black' && p.type === 'Soldier') {
            return true;
          }
        }
      }
    }
  } else {
    // Binh Đỏ đi lên (dr = -1), tấn công ô target từ dưới (target.row + 1)
    const belowRow = target.row + 1;
    if (isValidPosition(target.col, belowRow)) {
      const p = board.grid[target.col][belowRow];
      if (p && p.color === 'Red' && p.type === 'Soldier') {
        return true;
      }
    }
    // Tấn công ngang nếu Binh Đỏ đã qua sông (target.row <= 4)
    if (target.row <= 4) {
      for (const sideCol of [target.col - 1, target.col + 1]) {
        if (isValidPosition(sideCol, target.row)) {
          const p = board.grid[sideCol][target.row];
          if (p && p.color === 'Red' && p.type === 'Soldier') {
            return true;
          }
        }
      }
    }
  }

  return false;
}

/**
 * Kiểm tra tính hợp lệ của nước đi quân Tướng (GCT-56).
 */
export function isValidGeneralMove(
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

  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'General') {
    return false;
  }

  // 1. Phải đi ngang hoặc dọc đúng 1 giao điểm
  const dCol = to.col - from.col;
  const dRow = to.row - from.row;
  const absCol = Math.abs(dCol);
  const absRow = Math.abs(dRow);
  if (absCol + absRow !== 1) {
    return false;
  }

  // 2. Điểm đến phải nằm trong Cửu cung
  if (!isGeneralInPalace(piece.color, to.col, to.row)) {
    return false;
  }

  // 3. Không ăn quân đồng minh
  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    return false;
  }

  // 4. Mô phỏng thử nước đi để kiểm tra an toàn và Lộ mặt Tướng
  const opponentColor: PieceColor = piece.color === 'Red' ? 'Black' : 'Red';

  // Tạm thời áp dụng nước đi
  board.grid[from.col][from.row] = null;
  board.grid[to.col][to.row] = piece;

  let isValid = true;
  try {
    // 4a. Kiểm tra Lộ mặt Tướng
    const enemyGenPos = findGeneralPosition(board, opponentColor);
    if (enemyGenPos && areGeneralsFacing(board, to, enemyGenPos)) {
      isValid = false;
    }

    // 4b. Kiểm tra ô đích có bị khống chế hay không
    if (isValid && isSquareAttacked(board, to, opponentColor)) {
      isValid = false;
    }
  } finally {
    // Khôi phục
    board.grid[to.col][to.row] = targetPiece;
    board.grid[from.col][from.row] = piece;
  }

  return isValid;
}

/**
 * Sinh danh sách tất cả các nước đi hợp lệ cho quân Tướng tại vị trí 'from'.
 */
export function getGeneralLegalMoves(
  board: ChessBoardState,
  from: Position
): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'General') {
    return [];
  }

  const legalMoves: Position[] = [];

  for (const { dc, dr } of GENERAL_DIRECTIONS) {
    const targetCol = from.col + dc;
    const targetRow = from.row + dr;

    if (isValidPosition(targetCol, targetRow)) {
      const targetPos = { col: targetCol, row: targetRow };
      if (isValidGeneralMove(board, from, targetPos)) {
        legalMoves.push(targetPos);
      }
    }
  }

  return legalMoves;
}
