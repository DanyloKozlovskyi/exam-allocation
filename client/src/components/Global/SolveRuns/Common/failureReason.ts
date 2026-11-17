import { SolveRunFailureReason, SolveRunStatus, type SolveRunDto } from '../../../../api/dtos';
import { FailureReasonAction } from '../../../../enums/SolveRuns';

interface FailureReasonInfo {
  /** Actions offered next to the reason-specific message (run view). */
  actions: FailureReasonAction[];
  /** Show the server's `errorMessage` as a detail line (only for unexpected errors). */
  showErrorDetail: boolean;
}

/**
 * Reason-specific handling of failed runs (R-9, docs/05 `SolveRunDto.failureReason`); texts are
 * `failureReason.{title,suggestion,short}.<reason>`. Exhaustive: a new reason fails typecheck.
 */
export const FAILURE_REASON_INFO: Record<SolveRunFailureReason, FailureReasonInfo> = {
  [SolveRunFailureReason.Interrupted]: { actions: [FailureReasonAction.Rerun], showErrorDetail: false },
  [SolveRunFailureReason.NoSolutionInTime]: { actions: [FailureReasonAction.Rerun, FailureReasonAction.OpenRules], showErrorDetail: false },
  [SolveRunFailureReason.SolverError]: { actions: [FailureReasonAction.Rerun], showErrorDetail: true },
  // Too many classes × rooms: fixable by the user (fewer active rooms, coarser rules); the diagnostic explains it.
  [SolveRunFailureReason.ModelTooLarge]: { actions: [FailureReasonAction.OpenRules], showErrorDetail: false },
};

/** Reason of a failed run; a failed run without one (older API) counts as a solver error. */
export const runFailureReason = (run: SolveRunDto) => (run.status === SolveRunStatus.Failed ? (run.failureReason ?? SolveRunFailureReason.SolverError) : undefined);
