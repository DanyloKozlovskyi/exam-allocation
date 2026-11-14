import { api } from '../../../../api/axiosInstance';
import type { TestCenterDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const createCenter = async (sessionId: string, dto: TestCenterDto) => (await api.post<TestCenterDto>(apiRoutes.centers.create(sessionId), dto)).data;
