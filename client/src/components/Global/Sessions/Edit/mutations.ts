import { api } from '../../../../api/axiosInstance';
import type { SessionDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const editSession = async (dto: SessionDto) => (await api.put<SessionDto>(apiRoutes.sessions.edit(), dto)).data;
