import { api } from '../../../../api/axiosInstance';
import type { ParticipantDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const editParticipant = async (sessionId: string, dto: ParticipantDto) => (await api.put<ParticipantDto>(apiRoutes.participants.edit(sessionId), dto)).data;

export const deleteParticipant = async (sessionId: string, participantId: string) => {
  await api.delete(apiRoutes.participants.byId(sessionId, participantId));
};

export const deleteAllParticipants = async (sessionId: string) => {
  await api.post(apiRoutes.participants.deleteAll(sessionId));
};
