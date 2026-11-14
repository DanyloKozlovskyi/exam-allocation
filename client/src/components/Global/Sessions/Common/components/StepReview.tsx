import { Box } from '@mui/material';
import { useTranslation } from 'react-i18next';
import ReadOnlyBlock from '../../../../Common/ReadOnlyBlock';
import { SessionStep, SessionsTestId } from '../../../../../enums/Sessions';
import { I18nNamespace } from '../../../../../i18n';
import { formatDate } from '../formatters';
import type { SessionState } from '..';

interface Props {
  state: SessionState;
  setStep: (step: number) => void;
}

const StepReview = ({ state, setStep }: Props) => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  const notSet = t('form.notSet');
  return (
    <Box data-qa={SessionsTestId.Review}>
      <ReadOnlyBlock
        items={[
          { label: t('form.fields.name'), value: state.name.trim() },
          { label: t('form.fields.examDate'), value: state.examDate ? formatDate(state.examDate) : notSet },
          { label: t('form.fields.description'), value: state.description.trim() || notSet },
        ]}
        handleEdit={() => setStep(SessionStep.Details)}
      />
    </Box>
  );
};

export default StepReview;
