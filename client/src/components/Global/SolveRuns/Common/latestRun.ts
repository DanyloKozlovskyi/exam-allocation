import { SolveRunStatus, type SolveRunDto } from '../../../../api/dtos';

const byNewest = (left: SolveRunDto, right: SolveRunDto) => (right.createdAt ?? '').localeCompare(left.createdAt ?? '');

/** The most recently created run of a session, if any. */
export const latestRun = (runs: SolveRunDto[]) => [...runs].sort(byNewest)[0];

/** The most recent run that produced allocations (its first allocation is the best one). */
export const latestSucceededRun = (runs: SolveRunDto[]) => [...runs].sort(byNewest).find((run) => run.status === SolveRunStatus.Succeeded && (run.allocationIds?.length ?? 0) > 0);
