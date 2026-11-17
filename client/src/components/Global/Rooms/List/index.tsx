import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import { Box, Button, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { RoomDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { RoomsTestId } from '../../../../enums/Rooms';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { useRoomsListQuery } from '../../Queries/rooms';
import { deleteRoom } from './mutations';
import { invalidateSessionInputs } from '../../Queries/invalidation';

interface Props {
  centerId: string;
  onCreate: () => void;
  onEdit: (roomId: string) => void;
}

const NAME_MIN_WIDTH_PX = 180;
const NUMBER_COLUMN_WIDTH_PX = 130;
const ACTIONS_WIDTH_PX = 120;

const RoomsList = ({ centerId, onCreate, onEdit }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { data: rooms = [], isLoading } = useRoomsListQuery(centerId);
  const [toDelete, setToDelete] = useState<RoomDto | null>(null);

  const removal = useMutation({
    mutationKey: [MutationKey.DeleteRoom],
    mutationFn: (roomId: string) => deleteRoom(centerId, roomId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rooms.list(centerId) });
      void invalidateSessionInputs(queryClient);
      enqueueSnackbar(t('messages.deleted'), { variant: SNACKBAR_SUCCESS });
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
    onSettled: () => setToDelete(null),
  });

  const columns: GridColDef<RoomDto>[] = [
    { field: 'name', headerName: t('fields.name'), flex: 1, minWidth: NAME_MIN_WIDTH_PX },
    {
      field: 'size',
      headerName: t('fields.size'),
      width: NUMBER_COLUMN_WIDTH_PX,
      sortable: false,
      valueGetter: (_value, row) => t('list.sizeValue', { rows: row.layout?.rows ?? 0, columns: row.layout?.columns ?? 0, seats: row.layout?.seatsPerDesk ?? 0 }),
    },
    { field: 'capacity', headerName: t('fields.capacity'), type: 'number', width: NUMBER_COLUMN_WIDTH_PX },
    { field: 'accessibleCapacity', headerName: t('fields.accessibleCapacity'), type: 'number', width: NUMBER_COLUMN_WIDTH_PX },
    {
      field: 'costOverride',
      headerName: t('fields.costOverride'),
      width: NUMBER_COLUMN_WIDTH_PX,
      valueFormatter: (value: number | null | undefined) => (value === null || value === undefined ? t('list.defaultCost') : String(value)),
    },
    { field: 'isActive', headerName: t('fields.isActive'), type: 'boolean', width: NUMBER_COLUMN_WIDTH_PX },
    {
      field: 'actions',
      headerName: '',
      sortable: false,
      width: ACTIONS_WIDTH_PX,
      renderCell: ({ row }) => (
        <Box>
          <Tooltip title={t('actions.edit')}>
            <IconButton aria-label={t('actions.edit')} data-qa={RoomsTestId.EditButton} onClick={() => row.id && onEdit(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title={t('actions.delete')}>
            <IconButton aria-label={t('actions.delete')} data-qa={RoomsTestId.DeleteButton} onClick={() => setToDelete(row)}>
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5">{t('title')}</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={onCreate} data-qa={RoomsTestId.CreateButton}>
          {t('actions.create')}
        </Button>
      </Stack>
      <Box data-qa={RoomsTestId.List}>
        <DataGrid
          autoHeight
          disableVirtualization
          rows={rooms}
          columns={columns}
          loading={isLoading}
          getRowId={(row) => row.id ?? row.name ?? ''}
          disableRowSelectionOnClick
          hideFooter
          localeText={{ noRowsLabel: t('list.empty') }}
        />
      </Box>
      <ConfirmDialog
        open={Boolean(toDelete)}
        title={t('messages.deleteTitle')}
        message={t('messages.deleteMessage', { name: toDelete?.name ?? '' })}
        onConfirm={() => toDelete?.id && removal.mutate(toDelete.id)}
        onCancel={() => setToDelete(null)}
      />
    </Stack>
  );
};

export default RoomsList;
