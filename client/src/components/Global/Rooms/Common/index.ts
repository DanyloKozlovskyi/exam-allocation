import type { RoomDto, RoomLayoutDto } from '../../../../api/dtos';
import { parseSeatKey } from '../../../Common/RoomGrid/seatKey';
import { SeatState } from '../../../../enums/Rooms';
import {
  ROOM_DEFAULT_COLUMNS,
  ROOM_DEFAULT_ROWS,
  ROOM_DEFAULT_SEATS_PER_DESK,
  ROOM_MAX_COLUMNS,
  ROOM_MAX_ROWS,
  ROOM_MAX_SEATS_PER_DESK,
  ROOM_MIN_COLUMNS,
  ROOM_MIN_ROWS,
  ROOM_MIN_SEATS_PER_DESK,
  ROOM_NAME_MAX_LENGTH,
} from './constants';

export interface RoomState {
  id: string | null;
  version: number | null;
  name: string;
  rows: number;
  columns: number;
  seatsPerDesk: number;
  aislesAfterColumns: number[];
  disabledSeats: string[];
  accessibleSeats: string[];
  costOverride: number | null;
  isActive: boolean;
}

export enum RoomField {
  Name = 'name',
  Rows = 'rows',
  Columns = 'columns',
  SeatsPerDesk = 'seatsPerDesk',
  CostOverride = 'costOverride',
  Layout = 'layout',
}

export enum RoomValidationError {
  NameRequired = 'nameRequired',
  NameTooLong = 'nameTooLong',
  RowsRange = 'rowsRange',
  CostNegative = 'costNegative',
  NoSeats = 'noSeats',
}

export interface FieldError {
  error: RoomValidationError;
  min?: number;
  max?: number;
}

export type ValidationErrors = Partial<Record<RoomField, FieldError>>;

export enum RoomReducerAction {
  Init = 'Init',
  Name = 'Name',
  Rows = 'Rows',
  Columns = 'Columns',
  SeatsPerDesk = 'SeatsPerDesk',
  Aisles = 'Aisles',
  CostOverride = 'CostOverride',
  IsActive = 'IsActive',
  CycleSeat = 'CycleSeat',
}

export type RoomAction =
  | { type: RoomReducerAction.Init; payload: RoomState }
  | { type: RoomReducerAction.Name; payload: string }
  | { type: RoomReducerAction.Rows | RoomReducerAction.Columns | RoomReducerAction.SeatsPerDesk; payload: number }
  | { type: RoomReducerAction.Aisles; payload: number[] }
  | { type: RoomReducerAction.CostOverride; payload: number | null }
  | { type: RoomReducerAction.IsActive; payload: boolean }
  | { type: RoomReducerAction.CycleSeat; payload: string };

export const initialRoomState: RoomState = {
  id: null,
  version: null,
  name: '',
  rows: ROOM_DEFAULT_ROWS,
  columns: ROOM_DEFAULT_COLUMNS,
  seatsPerDesk: ROOM_DEFAULT_SEATS_PER_DESK,
  aislesAfterColumns: [],
  disabledSeats: [],
  accessibleSeats: [],
  costOverride: null,
  isActive: true,
};

/** Seat cycle in the layout editor: Normal → Accessible → Disabled → Normal. */
export const SEAT_CYCLE: Record<SeatState, SeatState> = {
  [SeatState.Normal]: SeatState.Accessible,
  [SeatState.Accessible]: SeatState.Disabled,
  [SeatState.Disabled]: SeatState.Normal,
  [SeatState.Occupied]: SeatState.Occupied,
  [SeatState.Violation]: SeatState.Violation,
  [SeatState.Selected]: SeatState.Selected,
};

export const seatStateOf = (state: RoomState, key: string) => {
  if (state.disabledSeats.includes(key)) {
    return SeatState.Disabled;
  }
  return state.accessibleSeats.includes(key) ? SeatState.Accessible : SeatState.Normal;
};

const withinDimensions = (rows: number, columns: number, seatsPerDesk: number) => (key: string) => {
  const position = parseSeatKey(key);
  return position !== null && position.row < rows && position.column < columns && position.seatIndex < seatsPerDesk;
};

const prune = (state: RoomState): RoomState => {
  const keep = withinDimensions(state.rows, state.columns, state.seatsPerDesk);
  return {
    ...state,
    aislesAfterColumns: state.aislesAfterColumns.filter((column) => column < state.columns - 1),
    disabledSeats: state.disabledSeats.filter(keep),
    accessibleSeats: state.accessibleSeats.filter(keep),
  };
};

const cycleSeat = (state: RoomState, key: string): RoomState => {
  const next = SEAT_CYCLE[seatStateOf(state, key)];
  const without = (keys: string[]) => keys.filter((item) => item !== key);
  return {
    ...state,
    accessibleSeats: next === SeatState.Accessible ? [...without(state.accessibleSeats), key] : without(state.accessibleSeats),
    disabledSeats: next === SeatState.Disabled ? [...without(state.disabledSeats), key] : without(state.disabledSeats),
  };
};

export const roomReducer = (state: RoomState, action: RoomAction): RoomState => {
  switch (action.type) {
    case RoomReducerAction.Init:
      return action.payload;
    case RoomReducerAction.Name:
      return { ...state, name: action.payload };
    case RoomReducerAction.Rows:
      return prune({ ...state, rows: action.payload });
    case RoomReducerAction.Columns:
      return prune({ ...state, columns: action.payload });
    case RoomReducerAction.SeatsPerDesk:
      return prune({ ...state, seatsPerDesk: action.payload });
    case RoomReducerAction.Aisles:
      return prune({ ...state, aislesAfterColumns: [...action.payload].sort((a, b) => a - b) });
    case RoomReducerAction.CostOverride:
      return { ...state, costOverride: action.payload };
    case RoomReducerAction.IsActive:
      return { ...state, isActive: action.payload };
    case RoomReducerAction.CycleSeat:
      return cycleSeat(state, action.payload);
    default:
      return state;
  }
};

export const toLayoutDto = (state: RoomState): RoomLayoutDto => ({
  rows: state.rows,
  columns: state.columns,
  seatsPerDesk: state.seatsPerDesk,
  aislesAfterColumns: state.aislesAfterColumns,
  disabledSeats: state.disabledSeats,
  accessibleSeats: state.accessibleSeats,
});

export const toRoomDto = (state: RoomState, centerId: string): RoomDto => ({
  id: state.id,
  centerId,
  name: state.name.trim(),
  layout: toLayoutDto(state),
  costOverride: state.costOverride,
  isActive: state.isActive,
  version: state.version,
});

export const fromRoomDto = (room: RoomDto): RoomState =>
  prune({
    id: room.id ?? null,
    version: room.version ?? null,
    name: room.name ?? '',
    rows: room.layout?.rows ?? ROOM_DEFAULT_ROWS,
    columns: room.layout?.columns ?? ROOM_DEFAULT_COLUMNS,
    seatsPerDesk: room.layout?.seatsPerDesk ?? ROOM_DEFAULT_SEATS_PER_DESK,
    aislesAfterColumns: room.layout?.aislesAfterColumns ?? [],
    disabledSeats: room.layout?.disabledSeats ?? [],
    accessibleSeats: room.layout?.accessibleSeats ?? [],
    costOverride: room.costOverride ?? null,
    isActive: room.isActive ?? true,
  });

export const enabledSeatCount = (state: RoomState) => state.rows * state.columns * state.seatsPerDesk - state.disabledSeats.length;

const range = (value: number, min: number, max: number): FieldError | undefined =>
  Number.isInteger(value) && value >= min && value <= max ? undefined : { error: RoomValidationError.RowsRange, min, max };

export const validateDetails = (state: RoomState): ValidationErrors => {
  const errors: ValidationErrors = {};
  const name = state.name.trim();
  if (!name) {
    errors[RoomField.Name] = { error: RoomValidationError.NameRequired };
  } else if (name.length > ROOM_NAME_MAX_LENGTH) {
    errors[RoomField.Name] = { error: RoomValidationError.NameTooLong, max: ROOM_NAME_MAX_LENGTH };
  }

  const rows = range(state.rows, ROOM_MIN_ROWS, ROOM_MAX_ROWS);
  const columns = range(state.columns, ROOM_MIN_COLUMNS, ROOM_MAX_COLUMNS);
  const seats = range(state.seatsPerDesk, ROOM_MIN_SEATS_PER_DESK, ROOM_MAX_SEATS_PER_DESK);
  if (rows) errors[RoomField.Rows] = rows;
  if (columns) errors[RoomField.Columns] = columns;
  if (seats) errors[RoomField.SeatsPerDesk] = seats;
  if (state.costOverride !== null && state.costOverride < 0) {
    errors[RoomField.CostOverride] = { error: RoomValidationError.CostNegative };
  }

  return errors;
};

export const validateLayout = (state: RoomState): ValidationErrors => (enabledSeatCount(state) > 0 ? {} : { [RoomField.Layout]: { error: RoomValidationError.NoSeats } });

export const hasErrors = (errors: ValidationErrors) => Object.keys(errors).length > 0;
