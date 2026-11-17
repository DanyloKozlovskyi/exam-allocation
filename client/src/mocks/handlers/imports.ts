import { http, HttpResponse, type HttpHandler } from 'msw';
import {
  DedupeStrategy,
  ErrorCode,
  ImportErrorCode,
  ImportMode,
  ParticipantField,
  type ImportErrorDto,
  type ImportMappingDto,
  type ImportPreviewDto,
  type ImportPreviewRequestDto,
  type ImportResultDto,
  type ImportValidationDto,
  type ParticipantDto,
} from '../../api/dtos';
import { apiRoutes } from '../../constants/apiRoutes';
import { MOCK_IMPORT_VALID_ROWS } from '../importFixture';
import { MockAttributeKey, mockAddParticipants, mockRemoveSessionParticipants } from './participants';
import { HttpStatus, MockPathParam, mockSessionExists, mockUrl, newId, pathParam, problem } from './sessions';

export enum MockImportHeader {
  Id = 'ID',
  LastName = 'Прізвище',
  FirstName = "Ім'я",
  MiddleName = 'По батькові',
  Group = 'Група',
  School = 'Навчальний заклад',
  NeedsAccessibleSeat = 'Особливі потреби',
  Profile = 'Профіль',
  Phone = 'Телефон',
}

export const MOCK_IMPORT_SHEETS = ['Учасники', 'Довідка'];
export { MOCK_IMPORT_VALID_ROWS };
export const XLSX_CONTENT_TYPE = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
const CONTENT_TYPE_HEADER = 'Content-Type';
const CONTENT_DISPOSITION_HEADER = 'Content-Disposition';
const TEMPLATE_DISPOSITION = 'attachment; filename="participants-template.xlsx"';

const MISSING_FIRST_NAME_ROW = 7;
const BAD_BOOLEAN_ROW = 15;
const DUPLICATE_ROW = 23;
const DUPLICATE_SOURCE_ROW = 4;

const HEADERS = Object.values(MockImportHeader);

const SAMPLE_ROWS: string[][] = [
  ['ZNO-000101', 'Гнатюк', 'Ярослав', 'Миколайович', '11-А', 'Ліцей № 171 «Лідер»', 'ні', 'Математика', '+380671234567'],
  ['ZNO-000102', 'Довженко', 'Олена', 'Петрівна', '11-А', 'Ліцей № 171 «Лідер»', 'так', 'Історія України', ''],
  ['ZNO-000103', 'Остапчук', 'Роман', '', '11-Б', 'Гімназія № 153 імені О. С. Пушкіна', '', 'Біологія', '+380501112233'],
  ['ZNO-000104', 'Яковенко', 'Ірина', 'Василівна', '10-А', 'Ліцей № 100 «Поділ»', 'ні', 'Фізика', ''],
  ['ZNO-000105', 'Зінченко', 'Назар', 'Сергійович', '10-Б', 'Природничо-науковий ліцей № 145', 'ні', 'Хімія', ''],
];

const SUGGESTED_FIELDS: Partial<Record<ParticipantField, string>> = {
  [ParticipantField.ExternalId]: MockImportHeader.Id,
  [ParticipantField.LastName]: MockImportHeader.LastName,
  [ParticipantField.FirstName]: MockImportHeader.FirstName,
  [ParticipantField.MiddleName]: MockImportHeader.MiddleName,
  [ParticipantField.Group]: MockImportHeader.Group,
  [ParticipantField.School]: MockImportHeader.School,
  [ParticipantField.NeedsAccessibleSeat]: MockImportHeader.NeedsAccessibleSeat,
};

interface MockImport {
  sessionId: string;
  preview: ImportPreviewDto;
}

const importsById = new Map<string, MockImport>();

const createPreview = (): ImportPreviewDto => ({
  importId: newId(),
  sheets: MOCK_IMPORT_SHEETS,
  headers: HEADERS,
  sampleRows: SAMPLE_ROWS,
  suggestedMapping: { fields: SUGGESTED_FIELDS, attributes: { [MockAttributeKey.Profile]: MockImportHeader.Profile } },
});

const validate = (dto: ImportMappingDto): ImportValidationDto => {
  const fields = dto.mapping?.fields ?? {};
  const hasNames = Boolean(fields[ParticipantField.FullName]) || (Boolean(fields[ParticipantField.LastName]) && Boolean(fields[ParticipantField.FirstName]));
  if (!hasNames) {
    const totalRows = MOCK_IMPORT_VALID_ROWS + SAMPLE_ROWS.length;
    return {
      validRows: 0,
      errorRows: totalRows,
      errors: [{ row: (dto.headerRow ?? 1) + 1, column: null, code: ImportErrorCode.RequiredMissing }],
      duplicates: [],
    };
  }
  const errors: ImportErrorDto[] = [{ row: MISSING_FIRST_NAME_ROW, column: fields[ParticipantField.FirstName] ?? null, code: ImportErrorCode.RequiredMissing }];
  if (fields[ParticipantField.NeedsAccessibleSeat]) {
    errors.push({ row: BAD_BOOLEAN_ROW, column: fields[ParticipantField.NeedsAccessibleSeat] ?? null, code: ImportErrorCode.BooleanUnparseable });
  }
  const duplicates: number[] = [];
  if (dto.dedupeBy !== DedupeStrategy.None) {
    errors.push({ row: DUPLICATE_ROW, column: fields[ParticipantField.ExternalId] ?? null, code: ImportErrorCode.DuplicateInFile });
    duplicates.push(DUPLICATE_SOURCE_ROW, DUPLICATE_ROW);
  }
  const errorRows = new Set(errors.map((error) => error.row)).size;
  return { validRows: MOCK_IMPORT_VALID_ROWS, errorRows, errors, duplicates };
};

const committedRows = (count: number): ParticipantDto[] =>
  Array.from({ length: count }, (_, index) => {
    const sample = SAMPLE_ROWS[index % SAMPLE_ROWS.length];
    const [externalId, lastName, firstName, middleName, group, school] = sample;
    return { externalId: `${externalId}-${index + 1}`, lastName, firstName, middleName: middleName || null, group, school, needsAccessibleSeat: false, attributes: {} };
  });

const importIdParam = pathParam(MockPathParam.ImportId);

const findImport = (importId: unknown) => importsById.get(String(importId));

export const importsHandlers: HttpHandler[] = [
  http.post(mockUrl(apiRoutes.imports.upload(pathParam(MockPathParam.SessionId))), ({ params }) => {
    const sessionId = String(params[MockPathParam.SessionId]);
    if (!mockSessionExists(sessionId)) {
      return problem(HttpStatus.NotFound, ErrorCode.SessionNotFound);
    }
    const preview = createPreview();
    importsById.set(preview.importId ?? '', { sessionId, preview });
    return HttpResponse.json(preview);
  }),

  http.post(mockUrl(apiRoutes.imports.preview(importIdParam)), async ({ params, request }) => {
    const pending = findImport(params[MockPathParam.ImportId]);
    if (!pending) {
      return problem(HttpStatus.NotFound, ErrorCode.ImportNotFound);
    }
    // One mock sheet: a later header row skips the sample rows above it.
    const { headerRow = 1 } = (await request.json()) as ImportPreviewRequestDto;
    const { preview } = pending;
    return HttpResponse.json({ ...preview, sampleRows: (preview.sampleRows ?? []).slice(Math.max(headerRow - 1, 0)) });
  }),

  http.post(mockUrl(apiRoutes.imports.validate(importIdParam)), async ({ params, request }) => {
    if (!findImport(params[MockPathParam.ImportId])) {
      return problem(HttpStatus.NotFound, ErrorCode.ImportNotFound);
    }
    return HttpResponse.json(validate((await request.json()) as ImportMappingDto));
  }),

  http.post(mockUrl(apiRoutes.imports.commit(importIdParam)), async ({ params, request }) => {
    const pending = findImport(params[MockPathParam.ImportId]);
    if (!pending) {
      return problem(HttpStatus.NotFound, ErrorCode.ImportNotFound);
    }
    const dto = (await request.json()) as ImportMappingDto;
    const validation = validate(dto);
    if ((validation.errorRows ?? 0) > 0 && !dto.skipInvalid) {
      return problem(HttpStatus.BadRequest, ErrorCode.ImportRowsInvalid);
    }
    if (dto.mode === ImportMode.Replace) {
      mockRemoveSessionParticipants(pending.sessionId);
    }
    const inserted = validation.validRows ?? 0;
    mockAddParticipants(pending.sessionId, committedRows(inserted));
    importsById.delete(String(params[MockPathParam.ImportId]));
    const result: ImportResultDto = { inserted, updated: 0, skipped: validation.errorRows ?? 0 };
    return HttpResponse.json(result);
  }),

  http.get(mockUrl(apiRoutes.imports.template()), () =>
    HttpResponse.arrayBuffer(new ArrayBuffer(0), { headers: { [CONTENT_TYPE_HEADER]: XLSX_CONTENT_TYPE, [CONTENT_DISPOSITION_HEADER]: TEMPLATE_DISPOSITION } })
  ),
];
