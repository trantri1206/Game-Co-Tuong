/**
 * Mô hình dữ liệu Bàn Cờ và Quân Cờ Cờ Tướng (GCT-42) cho Frontend Vue.
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

export interface ChessBoardState {
  grid: (Piece | null)[][]; // [9][10] hoặc [col][row]
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
