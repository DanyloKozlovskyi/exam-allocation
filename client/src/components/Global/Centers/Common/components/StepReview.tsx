import { Box } from '@mui/material';
import { useTranslation } from 'react-i18next';
import ReadOnlyBlock from '../../../../Common/ReadOnlyBlock';
import { CenterStep, CentersTestId } from '../../../../../enums/Centers';
import { I18nNamespace } from '../../../../../i18n';
import type { CenterState } from '..';

interface Props {
  state: CenterState;
  setStep: (step: number) => void;
}

const StepReview = ({ state, setStep }: Props) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const notSet = t('form.notSet');
  return (
    <Box data-qa={CentersTestId.Review}>
      <ReadOnlyBlock
        items={[
          { label: t('form.fields.name'), value: state.name.trim() },
          { label: t('form.fields.address'), value: state.address.trim() || notSet },
          { label: t('form.fields.latitude'), value: state.latitude },
          { label: t('form.fields.longitude'), value: state.longitude },
          { label: t('form.fields.costPerRoom'), value: state.costPerRoom || notSet },
          { label: t('form.fields.isActive'), value: state.isActive ? t('form.yes') : t('form.no') },
          ...(state.googlePlaceId ? [{ label: t('form.fields.googlePlaceId'), value: state.googlePlaceId }] : []),
        ]}
        handleEdit={() => setStep(CenterStep.Details)}
      />
    </Box>
  );
};

export default StepReview;
