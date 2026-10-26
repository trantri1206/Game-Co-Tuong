import { ChessBoardState, Position, isValidPosition } from '../model';
import { countPiecesBetween } from './chariot';

/**
 * Kiểm tra tính hợp lệ của nước đi quân Pháo (GCT-53).
 */
export function isValidCannonMove(
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
  if (!piece || piece.type !== 'Cannon') {
    return false;
  }

  const isVertical = from.col === to.col;
  const isHorizontal = from.row === to.row;
  if (!isVertical && !isHorizontal) {
    return false; // Chặn đi chéo
  }

  const targetPiece = board.grid[to.col][to.row];

  // Chặn ăn quân cùng màu (kể cả có ngòi)
  if (targetPiece && targetPiece.color === piece.color) {
    return false;
  }

  const piecesBetween = countPiecesBetween(board, from, to);
  if (piecesBetween < 0) {
    return false;
  }

  // 1. Khi KHÔNG ăn quân (ô đích trống): Đoạn giữa phải trống (0 quân cản)
  if (targetPiece === null) {
    return piecesBetween === 0;
  }

  // 2. Khi ĂN QUÂN (ô đích có quân đối phương): Đoạn giữa phải có ĐÚNG 1 quân làm ngòi
  return piecesBetween === 1;
}

/**
 * Sinh danh sách tất cả các nước đi hợp lệ cho quân Pháo tại vị trí 'from'.
 */
export function getCannonLegalMoves(
  board: ChessBoardState,
  from: Position
): Position[] {
  const piece = board.grid[from.col]?.[from.row];
  if (!piece || piece.type !== 'Cannon') {
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
    let hasPlatform = false;

    while (isValidPosition(c, r)) {
      const targetPiece = board.grid[c][r];

      if (!hasPlatform) {
        // Giai đoạn 1: Chưa gặp ngòi
        if (targetPiece === null) {
          legalMoves.push({ col: c, row: r });
        } else {
          // Gặp quân đầu tiên làm ngòi
          hasPlatform = true;
        }
      } else {
        // Giai đoạn 2: Đã có ngòi
        if (targetPiece !== null) {
          // Gặp quân thứ hai: nếu là quân đối phương thì ăn được
          if (targetPiece.color !== piece.color) {
            legalMoves.push({ col: c, row: r });
          }
          // Sau khi gặp quân thứ hai, dừng quét tia
          break;
        }
        // Ô trống sau ngòi bị bỏ qua
      }

      c += dc;
      r += dr;
    }
  }

  return legalMoves;
}
