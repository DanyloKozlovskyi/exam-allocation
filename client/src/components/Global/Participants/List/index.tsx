import DeleteIcon from '@mui/icons-material/Delete';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { DataGrid, GridActionsCellItem, type GridColDef, type GridPaginationModel, type GridSortModel } from '@mui/x-data-grid';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import type { ParticipantDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { QueryKey, queryKeys } from '../../../../constants/queryKeys';
import { DEFAULT_PAGE_SIZE, FIRST_PAGE } from '../../../../constants/queryParams';
import { routes } from '../../../../constants/routes';
import { AccessibleFilter, ParticipantColumn, ParticipantsTestId } from '../../../../enums/Participants';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { useAttributesQuery, useParticipantsPageQuery, type ParticipantsPageParams } from '../../Queries/participants';
import { GRID_HEIGHT_PX, GRID_SERVER_MODE } from '../../Sessions/Common/constants';
import { useFeedback } from '../../Sessions/Common/useFeedback';
import { attributeColumnField, fullName, PAGE_SIZE_OPTIONS, SEARCH_DEBOUNCE_MS, sortFieldByColumn, toAccessibleParam, toEditedParticipant, toParticipantsSort } from '../Common';
import { deleteAllParticipants, deleteParticipant, editParticipant } from './mutations';

const NAME_WIDTH_PX = 150;
const TEXT_WIDTH_PX = 130;
const SCHOOL_WIDTH_PX = 220;
const FLAG_WIDTH_PX = 130;
const ACTIONS_WIDTH_PX = 80;
const FILTER_WIDTH_PX = 180;
const FIRST_GRID_PAGE = 0;

class ParticipantRowError extends Error {}

interface Props {
  sessionId: string;
}

const ParticipantsList = ({ sessionId }: Props) => {
  const { t } = useTranslation(I18nNamespace.Participants);
  const queryClient = useQueryClient();
  const { notifySuccess, notifyError, notifyErrorMessage } = useFeedback();

  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: FIRST_GRID_PAGE, pageSize: DEFAULT_PAGE_SIZE });
  const [sortModel, setSortModel] = useState<GridSortModel>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [group, setGroup] = useState('');
  const [school, setSchool] = useState('');
  const [accessible, setAccessible] = useState<AccessibleFilter>(AccessibleFilter.Any);
  const [toDelete, setToDelete] = useState<ParticipantDto | null>(null);
  const [confirmDeleteAll, setConfirmDeleteAll] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput.trim()), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const resetPage = () => setPaginationModel((model) => ({ ...model, page: FIRST_GRID_PAGE }));

  const params: ParticipantsPageParams = {
    page: paginationModel.page + FIRST_PAGE,
    pageSize: paginationModel.pageSize,
    search: search || undefined,
    group: group.trim() || undefined,
    school: school.trim() || undefined,
    needsAccessibleSeat: toAccessibleParam(accessible),
    ...toParticipantsSort(sortModel),
  };

  const { data, isFetching } = useParticipantsPageQuery(sessionId, params);
  const attributes = useAttributesQuery(sessionId);
  const total = data?.total ?? 0;

  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: [QueryKey.Participants, sessionId] }),
      queryClient.invalidateQueries({ queryKey: queryKeys.participants.attributes(sessionId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.sessions.totals(sessionId) }),
    ]);

  const editMutation = useMutation({
    mutationKey: [MutationKey.EditParticipant],
    mutationFn: (dto: ParticipantDto) => editParticipant(sessionId, dto),
  });

  const deleteMutation = useMutation({
    mutationKey: [MutationKey.DeleteParticipant],
    mutationFn: (participantId: string) => deleteParticipant(sessionId, participantId),
    onSuccess: async () => {
      await invalidate();
      notifySuccess(t('messages.deleted'));
    },
    onError: notifyError,
  });

  const deleteAllMutation = useMutation({
    mutationKey: [MutationKey.DeleteAllParticipants],
    mutationFn: () => deleteAllParticipants(sessionId),
    onSuccess: async () => {
      resetPage();
      await invalidate();
      notifySuccess(t('messages.deletedAll'));
    },
    onError: notifyError,
  });

  const processRowUpdate = async (newRow: ParticipantDto, oldRow: ParticipantDto) => {
    const dto = toEditedParticipant(newRow);
    if (!dto) {
      throw new ParticipantRowError(t('messages.nameRequired'));
    }
    if (JSON.stringify(dto) === JSON.stringify(oldRow)) {
      return oldRow;
    }
    const saved = await editMutation.mutateAsync(dto);
    notifySuccess(t('messages.updated'));
    void invalidate();
    return saved;
  };

  const handleRowUpdateError = (error: unknown) => (error instanceof ParticipantRowError ? notifyErrorMessage(error.message) : notifyError(error));

  const attributeKeys = useMemo(() => (attributes.data ?? []).map((info) => info.key ?? '').filter(Boolean), [attributes.data]);

  const textColumn = (field: ParticipantColumn, width: number): GridColDef<ParticipantDto> => ({
    field,
    headerName: t(`list.columns.${field}`),
    width,
    editable: true,
    sortable: Boolean(sortFieldByColumn[field]),
  });

  const columns: GridColDef<ParticipantDto>[] = [
    textColumn(ParticipantColumn.ExternalId, TEXT_WIDTH_PX),
    textColumn(ParticipantColumn.LastName, NAME_WIDTH_PX),
    textColumn(ParticipantColumn.FirstName, NAME_WIDTH_PX),
    textColumn(ParticipantColumn.MiddleName, NAME_WIDTH_PX),
    textColumn(ParticipantColumn.Group, TEXT_WIDTH_PX),
    textColumn(ParticipantColumn.School, SCHOOL_WIDTH_PX),
    { ...textColumn(ParticipantColumn.NeedsAccessibleSeat, FLAG_WIDTH_PX), type: 'boolean' },
    ...attributeKeys.map<GridColDef<ParticipantDto>>((key) => ({
      field: attributeColumnField(key),
      headerName: key,
      width: TEXT_WIDTH_PX,
      editable: true,
      sortable: false,
      valueGetter: (_value, row) => row.attributes?.[key] ?? '',
      valueSetter: (value: string, row) => ({ ...row, attributes: { ...row.attributes, [key]: value } }),
    })),
    textColumn(ParticipantColumn.Notes, SCHOOL_WIDTH_PX),
    {
      field: ParticipantColumn.Actions,
      type: 'actions',
      headerName: t('list.columns.actions'),
      width: ACTIONS_WIDTH_PX,
      getActions: ({ row }) => [
        <GridActionsCellItem
          key={ParticipantColumn.Actions}
          icon={<DeleteIcon />}
          label={t('list.deleteTitle')}
          data-qa={ParticipantsTestId.DeleteButton}
          onClick={() => setToDelete(row)}
        />,
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
        <Stack>
          <Typography variant="h5">{t('list.title')}</Typography>
          <Typography color="text.secondary" data-qa={ParticipantsTestId.Total}>
            {t('list.total', { count: total })}
          </Typography>
        </Stack>
        <Stack direction="row" spacing={1}>
          <Button variant="contained" startIcon={<UploadFileIcon />} component={RouterLink} to={routes.participants.import(sessionId)} data-qa={ParticipantsTestId.ImportButton}>
            {t('list.import')}
          </Button>
          <Button color="error" startIcon={<DeleteSweepIcon />} disabled={total === 0} onClick={() => setConfirmDeleteAll(true)} data-qa={ParticipantsTestId.DeleteAllButton}>
            {t('list.deleteAll')}
          </Button>
        </Stack>
      </Stack>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        <TextField
          size="small"
          label={t('list.search')}
          value={searchInput}
          onChange={(event) => {
            setSearchInput(event.target.value);
            resetPage();
          }}
          slotProps={{ htmlInput: { 'data-qa': ParticipantsTestId.Search } }}
        />
        <TextField
          size="small"
          label={t('list.group')}
          value={group}
          onChange={(event) => {
            setGroup(event.target.value);
            resetPage();
          }}
          slotProps={{ htmlInput: { 'data-qa': ParticipantsTestId.GroupFilter } }}
        />
        <TextField
          size="small"
          label={t('list.school')}
          value={school}
          onChange={(event) => {
            setSchool(event.target.value);
            resetPage();
          }}
          slotProps={{ htmlInput: { 'data-qa': ParticipantsTestId.SchoolFilter } }}
        />
        <TextField
          select
          size="small"
          label={t('list.accessible')}
          value={accessible}
          sx={{ minWidth: FILTER_WIDTH_PX }}
          onChange={(event) => {
            setAccessible(event.target.value as AccessibleFilter);
            resetPage();
          }}
          slotProps={{ select: { SelectDisplayProps: { 'data-qa': ParticipantsTestId.AccessibleFilter } as React.HTMLAttributes<HTMLDivElement> } }}>
          {Object.values(AccessibleFilter).map((value) => (
            <MenuItem key={value} value={value}>
              {t(`list.accessibleFilter.${value}`)}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <Box sx={{ height: GRID_HEIGHT_PX }} data-qa={ParticipantsTestId.Grid}>
        <DataGrid
          rows={data?.items ?? []}
          columns={columns}
          getRowId={(row) => row.id ?? ''}
          loading={isFetching}
          rowCount={total}
          paginationMode={GRID_SERVER_MODE}
          sortingMode={GRID_SERVER_MODE}
          filterMode={GRID_SERVER_MODE}
          disableColumnFilter
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={PAGE_SIZE_OPTIONS}
          sortModel={sortModel}
          onSortModelChange={(model) => {
            setSortModel(model);
            resetPage();
          }}
          processRowUpdate={processRowUpdate}
          onProcessRowUpdateError={handleRowUpdateError}
          disableRowSelectionOnClick
          localeText={{ noRowsLabel: t('list.empty') }}
        />
      </Box>

      <ConfirmDialog
        open={Boolean(toDelete)}
        title={t('list.deleteTitle')}
        message={t('list.deleteMessage', { name: toDelete ? fullName(toDelete) : '' })}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
      <ConfirmDialog
        open={confirmDeleteAll}
        title={t('list.deleteAllTitle')}
        message={t('list.deleteAllMessage', { count: total })}
        onConfirm={() => {
          setConfirmDeleteAll(false);
          deleteAllMutation.mutate();
        }}
        onCancel={() => setConfirmDeleteAll(false)}
      />
    </Stack>
  );
};

export default ParticipantsList;
