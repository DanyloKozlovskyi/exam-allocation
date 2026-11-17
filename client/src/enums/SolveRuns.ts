/** How `useSolveProgress` currently receives updates. */
export enum SolveProgressTransport {
  Hub = 'Hub',
  Polling = 'Polling',
}

export enum SolveRunsTestId {
  DiagnosticUnminimized = 'diagnostic-unminimized',
  List = 'solve-runs-list',
  RunButton = 'solve-runs-run-button',
  RunDialog = 'solve-run-dialog',
  RunDialogSubmit = 'solve-run-dialog-submit',
  AlternativesField = 'solve-run-alternatives-field',
  TimeLimitField = 'solve-run-time-limit-field',
  ObjectiveModeField = 'solve-run-objective-mode-field',
  DiversityRatioField = 'solve-run-diversity-ratio-field',
  CostSlackField = 'solve-run-cost-slack-field',
  SeedField = 'solve-run-seed-field',
  AdvancedToggle = 'solve-run-advanced-toggle',
  StatusChip = 'solve-run-status-chip',
  StaleBadge = 'solve-run-stale-badge',
  StageStepper = 'solve-run-stage-stepper',
  ProgressBar = 'solve-run-progress-bar',
  ObjectiveChart = 'solve-run-objective-chart',
  CancelButton = 'solve-run-cancel-button',
  TransportIndicator = 'solve-run-transport',
  AllocationLink = 'solve-run-allocation-link',
  DiagnosticsPanel = 'diagnostics-panel',
  DiagnosticItem = 'diagnostics-item',
  DiagnosticSuggestion = 'diagnostics-suggestion',
  DiagnosticLink = 'diagnostics-link',
  FailureReason = 'solve-run-failure-reason',
  FailureReasonRerun = 'solve-run-failure-rerun',
  FailureReasonRules = 'solve-run-failure-rules',
  ListFailureReason = 'solve-runs-list-failure-reason',
}

/** Where a diagnostic deep link points (DiagnosticsPanel, docs/04 §7). */
export enum DiagnosticLinkTarget {
  /** Edit page of the rule in `ruleId` / each id of `RuleIds`. */
  Rule = 'Rule',
  RulesList = 'RulesList',
  /** Rooms of each center in `CenterId` (comma-separated). */
  CenterRooms = 'CenterRooms',
  CentersList = 'CentersList',
  /** Edit page (layout) of the room in `RoomId`. */
  RoomEdit = 'RoomEdit',
  Participants = 'Participants',
  /** Participants with the attribute value in `AttributeKey` / `Value`. */
  ParticipantsByAttribute = 'ParticipantsByAttribute',
  /** Participants who need an accessible seat. */
  ParticipantsAccessible = 'ParticipantsAccessible',
  ParticipantsImport = 'ParticipantsImport',
  /** The session's pins page. */
  Pins = 'Pins',
}

/** Failure-reason alert actions on the run view. */
export enum FailureReasonAction {
  Rerun = 'Rerun',
  OpenRules = 'OpenRules',
}

/** `arguments.Reason` of a `ConflictCore` diagnostic (mirrors `Solver.Allocation.ConflictCoreReason`). */
export enum ConflictCoreReason {
  /** The proof timed out: the core lists every hard rule and center, not a minimal subset. */
  Unminimized = 'Unminimized',
}
