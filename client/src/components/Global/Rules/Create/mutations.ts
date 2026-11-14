import type { RuleDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const createRule = async (sessionId: string, rule: RuleDto) => (await api.post<RuleDto>(apiRoutes.rules.create(sessionId), rule)).data;
