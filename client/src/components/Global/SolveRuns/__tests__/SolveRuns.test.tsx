import { act, fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SolveRunStatus, SolveStage, type SolveRunDto } from '../../../../api/dtos';
import { HubEvent, HubMethod } from '../../../../constants/hub';
import { RoutePath, routes } from '../../../../constants/routes';
import { SolveProgressTransport, SolveRunsTestId } from '../../../../enums/SolveRuns';
import { getMockRuns, resetSolveRunsMocks } from '../../../../mocks/handlers/solveRuns';
import { resetRulesMocks } from '../../../../mocks/handlers/rules';
import SolveRunsListView from '../../../../views/SolveRuns/List';
import { byQa, LOCATION_TEST_ID, renderWithProviders } from '../../../../test-utils';
import { startSolveRun } from '../RunDialog/mutations';
import SolveRunView from '../View';

// ---- mocked @microsoft/signalr ----
type Handler = (payload: unknown) => void;

interface FakeConnection {
  handlers: Map<string, Handler>;
  invoke: jest.Mock;
  start: jest.Mock;
  stop: jest.Mock;
  state: string;
  emit: (event: string, payload: unknown) => void;
}

const fakeHub: { connection: FakeConnection | null; startFails: boolean; url: string | null } = { connection: null, startFails: false, url: null };

jest.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    withUrl(url: string) {
      fakeHub.url = url;
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    build() {
      const handlers = new Map<string, Handler>();
      const connection: FakeConnection & { on: (e: string, h: Handler) => void; onclose: (h: Handler) => void; onreconnected: (h: Handler) => void } = {
        handlers,
        state: 'Disconnected',
        invoke: jest.fn(() => Promise.resolve()),
        start: jest.fn(() => {
          if (fakeHub.startFails) return Promise.reject(new Error('negotiate failed'));
          connection.state = 'Connected';
          return Promise.resolve();
        }),
        stop: jest.fn(() => Promise.resolve()),
        on: (event: string, handler: Handler) => handlers.set(event, handler),
        onclose: (handler: Handler) => handlers.set('close', handler),
        onreconnected: (handler: Handler) => handlers.set('reconnected', handler),
        emit: (event: string, payload: unknown) => handlers.get(event)?.(payload),
      };
      fakeHub.connection = connection;
      return connection;
    }
  }
  return { HubConnectionBuilder, HubConnectionState: { Connected: 'Connected', Disconnected: 'Disconnected' } };
});

const SESSION_ID = 'solve-session';

beforeEach(() => {
  resetSolveRunsMocks();
  resetRulesMocks();
  fakeHub.connection = null;
  fakeHub.startFails = false;
});

const renderRun = (runId: string, transport: SolveProgressTransport) =>
  renderWithProviders(<SolveRunView sessionId={SESSION_ID} runId={runId} transport={transport} />, {
    route: routes.solveRuns.view(SESSION_ID, runId),
    path: RoutePath.SolveRunView,
  });

describe('Solve runs history', () => {
  it('lists runs with status chips and stale badge, and opens a run', async () => {
    const { container } = renderWithProviders(<SolveRunsListView />, { route: routes.solveRuns.list(SESSION_ID), path: RoutePath.SolveRuns });

    expect(await screen.findByText('Успішно')).toBeInTheDocument();
    expect(screen.getByText('Неможливо розмістити')).toBeInTheDocument();
    expect(container.querySelectorAll(`[data-qa="${SolveRunsTestId.StaleBadge}"]`)).toHaveLength(2);

    fireEvent.click(screen.getByText('Успішно'));
    expect(await screen.findByText(routes.solveRuns.view(SESSION_ID, getMockRuns(SESSION_ID)[0].dto.id!))).toBeInTheDocument();
  });

  it('starts a run from the dialog and rejects a second active run', async () => {
    const user = userEvent.setup();
    const { container } = renderWithProviders(<SolveRunsListView />, { route: routes.solveRuns.list(SESSION_ID), path: RoutePath.SolveRuns });
    await screen.findByText('Успішно');

    await user.click(byQa(container, SolveRunsTestId.RunButton) as HTMLElement);
    const dialog = await screen.findByRole('dialog');
    const alternatives = within(dialog).getByLabelText('Кількість альтернатив');
    fireEvent.change(alternatives, { target: { value: '99' } });
    expect(within(dialog).getByRole('button', { name: 'Запустити' })).toBeDisabled();
    fireEvent.change(alternatives, { target: { value: '1' } });

    await user.click(within(dialog).getByText('Додаткові параметри'));
    expect(within(dialog).getByLabelText('Початкове значення генератора (seed)')).toHaveValue(42);

    await user.click(within(dialog).getByRole('button', { name: 'Запустити' }));
    await waitFor(() => expect(byQa(document.body, LOCATION_TEST_ID)?.textContent).toMatch(/\/solve-runs\/.+/));
    const created = getMockRuns(SESSION_ID)[2];
    expect(created.alternatives).toBe(1);

    await expect(startSolveRun(SESSION_ID, {})).rejects.toMatchObject({ response: { status: 409 } });
  });
});

describe('useSolveProgress / run view', () => {
  it('subscribes over SignalR, shows live progress and completion', async () => {
    const run = await startSolveRun(SESSION_ID, { alternatives: 1 });
    const { container } = renderRun(run.id!, SolveProgressTransport.Hub);

    await waitFor(() => expect(fakeHub.connection?.invoke).toHaveBeenCalledWith(HubMethod.SubscribeToRun, run.id));
    expect(fakeHub.url).toBe('http://localhost:5080/hubs/solve-progress');
    expect(byQa(container, SolveRunsTestId.TransportIndicator)).toHaveAttribute('data-transport', SolveProgressTransport.Hub);

    act(() => {
      fakeHub.connection!.emit(HubEvent.ProgressUpdated, { runId: run.id, stage: SolveStage.Stage1, percent: 20, objective: 14, bestBound: 9 });
      fakeHub.connection!.emit(HubEvent.ProgressUpdated, { runId: run.id, stage: SolveStage.Stage2, percent: 40, objective: 12, bestBound: 10, roomsDone: 2, roomsTotal: 5 });
      fakeHub.connection!.emit(HubEvent.ProgressUpdated, { runId: 'other-run', stage: SolveStage.Done, percent: 100 });
    });

    expect(await screen.findByText('40 %')).toBeInTheDocument();
    expect(screen.getByText('Аудиторій оброблено: 2 з 5')).toBeInTheDocument();
    expect(byQa(container, SolveRunsTestId.ObjectiveChart)).toBeInTheDocument();
    expect(byQa(container, SolveRunsTestId.CancelButton)).toBeInTheDocument();

    // After an automatic reconnect the run group is gone: the hook subscribes again.
    act(() => fakeHub.connection!.emit('reconnected', 'new-connection-id'));
    await waitFor(() => expect(fakeHub.connection?.invoke).toHaveBeenCalledTimes(2));
    expect(fakeHub.connection?.invoke).toHaveBeenLastCalledWith(HubMethod.SubscribeToRun, run.id);

    const completed: SolveRunDto = { ...run, status: SolveRunStatus.Succeeded, allocationIds: ['alloc-1', 'alloc-2'] };
    act(() => fakeHub.connection!.emit(HubEvent.RunCompleted, completed));

    await waitFor(() => expect(byQa(container, SolveRunsTestId.StatusChip)).toHaveAttribute('data-status', SolveRunStatus.Succeeded));
    expect(byQa(container, SolveRunsTestId.CancelButton)).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Найкращий варіант' })).toHaveAttribute('href', routes.allocations.view(SESSION_ID, 'alloc-1'));
    expect(screen.getByRole('link', { name: 'Варіант 1' })).toBeInTheDocument();
  });

  it('falls back to polling when the hub is unavailable and cancels the run', async () => {
    fakeHub.startFails = true;
    const run = await startSolveRun(SESSION_ID, {});
    const { container } = renderRun(run.id!, SolveProgressTransport.Hub);

    await waitFor(() => expect(byQa(container, SolveRunsTestId.TransportIndicator)).toHaveAttribute('data-transport', SolveProgressTransport.Polling));
    expect(screen.getByText('Оновлення кожні 2 с')).toBeInTheDocument();

    fireEvent.click(byQa(container, SolveRunsTestId.CancelButton) as HTMLElement);
    fireEvent.click(await screen.findByRole('button', { name: 'Підтвердити' }));
    expect(await screen.findByText('Запит на скасування надіслано')).toBeInTheDocument();
    await waitFor(() => expect(byQa(container, SolveRunsTestId.StatusChip)).toHaveAttribute('data-status', SolveRunStatus.Cancelled));
  });

  it('polls without a hub on mocks and shows diagnostics of an infeasible run', async () => {
    const infeasible = getMockRuns(SESSION_ID)[1].dto;
    renderRun(infeasible.id!, SolveProgressTransport.Polling);

    expect(await screen.findByText('Недостатньо місць')).toBeInTheDocument();
    expect(screen.getByText(/Додайте аудиторії або центри/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Перейти до центру' })).toBeInTheDocument();
    expect(fakeHub.connection).toBeNull();
  });
});
