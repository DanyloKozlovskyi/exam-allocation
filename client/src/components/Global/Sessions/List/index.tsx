import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
import { Box, Button, Stack, Typography } from '@mui/material';
import { DataGrid, GridActionsCellItem, type GridColDef } from '@mui/x-data-grid';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import type { SessionSummaryDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { routes } from '../../../../constants/routes';
import { SessionsTestId } from '../../../../enums/Sessions';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { useSessionsListQuery } from '../../Queries/sessions';
import { GRID_HEIGHT_PX } from '../../../../constants/ui';
import { formatDate, formatDateTime } from '../Common/formatters';
import { useFeedback } from '../../../../hooks/useFeedback';
import { deleteSession } from './mutations';

const NAME_FLEX = 2;
const COLUMN_FLEX = 1;
const ACTIONS_WIDTH_PX = 140;

const SessionsList = () => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError } = useFeedback();
  const { data = [], isPending } = useSessionsListQuery();
  const [toDelete, setToDelete] = useState<SessionSummaryDto | null>(null);

  const deleteMutation = useMutation({
    mutationKey: [MutationKey.DeleteSession],
    mutationFn: deleteSession,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.sessions.list() });
      notifySuccess(t('messages.deleted'));
    },
    onError: notifyError,
  });

  const columns: GridColDef<SessionSummaryDto>[] = [
    { field: 'name', headerName: t('list.columns.name'), flex: NAME_FLEX },
    { field: 'examDate', headerName: t('list.columns.examDate'), flex: COLUMN_FLEX, valueFormatter: (value: string | null) => formatDate(value) },
    { field: 'updatedAt', headerName: t('list.columns.updatedAt'), flex: COLUMN_FLEX, valueFormatter: (value: string | null) => formatDateTime(value) },
    {
      field: 'actions',
      type: 'actions',
      headerName: t('list.columns.actions'),
      width: ACTIONS_WIDTH_PX,
      getActions: ({ row }) => [
        <GridActionsCellItem key="open" icon={<OpenInNewIcon />} label={t('list.open')} onClick={() => navigate(routes.sessions.view(row.id ?? ''))} />,
        <GridActionsCellItem key="edit" icon={<EditIcon />} label={t('view.edit')} onClick={() => navigate(routes.sessions.edit(row.id ?? ''))} />,
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
        <Button variant="contained" startIcon={<AddIcon />} component={RouterLink} to={routes.sessions.create()} data-qa={SessionsTestId.CreateButton}>
          {t('list.create')}
        </Button>
      </Stack>
      <Box sx={{ height: GRID_HEIGHT_PX }} data-qa={SessionsTestId.List}>
        <DataGrid
          rows={data}
          columns={columns}
          loading={isPending}
          getRowId={(row) => row.id ?? ''}
          onRowDoubleClick={({ row }) => navigate(routes.sessions.view(row.id ?? ''))}
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

export default SessionsList;
