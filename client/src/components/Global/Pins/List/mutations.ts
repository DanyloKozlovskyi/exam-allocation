import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const deletePin = async (sessionId: string, participantId: string) => {
  await api.delete(apiRoutes.pins.byParticipant(sessionId, participantId));
};

export const deleteAllPins = async (sessionId: string) => {
  await api.post(apiRoutes.pins.deleteAll(sessionId));
};
