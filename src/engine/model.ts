/**
 * Mô hình dữ liệu Bàn Cờ, Quân Cờ và Mô Phỏng Nước Đi Cờ Tướng (GCT-42 & GCT-43) cho Frontend Vue.
 * Đồng bộ chuẩn 100% với Engine C# Backend.
 */

export type PieceColor = 'Red' | 'Black';

export type PieceType =
  | 'General'  // Tướng / Soái
  | 'Advisor'  // Sĩ
  | 'Elephant' // Tượng
  | 'Horse'    // Mã
  | 'Chariot'  // Xe
  | 'Cannon'   // Pháo
  | 'Soldier'; // Tốt / Binh

export interface Position {
  col: number; // 0..8
  row: number; // 0..9 (Đen ở trên: 0..4, Đỏ ở dưới: 5..9)
}

export interface Piece {
  type: PieceType;
  color: PieceColor;
  hanzi: string;
  name: string;
}

export interface MoveAction {
  from: Position;
  to: Position;
}

export interface UndoMoveRecord {
  from: Position;
  to: Position;
  movedPiece: Piece;
  capturedPiece: Piece | null;
  previousActiveColor: PieceColor;
  previousHalfMoveClock: number;
  previousFullMoveNumber: number;
  previousStateVersion: number;
}

export interface ChessBoardState {
  grid: (Piece | null)[][]; // grid[col][row] với col: 0..8, row: 0..9
  activeColor: PieceColor;
  stateVersion: number;
  halfMoveClock: number;
  fullMoveNumber: number;
}

export const BOARD_COLS = 9;
export const BOARD_ROWS = 10;
export const TOTAL_INTERSECTIONS = 90;

export function isValidPosition(col: number, row: number): boolean {
  return col >= 0 && col < BOARD_COLS && row >= 0 && row < BOARD_ROWS;
}

export function isRedPalace(col: number, row: number): boolean {
  return col >= 3 && col <= 5 && row >= 7 && row <= 9;
}

export function isBlackPalace(col: number, row: number): boolean {
  return col >= 3 && col <= 5 && row >= 0 && row <= 2;
}

export function createPiece(type: PieceType, color: PieceColor): Piece {
  return {
    type,
    color,
    hanzi: getPieceHanzi(type, color),
    name: getPieceName(type, color)
  };
}

export function getPieceHanzi(type: PieceType, color: PieceColor): string {
  switch (type) {
    case 'General': return color === 'Red' ? '帥' : '將';
    case 'Advisor': return color === 'Red' ? '仕' : '士';
    case 'Elephant': return color === 'Red' ? '相' : '象';
    case 'Horse': return color === 'Red' ? '傌' : '馬';
    case 'Chariot': return color === 'Red' ? '俥' : '車';
    case 'Cannon': return color === 'Red' ? '炮' : '砲';
    case 'Soldier': return color === 'Red' ? '兵' : '卒';
  }
}

export function getPieceName(type: PieceType, color: PieceColor): string {
  switch (type) {
    case 'General': return color === 'Red' ? 'Soái Đỏ' : 'Tướng Đen';
    case 'Advisor': return color === 'Red' ? 'Sĩ Đỏ' : 'Sĩ Đen';
    case 'Elephant': return color === 'Red' ? 'Tượng Đỏ' : 'Tượng Đen';
    case 'Horse': return color === 'Red' ? 'Mã Đỏ' : 'Mã Đen';
    case 'Chariot': return color === 'Red' ? 'Xe Đỏ' : 'Xe Đen';
    case 'Cannon': return color === 'Red' ? 'Pháo Đỏ' : 'Pháo Đen';
    case 'Soldier': return color === 'Red' ? 'Binh Đỏ' : 'Tốt Đen';
  }
}

/**
 * Khởi tạo bàn cờ với 32 quân ở vị trí xuất phát chuẩn Cờ Tướng.
 */
export function createInitialBoard(): ChessBoardState {
  const grid: (Piece | null)[][] = Array.from({ length: BOARD_COLS }, () =>
    Array.from({ length: BOARD_ROWS }, () => null)
  );

  function set(c: number, r: number, t: PieceType, col: PieceColor) {
    grid[c][r] = createPiece(t, col);
  }

  // Đen (Black) - Hàng 0 đến 4
  set(0, 0, 'Chariot', 'Black');
  set(8, 0, 'Chariot', 'Black');
  set(1, 0, 'Horse', 'Black');
  set(7, 0, 'Horse', 'Black');
  set(2, 0, 'Elephant', 'Black');
  set(6, 0, 'Elephant', 'Black');
  set(3, 0, 'Advisor', 'Black');
  set(5, 0, 'Advisor', 'Black');
  set(4, 0, 'General', 'Black');
  set(1, 2, 'Cannon', 'Black');
  set(7, 2, 'Cannon', 'Black');
  for (let c = 0; c <= 8; c += 2) {
    set(c, 3, 'Soldier', 'Black');
  }

  // Đỏ (Red) - Hàng 5 đến 9
  set(0, 9, 'Chariot', 'Red');
  set(8, 9, 'Chariot', 'Red');
  set(1, 9, 'Horse', 'Red');
  set(7, 9, 'Horse', 'Red');
  set(2, 9, 'Elephant', 'Red');
  set(6, 9, 'Elephant', 'Red');
  set(3, 9, 'Advisor', 'Red');
  set(5, 9, 'Advisor', 'Red');
  set(4, 9, 'General', 'Red');
  set(1, 7, 'Cannon', 'Red');
  set(7, 7, 'Cannon', 'Red');
  for (let c = 0; c <= 8; c += 2) {
    set(c, 6, 'Soldier', 'Red');
  }

  return {
    grid,
    activeColor: 'Red',
    stateVersion: 1,
    halfMoveClock: 0,
    fullMoveNumber: 1
  };
}

/**
 * Tạo bản sao sâu (deep clone) độc lập của bàn cờ.
 */
export function cloneBoard(board: ChessBoardState): ChessBoardState {
  const newGrid: (Piece | null)[][] = Array.from({ length: BOARD_COLS }, () =>
    Array.from({ length: BOARD_ROWS }, () => null)
  );

  for (let c = 0; c < BOARD_COLS; c++) {
    for (let r = 0; r < BOARD_ROWS; r++) {
      const p = board.grid[c][r];
      newGrid[c][r] = p ? { ...p } : null;
    }
  }

  return {
    grid: newGrid,
    activeColor: board.activeColor,
    stateVersion: board.stateVersion,
    halfMoveClock: board.halfMoveClock,
    fullMoveNumber: board.fullMoveNumber
  };
}

/**
 * Thực hiện nước đi trên bàn cờ và trả về bản ghi UndoMoveRecord.
 */
export function makeMove(board: ChessBoardState, move: MoveAction): UndoMoveRecord {
  const { from, to } = move;

  if (from.col === to.col && from.row === to.row) {
    throw new Error('Ô xuất phát và ô đích không được trùng nhau.');
  }

  const piece = board.grid[from.col][from.row];
  if (!piece) {
    throw new Error(`Không có quân cờ nào tại ô (${from.col}, ${from.row}).`);
  }

  if (piece.color !== board.activeColor) {
    throw new Error(`Chưa đến lượt đi của phe ${piece.color}. Lượt hiện tại: ${board.activeColor}.`);
  }

  const targetPiece = board.grid[to.col][to.row];
  if (targetPiece && targetPiece.color === piece.color) {
    throw new Error('Không thể ăn quân đồng minh.');
  }

  const undoRecord: UndoMoveRecord = {
    from,
    to,
    movedPiece: { ...piece },
    capturedPiece: targetPiece ? { ...targetPiece } : null,
    previousActiveColor: board.activeColor,
    previousHalfMoveClock: board.halfMoveClock,
    previousFullMoveNumber: board.fullMoveNumber,
    previousStateVersion: board.stateVersion
  };

  board.grid[to.col][to.row] = piece;
  board.grid[from.col][from.row] = null;

  if (targetPiece || piece.type === 'Soldier') {
    board.halfMoveClock = 0;
  } else {
    board.halfMoveClock++;
  }

  if (board.activeColor === 'Black') {
    board.fullMoveNumber++;
    board.activeColor = 'Red';
  } else {
    board.activeColor = 'Black';
  }

  board.stateVersion++;
  return undoRecord;
}

/**
 * Hoàn tác nước đi từ bản ghi UndoMoveRecord.
 */
export function undoMove(board: ChessBoardState, undoRecord: UndoMoveRecord): void {
  board.grid[undoRecord.from.col][undoRecord.from.row] = undoRecord.movedPiece;
  board.grid[undoRecord.to.col][undoRecord.to.row] = undoRecord.capturedPiece;
  board.activeColor = undoRecord.previousActiveColor;
  board.halfMoveClock = undoRecord.previousHalfMoveClock;
  board.fullMoveNumber = undoRecord.previousFullMoveNumber;
  board.stateVersion = undoRecord.previousStateVersion;
}

/**
 * Mô phỏng an toàn một nước đi trên bản sao, đảm bảo không thay đổi bàn cờ gốc.
 */
export function simulateMove<T>(
  board: ChessBoardState,
  move: MoveAction,
  callback: (simulatedBoard: ChessBoardState) => T
): T {
  const sandbox = cloneBoard(board);
  makeMove(sandbox, move);
  return callback(sandbox);
}

export * from './rules/soldier';
export * from './rules/chariot';
export * from './rules/horse';
export * from './rules/cannon';
export * from './rules/elephant';
export * from './rules/advisor';


