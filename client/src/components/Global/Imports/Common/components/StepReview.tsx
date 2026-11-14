import { Alert, Box, Stack } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ImportWizardStep, ImportsTestId } from '../../../../../enums/Imports';
import { I18nNamespace } from '../../../../../i18n';
import ReadOnlyBlock from '../../../../Common/ReadOnlyBlock';
import { isCommitBlocked, type ImportState } from '..';

interface Props {
  state: ImportState;
  setStep: (step: number) => void;
}

/** Commit step: summary of what will be imported. */
const StepReview = ({ state, setStep }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);
  return (
    <Stack spacing={2} data-qa={ImportsTestId.CommitSummary}>
      <Box>
        <ReadOnlyBlock
          items={[
            { label: t('commit.file'), value: state.file?.name ?? '' },
            { label: t('commit.sheet'), value: state.sheet },
            { label: t('commit.headerRow'), value: state.headerRow },
            { label: t('commit.rowsToImport'), value: String(state.validation?.validRows ?? 0) },
            { label: t('commit.mode'), value: t(`mode.modes.${state.mode}`) },
            { label: t('commit.dedupe'), value: t(`mode.dedupe.${state.dedupeBy}`) },
          ]}
          handleEdit={() => setStep(ImportWizardStep.Mode)}
        />
      </Box>
      {isCommitBlocked(state) && <Alert severity="warning">{t('commit.blocked')}</Alert>}
    </Stack>
  );
};

export default StepReview;
