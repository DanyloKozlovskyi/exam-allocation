import { useQuery } from '@tanstack/react-query';
import { api } from '../../../api/axiosInstance';
import type { SessionDto, SessionSummaryDto, SessionTotalsDto } from '../../../api/dtos';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

export const fetchSessions = async () => (await api.get<SessionSummaryDto[]>(apiRoutes.sessions.list())).data;
export const fetchSession = async (sessionId: string) => (await api.get<SessionDto>(apiRoutes.sessions.byId(sessionId))).data;
export const fetchSessionTotals = async (sessionId: string) => (await api.get<SessionTotalsDto>(apiRoutes.sessions.totals(sessionId))).data;

export const useSessionsListQuery = () => useQuery({ queryKey: queryKeys.sessions.list(), queryFn: fetchSessions });

export const useSessionQuery = (sessionId: string) =>
  useQuery({ queryKey: queryKeys.sessions.detail(sessionId), queryFn: () => fetchSession(sessionId), enabled: Boolean(sessionId) });

export const useSessionTotalsQuery = (sessionId: string) =>
  useQuery({ queryKey: queryKeys.sessions.totals(sessionId), queryFn: () => fetchSessionTotals(sessionId), enabled: Boolean(sessionId) });
