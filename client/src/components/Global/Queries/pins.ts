import { useQuery } from '@tanstack/react-query';
import { api } from '../../../api/axiosInstance';
import type { PinDto } from '../../../api/dtos';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

export const fetchPins = async (sessionId: string) => (await api.get<PinDto[]>(apiRoutes.pins.list(sessionId))).data;

export const usePinsListQuery = (sessionId: string) =>
  useQuery({ queryKey: queryKeys.pins.list(sessionId), queryFn: () => fetchPins(sessionId), enabled: Boolean(sessionId) });
