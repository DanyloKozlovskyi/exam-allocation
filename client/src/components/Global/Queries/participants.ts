import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { api } from '../../../api/axiosInstance';
import type { AttributeInfoDto, ParticipantDto, ParticipantDtoPagedDto, ParticipantSortField, SortDirection } from '../../../api/dtos';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

/** Query parameters of GET sessions/{id}/participants (docs/05 paging); `page` is 1-based (FIRST_PAGE). */
export interface ParticipantsPageParams {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: ParticipantSortField;
  sortDirection?: SortDirection;
  group?: string;
  school?: string;
  needsAccessibleSeat?: boolean;
}

export const fetchParticipantsPage = async (sessionId: string, params: ParticipantsPageParams) =>
  (await api.get<ParticipantDtoPagedDto>(apiRoutes.participants.list(sessionId), { params })).data;

export const fetchParticipant = async (sessionId: string, participantId: string) => (await api.get<ParticipantDto>(apiRoutes.participants.byId(sessionId, participantId))).data;

export const fetchAttributes = async (sessionId: string) => (await api.get<AttributeInfoDto[]>(apiRoutes.participants.attributes(sessionId))).data;

export const useParticipantsPageQuery = (sessionId: string, params: ParticipantsPageParams) =>
  useQuery({
    queryKey: queryKeys.participants.page(sessionId, params),
    queryFn: () => fetchParticipantsPage(sessionId, params),
    enabled: Boolean(sessionId),
    placeholderData: keepPreviousData,
  });

export const useParticipantQuery = (sessionId: string, participantId: string) =>
  useQuery({
    queryKey: queryKeys.participants.detail(sessionId, participantId),
    queryFn: () => fetchParticipant(sessionId, participantId),
    enabled: Boolean(sessionId && participantId),
  });

export const useAttributesQuery = (sessionId: string) =>
  useQuery({ queryKey: queryKeys.participants.attributes(sessionId), queryFn: () => fetchAttributes(sessionId), enabled: Boolean(sessionId) });
