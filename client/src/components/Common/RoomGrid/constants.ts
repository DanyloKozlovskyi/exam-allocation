import { AdjacencyKind } from '../../../api/dtos';
import { SeatState } from '../../../enums/Rooms';

export const DEFAULT_SEAT_WIDTH_PX = 28;
export const SEAT_HEIGHT_PX = 28;
export const SEAT_GAP_PX = 4;
export const DESK_PADDING_PX = 4;
export const DESK_GAP_PX = 12;
export const AISLE_WIDTH_PX = 36;
export const ROW_GAP_PX = 16;
export const GRID_MARGIN_PX = 12;
export const FRONT_HEIGHT_PX = 28;
export const FRONT_GAP_PX = 16;
export const SEAT_CORNER_RADIUS_PX = 4;
export const DESK_CORNER_RADIUS_PX = 6;
export const SEAT_STROKE_WIDTH_PX = 1.5;
export const SELECTED_STROKE_WIDTH_PX = 3;
export const EDGE_STROKE_WIDTH_PX = 2;
export const EDGE_OPACITY = 0.7;
export const LABEL_FONT_SIZE_PX = 12;
export const GLYPH_FONT_SIZE_PX = 14;
export const HALF = 2;

export const DESK_FILL = '#E3E7EC';
export const FRONT_FILL = '#455A64';
export const FRONT_TEXT_FILL = '#FFFFFF';
export const AISLE_STROKE = '#B0BEC5';
export const AISLE_DASH = '6 4';
export const DISABLED_DASH = '3 3';
export const ACROSS_AISLE_DASH = '4 3';
/** Pictogram drawn inside accessible seats (not user-facing text). */
export const ACCESSIBLE_GLYPH = '♿';

export interface SeatStyle {
  fill: string;
  stroke: string;
  dash?: string;
  strokeWidth: number;
}

export const SEAT_STATE_STYLE: Record<SeatState, SeatStyle> = {
  [SeatState.Normal]: { fill: '#FFFFFF', stroke: '#78909C', strokeWidth: SEAT_STROKE_WIDTH_PX },
  [SeatState.Accessible]: { fill: '#E3F2FD', stroke: '#1976D2', strokeWidth: SEAT_STROKE_WIDTH_PX },
  [SeatState.Disabled]: { fill: '#ECEFF1', stroke: '#B0BEC5', dash: DISABLED_DASH, strokeWidth: SEAT_STROKE_WIDTH_PX },
  [SeatState.Occupied]: { fill: '#E8F5E9', stroke: '#388E3C', strokeWidth: SEAT_STROKE_WIDTH_PX },
  [SeatState.Violation]: { fill: '#FFEBEE', stroke: '#D32F2F', strokeWidth: SELECTED_STROKE_WIDTH_PX },
  [SeatState.Selected]: { fill: '#FFF8E1', stroke: '#F57C00', strokeWidth: SELECTED_STROKE_WIDTH_PX },
};

export const ADJACENCY_KIND_COLOR: Record<AdjacencyKind, string> = {
  [AdjacencyKind.SameDesk]: '#8E24AA',
  [AdjacencyKind.Side]: '#E53935',
  [AdjacencyKind.SideAcrossAisle]: '#FB8C00',
  [AdjacencyKind.FrontBack]: '#1E88E5',
  [AdjacencyKind.Diagonal]: '#43A047',
};

export enum KeyboardKey {
  Enter = 'Enter',
  Space = ' ',
}
