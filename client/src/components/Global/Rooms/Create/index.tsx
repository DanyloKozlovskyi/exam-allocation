import { TextField, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useReducer, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { RoomsTestId } from '../../../../enums/Rooms';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { initialRoomState, roomReducer, toRoomDto } from '../Common';
import Steps from '../Common/components/Steps';
import { ROOM_MAX_DUPLICATES, ROOM_MIN_DUPLICATES } from '../Common/constants';
import { createRoom, createRoomsBulk } from './mutations';
import { invalidateSessionInputs } from '../../Queries/invalidation';

interface Props {
  centerId: string;
  onFinish: () => void;
}

const DUPLICATE_FIELD_MAX_WIDTH_PX = 320;

const RoomCreate = ({ centerId, onFinish }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const [state, dispatch] = useReducer(roomReducer, initialRoomState);
  const [copies, setCopies] = useState(ROOM_MIN_DUPLICATES);
  const copiesValid = Number.isInteger(copies) && copies >= ROOM_MIN_DUPLICATES && copies <= ROOM_MAX_DUPLICATES;

  const onSuccess = (count: number) => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.list(centerId) });
    void invalidateSessionInputs(queryClient);
    enqueueSnackbar(count > 1 ? t('messages.bulkCreated', { count }) : t('messages.created'), { variant: SNACKBAR_SUCCESS });
    onFinish();
  };
  const onError = (error: unknown) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR }));

  const single = useMutation({ mutationKey: [MutationKey.CreateRoom], mutationFn: () => createRoom(centerId, toRoomDto(state, centerId)), onSuccess: () => onSuccess(1), onError });
  const bulk = useMutation({
    mutationKey: [MutationKey.CreateRoomsBulk],
    mutationFn: () =>
      createRoomsBulk(centerId, {
        template: toRoomDto(state, centerId),
        names: Array.from({ length: copies }, (_, index) => t('bulk.nameTemplate', { name: state.name.trim(), index: index + 1 })),
      }),
    onSuccess: (rooms) => onSuccess(rooms.length),
    onError,
  });

  const handleComplete = () => {
    if (!copiesValid) {
      return;
    }
    if (copies > 1) {
      bulk.mutate();
    } else {
      single.mutate();
    }
  };

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('createTitle')}
      </Typography>
      <Steps
        state={state}
        dispatch={dispatch}
        completeLabel={copies > 1 ? t('actions.createRooms', { count: copies }) : t('actions.createRoom')}
        handleComplete={handleComplete}
        summaryExtras={
          <TextField
            type="number"
            label={t('fields.duplicateCount')}
            value={Number.isNaN(copies) ? '' : copies}
            onChange={(event) => setCopies(parseInt(event.target.value, 10))}
            error={!copiesValid}
            helperText={copiesValid ? t('fields.duplicateHelp') : t('validation.duplicateRange', { min: ROOM_MIN_DUPLICATES, max: ROOM_MAX_DUPLICATES })}
            slotProps={{ htmlInput: { min: ROOM_MIN_DUPLICATES, max: ROOM_MAX_DUPLICATES, 'data-qa': RoomsTestId.DuplicateCountField } }}
            sx={{ maxWidth: DUPLICATE_FIELD_MAX_WIDTH_PX }}
          />
        }
      />
      {(single.isPending || bulk.isPending) && <CustomBackdrop label={t('messages.saving')} />}
    </>
  );
};

export default RoomCreate;
