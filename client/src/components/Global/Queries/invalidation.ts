import type { QueryClient } from '@tanstack/react-query';
import { QueryKey } from '../../../constants/queryKeys';

/** Queries derived from a session's inputs: totals and drawer badges, center room counts/capacity, run staleness. */
const SESSION_DERIVED_KEYS = [QueryKey.SessionTotals, QueryKey.Centers, QueryKey.SolveRuns];

/** Call after any change to rooms, rules, participants or pins (by prefix: room/rule views only know their own ids). */
export const invalidateSessionInputs = (queryClient: QueryClient) => Promise.all(SESSION_DERIVED_KEYS.map((key) => queryClient.invalidateQueries({ queryKey: [key] })));
