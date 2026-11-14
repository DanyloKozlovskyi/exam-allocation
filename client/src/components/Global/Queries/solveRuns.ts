import { useQuery } from '@tanstack/react-query';
import type { SolveRunDto } from '../../../api/dtos';
import { api } from '../../../api/axiosInstance';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';
import { SOLVE_POLL_INTERVAL_MS } from '../../../constants/queryParams';
import { isRunActive } from '../SolveRuns/Common/constants';

export const fetchSolveRun = async (runId: string) => (await api.get<SolveRunDto>(apiRoutes.solveRuns.byId(runId))).data;

export const fetchSolveRuns = async (sessionId: string) => (await api.get<SolveRunDto[]>(apiRoutes.solveRuns.list(sessionId))).data;

/** Runs history (newest first); polls while a run is queued or running. */
export const useSolveRunsListQuery = (sessionId: string) =>
  useQuery({
    queryKey: queryKeys.solveRuns.list(sessionId),
    queryFn: () => fetchSolveRuns(sessionId),
    enabled: Boolean(sessionId),
    refetchInterval: (query) => (query.state.data?.some((run) => isRunActive(run.status)) ? SOLVE_POLL_INTERVAL_MS : false),
  });

interface SolveRunQueryOptions {
  /** Poll while the run is active (used as the SignalR fallback). */
  poll?: boolean;
}

export const useSolveRunQuery = (runId: string, { poll = false }: SolveRunQueryOptions = {}) =>
  useQuery({
    queryKey: queryKeys.solveRuns.detail(runId),
    queryFn: () => fetchSolveRun(runId),
    enabled: Boolean(runId),
    refetchInterval: (query) => (poll && isRunActive(query.state.data?.status) ? SOLVE_POLL_INTERVAL_MS : false),
  });
