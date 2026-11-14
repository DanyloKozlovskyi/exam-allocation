import { api } from '../../../../api/axiosInstance';
import type { TestCenterDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const editCenter = async (sessionId: string, dto: TestCenterDto) => (await api.put<TestCenterDto>(apiRoutes.centers.edit(sessionId), dto)).data;
