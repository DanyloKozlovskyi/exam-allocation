import type { CreateRoomsBulkDto, RoomDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const createRoom = async (centerId: string, room: RoomDto) => (await api.post<RoomDto>(apiRoutes.rooms.create(centerId), room)).data;

export const createRoomsBulk = async (centerId: string, dto: CreateRoomsBulkDto) => (await api.post<RoomDto[]>(apiRoutes.rooms.createBulk(centerId), dto)).data;
