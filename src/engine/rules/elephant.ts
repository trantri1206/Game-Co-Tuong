import { ChessBoardState, Position, PieceColor, isValidPosition } from '../model';

/**
 * Định nghĩa 4 bước nhảy chéo hình chữ Điền và độ dời của mắt Tượng tương ứng (GCT-54).
 */
export const ELEPHANT_MOVES = [
  { dc: -2, dr: -2, eyeDc: -1, eyeDr: -1 }, // Lên - Trái
  { dc: 2, dr: -2, eyeDc: 1, eyeDr: -1 },  // Lên - Phải
  { dc: -2, dr: 2, eyeDc: -1, eyeDr: 1 },  // Xuống - Trái
  { dc: 2, dr: 2, eyeDc: 1, eyeDr: 1 }    // Xuống - Phải
] as const;

/**
 * Xác định tọa độ mắt Tượng (giao điểm chính giữa) giữa ô xuất phát và ô đích.
 * Trả về null nếu không phải bước nhảy chéo đúng 2 giao điểm.
 */
export function getElephantEyePosition(from: Position, to: Position): Position | null {
  const dCol = to.col - from.col;
  const dRow = to.row - from.row;

  if (Math.abs(dCol) === 2 && Math.abs(dRow) === 2) {
    return {
      col: from.col + dCol / 2,
      row: from.row + dRow / 2
    };
  }

  return null;
}

/**
 * Kiểm tra xem mắt Tượng có bị quân cản chiếm giữ hay không.
 */
export function isElephantEyeBlocked(
  board: ChessBoardState,
  from: Position,
  to: Position
): boolean {
  const eye = getElephantEyePosition(from, to);
  if (!eye) {
    return false;
  }

  if (!isValidPosition(eye.col, eye.row)) {
    return false;
  }

  return board.grid[eye.col][eye.row] !== null;
}

/**
 * Kiểm tra xem ô đích có vi phạm quy tắc "Tượng không qua sông" hay không:
 * - Phe Đỏ: Nửa dưới (Row 5..9), vi phạm nếu Row <= 4.
 * - Phe Đen: Nửa trên (Row 0..4), vi phạm nếu Row >= 5.
 */
export function isElephantCrossingRiver(color: PieceColor, targetRow: number): boolean {
  if (color === 'Red') {
    return targetRow <= 4;
  } else {
    return targetRow >= 5;
  }
}

/**
 * Kiểm tra tính hợp lệ của nước đi quân Tượng (GCT-54).
 */
export function isValidElephantMove(
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
  if (!piece || piece.type !== 'Elephant') {
    return false;
  }

  // 1. Phải đi chéo đúng 2 ô: |dCol| === 2 && |dRow| === 2
  const dCol = to.col - from.col;
  const dRow = to.row - from.row;
  if (Math.abs(dCol) !== 2 || Math.abs(dRow) !== 2) {
    return false;
  }

  // 2. Tuyệt đối không qua sông
  if (isElephantCrossingRiver(piece.color, to.row)) {
    return false;
  }

  // 3. Mắt Tượng ở giữa phải hoàn toàn trống (chặn chẹn mắt Tượng)
  const eyeCol = from.col + dCol / 2;
  const eyeRow = from.row + dRow / 2;
  if (board.grid[eyeCol][eyeRow] !== null) {
    return false;
  }

  // 4. Điểm đến không được chứa quân đồng minh
  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    return false;
  }

  return true;
}

/**
 * Sinh danh sách tất cả các nước đi hợp lệ cho quân Tượng tại vị trí 'from'.
 */
export function getElephantLegalMoves(
  board: ChessBoardState,
  from: Position
): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Elephant') {
    return [];
  }

  const legalMoves: Position[] = [];

  for (const move of ELEPHANT_MOVES) {
    const targetCol = from.col + move.dc;
    const targetRow = from.row + move.dr;

    if (!isValidPosition(targetCol, targetRow)) {
      continue;
    }

    if (isElephantCrossingRiver(piece.color, targetRow)) {
      continue;
    }

    const eyeCol = from.col + move.eyeDc;
    const eyeRow = from.row + move.eyeDr;
    if (board.grid[eyeCol][eyeRow] !== null) {
      continue; // Bị chẹn mắt Tượng
    }

    const targetPiece = board.grid[targetCol][targetRow];
    if (targetPiece && targetPiece.color === piece.color) {
      continue; // Chặn quân cùng màu
    }

    legalMoves.push({ col: targetCol, row: targetRow });
  }

  return legalMoves;
}
