import { api } from '../../../../api/axiosInstance';
import type { SessionDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const createSession = async (dto: SessionDto) => (await api.post<SessionDto>(apiRoutes.sessions.create(), dto)).data;
