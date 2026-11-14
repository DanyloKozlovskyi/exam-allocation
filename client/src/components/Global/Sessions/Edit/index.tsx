import { Alert, Skeleton, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import type { SessionDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { useSessionQuery } from '../../Queries/sessions';
import { fromSessionDto, toSessionDto } from '../Common';
import { FORM_SKELETON_HEIGHT_PX } from '../Common/constants';
import Steps from '../Common/components/Steps';
import { useFeedback } from '../Common/useFeedback';
import { editSession } from './mutations';

interface Props {
  sessionId: string;
  onSaved: (session: SessionDto) => void;
}

const SessionEdit = ({ sessionId, onSaved }: Props) => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();
  const { data, isPending, isError } = useSessionQuery(sessionId);

  const mutation = useMutation({
    mutationKey: [MutationKey.EditSession],
    mutationFn: editSession,
    onSuccess: async (session) => {
      queryClient.setQueryData(queryKeys.sessions.detail(sessionId), session);
      await queryClient.invalidateQueries({ queryKey: queryKeys.sessions.list() });
      notifySuccess(t('messages.updated'));
      onSaved(session);
    },
    onError: notifyError,
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('form.editTitle')}
      </Typography>
      {isPending && <Skeleton variant="rounded" height={FORM_SKELETON_HEIGHT_PX} />}
      {isError && <Alert severity="error">{t('view.notFound')}</Alert>}
      {data && (
        <Steps
          key={data.version ?? data.id}
          initialState={fromSessionDto(data)}
          completeLabel={t('form.saveAction')}
          onComplete={(state) => mutation.mutate(toSessionDto(state))}
        />
      )}
      {mutation.isPending && <CustomBackdrop label={t('form.saving')} />}
    </>
  );
};

export default SessionEdit;
