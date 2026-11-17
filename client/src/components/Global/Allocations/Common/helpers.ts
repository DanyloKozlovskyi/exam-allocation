import { ExportFormat, type ParticipantBriefDto } from '../../../../api/dtos';
import { appConfig } from '../../../../constants/config';
import { ApiQueryParam, apiRoutes } from '../../../../constants/apiRoutes';
import { ELLIPSIS, SURNAME_MAX_CHARS } from './constants';

/** Absolute download URL of an export; `roomId` applies to PDF only. */
export const buildExportUrl = (allocationId: string, format: ExportFormat, roomId?: string) => {
  const query = new URLSearchParams({ [ApiQueryParam.Format]: format });
  if (roomId && format === ExportFormat.Pdf) {
    query.set(ApiQueryParam.RoomId, roomId);
  }
  return `${appConfig.apiUrl}/${apiRoutes.exports.export(allocationId)}?${query.toString()}`;
};

export const truncate = (value: string, max = SURNAME_MAX_CHARS) => (value.length > max ? `${value.slice(0, max - 1)}${ELLIPSIS}` : value);

export const fullName = (participant: ParticipantBriefDto | undefined | null) =>
  participant ? [participant.lastName, participant.firstName, participant.middleName].filter(Boolean).join(' ') : '';
