import { Alert, Box, Button, LinearProgress, Link, List, ListItem, Paper, Skeleton, Stack, Step, StepLabel, Stepper, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import { SolveRunStatus, SolveStage } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SOLVE_POLL_INTERVAL_SECONDS } from '../../../../constants/queryParams';
import { routes } from '../../../../constants/routes';
import { SNACKBAR_ERROR, SNACKBAR_INFO } from '../../../../constants/ui';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { processError } from '../../../../helpers/processError';
import { useSolveProgress } from '../../../../hooks/useSolveProgress';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { useAllocationsByRunQuery } from '../../Queries/allocations';
import { useRulesListQuery } from '../../Queries/rules';
import { MANUAL_ALLOCATION_INDEX } from '../../Allocations/Common/constants';
import { SOLVE_STAGE_ORDER } from '../Common/constants';
import DiagnosticsPanel from '../Common/DiagnosticsPanel';
import FailureReasonAlert from '../Common/FailureReasonAlert';
import { formatDateTime, formatNumber } from '../Common/formatters';
import ObjectiveChart from '../Common/ObjectiveChart';
import StatusChip from '../Common/StatusChip';
import { cancelSolveRun } from '../RunDialog/mutations';
import type { SolveProgressTransport } from '../../../../enums/SolveRuns';

interface Props {
  sessionId: string;
  runId: string;
  /** Forces a transport (tests); defaults to hub unless mocks are on. */
  transport?: SolveProgressTransport;
}

const SKELETON_HEIGHT_PX = 200;

const activeStageIndex = (status: SolveRunStatus | undefined, stage: SolveStage | undefined) => {
  if (status === SolveRunStatus.Succeeded) {
    return SOLVE_STAGE_ORDER.length;
  }
  return stage ? SOLVE_STAGE_ORDER.indexOf(stage) : 0;
};

const SolveRunView = ({ sessionId, runId, transport }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { run, progress, history, transport: currentTransport, isActive, isLoading } = useSolveProgress(runId, { transport });
  const { data: rules = [] } = useRulesListQuery(sessionId);
  const { data: allocationSummaries } = useAllocationsByRunQuery(run?.allocationIds?.length ? runId : undefined);
  const [confirmCancel, setConfirmCancel] = useState(false);

  const cancel = useMutation({
    mutationKey: [MutationKey.CancelSolveRun],
    mutationFn: () => cancelSolveRun(runId),
    onSuccess: () => {
      enqueueSnackbar(t('messages.cancelRequested'), { variant: SNACKBAR_INFO });
      void queryClient.invalidateQueries({ queryKey: queryKeys.solveRuns.detail(runId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.solveRuns.list(sessionId) });
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
    onSettled: () => setConfirmCancel(false),
  });

  if (isLoading || !run) {
    return <Skeleton variant="rectangular" height={SKELETON_HEIGHT_PX} />;
  }

  const allocationIds = run.allocationIds ?? [];
  // Solver allocations come first (best = index 0), manual versions after them: label by index, not position.
  const indexById = new Map((allocationSummaries ?? []).map((summary) => [summary.id, summary.index]));
  const allocationLabel = (allocationId: string, position: number) => {
    const index = indexById.get(allocationId) ?? position;
    if (index === MANUAL_ALLOCATION_INDEX) return t('view.manualAllocation');
    return index === 0 ? t('view.bestAllocation') : t('view.allocation', { index });
  };
  const diagnostics = run.diagnostics ?? [];
  const failedStage = run.status === SolveRunStatus.Infeasible || run.status === SolveRunStatus.Failed || run.status === SolveRunStatus.Cancelled;

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Typography variant="h5">{t('runTitle', { date: formatDateTime(run.createdAt) })}</Typography>
          <StatusChip status={run.status} isStale={run.isStale} />
        </Stack>
        {isActive && (
          <Button color="error" variant="outlined" onClick={() => setConfirmCancel(true)} loading={cancel.isPending} data-qa={SolveRunsTestId.CancelButton}>
            {t('actions.cancel')}
          </Button>
        )}
      </Stack>
      {run.isStale && <Alert severity="warning">{t('view.staleBanner')}</Alert>}

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack spacing={2}>
          <Stepper activeStep={activeStageIndex(run.status, progress?.stage)} alternativeLabel data-qa={SolveRunsTestId.StageStepper}>
            {SOLVE_STAGE_ORDER.map((stage) => (
              <Step key={stage}>
                <StepLabel error={failedStage && progress?.stage === stage}>{t(`stage.${stage}`)}</StepLabel>
              </Step>
            ))}
          </Stepper>
          {isActive && (
            <Box data-qa={SolveRunsTestId.ProgressBar}>
              <LinearProgress
                variant={progress?.percent !== null && progress?.percent !== undefined ? 'determinate' : 'indeterminate'}
                value={progress?.percent ?? undefined}
                aria-label={progress?.stage ? t(`stage.${progress.stage}`) : t(`status.${run.status ?? SolveRunStatus.Queued}`)}
              />
              <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
                {progress?.percent !== null && progress?.percent !== undefined && (
                  <Typography variant="body2">{t('view.percent', { percent: formatNumber(progress.percent) })}</Typography>
                )}
                {progress?.roomsTotal ? <Typography variant="body2">{t('view.rooms', { done: progress.roomsDone ?? 0, total: progress.roomsTotal })}</Typography> : null}
                {progress?.alternativesDone ? <Typography variant="body2">{t('view.alternativesDone', { count: progress.alternativesDone })}</Typography> : null}
                {progress?.objective !== null && progress?.objective !== undefined && (
                  <Typography variant="body2">
                    {t('view.objective')}: {formatNumber(progress.objective)}
                    {progress.bestBound !== null && progress.bestBound !== undefined ? ` · ${t('view.bestBound')}: ${formatNumber(progress.bestBound)}` : ''}
                  </Typography>
                )}
              </Stack>
            </Box>
          )}
          <ObjectiveChart history={history} />
          <Typography variant="caption" color="text.secondary" data-qa={SolveRunsTestId.TransportIndicator} data-transport={currentTransport}>
            {t(`transport.${currentTransport}`, { seconds: SOLVE_POLL_INTERVAL_SECONDS })}
          </Typography>
        </Stack>
      </Paper>

      {run.status === SolveRunStatus.Failed ? (
        <FailureReasonAlert run={run} sessionId={sessionId} />
      ) : (
        run.errorMessage && <Alert severity="error">{t('view.error', { message: run.errorMessage })}</Alert>
      )}

      {allocationIds.length > 0 && (
        <Box>
          <Typography variant="h6">{t('view.allocations')}</Typography>
          <List dense>
            {allocationIds.map((allocationId, index) => (
              <ListItem key={allocationId}>
                <Link component={RouterLink} to={routes.allocations.view(sessionId, allocationId)} data-qa={SolveRunsTestId.AllocationLink}>
                  {allocationLabel(allocationId, index)}
                </Link>
              </ListItem>
            ))}
          </List>
        </Box>
      )}

      {run.stats && (
        <Typography variant="body2" color="text.secondary">
          {t('view.stats')}: {t('view.statsValue', { classes: run.stats.classes ?? 0, variables: run.stats.variables ?? 0, constraints: run.stats.constraints ?? 0 })}
        </Typography>
      )}

      {(diagnostics.length > 0 || run.status === SolveRunStatus.Infeasible) && (
        <DiagnosticsPanel
          diagnostics={diagnostics}
          sessionId={sessionId}
          rules={rules}
          emptyText={run.status === SolveRunStatus.Infeasible ? t('diagnostics.unexplained') : undefined}
        />
      )}

      <ConfirmDialog
        open={confirmCancel}
        title={t('view.cancelTitle')}
        message={t('view.cancelMessage')}
        onConfirm={() => cancel.mutate()}
        onCancel={() => setConfirmCancel(false)}
      />
    </Stack>
  );
};

export default SolveRunView;
