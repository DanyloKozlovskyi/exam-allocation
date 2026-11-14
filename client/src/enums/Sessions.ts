// UI-only enums of the Sessions feature (docs/10 §7). API enums come from api/dtos.ts.

/** Mirrors api/src/Domain/Sessions/ExamSessionConstraints.cs. */
export enum SessionConstraint {
  NameMaxLength = 200,
  DescriptionMaxLength = 2000,
}

export enum SessionStep {
  Details = 0,
  Summary = 1,
}

/** Validation error kinds shared by the forms of this workstream; texts live under `validation.*` in each namespace. */
export enum FormFieldError {
  Required = 'required',
  TooLong = 'tooLong',
  OutOfRange = 'outOfRange',
  NotANumber = 'notANumber',
}

export enum SessionChecklistItem {
  Centers = 'Centers',
  Rooms = 'Rooms',
  Participants = 'Participants',
  Capacity = 'Capacity',
  AccessibleCapacity = 'AccessibleCapacity',
  Rules = 'Rules',
}

export enum SessionTotal {
  Centers = 'centers',
  Rooms = 'rooms',
  TotalCapacity = 'totalCapacity',
  AccessibleCapacity = 'accessibleCapacity',
  Participants = 'participants',
  NeedsAccessibleSeat = 'needsAccessibleSeat',
  Groups = 'groups',
  Rules = 'rules',
}

export enum SessionsTestId {
  List = 'sessions-list',
  CreateButton = 'sessions-create-button',
  NameField = 'session-name-field',
  ExamDateField = 'session-exam-date-field',
  DescriptionField = 'session-description-field',
  Review = 'session-review',
  Dashboard = 'session-dashboard',
  Totals = 'session-totals',
  Checklist = 'session-checklist',
  StaleWarning = 'session-stale-warning',
  EditButton = 'session-edit-button',
}
