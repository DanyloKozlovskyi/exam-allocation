import { Skeleton, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useEffect, useReducer } from 'react';
import { useTranslation } from 'react-i18next';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { useRoomQuery } from '../../Queries/rooms';
import { fromRoomDto, initialRoomState, roomReducer, RoomReducerAction, toRoomDto } from '../Common';
import Steps from '../Common/components/Steps';
import { editRoom } from './mutations';

interface Props {
  centerId: string;
  roomId: string;
  onFinish: () => void;
}

const SKELETON_HEIGHT_PX = 320;

const RoomEdit = ({ centerId, roomId, onFinish }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { data: room, isLoading } = useRoomQuery(centerId, roomId);
  const [state, dispatch] = useReducer(roomReducer, initialRoomState);

  useEffect(() => {
    if (room) {
      dispatch({ type: RoomReducerAction.Init, payload: fromRoomDto(room) });
    }
  }, [room]);

  const mutation = useMutation({
    mutationKey: [MutationKey.EditRoom],
    mutationFn: () => editRoom(centerId, toRoomDto(state, centerId)),
    onSuccess: (saved) => {
      queryClient.setQueryData(queryKeys.rooms.detail(centerId, roomId), saved);
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.list(centerId) });
      enqueueSnackbar(t('messages.saved'), { variant: SNACKBAR_SUCCESS });
      onFinish();
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('editTitle')}
      </Typography>
      {isLoading || !state.id ? (
        <Skeleton variant="rectangular" height={SKELETON_HEIGHT_PX} />
      ) : (
        <Steps state={state} dispatch={dispatch} completeLabel={t('actions.save')} handleComplete={() => mutation.mutate()} />
      )}
      {mutation.isPending && <CustomBackdrop label={t('messages.saving')} />}
    </>
  );
};

export default RoomEdit;
