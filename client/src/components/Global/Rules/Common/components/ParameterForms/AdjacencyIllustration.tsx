import { AdjacencyKind } from '../../../../../../api/dtos';
import { ADJACENCY_KIND_COLOR } from '../../../../../Common/RoomGrid/constants';

interface Props {
  kind: AdjacencyKind;
}

// Tiny 2×2 desk sketch (two seats per desk) highlighting the seat pair a kind connects.
const CELL_PX = 10;
const GAP_PX = 2;
const DESK_GAP_PX = 6;
const AISLE_EXTRA_PX = 6;
const SIZE_PX = 56;
const STROKE_WIDTH_PX = 2;
const INACTIVE_FILL = '#CFD8DC';
const SEATS_PER_DESK = 2;
const ROWS = 2;
const COLUMNS = 2;

interface Cell {
  row: number;
  column: number;
  seat: number;
}

const PAIRS: Record<AdjacencyKind, [Cell, Cell]> = {
  [AdjacencyKind.SameDesk]: [
    { row: 0, column: 0, seat: 0 },
    { row: 0, column: 0, seat: 1 },
  ],
  [AdjacencyKind.Side]: [
    { row: 0, column: 0, seat: 1 },
    { row: 0, column: 1, seat: 0 },
  ],
  [AdjacencyKind.SideAcrossAisle]: [
    { row: 0, column: 0, seat: 1 },
    { row: 0, column: 1, seat: 0 },
  ],
  [AdjacencyKind.FrontBack]: [
    { row: 0, column: 0, seat: 0 },
    { row: 1, column: 0, seat: 0 },
  ],
  [AdjacencyKind.Diagonal]: [
    { row: 0, column: 0, seat: 1 },
    { row: 1, column: 1, seat: 0 },
  ],
};

const AdjacencyIllustration = ({ kind }: Props) => {
  const aisle = kind === AdjacencyKind.SideAcrossAisle ? AISLE_EXTRA_PX : 0;
  const x = (cell: Cell) => GAP_PX + cell.column * (SEATS_PER_DESK * (CELL_PX + GAP_PX) + DESK_GAP_PX + aisle) + cell.seat * (CELL_PX + GAP_PX);
  const y = (cell: Cell) => GAP_PX + cell.row * (CELL_PX + DESK_GAP_PX);
  const highlighted = PAIRS[kind];
  const isHighlighted = (cell: Cell) => highlighted.some((item) => item.row === cell.row && item.column === cell.column && item.seat === cell.seat);
  const cells: Cell[] = [];
  for (let row = 0; row < ROWS; row++) {
    for (let column = 0; column < COLUMNS; column++) {
      for (let seat = 0; seat < SEATS_PER_DESK; seat++) {
        cells.push({ row, column, seat });
      }
    }
  }
  const [a, b] = highlighted;
  const half = CELL_PX / SEATS_PER_DESK;

  return (
    <svg width={SIZE_PX} height={SIZE_PX / SEATS_PER_DESK} viewBox={`0 0 ${SIZE_PX} ${SIZE_PX / SEATS_PER_DESK}`} aria-hidden>
      {cells.map((cell) => (
        <rect
          key={`${cell.row}${cell.column}${cell.seat}`}
          x={x(cell)}
          y={y(cell)}
          width={CELL_PX}
          height={CELL_PX}
          fill={isHighlighted(cell) ? ADJACENCY_KIND_COLOR[kind] : INACTIVE_FILL}
        />
      ))}
      <line x1={x(a) + half} y1={y(a) + half} x2={x(b) + half} y2={y(b) + half} stroke={ADJACENCY_KIND_COLOR[kind]} strokeWidth={STROKE_WIDTH_PX} />
    </svg>
  );
};

export default AdjacencyIllustration;
