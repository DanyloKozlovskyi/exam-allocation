import type { RoomDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const editRoom = async (centerId: string, room: RoomDto) => (await api.put<RoomDto>(apiRoutes.rooms.edit(centerId), room)).data;
