import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { AllocationDto, AllocationRowDtoPagedDto, AllocationSummaryDto, RoomSeatingDto } from '../../../api/dtos';
import { api } from '../../../api/axiosInstance';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

export interface AllocationParticipantsParams {
  page: number;
  pageSize: number;
  search: string;
}

export const useAllocationsByRunQuery = (runId: string | undefined) =>
  useQuery({
    queryKey: queryKeys.allocations.byRun(runId ?? ''),
    queryFn: async () => (await api.get<AllocationSummaryDto[]>(apiRoutes.allocations.byRun(runId!))).data,
    enabled: Boolean(runId),
  });

export const useAllocationQuery = (allocationId: string) =>
  useQuery({
    queryKey: queryKeys.allocations.detail(allocationId),
    queryFn: async () => (await api.get<AllocationDto>(apiRoutes.allocations.byId(allocationId))).data,
    enabled: Boolean(allocationId),
  });

export const useRoomSeatingQuery = (allocationId: string, roomId: string) =>
  useQuery({
    queryKey: queryKeys.allocations.room(allocationId, roomId),
    queryFn: async () => (await api.get<RoomSeatingDto>(apiRoutes.allocations.roomSeating(allocationId, roomId))).data,
    enabled: Boolean(allocationId && roomId),
  });

export const useAllocationParticipantsQuery = (allocationId: string, params: AllocationParticipantsParams) =>
  useQuery({
    queryKey: queryKeys.allocations.participants(allocationId, params),
    queryFn: async () =>
      (
        await api.get<AllocationRowDtoPagedDto>(apiRoutes.allocations.participants(allocationId), {
          params: { page: params.page, pageSize: params.pageSize, search: params.search || undefined },
        })
      ).data,
    enabled: Boolean(allocationId),
    placeholderData: keepPreviousData,
  });
