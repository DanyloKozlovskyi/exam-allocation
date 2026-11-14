import type { AlertColor, ChipProps } from '@mui/material';
import { DiagnosticSeverity, SolveRunStatus, SolveStage } from '../../../../api/dtos';

export const DIAGNOSTIC_SEVERITY_COLOR: Record<DiagnosticSeverity, AlertColor> = {
  [DiagnosticSeverity.Info]: 'info',
  [DiagnosticSeverity.Warning]: 'warning',
  [DiagnosticSeverity.Error]: 'error',
};

/** Status chip colour map (docs/06 Solve). */
export const SOLVE_RUN_STATUS_COLOR: Record<SolveRunStatus, ChipProps['color']> = {
  [SolveRunStatus.Queued]: 'default',
  [SolveRunStatus.Running]: 'info',
  [SolveRunStatus.Succeeded]: 'success',
  [SolveRunStatus.Infeasible]: 'warning',
  [SolveRunStatus.Failed]: 'error',
  [SolveRunStatus.Cancelled]: 'default',
};

export const ACTIVE_RUN_STATUSES: SolveRunStatus[] = [SolveRunStatus.Queued, SolveRunStatus.Running];

export const isRunActive = (status: SolveRunStatus | undefined) => status === undefined || ACTIVE_RUN_STATUSES.includes(status);

/** Stages shown in the progress stepper, in pipeline order (docs/04 §1). */
export const SOLVE_STAGE_ORDER: SolveStage[] = [
  SolveStage.Precheck,
  SolveStage.Stage1,
  SolveStage.Stage2,
  SolveStage.Feedback,
  SolveStage.Alternatives,
  SolveStage.Expanding,
  SolveStage.Done,
];

// Solve options limits/defaults mirror backend `SolveOptions` (docs/04 §1).
export const SOLVE_DEFAULT_ALTERNATIVES = 5;
export const SOLVE_MIN_ALTERNATIVES = 0;
export const SOLVE_MAX_ALTERNATIVES = 20;
export const SOLVE_DEFAULT_TIME_LIMIT_SECONDS = 60;
export const SOLVE_MIN_TIME_LIMIT_SECONDS = 1;
export const SOLVE_MAX_TIME_LIMIT_SECONDS = 3600;
export const SOLVE_DEFAULT_DIVERSITY_PERCENT = 10;
export const SOLVE_DEFAULT_COST_SLACK_PERCENT = 10;
export const PERCENT_MIN = 0;
export const PERCENT_MAX = 100;
export const PERCENT_FACTOR = 100;
export const SOLVE_DEFAULT_SEED = 42;
export const OBJECTIVE_HISTORY_LIMIT = 200;
