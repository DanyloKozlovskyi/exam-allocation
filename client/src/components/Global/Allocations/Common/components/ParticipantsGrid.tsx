import { Stack, TextField } from '@mui/material';
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { AllocationRowDto } from '../../../../../api/dtos';
import { DEFAULT_PAGE_SIZE, FIRST_PAGE } from '../../../../../constants/queryParams';
import { AllocationsTestId } from '../../../../../enums/Allocations';
import { I18nNamespace } from '../../../../../i18n';
import { parseSeatKey, toDisplayNumber } from '../../../../Common/RoomGrid/seatKey';
import { useAllocationParticipantsQuery } from '../../../Queries/allocations';
import { SEARCH_DEBOUNCE_MS } from '../../../../../constants/ui';

interface Props {
  allocationId: string;
}

const COLUMN_MIN_WIDTH_PX = 140;
const PAGE_SIZE_OPTIONS = [DEFAULT_PAGE_SIZE];

/** Who sits where — server-mode paging and search over `allocations/{id}/participants`. */
const ParticipantsGrid = ({ allocationId }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const [pagination, setPagination] = useState<GridPaginationModel>({ page: FIRST_PAGE - 1, pageSize: DEFAULT_PAGE_SIZE });
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPagination((current) => ({ ...current, page: FIRST_PAGE - 1 }));
    }, SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const { data, isFetching } = useAllocationParticipantsQuery(allocationId, { page: pagination.page + FIRST_PAGE, pageSize: pagination.pageSize, search });

  const seatLabel = (seatKey: string | undefined) => {
    const position = seatKey ? parseSeatKey(seatKey) : null;
    return position ? t('participants.seatLabel', { row: toDisplayNumber(position.row), column: toDisplayNumber(position.column), seat: toDisplayNumber(position.seatIndex) }) : '';
  };

  const column = (field: string, headerName: string, valueGetter: (row: AllocationRowDto) => string | null | undefined): GridColDef<AllocationRowDto> => ({
    field,
    headerName,
    flex: 1,
    minWidth: COLUMN_MIN_WIDTH_PX,
    sortable: false,
    valueGetter: (_value, row) => valueGetter(row) ?? '',
  });

  const columns = [
    column('lastName', t('participants.lastName'), (row) => row.participant?.lastName),
    column('firstName', t('participants.firstName'), (row) => row.participant?.firstName),
    column('middleName', t('participants.middleName'), (row) => row.participant?.middleName),
    column('group', t('participants.group'), (row) => row.participant?.group),
    column('school', t('participants.school'), (row) => row.participant?.school),
    column('center', t('participants.center'), (row) => row.centerName),
    column('room', t('participants.room'), (row) => row.roomName),
    column('seat', t('participants.seat'), (row) => seatLabel(row.seatKey)),
  ];

  return (
    <Stack spacing={2} data-qa={AllocationsTestId.ParticipantsGrid}>
      <TextField
        size="small"
        label={t('participants.search')}
        value={searchInput}
        onChange={(event) => setSearchInput(event.target.value)}
        slotProps={{ htmlInput: { 'data-qa': AllocationsTestId.SearchField } }}
      />
      <DataGrid
        autoHeight
        disableVirtualization
        rows={data?.items ?? []}
        columns={columns}
        getRowId={(row) => row.participant?.id ?? ''}
        loading={isFetching}
        paginationMode="server"
        rowCount={data?.total ?? 0}
        paginationModel={pagination}
        onPaginationModelChange={setPagination}
        pageSizeOptions={PAGE_SIZE_OPTIONS}
        disableRowSelectionOnClick
        disableColumnFilter
        localeText={{ noRowsLabel: t('participants.empty') }}
      />
    </Stack>
  );
};

export default ParticipantsGrid;
