import { SolveRunStatus, type SolveRunDto } from '../../api/dtos';
import { routes } from '../../constants/routes';
import { buildNavigation, NavigationItemKey } from './navigation';

const SESSION_ID = 'session-1';
const CENTER_ID = 'center-1';

const run = (id: string, createdAt: string, status: SolveRunStatus, allocationIds: string[] = []): SolveRunDto => ({ id, createdAt, status, allocationIds });

describe('buildNavigation', () => {
  it('shows only the sessions entry outside a session', () => {
    expect(buildNavigation({}).map((item) => item.key)).toEqual([NavigationItemKey.Sessions]);
  });

  it('adds session steps with badges from the totals', () => {
    const items = buildNavigation({ sessionId: SESSION_ID, totals: { centers: 3, rooms: 12, participants: 120, rules: 2 } });

    expect(items.map((item) => item.key)).toEqual([
      NavigationItemKey.Sessions,
      NavigationItemKey.Overview,
      NavigationItemKey.Centers,
      NavigationItemKey.Participants,
      NavigationItemKey.Rules,
      NavigationItemKey.Pins,
      NavigationItemKey.SolveRuns,
    ]);
    expect(items.find((item) => item.key === NavigationItemKey.Participants)?.badge).toBe(120);
  });

  it('adds rooms inside a center and results of the latest succeeded run', () => {
    const runs = [
      run('old', '2026-01-01T10:00:00Z', SolveRunStatus.Succeeded, ['old-best']),
      run('new', '2026-01-02T10:00:00Z', SolveRunStatus.Succeeded, ['new-best', 'new-alt']),
      run('failed', '2026-01-03T10:00:00Z', SolveRunStatus.Failed),
    ];

    const items = buildNavigation({ sessionId: SESSION_ID, centerId: CENTER_ID, totals: { rooms: 12 }, runs });

    expect(items.find((item) => item.key === NavigationItemKey.Rooms)).toEqual({ key: NavigationItemKey.Rooms, to: routes.rooms.list(SESSION_ID, CENTER_ID), badge: 12 });
    expect(items.find((item) => item.key === NavigationItemKey.Results)?.to).toBe(routes.allocations.view(SESSION_ID, 'new-best'));
    expect(items.find((item) => item.key === NavigationItemKey.SolveRuns)?.badge).toBe(3);
  });
});
