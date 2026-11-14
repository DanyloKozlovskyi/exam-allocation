import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import { Box, Button, Stack, Typography } from '@mui/material';
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { SolveRunDto } from '../../../../api/dtos';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { I18nNamespace } from '../../../../i18n';
import { useSolveRunsListQuery } from '../../Queries/solveRuns';
import { durationSeconds, formatDateTime } from '../Common/formatters';
import StatusChip from '../Common/StatusChip';
import RunDialog from '../RunDialog';

interface Props {
  sessionId: string;
  onOpen: (runId: string) => void;
}

const DATE_WIDTH_PX = 180;
const STATUS_MIN_WIDTH_PX = 260;
const NUMBER_WIDTH_PX = 140;

const SolveRunsList = ({ sessionId, onOpen }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  const { data: runs = [], isLoading } = useSolveRunsListQuery(sessionId);
  const [dialogOpen, setDialogOpen] = useState(false);

  const columns: GridColDef<SolveRunDto>[] = [
    { field: 'createdAt', headerName: t('list.createdAt'), width: DATE_WIDTH_PX, valueFormatter: (value: string) => formatDateTime(value) },
    { field: 'status', headerName: t('list.status'), flex: 1, minWidth: STATUS_MIN_WIDTH_PX, renderCell: ({ row }) => <StatusChip status={row.status} isStale={row.isStale} /> },
    {
      field: 'duration',
      headerName: t('list.duration'),
      width: NUMBER_WIDTH_PX,
      sortable: false,
      valueGetter: (_value, row) => {
        const seconds = durationSeconds(row.startedAt, row.finishedAt);
        return seconds === null ? '' : t('list.durationValue', { seconds });
      },
    },
    { field: 'allocations', headerName: t('list.alternatives'), width: NUMBER_WIDTH_PX, type: 'number', valueGetter: (_value, row) => row.allocationIds?.length ?? 0 },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5">{t('title')}</Typography>
        <Button variant="contained" startIcon={<PlayArrowIcon />} onClick={() => setDialogOpen(true)} data-qa={SolveRunsTestId.RunButton}>
          {t('actions.run')}
        </Button>
      </Stack>
      <Box data-qa={SolveRunsTestId.List}>
        <DataGrid
          autoHeight
          disableVirtualization
          rows={runs}
          columns={columns}
          loading={isLoading}
          getRowId={(row) => row.id ?? ''}
          onRowClick={({ row }) => row.id && onOpen(row.id)}
          disableRowSelectionOnClick
          hideFooter
          localeText={{ noRowsLabel: t('list.empty') }}
          sx={{ '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Box>
      <RunDialog
        open={dialogOpen}
        sessionId={sessionId}
        onClose={() => setDialogOpen(false)}
        onStarted={(run) => {
          setDialogOpen(false);
          if (run.id) onOpen(run.id);
        }}
      />
    </Stack>
  );
};

export default SolveRunsList;
