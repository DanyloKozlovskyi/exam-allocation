import type { SessionTotalsDto, SolveRunDto } from '../../api/dtos';
import { routes } from '../../constants/routes';
import { latestSucceededRun } from '../Global/SolveRuns/Common/latestRun';

/** Drawer entries in workflow order (docs/06 Layout). */
export enum NavigationItemKey {
  Sessions = 'sessions',
  Overview = 'overview',
  Centers = 'centers',
  Rooms = 'rooms',
  Participants = 'participants',
  Rules = 'rules',
  SolveRuns = 'solveRuns',
  Results = 'results',
}

export interface NavigationItem {
  key: NavigationItemKey;
  to: string;
  /** Count shown next to the label (from `sessions/{id}/totals`). */
  badge?: number;
}

interface NavigationContext {
  sessionId?: string;
  /** Rooms belong to a center: the entry appears once a center is open (centers list links to its rooms). */
  centerId?: string;
  totals?: SessionTotalsDto;
  runs?: SolveRunDto[];
}

export const buildNavigation = ({ sessionId, centerId, totals, runs = [] }: NavigationContext): NavigationItem[] => {
  const items: NavigationItem[] = [{ key: NavigationItemKey.Sessions, to: routes.sessions.list() }];
  if (!sessionId) {
    return items;
  }

  items.push(
    { key: NavigationItemKey.Overview, to: routes.sessions.view(sessionId) },
    { key: NavigationItemKey.Centers, to: routes.centers.list(sessionId), badge: totals?.centers }
  );
  if (centerId) {
    items.push({ key: NavigationItemKey.Rooms, to: routes.rooms.list(sessionId, centerId), badge: totals?.rooms });
  }
  items.push(
    { key: NavigationItemKey.Participants, to: routes.participants.list(sessionId), badge: totals?.participants },
    { key: NavigationItemKey.Rules, to: routes.rules.list(sessionId), badge: totals?.rules },
    { key: NavigationItemKey.SolveRuns, to: routes.solveRuns.list(sessionId), badge: runs.length }
  );

  const bestAllocationId = latestSucceededRun(runs)?.allocationIds?.[0];
  if (bestAllocationId) {
    items.push({ key: NavigationItemKey.Results, to: routes.allocations.view(sessionId, bestAllocationId) });
  }
  return items;
};
