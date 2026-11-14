import type { GridSortModel } from '@mui/x-data-grid';
import { ParticipantSortField, SortDirection, type ParticipantDto } from '../../../../api/dtos';
import { AccessibleFilter, ParticipantColumn, ParticipantConstraint } from '../../../../enums/Participants';
import { DEFAULT_PAGE_SIZE } from '../../../../constants/queryParams';
import { emptyToNull } from '../../Sessions/Common/formHelpers';

export const SEARCH_DEBOUNCE_MS = 300;
const SMALL_PAGE_SIZE = 25;
const LARGE_PAGE_SIZE = 100;
export const PAGE_SIZE_OPTIONS = [SMALL_PAGE_SIZE, DEFAULT_PAGE_SIZE, LARGE_PAGE_SIZE];
export const ATTRIBUTE_COLUMN_PREFIX = 'attr:';

// MUI X sort direction is a third-party literal type.
const GRID_SORT_DESC = 'desc';

/** Grid columns that the API can sort by. */
export const sortFieldByColumn: Partial<Record<string, ParticipantSortField>> = {
  [ParticipantColumn.LastName]: ParticipantSortField.LastName,
  [ParticipantColumn.FirstName]: ParticipantSortField.FirstName,
  [ParticipantColumn.Group]: ParticipantSortField.Group,
  [ParticipantColumn.School]: ParticipantSortField.School,
  [ParticipantColumn.ExternalId]: ParticipantSortField.ExternalId,
};

export interface ParticipantsSort {
  sortBy?: ParticipantSortField;
  sortDirection?: SortDirection;
}

export const toParticipantsSort = (model: GridSortModel): ParticipantsSort => {
  const [first] = model;
  const sortBy = first ? sortFieldByColumn[first.field] : undefined;
  if (!first || !sortBy) {
    return {};
  }
  return { sortBy, sortDirection: first.sort === GRID_SORT_DESC ? SortDirection.Descending : SortDirection.Ascending };
};

export const toAccessibleParam = (filter: AccessibleFilter): boolean | undefined => (filter === AccessibleFilter.Any ? undefined : filter === AccessibleFilter.Yes);

export const attributeColumnField = (key: string) => `${ATTRIBUTE_COLUMN_PREFIX}${key}`;

export const fullName = (participant: ParticipantDto) => [participant.lastName, participant.firstName, participant.middleName].filter(Boolean).join(' ');

const trimTo = (value: string | null | undefined, maxLength: number): string => (value ? value.slice(0, maxLength) : '');

/** Normalizes an inline-edited row into the DTO sent to `participants/edit`; returns null when names are empty. */
export const toEditedParticipant = (row: ParticipantDto): ParticipantDto | null => {
  const lastName = row.lastName?.trim() ?? '';
  const firstName = row.firstName?.trim() ?? '';
  if (!lastName || !firstName) {
    return null;
  }
  const attributes = Object.fromEntries(
    Object.entries(row.attributes ?? {})
      .map(([key, value]) => [key, trimTo(value?.trim(), ParticipantConstraint.AttributeValueMaxLength)])
      .filter(([, value]) => value)
  );
  return {
    ...row,
    lastName: trimTo(lastName, ParticipantConstraint.NamePartMaxLength),
    firstName: trimTo(firstName, ParticipantConstraint.NamePartMaxLength),
    middleName: emptyToNull(row.middleName ?? ''),
    externalId: emptyToNull(row.externalId ?? ''),
    group: emptyToNull(row.group ?? ''),
    school: emptyToNull(row.school ?? ''),
    notes: emptyToNull(row.notes ?? ''),
    attributes,
  };
};
