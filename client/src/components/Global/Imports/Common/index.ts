import {
  DedupeStrategy,
  ImportMode,
  ParticipantField,
  type ColumnMappingDto,
  type ImportMappingDto,
  type ImportPreviewDto,
  type ImportResultDto,
  type ImportValidationDto,
} from '../../../../api/dtos';
import { ImportFileExtension, ImportLimit } from '../../../../enums/Imports';
import { FormFieldError } from '../../../../enums/Sessions';

/** Mirrors api/src/Domain/ValueObjects/AttributeKey.cs. */
export const ATTRIBUTE_KEY_MAX_LENGTH = 64;
export const TEMPLATE_FILE_NAME = 'participants-template.xlsx';
export const UPLOAD_FORM_FIELD = 'file';

export interface AttributeMapping {
  key: string;
  enabled: boolean;
}

export interface ImportState {
  file: File | null;
  fileError: FileError | null;
  preview: ImportPreviewDto | null;
  /** Sheet and header row the current preview (headers, sample, suggested mapping) was read for. */
  previewFor: string;
  sheet: string;
  headerRow: string;
  /** ParticipantField → source header ('' = not imported). */
  fields: Partial<Record<ParticipantField, string>>;
  /** Source header → attribute mapping (only for headers not mapped to a field). */
  attributes: Record<string, AttributeMapping>;
  mode: ImportMode;
  dedupeBy: DedupeStrategy;
  skipInvalid: boolean;
  validation: ImportValidationDto | null;
  result: ImportResultDto | null;
}

export enum FileError {
  TooLarge = 'tooLarge',
  WrongType = 'wrongType',
}

export enum MappingError {
  NamesRequired = 'namesRequired',
  DuplicateHeader = 'duplicateHeader',
}

export interface MappingValidationErrors {
  general?: MappingError;
  duplicateHeader?: string;
  attributeKeys: Record<string, FormFieldError | undefined>;
}

export enum ImportReducerAction {
  File = 'file',
  Preview = 'preview',
  RePreview = 'rePreview',
  Sheet = 'sheet',
  HeaderRow = 'headerRow',
  Field = 'field',
  Attribute = 'attribute',
  Mode = 'mode',
  DedupeBy = 'dedupeBy',
  SkipInvalid = 'skipInvalid',
  Validation = 'validation',
  Result = 'result',
}

export interface ImportAction {
  type: ImportReducerAction;
  payload: Partial<ImportState>;
  /** For Field / Attribute actions. */
  field?: ParticipantField;
  header?: string;
  value?: string;
  attribute?: Partial<AttributeMapping>;
}

export const initialImportState: ImportState = {
  file: null,
  fileError: null,
  preview: null,
  previewFor: '',
  sheet: '',
  headerRow: String(ImportLimit.FirstHeaderRow),
  fields: {},
  attributes: {},
  mode: ImportMode.Append,
  dedupeBy: DedupeStrategy.ExternalId,
  skipInvalid: false,
  validation: null,
  result: null,
};

const WHITESPACE = /\s+/g;
const NON_KEY_CHARACTERS = /[^\p{L}\p{N}_]/gu;
const KEY_SEPARATOR = '_';

/** Default attribute key for a header the server did not suggest (the server slugifies on its side too). */
export const toAttributeKey = (header: string) => header.trim().toLowerCase().replace(WHITESPACE, KEY_SEPARATOR).replace(NON_KEY_CHARACTERS, '').slice(0, ATTRIBUTE_KEY_MAX_LENGTH);

const fileExtension = (name: string) => name.slice(name.lastIndexOf('.')).toLowerCase();

export const checkFile = (file: File): FileError | null => {
  if (!Object.values<string>(ImportFileExtension).includes(fileExtension(file.name))) {
    return FileError.WrongType;
  }
  return file.size > ImportLimit.MaxFileSizeMb * ImportLimit.BytesPerMb ? FileError.TooLarge : null;
};

const PREVIEW_KEY_SEPARATOR = '\u0000';

/** Identifies the sheet / header row combination a preview was read for. */
export const previewKey = (sheet: string, headerRow: string) => `${sheet}${PREVIEW_KEY_SEPARATOR}${headerRow}`;

/** True when the chosen sheet / header row differ from the ones the current preview was read for. */
export const needsRePreview = (state: ImportState) => Boolean(state.preview) && previewKey(state.sheet, state.headerRow) !== state.previewFor;

const stateFromPreview = (state: ImportState, preview: ImportPreviewDto, sheet: string, headerRow: string): ImportState => {
  const suggestedAttributes = preview.suggestedMapping?.attributes ?? {};
  const keyByHeader = Object.fromEntries(Object.entries(suggestedAttributes).map(([key, header]) => [header, key]));
  const fields = { ...(preview.suggestedMapping?.fields ?? {}) } as Partial<Record<ParticipantField, string>>;
  const attributes = Object.fromEntries((preview.headers ?? []).map((header) => [header, { key: keyByHeader[header] ?? toAttributeKey(header), enabled: header in keyByHeader }]));
  return {
    ...state,
    preview,
    previewFor: previewKey(sheet, headerRow),
    sheet,
    headerRow,
    fields,
    attributes,
    validation: null,
    result: null,
  };
};

export const importReducer = (state: ImportState, action: ImportAction): ImportState => {
  switch (action.type) {
    case ImportReducerAction.Preview:
      return action.payload.preview ? stateFromPreview(state, action.payload.preview, action.payload.preview.sheets?.[0] ?? '', String(ImportLimit.FirstHeaderRow)) : state;
    case ImportReducerAction.RePreview:
      return action.payload.preview ? stateFromPreview(state, action.payload.preview, state.sheet, state.headerRow) : state;
    case ImportReducerAction.Field:
      return action.field ? { ...state, fields: { ...state.fields, [action.field]: action.value ?? '' }, validation: null } : state;
    case ImportReducerAction.Attribute:
      return action.header
        ? { ...state, attributes: { ...state.attributes, [action.header]: { ...state.attributes[action.header], ...action.attribute } }, validation: null }
        : state;
    case ImportReducerAction.File:
      return { ...initialImportState, ...action.payload };
    case ImportReducerAction.Sheet:
    case ImportReducerAction.HeaderRow:
    case ImportReducerAction.Mode:
    case ImportReducerAction.DedupeBy:
      return { ...state, ...action.payload, validation: null };
    case ImportReducerAction.SkipInvalid:
    case ImportReducerAction.Validation:
    case ImportReducerAction.Result:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};

export const mappedHeaders = (state: ImportState) => new Set(Object.values(state.fields).filter(Boolean));

/** Headers that are not mapped to a ParticipantField and may become attributes. */
export const attributeHeaders = (state: ImportState) => {
  const used = mappedHeaders(state);
  return (state.preview?.headers ?? []).filter((header) => !used.has(header));
};

export const validateMapping = (state: ImportState): MappingValidationErrors => {
  const errors: MappingValidationErrors = { attributeKeys: {} };
  const { fields } = state;
  const hasNames = Boolean(fields[ParticipantField.FullName]) || (Boolean(fields[ParticipantField.LastName]) && Boolean(fields[ParticipantField.FirstName]));
  if (!hasNames) {
    errors.general = MappingError.NamesRequired;
  }
  const seen = new Set<string>();
  Object.values(fields)
    .filter((header): header is string => Boolean(header))
    .forEach((header) => {
      if (seen.has(header) && !errors.general) {
        errors.general = MappingError.DuplicateHeader;
        errors.duplicateHeader = header;
      }
      seen.add(header);
    });
  attributeHeaders(state).forEach((header) => {
    const attribute = state.attributes[header];
    if (attribute?.enabled) {
      const key = attribute.key.trim();
      errors.attributeKeys[header] = !key ? FormFieldError.Required : key.length > ATTRIBUTE_KEY_MAX_LENGTH ? FormFieldError.TooLong : undefined;
    }
  });
  return errors;
};

export const hasMappingErrors = (errors: MappingValidationErrors) => Boolean(errors.general) || Object.values(errors.attributeKeys).some(Boolean);

export const toColumnMapping = (state: ImportState): ColumnMappingDto => ({
  fields: Object.fromEntries(Object.entries(state.fields).filter(([, header]) => Boolean(header))),
  attributes: Object.fromEntries(
    attributeHeaders(state)
      .filter((header) => state.attributes[header]?.enabled)
      .map((header) => [state.attributes[header].key.trim(), header])
  ),
});

export const toImportMappingDto = (state: ImportState): ImportMappingDto => ({
  sheet: state.sheet,
  headerRow: Number(state.headerRow) || ImportLimit.FirstHeaderRow,
  mapping: toColumnMapping(state),
  mode: state.mode,
  dedupeBy: state.dedupeBy,
  skipInvalid: state.skipInvalid,
});

export const isCommitBlocked = (state: ImportState) => (state.validation?.errorRows ?? 0) > 0 && !state.skipInvalid;
