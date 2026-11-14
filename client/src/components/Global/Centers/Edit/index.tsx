import { Alert, Skeleton, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import type { TestCenterDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { useCenterQuery } from '../../Queries/centers';
import { FORM_SKELETON_HEIGHT_PX } from '../../Sessions/Common/constants';
import { useFeedback } from '../../Sessions/Common/useFeedback';
import { fromCenterDto, toCenterDto } from '../Common';
import Steps from '../Common/components/Steps';
import { editCenter } from './mutations';

interface Props {
  sessionId: string;
  centerId: string;
  onSaved: (center: TestCenterDto) => void;
}

const CenterEdit = ({ sessionId, centerId, onSaved }: Props) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const { t: tCommon } = useTranslation();
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();
  const { data, isPending, isError } = useCenterQuery(sessionId, centerId);

  const mutation = useMutation({
    mutationKey: [MutationKey.EditTestCenter],
    mutationFn: (dto: TestCenterDto) => editCenter(sessionId, dto),
    onSuccess: async (center) => {
      queryClient.setQueryData(queryKeys.centers.detail(sessionId, centerId), center);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.centers.list(sessionId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.sessions.totals(sessionId) }),
      ]);
      notifySuccess(t('messages.updated'));
      onSaved(center);
    },
    onError: notifyError,
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('form.editTitle')}
      </Typography>
      {isPending && <Skeleton variant="rounded" height={FORM_SKELETON_HEIGHT_PX} />}
      {isError && <Alert severity="error">{tCommon('errors.TestCenterNotFound')}</Alert>}
      {data && (
        <Steps
          key={data.version ?? data.id}
          initialState={fromCenterDto(data)}
          completeLabel={t('form.saveAction')}
          onComplete={(state) => mutation.mutate(toCenterDto(state, sessionId))}
        />
      )}
      {mutation.isPending && <CustomBackdrop label={t('form.saving')} />}
    </>
  );
};

export default CenterEdit;
