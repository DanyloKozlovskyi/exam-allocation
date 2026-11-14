import type { SolveOptionsDto, SolveRunDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const startSolveRun = async (sessionId: string, options: SolveOptionsDto) => (await api.post<SolveRunDto>(apiRoutes.solveRuns.create(sessionId), options)).data;

export const cancelSolveRun = async (runId: string) => {
  await api.post(apiRoutes.solveRuns.cancel(runId));
};
