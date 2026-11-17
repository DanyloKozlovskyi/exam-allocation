import { Alert, Button, Paper, Stack, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useReducer, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ImportMappingDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { QueryKey, queryKeys } from '../../../../constants/queryKeys';
import { ImportWizardStep, ImportsTestId } from '../../../../enums/Imports';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import CustomStepper from '../../../Common/CustomStepper';
import { fetchImportTemplate } from '../../Queries/imports';
import { FULL_GRID_SIZE } from '../../Sessions/Common/constants';
import { useFeedback } from '../../../../hooks/useFeedback';
import {
  hasMappingErrors,
  ImportReducerAction,
  importReducer,
  initialImportState,
  isCommitBlocked,
  needsRePreview,
  TEMPLATE_FILE_NAME,
  toImportMappingDto,
  validateMapping,
  type MappingValidationErrors,
} from '../Common';
import { buildImportSteps } from '../Common/components/Steps';
import { commitImport, previewImport, uploadImport, validateImport } from './mutations';
import { invalidateSessionInputs } from '../../Queries/invalidation';

interface Props {
  sessionId: string;
  onFinished: () => void;
}

interface ValidateRequest {
  dto: ImportMappingDto;
  nextStep: ImportWizardStep;
}

const EMPTY_MAPPING_ERRORS: MappingValidationErrors = { attributeKeys: {} };

const saveBlob = (blob: Blob, fileName: string) => {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
};

const ImportWizard = ({ sessionId, onFinished }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();
  const [state, dispatch] = useReducer(importReducer, initialImportState);
  const [step, setStep] = useState<number>(ImportWizardStep.Upload);
  const [mappingErrors, setMappingErrors] = useState<MappingValidationErrors>(EMPTY_MAPPING_ERRORS);
  const importId = state.preview?.importId ?? '';

  const uploadMutation = useMutation({
    mutationKey: [MutationKey.UploadImport],
    mutationFn: (file: File) => uploadImport(sessionId, file),
    onSuccess: (preview) => {
      dispatch({ type: ImportReducerAction.Preview, payload: { preview } });
      setStep(ImportWizardStep.Sheet);
    },
    onError: notifyError,
  });

  const previewMutation = useMutation({
    mutationKey: [MutationKey.PreviewImport],
    mutationFn: () => previewImport(importId, { sheet: state.sheet, headerRow: Number(state.headerRow) }),
    onSuccess: (preview) => {
      dispatch({ type: ImportReducerAction.RePreview, payload: { preview } });
      setStep(ImportWizardStep.Mapping);
    },
    onError: notifyError,
  });

  const validateMutation = useMutation({
    mutationKey: [MutationKey.ValidateImport],
    mutationFn: ({ dto }: ValidateRequest) => validateImport(importId, dto),
    onSuccess: (validation, { nextStep }) => {
      dispatch({ type: ImportReducerAction.Validation, payload: { validation } });
      setStep(nextStep);
    },
    onError: notifyError,
  });

  const commitMutation = useMutation({
    mutationKey: [MutationKey.CommitImport],
    mutationFn: (dto: ImportMappingDto) => commitImport(importId, dto),
    onSuccess: async (result) => {
      dispatch({ type: ImportReducerAction.Result, payload: { result } });
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [QueryKey.Participants, sessionId] }),
        queryClient.invalidateQueries({ queryKey: queryKeys.participants.attributes(sessionId) }),
        invalidateSessionInputs(queryClient),
      ]);
      notifySuccess(t('messages.committed', { count: result.inserted ?? 0 }));
    },
    onError: notifyError,
  });

  const downloadTemplate = async () => {
    try {
      saveBlob(await fetchImportTemplate(), TEMPLATE_FILE_NAME);
    } catch (error) {
      notifyError(error);
    }
  };

  /** Returns true to keep the stepper on the current step; async steps advance themselves on success. */
  const validate = (current: number) => {
    switch (current) {
      case ImportWizardStep.Upload:
        if (!state.file || state.fileError) {
          return true;
        }
        if (state.preview) {
          return false;
        }
        uploadMutation.mutate(state.file);
        return true;
      case ImportWizardStep.Sheet:
        // Headers, sample rows and the suggested mapping depend on the sheet and header row.
        if (!needsRePreview(state)) {
          return false;
        }
        previewMutation.mutate();
        return true;
      case ImportWizardStep.Mapping: {
        const errors = validateMapping(state);
        setMappingErrors(errors);
        if (hasMappingErrors(errors)) {
          return true;
        }
        validateMutation.mutate({ dto: toImportMappingDto(state), nextStep: ImportWizardStep.Validation });
        return true;
      }
      case ImportWizardStep.Mode:
        // Mode and dedupe strategy change the report (DuplicateExisting), so validate again before the summary.
        validateMutation.mutate({ dto: toImportMappingDto(state), nextStep: ImportWizardStep.Commit });
        return true;
      default:
        return false;
    }
  };

  const busyLabel = uploadMutation.isPending
    ? t('wizard.uploading')
    : previewMutation.isPending
      ? t('wizard.previewing')
      : validateMutation.isPending
        ? t('wizard.validating')
        : commitMutation.isPending
          ? t('wizard.committing')
          : null;
  const isNextDisabled = (step === ImportWizardStep.Upload && (!state.file || Boolean(state.fileError))) || (step === ImportWizardStep.Commit && isCommitBlocked(state));

  if (state.result) {
    return (
      <Paper sx={{ p: 3 }} data-qa={ImportsTestId.Result}>
        <Stack spacing={2}>
          <Typography variant="h5">{t('commit.resultTitle')}</Typography>
          <Alert severity="success">
            <div>{t('commit.inserted', { count: state.result.inserted ?? 0 })}</div>
            <div>{t('commit.updated', { count: state.result.updated ?? 0 })}</div>
            <div>{t('commit.skipped', { count: state.result.skipped ?? 0 })}</div>
          </Alert>
          <div>
            <Button variant="contained" onClick={onFinished} data-qa={ImportsTestId.FinishButton}>
              {t('commit.finish')}
            </Button>
          </div>
        </Stack>
      </Paper>
    );
  }

  return (
    <Stack spacing={2} data-qa={ImportsTestId.Wizard}>
      <Typography variant="h5">{t('wizard.title')}</Typography>
      <CustomStepper
        step={step}
        setStep={setStep}
        stepperSteps={buildImportSteps({ t, state, dispatch, mappingErrors, setStep, onDownloadTemplate: () => void downloadTemplate() })}
        completeLabel={t('wizard.commitAction')}
        isNextDisabled={isNextDisabled || Boolean(busyLabel)}
        validate={validate}
        handleComplete={() => commitMutation.mutate(toImportMappingDto(state))}
        gridSize={FULL_GRID_SIZE}
      />
      {busyLabel && <CustomBackdrop label={busyLabel} />}
    </Stack>
  );
};

export default ImportWizard;
