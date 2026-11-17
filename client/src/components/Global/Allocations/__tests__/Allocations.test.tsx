import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ExportFormat } from '../../../../api/dtos';
import { RoutePath, routes } from '../../../../constants/routes';
import { AllocationsTestId } from '../../../../enums/Allocations';
import { SeatState } from '../../../../enums/Rooms';
import { getAllocationIdsForRun } from '../../../../mocks/handlers/allocations';
import { getMockRooms, MOCK_CENTERS, resetRoomsMocks } from '../../../../mocks/handlers/rooms';
import { resetRulesMocks } from '../../../../mocks/handlers/rules';
import { getMockRuns, resetSolveRunsMocks } from '../../../../mocks/handlers/solveRuns';
import AllocationsRoomView from '../../../../views/Allocations/Room';
import AllocationsViewView from '../../../../views/Allocations/View';
import { allByQa, byQa, LOCATION_TEST_ID, renderWithProviders } from '../../../../test-utils';
import { groupColor, GROUP_PALETTE, NO_GROUP_COLOR } from '../Common/constants';
import { buildExportUrl } from '../Common/helpers';

const SESSION_ID = 'alloc-session';

let allocationIds: string[] = [];

beforeEach(() => {
  resetRoomsMocks();
  resetRulesMocks();
  resetSolveRunsMocks();
  const run = getMockRuns(SESSION_ID)[0];
  allocationIds = getAllocationIdsForRun(run.dto.id!);
});

const location = () => byQa(document.body, LOCATION_TEST_ID)?.textContent;

describe('allocation helpers', () => {
  it('gives every group a stable palette colour', () => {
    expect(groupColor('11-А')).toBe(groupColor('11-А'));
    expect(GROUP_PALETTE).toContain(groupColor('10-Б'));
    expect(groupColor(null)).toBe(NO_GROUP_COLOR);
  });

  it('builds export URLs per ExportFormat (room only for PDF)', () => {
    expect(buildExportUrl('a1', ExportFormat.Xlsx, 'r1')).toBe('http://localhost:5080/allocations/a1/export?format=Xlsx');
    expect(buildExportUrl('a1', ExportFormat.Pdf, 'r1')).toBe('http://localhost:5080/allocations/a1/export?format=Pdf&roomId=r1');
  });
});

describe('Allocation view', () => {
  const renderView = (allocationId = allocationIds[0]) =>
    renderWithProviders(<AllocationsViewView />, { route: routes.allocations.view(SESSION_ID, allocationId), path: RoutePath.AllocationView });

  it('shows alternative tabs, rooms with fill bars and the export menu', async () => {
    const user = userEvent.setup();
    const { container } = renderView();

    expect(await screen.findByText('Результат розподілу — Найкращий')).toBeInTheDocument();
    const tabs = await waitFor(() => {
      const list = within(byQa(container, AllocationsTestId.AlternativeTabs) as HTMLElement).getAllByRole('tab');
      expect(list).toHaveLength(3);
      return list;
    });
    expect(tabs[0]).toHaveAttribute('aria-selected', 'true');

    expect(allByQa(container, AllocationsTestId.RoomFillBar).length).toBeGreaterThan(0);
    expect(within(byQa(container, AllocationsTestId.OverviewMap) as HTMLElement).getByText(/Київський ліцей №142/)).toBeInTheDocument();

    await user.click(byQa(container, AllocationsTestId.ExportButton) as HTMLElement);
    const items = await screen.findAllByRole('menuitem');
    expect(items.map((item) => item.getAttribute('data-format'))).toEqual(Object.values(ExportFormat));
    expect(items[0]).toHaveAttribute('href', buildExportUrl(allocationIds[0], ExportFormat.Xlsx));
    await user.keyboard('{Escape}');

    await user.click(tabs[1]);
    await waitFor(() => expect(location()).toBe(routes.allocations.view(SESSION_ID, allocationIds[1])));
  });

  it('filters rooms by the selected center and opens a room', async () => {
    const user = userEvent.setup();
    const { container } = renderView();
    await screen.findByText('Актова зала');
    const list = byQa(container, AllocationsTestId.RoomsList) as HTMLElement;

    await user.click(within(byQa(container, AllocationsTestId.OverviewMap) as HTMLElement).getByText(/Київський ліцей №142/));
    expect(within(list).queryByText('Актова зала')).not.toBeInTheDocument();

    const room = getMockRooms(MOCK_CENTERS[0].id)[0];
    await user.click(within(list).getByText(room.name!));
    expect(location()).toBe(routes.allocations.room(SESSION_ID, allocationIds[0], room.id!));
  });

  it('lists participants with server-side search', async () => {
    const user = userEvent.setup();
    renderView();
    await user.click(await screen.findByRole('tab', { name: 'Учасники' }));
    expect(await screen.findAllByText('Коваленко')).not.toHaveLength(0);

    await user.type(screen.getByLabelText("Пошук за прізвищем, ім'ям або групою"), 'Шевченко');
    await waitFor(() => expect(screen.queryByText('Коваленко')).not.toBeInTheDocument());
    expect(screen.getAllByText('Шевченко').length).toBeGreaterThan(0);
  });

  it('pins all participants and offers the re-run dialog', async () => {
    const user = userEvent.setup();
    const { container } = renderView();
    await screen.findByText('Результат розподілу — Найкращий');
    await user.click(byQa(container, AllocationsTestId.PinAllButton) as HTMLElement);

    expect(await screen.findByText('Зафіксовано учасників: 72')).toBeInTheDocument();
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Запустити' }));
    await waitFor(() => expect(location()).toMatch(/\/solve-runs\/.+/));
  });
});

describe('Room seating', () => {
  const room = () => getMockRooms(MOCK_CENTERS[0].id)[0];
  const renderRoom = () =>
    renderWithProviders(<AllocationsRoomView />, { route: routes.allocations.room(SESSION_ID, allocationIds[0], room().id!), path: RoutePath.AllocationRoom });
  const seat = (container: HTMLElement, key: string) => container.querySelector(`[data-seat-key="${key}"]`) as HTMLElement;

  it('renders surnames, group colours and violations', async () => {
    const { container } = renderRoom();
    expect(await screen.findByLabelText('Ряд 1, парта 1, місце 1: Шевченко Олександр Олександрович')).toBeInTheDocument();
    expect(screen.getByLabelText('Ряд 1, парта 1, місце 2: Вільне місце')).toBeInTheDocument();
    expect(seat(container, 'r0c0s0')).toHaveAttribute('data-state', SeatState.Occupied);
    expect(within(seat(container, 'r0c0s0')).getByText('Шевченко')).toBeInTheDocument();
    expect(byQa(container, AllocationsTestId.ViolationLegend)).toBeInTheDocument();
  });

  it('moves and swaps participants, validates and saves a manual version', async () => {
    const user = userEvent.setup();
    const { container } = renderRoom();
    await screen.findByLabelText(/Шевченко Олександр/);

    fireEvent.click(byQa(container, AllocationsTestId.MoveModeToggle) as HTMLElement);
    expect(screen.getByText('Оберіть учасника, якого потрібно пересадити.')).toBeInTheDocument();

    // Move Шевченко to the free seat next to him.
    fireEvent.click(seat(container, 'r0c0s0'));
    expect(seat(container, 'r0c0s0')).toHaveAttribute('data-state', SeatState.Selected);
    fireEvent.click(seat(container, 'r0c0s1'));
    expect(seat(container, 'r0c0s1')).toHaveAttribute('aria-label', expect.stringContaining('Шевченко'));

    // Swap him with the participant at desk 2.
    fireEvent.click(seat(container, 'r0c0s1'));
    fireEvent.click(seat(container, 'r0c1s0'));
    const pending = byQa(container, AllocationsTestId.PendingMoves) as HTMLElement;
    expect(within(pending).getByText('Змін: 2')).toBeInTheDocument();
    expect(seat(container, 'r0c0s1')).toHaveAttribute('aria-label', expect.stringContaining('Коваленко'));

    expect(byQa(container, AllocationsTestId.ApplyMovesButton)).toBeDisabled();
    await user.click(byQa(container, AllocationsTestId.ValidateMovesButton) as HTMLElement);
    expect(await screen.findByText(/Зміни не порушують обов'язкових правил/)).toBeInTheDocument();

    await user.click(byQa(container, AllocationsTestId.ApplyMovesButton) as HTMLElement);
    expect(await screen.findByText('Ручну версію збережено')).toBeInTheDocument();
    const ids = getAllocationIdsForRun(getMockRuns(SESSION_ID)[0].dto.id!);
    expect(ids).toHaveLength(4);
    await waitFor(() => expect(location()).toBe(routes.allocations.room(SESSION_ID, ids[3], room().id!)));
  });
});
