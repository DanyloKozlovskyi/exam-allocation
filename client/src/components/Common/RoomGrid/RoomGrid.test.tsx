import { fireEvent, render, screen } from '@testing-library/react';
import { AdjacencyKind, type RoomLayoutDto } from '../../../api/dtos';
import { RoomGridTestId, SeatState } from '../../../enums/Rooms';
import RoomGrid from '.';
import { toAdjacencyOverlays } from './adjacency';
import { buildSeatKey, parseSeatKey } from './seatKey';

const layout: RoomLayoutDto = {
  rows: 2,
  columns: 3,
  seatsPerDesk: 2,
  aislesAfterColumns: [0],
  disabledSeats: ['r1c2s1'],
  accessibleSeats: ['r0c0s0'],
};

const seats = (container: HTMLElement) => Array.from(container.querySelectorAll(`[data-qa="${RoomGridTestId.Seat}"]`));

describe('RoomGrid', () => {
  it('renders every desk, seat and aisle of the layout', () => {
    const { container } = render(<RoomGrid layout={layout} />);

    expect(seats(container)).toHaveLength(12);
    expect(container.querySelectorAll(`[data-qa="${RoomGridTestId.Desk}"]`)).toHaveLength(6);
    expect(container.querySelectorAll(`[data-qa="${RoomGridTestId.Aisle}"]`)).toHaveLength(1);
  });

  it('derives default seat states from the layout and exposes aria labels', () => {
    const { container } = render(<RoomGrid layout={layout} />);

    expect(container.querySelector('[data-seat-key="r1c2s1"]')).toHaveAttribute('data-state', SeatState.Disabled);
    expect(container.querySelector('[data-seat-key="r0c0s0"]')).toHaveAttribute('data-state', SeatState.Accessible);
    expect(container.querySelector('[data-seat-key="r0c1s0"]')).toHaveAttribute('data-state', SeatState.Normal);
    expect(screen.getByLabelText('Ряд 1, Парта 1, Місце 1 — для осіб з інвалідністю')).toBeInTheDocument();
  });

  it('calls onSeatClick on click and keyboard activation', () => {
    const onSeatClick = jest.fn();
    render(<RoomGrid layout={layout} onSeatClick={onSeatClick} />);

    const seat = screen.getByRole('button', { name: /Ряд 2, Парта 3, Місце 2/ });
    fireEvent.click(seat);
    fireEvent.keyDown(seat, { key: 'Enter' });

    expect(onSeatClick).toHaveBeenCalledTimes(2);
    expect(onSeatClick.mock.calls[0][0]).toMatchObject({ key: 'r1c2s1', row: 1, column: 2, seatIndex: 1, isDisabled: true });
  });

  it('uses custom seat state and render prop', () => {
    const { container } = render(
      <RoomGrid
        layout={layout}
        getSeatState={(seat) => (seat.key === 'r0c1s1' ? SeatState.Occupied : SeatState.Normal)}
        renderSeat={(seat) => (seat.key === 'r0c1s1' ? <text>Шевченко</text> : null)}
      />
    );

    expect(container.querySelector('[data-seat-key="r0c1s1"]')).toHaveAttribute('data-state', SeatState.Occupied);
    expect(screen.getByText('Шевченко')).toBeInTheDocument();
  });

  it('draws adjacency overlay edges per kind', () => {
    const { container } = render(
      <RoomGrid
        layout={layout}
        adjacency={[
          { kind: AdjacencyKind.SameDesk, pairs: [{ a: 'r0c0s0', b: 'r0c0s1' }] },
          {
            kind: AdjacencyKind.FrontBack,
            pairs: [
              { a: 'r0c1s0', b: 'r1c1s0' },
              { a: 'missing', b: 'r1c1s0' },
            ],
          },
        ]}
      />
    );

    const edges = container.querySelectorAll(`[data-qa="${RoomGridTestId.AdjacencyEdge}"]`);
    expect(edges).toHaveLength(2);
    expect(edges[1]).toHaveAttribute('data-kind', AdjacencyKind.FrontBack);
  });
});

describe('seat keys and overlays', () => {
  it('builds and parses seat keys', () => {
    expect(buildSeatKey({ row: 3, column: 1, seatIndex: 0 })).toBe('r3c1s0');
    expect(parseSeatKey('r3c1s0')).toEqual({ row: 3, column: 1, seatIndex: 0 });
    expect(parseSeatKey('bad')).toBeNull();
  });

  it('converts preview edges by seat index and filters kinds', () => {
    const overlays = toAdjacencyOverlays(
      {
        seats: [{ seatKey: 'r0c0s0' }, { seatKey: 'r0c0s1' }, { seatKey: 'r1c0s0' }],
        adjacency: [
          { kind: AdjacencyKind.SameDesk, edges: [{ a: 0, b: 1 }] },
          { kind: AdjacencyKind.FrontBack, edges: [{ a: 0, b: 2 }] },
        ],
      },
      [AdjacencyKind.FrontBack]
    );

    expect(overlays).toEqual([{ kind: AdjacencyKind.FrontBack, pairs: [{ a: 'r0c0s0', b: 'r1c0s0' }] }]);
  });
});
