import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const deleteSession = async (sessionId: string) => {
  await api.delete(apiRoutes.sessions.byId(sessionId));
};
