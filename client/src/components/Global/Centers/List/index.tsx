import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import MeetingRoomIcon from '@mui/icons-material/MeetingRoom';
import { Box, Button, Stack, Switch, Typography } from '@mui/material';
import { DataGrid, GridActionsCellItem, type GridColDef } from '@mui/x-data-grid';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { TestCenterDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { routes } from '../../../../constants/routes';
import { CentersTestId } from '../../../../enums/Centers';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { useCentersListQuery } from '../../Queries/centers';
import { GRID_HEIGHT_PX } from '../../Sessions/Common/constants';
import { formatNumber } from '../../Sessions/Common/formatters';
import { useFeedback } from '../../Sessions/Common/useFeedback';
import { editCenter } from '../Edit/mutations';
import CentersMap from '../Map/CentersMap';
import type { PickedPlace } from '../Map/types';
import { deleteCenter } from './mutations';

const NAME_FLEX = 2;
const ADDRESS_FLEX = 2;
const NUMBER_WIDTH_PX = 110;
const ACTIVE_WIDTH_PX = 100;
const ACTIONS_WIDTH_PX = 140;
const SELECTED_ROW_CLASS = 'center-row--selected';

interface Props {
  sessionId: string;
  /** Opens Create, optionally prefilled from the map (click point or Places pick). */
  onCreate: (prefill?: Partial<PickedPlace>) => void;
}

const CentersList = ({ sessionId, onCreate }: Props) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();
  const { data = [], isPending } = useCentersListQuery(sessionId);
  const [selectedCenterId, setSelectedCenterId] = useState<string | null>(null);
  const [toDelete, setToDelete] = useState<TestCenterDto | null>(null);

  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.centers.list(sessionId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.sessions.totals(sessionId) }),
    ]);

  const deleteMutation = useMutation({
    mutationKey: [MutationKey.DeleteTestCenter],
    mutationFn: (centerId: string) => deleteCenter(sessionId, centerId),
    onSuccess: async () => {
      await invalidate();
      notifySuccess(t('messages.deleted'));
    },
    onError: notifyError,
  });

  const toggleMutation = useMutation({
    mutationKey: [MutationKey.EditTestCenter],
    mutationFn: (center: TestCenterDto) => editCenter(sessionId, { ...center, isActive: !center.isActive }),
    onSuccess: async (center) => {
      await invalidate();
      notifySuccess(center.isActive ? t('messages.activated') : t('messages.deactivated'));
    },
    onError: notifyError,
  });

  const columns: GridColDef<TestCenterDto>[] = [
    { field: 'name', headerName: t('list.columns.name'), flex: NAME_FLEX },
    { field: 'address', headerName: t('list.columns.address'), flex: ADDRESS_FLEX },
    { field: 'roomCount', headerName: t('list.columns.roomCount'), type: 'number', width: NUMBER_WIDTH_PX },
    { field: 'capacity', headerName: t('list.columns.capacity'), type: 'number', width: NUMBER_WIDTH_PX, valueFormatter: (value: number) => formatNumber(value) },
    {
      field: 'isActive',
      headerName: t('list.columns.isActive'),
      width: ACTIVE_WIDTH_PX,
      sortable: false,
      renderCell: ({ row }) => (
        <Switch
          checked={row.isActive ?? false}
          disabled={toggleMutation.isPending}
          onChange={() => toggleMutation.mutate(row)}
          onClick={(event) => event.stopPropagation()}
          slotProps={{ input: { 'aria-label': `${t('list.columns.isActive')}: ${row.name}` } }}
        />
      ),
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: t('list.columns.actions'),
      width: ACTIONS_WIDTH_PX,
      getActions: ({ row }) => [
        <GridActionsCellItem
          key="rooms"
          icon={<MeetingRoomIcon />}
          label={t('list.rooms')}
          data-qa={CentersTestId.RoomsLink}
          onClick={() => navigate(routes.rooms.list(sessionId, row.id ?? ''))}
        />,
        <GridActionsCellItem key="edit" icon={<EditIcon />} label={t('form.editTitle')} onClick={() => navigate(routes.centers.edit(sessionId, row.id ?? ''))} />,
        <GridActionsCellItem key="delete" icon={<DeleteIcon />} label={t('list.deleteTitle')} onClick={() => setToDelete(row)} />,
      ],
    },
  ];

  const confirmDelete = () => {
    if (toDelete?.id) {
      deleteMutation.mutate(toDelete.id);
    }
    setToDelete(null);
  };

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5">{t('list.title')}</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => onCreate()} data-qa={CentersTestId.CreateButton}>
          {t('list.create')}
        </Button>
      </Stack>
      <CentersMap
        centers={data}
        selectedCenterId={selectedCenterId}
        onSelect={setSelectedCenterId}
        onAddAtPoint={(point) => onCreate(point)}
        onPlacePicked={(place) => onCreate(place)}
      />
      <Box sx={{ height: GRID_HEIGHT_PX, [`& .${SELECTED_ROW_CLASS}`]: { bgcolor: 'action.selected' } }} data-qa={CentersTestId.List}>
        <DataGrid
          rows={data}
          columns={columns}
          loading={isPending}
          getRowId={(row) => row.id ?? ''}
          onRowClick={({ row }) => setSelectedCenterId(row.id ?? null)}
          getRowClassName={({ row }) => (row.id === selectedCenterId ? SELECTED_ROW_CLASS : '')}
          disableRowSelectionOnClick
          localeText={{ noRowsLabel: t('list.empty') }}
        />
      </Box>
      <ConfirmDialog
        open={Boolean(toDelete)}
        title={t('list.deleteTitle')}
        message={t('list.deleteMessage', { name: toDelete?.name })}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </Stack>
  );
};

export default CentersList;
