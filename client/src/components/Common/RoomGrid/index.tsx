import { Tooltip } from '@mui/material';
import { useMemo, type KeyboardEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AdjacencyKind, type RoomLayoutDto } from '../../../api/dtos';
import { RoomGridTestId, SeatState } from '../../../enums/Rooms';
import { I18nNamespace } from '../../../i18n';
import type { AdjacencyOverlay } from './adjacency';
import {
  ACCESSIBLE_GLYPH,
  ACROSS_AISLE_DASH,
  ADJACENCY_KIND_COLOR,
  AISLE_DASH,
  AISLE_STROKE,
  DEFAULT_SEAT_WIDTH_PX,
  DESK_CORNER_RADIUS_PX,
  DESK_FILL,
  EDGE_OPACITY,
  EDGE_STROKE_WIDTH_PX,
  FRONT_FILL,
  FRONT_TEXT_FILL,
  GLYPH_FONT_SIZE_PX,
  HALF,
  KeyboardKey,
  LABEL_FONT_SIZE_PX,
  SEAT_CORNER_RADIUS_PX,
  SEAT_STATE_STYLE,
} from './constants';
import { boxCenter, computeGeometry, defaultSeatState, type Box, type GridSeat, type PositionedSeat } from './geometry';
import { toDisplayNumber } from './seatKey';

export interface RoomGridProps {
  layout: RoomLayoutDto;
  /** Seat state; defaults to Disabled / Accessible / Normal from the layout. */
  getSeatState?: (seat: GridSeat) => SeatState;
  /** Extra SVG content drawn inside the seat box (e.g. surname, group colour). */
  renderSeat?: (seat: GridSeat, box: Box, state: SeatState) => ReactNode;
  /** Accessible name of a seat; defaults to "Ряд N, Парта N, Місце N" + state. */
  getSeatLabel?: (seat: GridSeat, state: SeatState) => string;
  getSeatTooltip?: (seat: GridSeat) => ReactNode;
  onSeatClick?: (seat: GridSeat) => void;
  adjacency?: AdjacencyOverlay[];
  seatWidth?: number;
  title?: string;
  testId?: string;
}

const RoomGrid = ({
  layout,
  getSeatState = defaultSeatState,
  renderSeat,
  getSeatLabel,
  getSeatTooltip,
  onSeatClick,
  adjacency = [],
  seatWidth = DEFAULT_SEAT_WIDTH_PX,
  title,
  testId = RoomGridTestId.Grid,
}: RoomGridProps) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const geometry = useMemo(() => computeGeometry(layout, seatWidth), [layout, seatWidth]);
  const seatByKey = useMemo(() => new Map(geometry.seats.map((seat) => [seat.key, seat])), [geometry]);

  const labelFor = (seat: GridSeat, state: SeatState) =>
    getSeatLabel?.(seat, state) ??
    t('grid.seatLabel', {
      row: toDisplayNumber(seat.row),
      column: toDisplayNumber(seat.column),
      seat: toDisplayNumber(seat.seatIndex),
      state: t(`seatState.${state}`),
    });

  const handleKeyDown = (seat: GridSeat) => (event: KeyboardEvent<SVGGElement>) => {
    if (event.key === KeyboardKey.Enter || event.key === KeyboardKey.Space) {
      event.preventDefault();
      onSeatClick?.(seat);
    }
  };

  const renderSeatElement = (seat: PositionedSeat) => {
    const state = getSeatState(seat);
    const style = SEAT_STATE_STYLE[state];
    const center = boxCenter(seat.box);
    const element = (
      <g
        key={seat.key}
        data-qa={RoomGridTestId.Seat}
        data-seat-key={seat.key}
        data-state={state}
        role={onSeatClick ? 'button' : 'img'}
        aria-label={labelFor(seat, state)}
        aria-pressed={onSeatClick ? state === SeatState.Selected : undefined}
        tabIndex={onSeatClick ? 0 : undefined}
        onClick={onSeatClick ? () => onSeatClick(seat) : undefined}
        onKeyDown={onSeatClick ? handleKeyDown(seat) : undefined}
        style={{ cursor: onSeatClick ? 'pointer' : 'default' }}>
        <rect
          x={seat.box.x}
          y={seat.box.y}
          width={seat.box.width}
          height={seat.box.height}
          rx={SEAT_CORNER_RADIUS_PX}
          fill={style.fill}
          stroke={style.stroke}
          strokeWidth={style.strokeWidth}
          strokeDasharray={style.dash}
        />
        {seat.isAccessible && !renderSeat && (
          <text x={center.x} y={center.y} textAnchor="middle" dominantBaseline="central" fontSize={GLYPH_FONT_SIZE_PX} fill={style.stroke} aria-hidden>
            {ACCESSIBLE_GLYPH}
          </text>
        )}
        {renderSeat?.(seat, seat.box, state)}
      </g>
    );

    const tooltip = getSeatTooltip?.(seat);
    return tooltip ? (
      <Tooltip key={seat.key} title={tooltip} arrow>
        {element}
      </Tooltip>
    ) : (
      element
    );
  };

  return (
    <svg
      data-qa={testId}
      role="group"
      aria-label={title ?? t('grid.title')}
      width={geometry.width}
      height={geometry.height}
      viewBox={`0 0 ${geometry.width} ${geometry.height}`}
      style={{ maxWidth: '100%', height: 'auto' }}>
      <rect x={geometry.front.x} y={geometry.front.y} width={geometry.front.width} height={geometry.front.height} rx={DESK_CORNER_RADIUS_PX} fill={FRONT_FILL} />
      <text
        x={geometry.front.x + geometry.front.width / HALF}
        y={geometry.front.y + geometry.front.height / HALF}
        textAnchor="middle"
        dominantBaseline="central"
        fill={FRONT_TEXT_FILL}
        fontSize={LABEL_FONT_SIZE_PX}>
        {t('grid.front')}
      </text>
      {geometry.aisles.map((aisle) => (
        <line
          key={aisle.x}
          data-qa={RoomGridTestId.Aisle}
          x1={aisle.x + aisle.width / HALF}
          x2={aisle.x + aisle.width / HALF}
          y1={aisle.y}
          y2={aisle.y + aisle.height}
          stroke={AISLE_STROKE}
          strokeDasharray={AISLE_DASH}
          aria-label={t('grid.aisle')}
        />
      ))}
      {geometry.desks.map((desk) => (
        <rect
          key={`${desk.row}-${desk.column}`}
          data-qa={RoomGridTestId.Desk}
          x={desk.box.x}
          y={desk.box.y}
          width={desk.box.width}
          height={desk.box.height}
          rx={DESK_CORNER_RADIUS_PX}
          fill={DESK_FILL}
        />
      ))}
      {geometry.seats.map(renderSeatElement)}
      <g aria-hidden pointerEvents="none">
        {adjacency.flatMap((overlay) =>
          overlay.pairs.flatMap((pair) => {
            const a = seatByKey.get(pair.a);
            const b = seatByKey.get(pair.b);
            if (!a || !b) {
              return [];
            }

            const from = boxCenter(a.box);
            const to = boxCenter(b.box);
            return [
              <line
                key={`${overlay.kind}-${pair.a}-${pair.b}`}
                data-qa={RoomGridTestId.AdjacencyEdge}
                data-kind={overlay.kind}
                x1={from.x}
                y1={from.y}
                x2={to.x}
                y2={to.y}
                stroke={ADJACENCY_KIND_COLOR[overlay.kind]}
                strokeWidth={EDGE_STROKE_WIDTH_PX}
                strokeOpacity={EDGE_OPACITY}
                strokeDasharray={overlay.kind === AdjacencyKind.SideAcrossAisle ? ACROSS_AISLE_DASH : undefined}
              />,
            ];
          })
        )}
      </g>
    </svg>
  );
};

export default RoomGrid;
