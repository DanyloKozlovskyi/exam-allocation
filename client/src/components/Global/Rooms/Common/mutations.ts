import type { RoomLayoutDto, RoomLayoutPreviewDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const previewRoomLayout = async (layout: RoomLayoutDto) => (await api.post<RoomLayoutPreviewDto>(apiRoutes.rooms.previewLayout(), layout)).data;
