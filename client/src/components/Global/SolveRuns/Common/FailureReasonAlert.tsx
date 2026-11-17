import { Alert, AlertTitle, Button, Stack, Typography } from '@mui/material';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import type { SolveRunDto } from '../../../../api/dtos';
import { routes } from '../../../../constants/routes';
import { FailureReasonAction, SolveRunsTestId } from '../../../../enums/SolveRuns';
import { I18nNamespace } from '../../../../i18n';
import RunDialog from '../RunDialog';
import { FAILURE_REASON_INFO, runFailureReason } from './failureReason';

interface Props {
  run: SolveRunDto;
  sessionId: string;
}

/** Reason-specific message, suggestion and next steps for a failed run (R-9). Renders nothing for other statuses. */
const FailureReasonAlert = ({ run, sessionId }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  const navigate = useNavigate();
  const [dialogOpen, setDialogOpen] = useState(false);
  const reason = runFailureReason(run);
  if (!reason) return null;
  const info = FAILURE_REASON_INFO[reason];

  return (
    <Alert severity="error" data-qa={SolveRunsTestId.FailureReason} data-reason={reason}>
      <AlertTitle>{t(`failureReason.title.${reason}`)}</AlertTitle>
      <Typography variant="body2">{t(`failureReason.suggestion.${reason}`)}</Typography>
      {info.showErrorDetail && run.errorMessage && (
        <Typography variant="body2" sx={{ mt: 0.5 }}>
          {t('failureReason.detail', { message: run.errorMessage })}
        </Typography>
      )}
      <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
        {info.actions.includes(FailureReasonAction.Rerun) && (
          <Button size="small" variant="outlined" color="inherit" onClick={() => setDialogOpen(true)} data-qa={SolveRunsTestId.FailureReasonRerun}>
            {t('failureReason.rerun')}
          </Button>
        )}
        {info.actions.includes(FailureReasonAction.OpenRules) && (
          <Button size="small" color="inherit" component={RouterLink} to={routes.rules.list(sessionId)} data-qa={SolveRunsTestId.FailureReasonRules}>
            {t('failureReason.openRules')}
          </Button>
        )}
      </Stack>
      <RunDialog
        open={dialogOpen}
        sessionId={sessionId}
        onClose={() => setDialogOpen(false)}
        onStarted={(started) => {
          setDialogOpen(false);
          if (started.id) navigate(routes.solveRuns.view(sessionId, started.id));
        }}
      />
    </Alert>
  );
};

export default FailureReasonAlert;
