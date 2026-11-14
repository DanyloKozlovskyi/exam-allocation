import type { RoomLayoutDto } from '../../../api/dtos';
import { SeatState } from '../../../enums/Rooms';
import { DESK_GAP_PX, DESK_PADDING_PX, AISLE_WIDTH_PX, FRONT_GAP_PX, FRONT_HEIGHT_PX, GRID_MARGIN_PX, HALF, ROW_GAP_PX, SEAT_GAP_PX, SEAT_HEIGHT_PX } from './constants';
import { buildSeatKey } from './seatKey';

export interface Box {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface GridSeat {
  key: string;
  row: number;
  column: number;
  seatIndex: number;
  isAccessible: boolean;
  isDisabled: boolean;
}

export interface PositionedSeat extends GridSeat {
  box: Box;
}

export interface PositionedDesk {
  row: number;
  column: number;
  box: Box;
}

export interface RoomGeometry {
  width: number;
  height: number;
  front: Box;
  desks: PositionedDesk[];
  seats: PositionedSeat[];
  aisles: Box[];
}

const range = (count: number) => Array.from({ length: Math.max(count, 0) }, (_, index) => index);

export const getLayoutSeats = (layout: RoomLayoutDto): GridSeat[] => {
  const disabled = new Set(layout.disabledSeats ?? []);
  const accessible = new Set(layout.accessibleSeats ?? []);
  return range(layout.rows ?? 0).flatMap((row) =>
    range(layout.columns ?? 0).flatMap((column) =>
      range(layout.seatsPerDesk ?? 0).map((seatIndex) => {
        const key = buildSeatKey({ row, column, seatIndex });
        return { key, row, column, seatIndex, isAccessible: accessible.has(key), isDisabled: disabled.has(key) };
      })
    )
  );
};

export const computeGeometry = (layout: RoomLayoutDto, seatWidth: number): RoomGeometry => {
  const rows = layout.rows ?? 0;
  const columns = layout.columns ?? 0;
  const seatsPerDesk = layout.seatsPerDesk ?? 0;
  const aisles = new Set((layout.aislesAfterColumns ?? []).filter((column) => column >= 0 && column < columns - 1));

  const deskWidth = seatsPerDesk * seatWidth + Math.max(seatsPerDesk - 1, 0) * SEAT_GAP_PX + HALF * DESK_PADDING_PX;
  const deskHeight = SEAT_HEIGHT_PX + HALF * DESK_PADDING_PX;
  const top = GRID_MARGIN_PX + FRONT_HEIGHT_PX + FRONT_GAP_PX;

  const columnX: number[] = [];
  let x = GRID_MARGIN_PX;
  range(columns).forEach((column) => {
    columnX.push(x);
    x += deskWidth + (aisles.has(column) ? AISLE_WIDTH_PX : DESK_GAP_PX);
  });

  const contentRight = columns > 0 ? columnX[columns - 1] + deskWidth : GRID_MARGIN_PX;
  const width = contentRight + GRID_MARGIN_PX;
  const rowY = (row: number) => top + row * (deskHeight + ROW_GAP_PX);
  const contentBottom = rows > 0 ? rowY(rows - 1) + deskHeight : top;
  const height = contentBottom + GRID_MARGIN_PX;

  const desks: PositionedDesk[] = range(rows).flatMap((row) =>
    range(columns).map((column) => ({ row, column, box: { x: columnX[column], y: rowY(row), width: deskWidth, height: deskHeight } }))
  );

  const seats: PositionedSeat[] = getLayoutSeats(layout).map((seat) => ({
    ...seat,
    box: {
      x: columnX[seat.column] + DESK_PADDING_PX + seat.seatIndex * (seatWidth + SEAT_GAP_PX),
      y: rowY(seat.row) + DESK_PADDING_PX,
      width: seatWidth,
      height: SEAT_HEIGHT_PX,
    },
  }));

  const aisleBoxes: Box[] = [...aisles].sort((a, b) => a - b).map((column) => ({ x: columnX[column] + deskWidth, y: top, width: AISLE_WIDTH_PX, height: contentBottom - top }));

  return {
    width,
    height,
    front: { x: GRID_MARGIN_PX, y: GRID_MARGIN_PX, width: contentRight - GRID_MARGIN_PX, height: FRONT_HEIGHT_PX },
    desks,
    seats,
    aisles: aisleBoxes,
  };
};

export const boxCenter = (box: Box) => ({ x: box.x + box.width / HALF, y: box.y + box.height / HALF });

export const defaultSeatState = (seat: GridSeat) => {
  if (seat.isDisabled) {
    return SeatState.Disabled;
  }

  return seat.isAccessible ? SeatState.Accessible : SeatState.Normal;
};
