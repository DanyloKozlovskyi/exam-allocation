import { Alert, Box, Chip, Stack } from '@mui/material';
import { DataGrid, type GridColDef } from '@mui/x-data-grid';
import { useTranslation } from 'react-i18next';
import { ImportErrorCode, type ImportErrorDto } from '../../../../../api/dtos';
import { ImportsTestId } from '../../../../../enums/Imports';
import { I18nNamespace } from '../../../../../i18n';
import type { ImportState } from '..';

const ERRORS_GRID_HEIGHT_PX = 320;
const ROW_WIDTH_PX = 100;
const COLUMN_FLEX = 1;
const CODE_FLEX = 2;

interface ErrorRow extends ImportErrorDto {
  id: number;
}

interface Props {
  state: ImportState;
}

const StepValidation = ({ state }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);
  const validation = state.validation;
  const errors = validation?.errors ?? [];
  const rows: ErrorRow[] = errors.map((error, index) => ({ ...error, id: index }));
  const countByCode = Object.values(ImportErrorCode)
    .map((code) => ({ code, count: errors.filter((error) => error.code === code).length }))
    .filter((item) => item.count > 0);

  const columns: GridColDef<ErrorRow>[] = [
    { field: 'row', headerName: t('validation.columns.row'), width: ROW_WIDTH_PX, type: 'number' },
    { field: 'column', headerName: t('validation.columns.column'), flex: COLUMN_FLEX },
    { field: 'code', headerName: t('validation.columns.code'), flex: CODE_FLEX, valueFormatter: (value: ImportErrorCode) => (value ? t(`validation.codes.${value}`) : '') },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }} data-qa={ImportsTestId.ValidationSummary}>
        <Chip color="success" label={t('validation.validRows', { count: validation?.validRows ?? 0 })} />
        <Chip color={(validation?.errorRows ?? 0) > 0 ? 'error' : 'default'} label={t('validation.errorRows', { count: validation?.errorRows ?? 0 })} />
        {(validation?.duplicates?.length ?? 0) > 0 && <Chip color="warning" label={t('validation.duplicates', { count: validation?.duplicates?.length ?? 0 })} />}
        {countByCode.map((item) => (
          <Chip key={item.code} variant="outlined" label={`${t(`validation.codes.${item.code}`)}: ${item.count}`} />
        ))}
      </Stack>
      {errors.length === 0 ? (
        <Alert severity="success">{t('validation.noErrors')}</Alert>
      ) : (
        <Box sx={{ height: ERRORS_GRID_HEIGHT_PX }} data-qa={ImportsTestId.ValidationErrors}>
          <DataGrid rows={rows} columns={columns} disableRowSelectionOnClick />
        </Box>
      )}
    </Stack>
  );
};

export default StepValidation;
