// Seat keys follow the backend `SeatKey` format `r{row}c{column}s{seatIndex}` (docs/03), all 0-based.
const ROW_PREFIX = 'r';
const COLUMN_PREFIX = 'c';
const SEAT_PREFIX = 's';
const SEAT_KEY_PATTERN = /^r(\d+)c(\d+)s(\d+)$/;
const RADIX = 10;

export interface SeatPosition {
  row: number;
  column: number;
  seatIndex: number;
}

export const buildSeatKey = ({ row, column, seatIndex }: SeatPosition) => `${ROW_PREFIX}${row}${COLUMN_PREFIX}${column}${SEAT_PREFIX}${seatIndex}`;

export const parseSeatKey = (key: string): SeatPosition | null => {
  const match = SEAT_KEY_PATTERN.exec(key);
  if (!match) {
    return null;
  }

  const [, row, column, seatIndex] = match;
  return { row: parseInt(row, RADIX), column: parseInt(column, RADIX), seatIndex: parseInt(seatIndex, RADIX) };
};

/** Human-readable seat numbers are 1-based (`Ряд 1, Парта 1, Місце 1`). */
export const toDisplayNumber = (index: number) => index + 1;
