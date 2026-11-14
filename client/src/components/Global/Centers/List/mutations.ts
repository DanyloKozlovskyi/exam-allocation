import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const deleteCenter = async (sessionId: string, centerId: string) => {
  await api.delete(apiRoutes.centers.byId(sessionId, centerId));
};
