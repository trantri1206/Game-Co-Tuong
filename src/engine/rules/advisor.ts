import { ChessBoardState, Position, PieceColor, isValidPosition } from '../model';

/**
 * 4 hướng đi chéo đúng 1 giao điểm của quân Sĩ (GCT-55).
 */
export const ADVISOR_DIRECTIONS = [
  { dc: -1, dr: -1 }, // Lên - Trái
  { dc: 1, dr: -1 },  // Lên - Phải
  { dc: -1, dr: 1 },  // Xuống - Trái
  { dc: 1, dr: 1 }   // Xuống - Phải
] as const;

/**
 * Kiểm tra xem một ô (col, row) có nằm trong phạm vi Cửu cung của phe chỉ định hay không.
 * - Cột Cửu cung chung: Col từ 3 đến 5 (3, 4, 5).
 * - Cung Đen: Row từ 0 đến 2 (0, 1, 2).
 * - Cung Đỏ: Row từ 7 đến 9 (7, 8, 9).
 */
export function isAdvisorInPalace(color: PieceColor, col: number, row: number): boolean {
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
 * Kiểm tra tính hợp lệ của nước đi quân Sĩ (GCT-55).
 */
export function isValidAdvisorMove(
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
  if (!piece || piece.type !== 'Advisor') {
    return false;
  }

  // 1. Phải đi chéo đúng 1 giao điểm: |dCol| === 1 && |dRow| === 1
  const dCol = to.col - from.col;
  const dRow = to.row - from.row;
  if (Math.abs(dCol) !== 1 || Math.abs(dRow) !== 1) {
    return false;
  }

  // 2. Cả ô xuất phát và ô đích đều phải nằm trong Cửu cung của phe mình
  if (!isAdvisorInPalace(piece.color, from.col, from.row) || !isAdvisorInPalace(piece.color, to.col, to.row)) {
    return false;
  }

  // 3. Điểm đến không được chứa quân đồng minh
  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    return false;
  }

  return true;
}

/**
 * Sinh danh sách tất cả các nước đi hợp lệ cho quân Sĩ tại vị trí 'from'.
 */
export function getAdvisorLegalMoves(
  board: ChessBoardState,
  from: Position
): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Advisor') {
    return [];
  }

  // Nếu ô xuất phát nằm ngoài Cửu cung thì không có nước đi hợp lệ
  if (!isAdvisorInPalace(piece.color, from.col, from.row)) {
    return [];
  }

  const legalMoves: Position[] = [];

  for (const { dc, dr } of ADVISOR_DIRECTIONS) {
    const targetCol = from.col + dc;
    const targetRow = from.row + dr;

    // Kiểm tra điểm đến phải nằm trong Cửu cung
    if (!isAdvisorInPalace(piece.color, targetCol, targetRow)) {
      continue;
    }

    // Kiểm tra điểm đến không chứa quân đồng minh
    const targetPiece = board.grid[targetCol][targetRow];
    if (targetPiece && targetPiece.color === piece.color) {
      continue;
    }

    legalMoves.push({ col: targetCol, row: targetRow });
  }

  return legalMoves;
}
