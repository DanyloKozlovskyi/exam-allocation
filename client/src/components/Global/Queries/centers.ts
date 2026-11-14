import { useQuery } from '@tanstack/react-query';
import { api } from '../../../api/axiosInstance';
import type { TestCenterDto } from '../../../api/dtos';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

export const fetchCenters = async (sessionId: string) => (await api.get<TestCenterDto[]>(apiRoutes.centers.list(sessionId))).data;
export const fetchCenter = async (sessionId: string, centerId: string) => (await api.get<TestCenterDto>(apiRoutes.centers.byId(sessionId, centerId))).data;

export const useCentersListQuery = (sessionId: string) =>
  useQuery({ queryKey: queryKeys.centers.list(sessionId), queryFn: () => fetchCenters(sessionId), enabled: Boolean(sessionId) });

export const useCenterQuery = (sessionId: string, centerId: string) =>
  useQuery({ queryKey: queryKeys.centers.detail(sessionId, centerId), queryFn: () => fetchCenter(sessionId, centerId), enabled: Boolean(sessionId && centerId) });
