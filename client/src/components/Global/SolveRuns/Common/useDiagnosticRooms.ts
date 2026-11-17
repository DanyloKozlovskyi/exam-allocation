import { useQueries } from '@tanstack/react-query';
import { useMemo } from 'react';
import type { DiagnosticDto } from '../../../../api/dtos';
import { queryKeys } from '../../../../constants/queryKeys';
import { useCentersListQuery } from '../../Queries/centers';
import { fetchRooms } from '../../Queries/rooms';
import { diagnosticRoomIds, type RoomRef } from './diagnosticLinks';

/**
 * Diagnostics carry only `RoomId`, but the room edit route needs its center: loads the session's
 * centers and, only if some diagnostic names a room, the rooms of each center (same query keys as
 * the Rooms feature, so the cache is shared). Also returns center names for link labels.
 */
export const useDiagnosticRooms = (sessionId: string, diagnostics: DiagnosticDto[]) => {
  const { data: centers = [] } = useCentersListQuery(sessionId);
  const needsRooms = diagnostics.some((diagnostic) => diagnosticRoomIds(diagnostic).length > 0);
  const roomQueries = useQueries({
    queries: centers.map((center) => ({
      queryKey: queryKeys.rooms.list(center.id ?? ''),
      queryFn: () => fetchRooms(center.id ?? ''),
      enabled: needsRooms && Boolean(center.id),
    })),
  });
  // Cheap (rooms of one session) — rebuilt each render rather than memoised over a variable-length list.
  const rooms = new Map<string, RoomRef>();
  roomQueries.forEach((query, index) => {
    const centerId = centers[index]?.id;
    if (!centerId) return;
    (query.data ?? []).forEach((room) => room.id && rooms.set(room.id, { centerId, name: room.name }));
  });

  const centerNames = useMemo(() => new Map(centers.filter((center) => center.id).map((center) => [center.id!, center.name ?? ''])), [centers]);
  return { rooms, centerNames };
};
