import { Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import type { SessionDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { initialSessionState, toSessionDto } from '../Common';
import Steps from '../Common/components/Steps';
import { useFeedback } from '../Common/useFeedback';
import { createSession } from './mutations';

interface Props {
  onCreated: (session: SessionDto) => void;
}

const SessionCreate = ({ onCreated }: Props) => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();

  const mutation = useMutation({
    mutationKey: [MutationKey.CreateSession],
    mutationFn: createSession,
    onSuccess: async (session) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.sessions.list() });
      notifySuccess(t('messages.created'));
      onCreated(session);
    },
    onError: notifyError,
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('form.createTitle')}
      </Typography>
      <Steps initialState={initialSessionState} completeLabel={t('form.createAction')} onComplete={(state) => mutation.mutate(toSessionDto(state))} />
      {mutation.isPending && <CustomBackdrop label={t('form.saving')} />}
    </>
  );
};

export default SessionCreate;
