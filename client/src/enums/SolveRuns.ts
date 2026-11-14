/** How `useSolveProgress` currently receives updates. */
export enum SolveProgressTransport {
  Hub = 'Hub',
  Polling = 'Polling',
}

export enum SolveRunsTestId {
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
}
