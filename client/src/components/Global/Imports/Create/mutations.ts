import { api } from '../../../../api/axiosInstance';
import type { ImportMappingDto, ImportPreviewDto, ImportResultDto, ImportValidationDto } from '../../../../api/dtos';
import { apiRoutes } from '../../../../constants/apiRoutes';
import { UPLOAD_FORM_FIELD } from '../Common';

export const uploadImport = async (sessionId: string, file: File) => {
  const form = new FormData();
  form.append(UPLOAD_FORM_FIELD, file);
  return (await api.post<ImportPreviewDto>(apiRoutes.imports.upload(sessionId), form)).data;
};

export const validateImport = async (importId: string, dto: ImportMappingDto) => (await api.post<ImportValidationDto>(apiRoutes.imports.validate(importId), dto)).data;

export const commitImport = async (importId: string, dto: ImportMappingDto) => (await api.post<ImportResultDto>(apiRoutes.imports.commit(importId), dto)).data;
