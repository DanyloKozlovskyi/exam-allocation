import { Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ImportLimit, ImportsTestId } from '../../../../../enums/Imports';
import { I18nNamespace } from '../../../../../i18n';
import { ImportReducerAction, type ImportAction, type ImportState } from '..';

interface Props {
  state: ImportState;
  dispatch: React.Dispatch<ImportAction>;
}

const StepSheet = ({ state, dispatch }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);
  const headers = state.preview?.headers ?? [];
  const rows = state.preview?.sampleRows ?? [];
  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          select
          label={t('sheet.sheet')}
          value={state.sheet}
          onChange={(event) => dispatch({ type: ImportReducerAction.Sheet, payload: { sheet: event.target.value } })}
          slotProps={{ select: { native: true }, htmlInput: { 'data-qa': ImportsTestId.SheetSelect } }}>
          {(state.preview?.sheets ?? []).map((sheet) => (
            <option key={sheet} value={sheet}>
              {sheet}
            </option>
          ))}
        </TextField>
        <TextField
          type="number"
          label={t('sheet.headerRow')}
          helperText={t('sheet.headerRowHint')}
          value={state.headerRow}
          onChange={(event) => dispatch({ type: ImportReducerAction.HeaderRow, payload: { headerRow: event.target.value } })}
          slotProps={{ htmlInput: { min: ImportLimit.FirstHeaderRow, max: ImportLimit.MaxHeaderRow, 'data-qa': ImportsTestId.HeaderRowField } }}
        />
      </Stack>
      <Typography variant="subtitle2">{t('sheet.preview')}</Typography>
      <TableContainer data-qa={ImportsTestId.SamplePreview}>
        <Table size="small">
          <TableHead>
            <TableRow>
              {headers.map((header) => (
                <TableCell key={header}>{header}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row, rowIndex) => (
              <TableRow key={rowIndex}>
                {headers.map((header, columnIndex) => (
                  <TableCell key={header}>{row[columnIndex]}</TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
};

export default StepSheet;
