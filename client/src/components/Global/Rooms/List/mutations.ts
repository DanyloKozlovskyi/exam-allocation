import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const deleteRoom = async (centerId: string, roomId: string) => {
  await api.delete(apiRoutes.rooms.byId(centerId, roomId));
};
