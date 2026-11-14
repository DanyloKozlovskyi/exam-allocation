import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const deleteRule = async (sessionId: string, ruleId: string) => {
  await api.delete(apiRoutes.rules.byId(sessionId, ruleId));
};
