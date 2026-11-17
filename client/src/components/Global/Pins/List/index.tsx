import DeleteIcon from '@mui/icons-material/Delete';
import LinkOffIcon from '@mui/icons-material/LinkOff';
import { Box, Button, IconButton, Link, Stack, Tooltip, Typography } from '@mui/material';
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import type { PinDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { routes } from '../../../../constants/routes';
import { GRID_HEIGHT_PX } from '../../../../constants/ui';
import { PinColumn, PinsTestId } from '../../../../enums/Pins';
import { useFeedback } from '../../../../hooks/useFeedback';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { parseSeatKey, toDisplayNumber } from '../../../Common/RoomGrid/seatKey';
import { invalidateSessionInputs } from '../../Queries/invalidation';
import { usePinsListQuery } from '../../Queries/pins';
import { deleteAllPins, deletePin } from './mutations';

interface Props {
  sessionId: string;
}

// Feature-local column sizes (drawing constants of this grid only).
const NAME_MIN_WIDTH_PX = 240;
const ROOM_MIN_WIDTH_PX = 180;
const SEAT_WIDTH_PX = 220;
const ACTIONS_WIDTH_PX = 100;

/** Session pins (docs/06): who is fixed to which room/seat, with remove and remove-all. */
const PinsList = ({ sessionId }: Props) => {
  const { t } = useTranslation(I18nNamespace.Pins);
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();
  const { data: pins = [], isLoading } = usePinsListQuery(sessionId);
  const [toRemove, setToRemove] = useState<PinDto | null>(null);
  const [confirmRemoveAll, setConfirmRemoveAll] = useState(false);

  // Pins are solver input: totals, center data and run staleness change with them.
  const invalidate = () => Promise.all([queryClient.invalidateQueries({ queryKey: queryKeys.pins.list(sessionId) }), invalidateSessionInputs(queryClient)]);
  const participantName = (pin: PinDto) => pin.participantName || t('unknownParticipant');

  const removal = useMutation({
    mutationKey: [MutationKey.DeletePin],
    mutationFn: (participantId: string) => deletePin(sessionId, participantId),
    onSuccess: () => {
      void invalidate();
      notifySuccess(t('messages.removed'));
    },
    onError: notifyError,
    onSettled: () => setToRemove(null),
  });

  const removalAll = useMutation({
    mutationKey: [MutationKey.DeleteAllPins],
    mutationFn: () => deleteAllPins(sessionId),
    onSuccess: () => {
      void invalidate();
      notifySuccess(t('messages.removedAll'));
    },
    onError: notifyError,
    onSettled: () => setConfirmRemoveAll(false),
  });

  const seatLabel = (seatKey: string | null | undefined) => {
    const position = seatKey ? parseSeatKey(seatKey) : null;
    return position ? t('seatLabel', { row: toDisplayNumber(position.row), column: toDisplayNumber(position.column), seat: toDisplayNumber(position.seatIndex) }) : t('seatAny');
  };

  const columns: GridColDef<PinDto>[] = [
    { field: PinColumn.Participant, headerName: t('columns.participant'), flex: 1, minWidth: NAME_MIN_WIDTH_PX, valueGetter: (_value, row) => participantName(row) },
    {
      field: PinColumn.Room,
      headerName: t('columns.room'),
      flex: 1,
      minWidth: ROOM_MIN_WIDTH_PX,
      renderCell: ({ row }) =>
        row.centerId && row.roomId ? (
          <Link component={RouterLink} to={routes.rooms.edit(sessionId, row.centerId, row.roomId)}>
            {row.roomName || t('unknownRoom')}
          </Link>
        ) : (
          row.roomName || t('unknownRoom')
        ),
    },
    { field: PinColumn.Seat, headerName: t('columns.seat'), width: SEAT_WIDTH_PX, valueGetter: (_value, row) => seatLabel(row.seatKey) },
    {
      field: PinColumn.Actions,
      headerName: t('columns.actions'),
      width: ACTIONS_WIDTH_PX,
      sortable: false,
      renderCell: ({ row }) => (
        <Tooltip title={t('actions.remove')}>
          <IconButton size="small" aria-label={t('actions.remove')} onClick={() => setToRemove(row)} data-qa={PinsTestId.RemoveButton}>
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      ),
    },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
        <Typography variant="h5">{t('title')}</Typography>
        <Button
          variant="outlined"
          color="error"
          startIcon={<LinkOffIcon />}
          disabled={pins.length === 0}
          onClick={() => setConfirmRemoveAll(true)}
          data-qa={PinsTestId.RemoveAllButton}>
          {t('actions.removeAll')}
        </Button>
      </Stack>
      <Typography variant="body2" color="text.secondary">
        {t('description')}
      </Typography>
      <Box sx={{ height: GRID_HEIGHT_PX }} data-qa={PinsTestId.List}>
        <DataGrid
          rows={pins}
          columns={columns}
          getRowId={(row) => row.participantId ?? ''}
          loading={isLoading}
          disableRowSelectionOnClick
          localeText={{ noRowsLabel: t('empty') }}
        />
      </Box>
      <ConfirmDialog
        open={Boolean(toRemove)}
        title={t('confirm.removeTitle')}
        message={toRemove ? t('confirm.removeMessage', { name: participantName(toRemove) }) : ''}
        onConfirm={() => toRemove?.participantId && removal.mutate(toRemove.participantId)}
        onCancel={() => setToRemove(null)}
      />
      <ConfirmDialog
        open={confirmRemoveAll}
        title={t('confirm.removeAllTitle')}
        message={t('confirm.removeAllMessage', { count: pins.length })}
        onConfirm={() => removalAll.mutate()}
        onCancel={() => setConfirmRemoveAll(false)}
      />
    </Stack>
  );
};

export default PinsList;
