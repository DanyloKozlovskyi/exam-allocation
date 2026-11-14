import { Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import type { TestCenterDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { useFeedback } from '../../Sessions/Common/useFeedback';
import { fromPrefill, toCenterDto } from '../Common';
import Steps from '../Common/components/Steps';
import type { PickedPlace } from '../Map/types';
import { createCenter } from './mutations';

interface Props {
  sessionId: string;
  /** Values from a map click or a Places pick. */
  prefill?: Partial<PickedPlace> | null;
  onCreated: (center: TestCenterDto) => void;
}

const CenterCreate = ({ sessionId, prefill, onCreated }: Props) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();

  const mutation = useMutation({
    mutationKey: [MutationKey.CreateTestCenter],
    mutationFn: (dto: TestCenterDto) => createCenter(sessionId, dto),
    onSuccess: async (center) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.centers.list(sessionId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.sessions.totals(sessionId) }),
      ]);
      notifySuccess(t('messages.created'));
      onCreated(center);
    },
    onError: notifyError,
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('form.createTitle')}
      </Typography>
      <Steps initialState={fromPrefill(prefill)} completeLabel={t('form.createAction')} onComplete={(state) => mutation.mutate(toCenterDto(state, sessionId))} />
      {mutation.isPending && <CustomBackdrop label={t('form.saving')} />}
    </>
  );
};

export default CenterCreate;
