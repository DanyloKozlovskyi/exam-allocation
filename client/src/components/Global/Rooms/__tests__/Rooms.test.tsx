import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RoutePath, routes } from '../../../../constants/routes';
import { RoomsTestId, SeatState } from '../../../../enums/Rooms';
import { getMockRooms, MOCK_CENTERS, resetRoomsMocks } from '../../../../mocks/handlers/rooms';
import RoomsCreateView from '../../../../views/Rooms/Create';
import RoomsEditView from '../../../../views/Rooms/Edit';
import RoomsListView from '../../../../views/Rooms/List';
import { byQa, LOCATION_TEST_ID, renderWithProviders } from '../../../../test-utils';
import { fromRoomDto, roomReducer, RoomReducerAction, initialRoomState, validateDetails, RoomField, RoomValidationError } from '../Common';

const SESSION_ID = 's1';
const CENTER_ID = MOCK_CENTERS[0].id;

const qaInput = (container: HTMLElement, qa: RoomsTestId) => byQa(container, qa) as HTMLInputElement;

beforeEach(() => resetRoomsMocks());

describe('room reducer', () => {
  it('cycles a seat Normal → Accessible → Disabled → Normal', () => {
    const key = 'r0c0s0';
    const s1 = roomReducer(initialRoomState, { type: RoomReducerAction.CycleSeat, payload: key });
    expect(s1.accessibleSeats).toEqual([key]);
    const s2 = roomReducer(s1, { type: RoomReducerAction.CycleSeat, payload: key });
    expect(s2.accessibleSeats).toEqual([]);
    expect(s2.disabledSeats).toEqual([key]);
    const s3 = roomReducer(s2, { type: RoomReducerAction.CycleSeat, payload: key });
    expect(s3.disabledSeats).toEqual([]);
  });

  it('prunes seats and aisles outside the new dimensions', () => {
    const state = fromRoomDto({ name: 'A', layout: { rows: 3, columns: 3, seatsPerDesk: 2, aislesAfterColumns: [1], disabledSeats: ['r2c2s1'], accessibleSeats: ['r0c0s0'] } });
    const next = roomReducer(state, { type: RoomReducerAction.Columns, payload: 2 });
    expect(next.disabledSeats).toEqual([]);
    expect(next.aislesAfterColumns).toEqual([]);
    expect(next.accessibleSeats).toEqual(['r0c0s0']);
  });

  it('validates details', () => {
    const errors = validateDetails({ ...initialRoomState, rows: 0, costOverride: -1 });
    expect(errors[RoomField.Name]?.error).toBe(RoomValidationError.NameRequired);
    expect(errors[RoomField.Rows]?.error).toBe(RoomValidationError.RowsRange);
    expect(errors[RoomField.CostOverride]?.error).toBe(RoomValidationError.CostNegative);
  });
});

describe('Rooms list', () => {
  it('shows rooms of the center and deletes one after confirmation', async () => {
    const { container } = renderWithProviders(<RoomsListView />, { route: routes.rooms.list(SESSION_ID, CENTER_ID), path: RoutePath.Rooms });

    expect(await screen.findByText('Ауд. 214')).toBeInTheDocument();
    expect(screen.getByText('Ауд. 101')).toBeInTheDocument();

    const rows = container.querySelectorAll(`[data-qa="${RoomsTestId.DeleteButton}"]`);
    fireEvent.click(rows[0]);
    fireEvent.click(await screen.findByRole('button', { name: 'Підтвердити' }));

    expect(await screen.findByText('Аудиторію видалено')).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText('Ауд. 101')).not.toBeInTheDocument());
  });

  it('navigates to create', async () => {
    const { container } = renderWithProviders(<RoomsListView />, { route: routes.rooms.list(SESSION_ID, CENTER_ID), path: RoutePath.Rooms });
    fireEvent.click(byQa(container, RoomsTestId.CreateButton)!);
    expect(await screen.findByText(routes.rooms.create(SESSION_ID, CENTER_ID))).toBeInTheDocument();
  });
});

describe('Room create', () => {
  it('walks details → layout → summary and creates duplicated rooms in bulk', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<RoomsCreateView />, { route: routes.rooms.create(SESSION_ID, CENTER_ID), path: RoutePath.RoomCreate });

    await user.click(screen.getByRole('button', { name: 'Далі' }));
    expect(screen.getByText('Вкажіть назву')).toBeInTheDocument();

    await user.type(qaInput(container, RoomsTestId.NameField), 'Ауд. 3');
    fireEvent.change(qaInput(container, RoomsTestId.RowsField), { target: { value: '2' } });
    fireEvent.change(qaInput(container, RoomsTestId.ColumnsField), { target: { value: '2' } });
    await user.click(screen.getByRole('button', { name: 'Далі' }));

    expect(await screen.findByText('Місткість: 8, з них для осіб з інвалідністю: 0')).toBeInTheDocument();
    const seat = screen.getByRole('button', { name: /Ряд 1, Парта 1, Місце 1/ });
    await user.click(seat);
    expect(seat).toHaveAttribute('data-state', SeatState.Accessible);
    expect(await screen.findByText('Місткість: 8, з них для осіб з інвалідністю: 1')).toBeInTheDocument();

    await user.click(screen.getByLabelText('Спереду / позаду'));
    await waitFor(() => expect(container.querySelectorAll('[data-kind="FrontBack"]').length).toBe(8));

    await user.click(screen.getByRole('button', { name: 'Далі' }));
    const summary = byQa(container, RoomsTestId.Summary) as HTMLElement;
    expect(within(summary).getByText('Ауд. 3')).toBeInTheDocument();

    const copies = qaInput(container, RoomsTestId.DuplicateCountField);
    fireEvent.change(copies, { target: { value: '3' } });
    await user.click(screen.getByRole('button', { name: 'Створити аудиторії (3)' }));

    expect(await screen.findByText(routes.rooms.list(SESSION_ID, CENTER_ID))).toBeInTheDocument();
    const names = getMockRooms(CENTER_ID).map((room) => room.name);
    expect(names).toEqual(expect.arrayContaining(['Ауд. 3-1', 'Ауд. 3-2', 'Ауд. 3-3']));
    expect(getMockRooms(CENTER_ID).find((room) => room.name === 'Ауд. 3-2')?.capacity).toBe(8);
  });
});

describe('Room edit', () => {
  it('loads the room, edits the name and saves', async () => {
    const user = userEvent.setup();
    const room = getMockRooms(CENTER_ID)[2];
    const { container } = renderWithProviders(<RoomsEditView />, { route: routes.rooms.edit(SESSION_ID, CENTER_ID, room.id!), path: RoutePath.RoomEdit });

    await waitFor(() => expect(qaInput(container, RoomsTestId.NameField)).toHaveValue('Ауд. 214'));
    await user.clear(qaInput(container, RoomsTestId.NameField));
    await user.type(qaInput(container, RoomsTestId.NameField), 'Ауд. 215');
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    await user.click(screen.getByRole('button', { name: 'Далі' }));
    await user.click(screen.getByRole('button', { name: 'Зберегти аудиторію' }));

    expect(await screen.findByText('Аудиторію збережено')).toBeInTheDocument();
    expect(byQa(container, LOCATION_TEST_ID)).toHaveTextContent(routes.rooms.list(SESSION_ID, CENTER_ID));
    expect(getMockRooms(CENTER_ID)[2]).toMatchObject({ name: 'Ауд. 215', version: 2 });
  });
});
