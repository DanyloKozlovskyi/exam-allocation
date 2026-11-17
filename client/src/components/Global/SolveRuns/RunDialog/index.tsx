import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Accordion, AccordionDetails, AccordionSummary, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ObjectiveMode, type SolveOptionsDto, type SolveRunDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import {
  PERCENT_FACTOR,
  PERCENT_MAX,
  PERCENT_MIN,
  SOLVE_DEFAULT_ALTERNATIVES,
  SOLVE_DEFAULT_COST_SLACK_PERCENT,
  SOLVE_DEFAULT_DIVERSITY_PERCENT,
  SOLVE_DEFAULT_SEED,
  SOLVE_MAX_SEED,
  SOLVE_DEFAULT_TIME_LIMIT_SECONDS,
  SOLVE_MAX_ALTERNATIVES,
  SOLVE_MAX_TIME_LIMIT_SECONDS,
  SOLVE_MIN_ALTERNATIVES,
  SOLVE_MIN_TIME_LIMIT_SECONDS,
} from '../Common/constants';
import { startSolveRun } from './mutations';

interface Props {
  open: boolean;
  sessionId: string;
  onClose: () => void;
  onStarted: (run: SolveRunDto) => void;
}

interface OptionsForm {
  alternatives: number;
  timeLimitSeconds: number;
  objectiveMode: ObjectiveMode;
  diversityPercent: number;
  costSlackPercent: number;
  seed: number;
}

enum NumericField {
  Alternatives = 'alternatives',
  TimeLimitSeconds = 'timeLimitSeconds',
  DiversityPercent = 'diversityPercent',
  CostSlackPercent = 'costSlackPercent',
  Seed = 'seed',
}

/** i18n keys (solveRuns namespace) of the numeric fields. */
enum FieldLabelKey {
  Alternatives = 'dialog.alternatives',
  TimeLimitSeconds = 'dialog.timeLimitSeconds',
  DiversityRatio = 'dialog.diversityRatio',
  CostSlack = 'dialog.costSlack',
  Seed = 'dialog.seed',
}

interface FieldSpec {
  label: FieldLabelKey;
  min: number;
  max: number;
  qa: SolveRunsTestId;
  /** Integer fields bind to C# int: fractions are rejected client-side. */
  integer?: boolean;
}

const FIELDS: Record<NumericField, FieldSpec> = {
  [NumericField.Alternatives]: {
    label: FieldLabelKey.Alternatives,
    min: SOLVE_MIN_ALTERNATIVES,
    max: SOLVE_MAX_ALTERNATIVES,
    qa: SolveRunsTestId.AlternativesField,
    integer: true,
  },
  [NumericField.TimeLimitSeconds]: {
    label: FieldLabelKey.TimeLimitSeconds,
    min: SOLVE_MIN_TIME_LIMIT_SECONDS,
    max: SOLVE_MAX_TIME_LIMIT_SECONDS,
    qa: SolveRunsTestId.TimeLimitField,
    integer: true,
  },
  [NumericField.DiversityPercent]: { label: FieldLabelKey.DiversityRatio, min: PERCENT_MIN, max: PERCENT_MAX, qa: SolveRunsTestId.DiversityRatioField },
  [NumericField.CostSlackPercent]: { label: FieldLabelKey.CostSlack, min: PERCENT_MIN, max: PERCENT_MAX, qa: SolveRunsTestId.CostSlackField },
  [NumericField.Seed]: { label: FieldLabelKey.Seed, min: 0, max: SOLVE_MAX_SEED, qa: SolveRunsTestId.SeedField, integer: true },
};

const DEFAULT_FORM: OptionsForm = {
  alternatives: SOLVE_DEFAULT_ALTERNATIVES,
  timeLimitSeconds: SOLVE_DEFAULT_TIME_LIMIT_SECONDS,
  objectiveMode: ObjectiveMode.CostThenSoft,
  diversityPercent: SOLVE_DEFAULT_DIVERSITY_PERCENT,
  costSlackPercent: SOLVE_DEFAULT_COST_SLACK_PERCENT,
  seed: SOLVE_DEFAULT_SEED,
};

const isFieldValid = (field: NumericField, value: number) =>
  Number.isFinite(value) && value >= FIELDS[field].min && value <= FIELDS[field].max && (!FIELDS[field].integer || Number.isInteger(value));

const toSolveOptionsDto = (form: OptionsForm): SolveOptionsDto => ({
  alternatives: form.alternatives,
  timeLimitSeconds: form.timeLimitSeconds,
  objectiveMode: form.objectiveMode,
  diversityRatio: form.diversityPercent / PERCENT_FACTOR,
  costSlack: form.costSlackPercent / PERCENT_FACTOR,
  seed: form.seed,
});

/** "Run solve" dialog with SolveOptionsDto fields; advanced options are collapsed. */
const RunDialog = ({ open, sessionId, onClose, onStarted }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<OptionsForm>(DEFAULT_FORM);
  const valid = Object.values(NumericField).every((field) => isFieldValid(field, form[field]));

  const mutation = useMutation({
    mutationKey: [MutationKey.StartSolveRun],
    mutationFn: () => startSolveRun(sessionId, toSolveOptionsDto(form)),
    onSuccess: (run) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.solveRuns.list(sessionId) });
      enqueueSnackbar(t('messages.started'), { variant: SNACKBAR_SUCCESS });
      onStarted(run);
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
  });

  const numberField = (field: NumericField) => {
    const spec = FIELDS[field];
    const value = form[field];
    const fieldValid = isFieldValid(field, value);
    return (
      <TextField
        type="number"
        fullWidth
        label={t(spec.label)}
        value={Number.isNaN(value) ? '' : value}
        onChange={(event) => setForm((current) => ({ ...current, [field]: parseFloat(event.target.value) }))}
        error={!fieldValid}
        helperText={fieldValid ? undefined : t('dialog.invalid', { min: spec.min, max: spec.max })}
        slotProps={{ htmlInput: { min: spec.min, max: spec.max, 'data-qa': spec.qa } }}
      />
    );
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth data-qa={SolveRunsTestId.RunDialog}>
      <DialogTitle>{t('dialog.title')}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {numberField(NumericField.Alternatives)}
          {numberField(NumericField.TimeLimitSeconds)}
          <Accordion disableGutters variant="outlined">
            <AccordionSummary expandIcon={<ExpandMoreIcon />} data-qa={SolveRunsTestId.AdvancedToggle}>
              <Typography>{t('actions.advanced')}</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Stack spacing={2}>
                <TextField
                  select
                  label={t('dialog.objectiveMode')}
                  value={form.objectiveMode}
                  onChange={(event) => setForm((current) => ({ ...current, objectiveMode: event.target.value as ObjectiveMode }))}
                  slotProps={{ htmlInput: { 'data-qa': SolveRunsTestId.ObjectiveModeField } }}>
                  {Object.values(ObjectiveMode).map((mode) => (
                    <MenuItem key={mode} value={mode}>
                      {t(`objectiveMode.${mode}`)}
                    </MenuItem>
                  ))}
                </TextField>
                {numberField(NumericField.DiversityPercent)}
                {numberField(NumericField.CostSlackPercent)}
                {numberField(NumericField.Seed)}
              </Stack>
            </AccordionDetails>
          </Accordion>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{tc('actions.cancel')}</Button>
        <Button variant="contained" disabled={!valid} loading={mutation.isPending} onClick={() => mutation.mutate()} data-qa={SolveRunsTestId.RunDialogSubmit}>
          {t('actions.start')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default RunDialog;
