import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import FactCheckIcon from '@mui/icons-material/FactCheck';
import { Box, Button, Chip, IconButton, Stack, Switch, Tooltip, Typography } from '@mui/material';
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { DiagnosticDto, RuleDto } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { RulesTestId } from '../../../../enums/Rules';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import { useRulesListQuery } from '../../Queries/rules';
import DiagnosticsPanel from '../../SolveRuns/Common/DiagnosticsPanel';
import { runPrecheck } from '../../SolveRuns/Common/mutations';
import { editRule } from '../Edit/mutations';
import { deleteRule } from './mutations';

interface Props {
  sessionId: string;
  onCreate: () => void;
  onEdit: (ruleId: string) => void;
}

const NAME_MIN_WIDTH_PX = 220;
const COLUMN_WIDTH_PX = 140;
const ACTIONS_WIDTH_PX = 120;

const RulesList = ({ sessionId, onCreate, onEdit }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { data: rules = [], isLoading } = useRulesListQuery(sessionId);
  const [toDelete, setToDelete] = useState<RuleDto | null>(null);
  const [diagnostics, setDiagnostics] = useState<DiagnosticDto[] | null>(null);

  const notifyError = (error: unknown) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR }));
  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.rules.list(sessionId) });
  const ruleName = (rule: RuleDto) => rule.displayName || (rule.type ? t(`ruleType.${rule.type}`) : '');

  const toggle = useMutation({
    mutationKey: [MutationKey.EditRule],
    mutationFn: (rule: RuleDto) => editRule(sessionId, { ...rule, isEnabled: !rule.isEnabled }),
    onSuccess: (saved) => {
      void invalidate();
      enqueueSnackbar(saved.isEnabled ? t('messages.enabled') : t('messages.disabled'), { variant: SNACKBAR_SUCCESS });
    },
    onError: notifyError,
  });

  const removal = useMutation({
    mutationKey: [MutationKey.DeleteRule],
    mutationFn: (ruleId: string) => deleteRule(sessionId, ruleId),
    onSuccess: () => {
      void invalidate();
      enqueueSnackbar(t('messages.deleted'), { variant: SNACKBAR_SUCCESS });
    },
    onError: notifyError,
    onSettled: () => setToDelete(null),
  });

  const precheck = useMutation({
    mutationKey: [MutationKey.RunPrecheck],
    mutationFn: () => runPrecheck(sessionId),
    onSuccess: setDiagnostics,
    onError: notifyError,
  });

  const columns: GridColDef<RuleDto>[] = [
    { field: 'displayName', headerName: t('list.name'), flex: 1, minWidth: NAME_MIN_WIDTH_PX, valueGetter: (_value, row) => ruleName(row) },
    { field: 'type', headerName: t('fields.type'), flex: 1, minWidth: NAME_MIN_WIDTH_PX, valueGetter: (_value, row) => (row.type ? t(`ruleType.${row.type}`) : '') },
    {
      field: 'isHard',
      headerName: t('fields.kind'),
      width: COLUMN_WIDTH_PX,
      renderCell: ({ row }) => <Chip size="small" color={row.isHard ? 'error' : 'info'} label={row.isHard ? t('list.hardChip') : t('list.softChip')} />,
    },
    { field: 'weight', headerName: t('fields.weight'), width: COLUMN_WIDTH_PX, valueGetter: (_value, row) => (row.isHard ? t('list.noWeight') : String(row.weight ?? '')) },
    {
      field: 'isEnabled',
      headerName: t('fields.isEnabled'),
      width: COLUMN_WIDTH_PX,
      renderCell: ({ row }) => (
        <Switch
          checked={Boolean(row.isEnabled)}
          onChange={() => toggle.mutate(row)}
          slotProps={{ input: { 'aria-label': `${t('fields.isEnabled')}: ${ruleName(row)}`, 'data-qa': RulesTestId.EnabledSwitch } as object }}
        />
      ),
    },
    {
      field: 'actions',
      headerName: '',
      sortable: false,
      width: ACTIONS_WIDTH_PX,
      renderCell: ({ row }) => (
        <Box>
          <Tooltip title={t('actions.edit')}>
            <IconButton aria-label={t('actions.edit')} data-qa={RulesTestId.EditButton} onClick={() => row.id && onEdit(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title={t('actions.delete')}>
            <IconButton aria-label={t('actions.delete')} data-qa={RulesTestId.DeleteButton} onClick={() => setToDelete(row)}>
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5">{t('title')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" startIcon={<FactCheckIcon />} loading={precheck.isPending} onClick={() => precheck.mutate()} data-qa={RulesTestId.PrecheckButton}>
            {t('actions.precheck')}
          </Button>
          <Button variant="contained" startIcon={<AddIcon />} onClick={onCreate} data-qa={RulesTestId.CreateButton}>
            {t('actions.create')}
          </Button>
        </Stack>
      </Stack>
      <Box data-qa={RulesTestId.List}>
        <DataGrid
          autoHeight
          disableVirtualization
          rows={rules}
          columns={columns}
          loading={isLoading}
          getRowId={(row) => row.id ?? ''}
          disableRowSelectionOnClick
          hideFooter
          localeText={{ noRowsLabel: t('list.empty') }}
        />
      </Box>
      {diagnostics && <DiagnosticsPanel diagnostics={diagnostics} sessionId={sessionId} rules={rules} />}
      <ConfirmDialog
        open={Boolean(toDelete)}
        title={t('messages.deleteTitle')}
        message={t('messages.deleteMessage', { name: toDelete ? ruleName(toDelete) : '' })}
        onConfirm={() => toDelete?.id && removal.mutate(toDelete.id)}
        onCancel={() => setToDelete(null)}
      />
    </Stack>
  );
};

export default RulesList;
