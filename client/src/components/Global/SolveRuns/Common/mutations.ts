import type { DiagnosticDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const runPrecheck = async (sessionId: string) => (await api.post<DiagnosticDto[]>(apiRoutes.solveRuns.precheck(sessionId))).data;
