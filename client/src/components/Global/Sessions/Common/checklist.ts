import type { SessionTotalsDto } from '../../../../api/dtos';
import { routes } from '../../../../constants/routes';
import { SessionChecklistItem } from '../../../../enums/Sessions';

interface ChecklistEntry {
  item: SessionChecklistItem;
  done: boolean;
  to: string;
}

/** Derives the preparation checklist from session totals (pure, exported for tests). */
export const buildChecklist = (sessionId: string, totals: SessionTotalsDto): ChecklistEntry[] => {
  const participants = totals.participants ?? 0;
  return [
    { item: SessionChecklistItem.Centers, done: (totals.centers ?? 0) > 0, to: routes.centers.list(sessionId) },
    { item: SessionChecklistItem.Rooms, done: (totals.rooms ?? 0) > 0, to: routes.centers.list(sessionId) },
    { item: SessionChecklistItem.Participants, done: participants > 0, to: routes.participants.list(sessionId) },
    { item: SessionChecklistItem.Capacity, done: participants > 0 && (totals.totalCapacity ?? 0) >= participants, to: routes.centers.list(sessionId) },
    { item: SessionChecklistItem.AccessibleCapacity, done: (totals.accessibleCapacity ?? 0) >= (totals.needsAccessibleSeat ?? 0), to: routes.centers.list(sessionId) },
    { item: SessionChecklistItem.Rules, done: (totals.rules ?? 0) > 0, to: routes.rules.list(sessionId) },
  ];
};
