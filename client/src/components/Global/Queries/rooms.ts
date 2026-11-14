import { useQuery } from '@tanstack/react-query';
import type { RoomDto } from '../../../api/dtos';
import { api } from '../../../api/axiosInstance';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

export const fetchRooms = async (centerId: string) => (await api.get<RoomDto[]>(apiRoutes.rooms.list(centerId))).data;

export const fetchRoom = async (centerId: string, roomId: string) => (await api.get<RoomDto>(apiRoutes.rooms.byId(centerId, roomId))).data;

export const useRoomsListQuery = (centerId: string) =>
  useQuery({
    queryKey: queryKeys.rooms.list(centerId),
    queryFn: () => fetchRooms(centerId),
    enabled: Boolean(centerId),
  });

export const useRoomQuery = (centerId: string, roomId: string) =>
  useQuery({
    queryKey: queryKeys.rooms.detail(centerId, roomId),
    queryFn: () => fetchRoom(centerId, roomId),
    enabled: Boolean(centerId && roomId),
  });
