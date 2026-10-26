import { ChessBoardState, Position, isValidPosition } from '../model';

/**
 * Định nghĩa 8 bước nhảy chữ L và độ dời của ô chân cản tương ứng (GCT-52).
 */
export const HORSE_MOVES = [
  // Nhảy lên trên (dr = -2): chân cản tại (0, -1)
  { dc: -1, dr: -2, footDc: 0, footDr: -1 },
  { dc: 1, dr: -2, footDc: 0, footDr: -1 },

  // Nhảy xuống dưới (dr = 2): chân cản tại (0, 1)
  { dc: -1, dr: 2, footDc: 0, footDr: 1 },
  { dc: 1, dr: 2, footDc: 0, footDr: 1 },

  // Nhảy sang trái (dc = -2): chân cản tại (-1, 0)
  { dc: -2, dr: -1, footDc: -1, footDr: 0 },
  { dc: -2, dr: 1, footDc: -1, footDr: 0 },

  // Nhảy sang phải (dc = 2): chân cản tại (1, 0)
  { dc: 2, dr: -1, footDc: 1, footDr: 0 },
  { dc: 2, dr: 1, footDc: 1, footDr: 0 }
] as const;

/**
 * Xác định tọa độ ô chân cản Mã (obstacle square) giữa ô xuất phát 'from' và ô đích 'to'.
 * Trả về null nếu không phải bước nhảy hình chữ L của Mã.
 */
export function getHorseObstacleSquare(from: Position, to: Position): Position | null {
  const dCol = to.col - from.col;
  const dRow = to.row - from.row;
  const absCol = Math.abs(dCol);
  const absRow = Math.abs(dRow);

  // Nhảy 2 dọc, 1 ngang -> Chân cản tại (from.col, from.row + dRow / 2)
  if (absRow === 2 && absCol === 1) {
    return { col: from.col, row: from.row + dRow / 2 };
  }

  // Nhảy 2 ngang, 1 dọc -> Chân cản tại (from.col + dCol / 2, from.row)
  if (absCol === 2 && absRow === 1) {
    return { col: from.col + dCol / 2, row: from.row };
  }

  return null;
}

/**
 * Kiểm tra xem hướng nhảy của Mã có bị cản chân (bán mã cước) hay không.
 * Nếu có bất kỳ quân cờ nào (quân ta hoặc quân địch) tại ô chân cản -> trả về true (bị cản).
 */
export function isHorseHobbled(
  board: ChessBoardState,
  from: Position,
  to: Position
): boolean {
  const obstacle = getHorseObstacleSquare(from, to);
  if (!obstacle) {
    return false;
  }

  if (!isValidPosition(obstacle.col, obstacle.row)) {
    return false;
  }

  return board.grid[obstacle.col][obstacle.row] !== null;
}

/**
 * Kiểm tra tính hợp lệ của nước đi quân Mã (GCT-52).
 */
export function isValidHorseMove(
  board: ChessBoardState,
  from: Position,
  to: Position
): boolean {
  // 1. Kiểm tra tọa độ trong bàn cờ
  if (!isValidPosition(from.col, from.row) || !isValidPosition(to.col, to.row)) {
    return false;
  }

  // 2. Chặn đứng yên tại chỗ
  if (from.col === to.col && from.row === to.row) {
    return false;
  }

  // 3. Quân tại ô xuất phát phải là quân Mã
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Horse') {
    return false;
  }

  // 4. Kiểm tra đúng hình chữ L
  const obstacle = getHorseObstacleSquare(from, to);
  if (!obstacle) {
    return false;
  }

  // 5. Kiểm tra cản chân Mã (bất kể màu quân cản)
  if (board.grid[obstacle.col][obstacle.row] !== null) {
    return false;
  }

  // 6. Điểm đến không được chứa quân đồng minh
  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    return false;
  }

  return true;
}

/**
 * Sinh danh sách tất cả các nước đi hợp lệ cho quân Mã tại vị trí 'from'.
 */
export function getHorseLegalMoves(
  board: ChessBoardState,
  from: Position
): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Horse') {
    return [];
  }

  const legalMoves: Position[] = [];

  for (const move of HORSE_MOVES) {
    const footCol = from.col + move.footDc;
    const footRow = from.row + move.footDr;

    // Kiểm tra ô chân cản: nếu nằm ngoài bàn cờ hoặc có quân cản -> bỏ qua
    if (!isValidPosition(footCol, footRow) || board.grid[footCol][footRow] !== null) {
      continue;
    }

    const targetCol = from.col + move.dc;
    const targetRow = from.row + move.dr;

    if (!isValidPosition(targetCol, targetRow)) {
      continue;
    }

    const targetPiece = board.grid[targetCol][targetRow];
    if (targetPiece && targetPiece.color === piece.color) {
      continue; // Chặn quân cùng màu
    }

    legalMoves.push({ col: targetCol, row: targetRow });
  }

  return legalMoves;
}
