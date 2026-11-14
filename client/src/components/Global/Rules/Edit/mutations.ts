import type { RuleDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const editRule = async (sessionId: string, rule: RuleDto) => (await api.put<RuleDto>(apiRoutes.rules.edit(sessionId), rule)).data;
