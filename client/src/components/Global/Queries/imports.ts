import { api } from '../../../api/axiosInstance';
import { apiRoutes } from '../../../constants/apiRoutes';

// Imports have no cacheable reads: upload/validate/commit are mutations (Imports/Create/mutations.ts).
// The template is a file download, fetched on demand.

const BLOB_RESPONSE = 'blob' as const;

export const fetchImportTemplate = async () => (await api.get<Blob>(apiRoutes.imports.template(), { responseType: BLOB_RESPONSE })).data;
