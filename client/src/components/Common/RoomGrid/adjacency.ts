import type { AdjacencyKind, RoomLayoutPreviewDto } from '../../../api/dtos';

export interface SeatPair {
  a: string;
  b: string;
}

export interface AdjacencyOverlay {
  kind: AdjacencyKind;
  pairs: SeatPair[];
}

/** Converts index-based preview edges (`rooms/preview-layout`) into seat-key pairs for the grid overlay. */
export const toAdjacencyOverlays = (preview: RoomLayoutPreviewDto | undefined, kinds?: AdjacencyKind[]): AdjacencyOverlay[] => {
  const seats = preview?.seats ?? [];
  const keyAt = (index: number | undefined) => (index === undefined ? undefined : seats[index]?.seatKey);
  return (preview?.adjacency ?? [])
    .filter((group) => group.kind && (!kinds || kinds.includes(group.kind)))
    .map((group) => ({
      kind: group.kind as AdjacencyKind,
      pairs: (group.edges ?? []).flatMap((edge) => {
        const a = keyAt(edge.a);
        const b = keyAt(edge.b);
        return a && b ? [{ a, b }] : [];
      }),
    }));
};
