// UI-only enums of the Participants feature (docs/10 §7).

/** Mirrors api/src/Domain/Participants/ParticipantConstraints.cs and PersonName (an object: several limits share a value). */
export const ParticipantConstraint = {
  NamePartMaxLength: 100,
  ExternalIdMaxLength: 100,
  GroupMaxLength: 100,
  SchoolMaxLength: 200,
  NotesMaxLength: 1000,
  AttributeValueMaxLength: 200,
} as const;

/** DataGrid column fields (keys of ParticipantDto shown as columns, plus UI-only ones). */
export enum ParticipantColumn {
  ExternalId = 'externalId',
  LastName = 'lastName',
  FirstName = 'firstName',
  MiddleName = 'middleName',
  Group = 'group',
  School = 'school',
  NeedsAccessibleSeat = 'needsAccessibleSeat',
  Notes = 'notes',
  Actions = 'actions',
}

/** Tri-state filter for `needsAccessibleSeat`. */
export enum AccessibleFilter {
  Any = 'any',
  Yes = 'yes',
  No = 'no',
}

export enum ParticipantsTestId {
  Grid = 'participants-grid',
  Search = 'participants-search',
  GroupFilter = 'participants-group-filter',
  SchoolFilter = 'participants-school-filter',
  AccessibleFilter = 'participants-accessible-filter',
  ImportButton = 'participants-import-button',
  DeleteAllButton = 'participants-delete-all-button',
  DeleteButton = 'participants-delete-button',
  Total = 'participants-total',
}
