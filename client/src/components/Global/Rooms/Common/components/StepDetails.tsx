import { Checkbox, FormControlLabel, Grid, ListItemText, MenuItem, TextField } from '@mui/material';
import type { Dispatch } from 'react';
import { useTranslation } from 'react-i18next';
import { RoomsTestId } from '../../../../../enums/Rooms';
import { I18nNamespace } from '../../../../../i18n';
import { toDisplayNumber } from '../../../../Common/RoomGrid/seatKey';
import { ROOM_COST_STEP, ROOM_MAX_COLUMNS, ROOM_MAX_ROWS, ROOM_MAX_SEATS_PER_DESK, ROOM_MIN_COLUMNS, ROOM_MIN_ROWS, ROOM_MIN_SEATS_PER_DESK } from '../constants';
import { useFieldErrorText } from '../useFieldErrorText';
import { RoomField, RoomReducerAction, type RoomAction, type RoomState, type ValidationErrors } from '..';

interface Props {
  state: RoomState;
  dispatch: Dispatch<RoomAction>;
  errors: ValidationErrors;
}

const HALF_WIDTH = 6;
const THIRD_WIDTH = 4;

const StepDetails = ({ state, dispatch, errors }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const errorText = useFieldErrorText();
  const aisleOptions = Array.from({ length: Math.max(state.columns - 1, 0) }, (_, index) => index);

  const numberField = (
    field: RoomField,
    action: RoomReducerAction.Rows | RoomReducerAction.Columns | RoomReducerAction.SeatsPerDesk,
    value: number,
    min: number,
    max: number,
    qa: RoomsTestId
  ) => (
    <TextField
      fullWidth
      type="number"
      label={t(`fields.${field as RoomField.Rows | RoomField.Columns | RoomField.SeatsPerDesk}`)}
      value={Number.isNaN(value) ? '' : value}
      onChange={(event) => dispatch({ type: action, payload: parseInt(event.target.value, 10) })}
      error={Boolean(errors[field])}
      helperText={errorText(errors[field])}
      slotProps={{ htmlInput: { min, max, 'data-qa': qa } }}
    />
  );

  return (
    <Grid container spacing={2}>
      <Grid size={12}>
        <TextField
          fullWidth
          required
          label={t('fields.name')}
          value={state.name}
          onChange={(event) => dispatch({ type: RoomReducerAction.Name, payload: event.target.value })}
          error={Boolean(errors[RoomField.Name])}
          helperText={errorText(errors[RoomField.Name])}
          slotProps={{ htmlInput: { 'data-qa': RoomsTestId.NameField } }}
        />
      </Grid>
      <Grid size={THIRD_WIDTH}>{numberField(RoomField.Rows, RoomReducerAction.Rows, state.rows, ROOM_MIN_ROWS, ROOM_MAX_ROWS, RoomsTestId.RowsField)}</Grid>
      <Grid size={THIRD_WIDTH}>{numberField(RoomField.Columns, RoomReducerAction.Columns, state.columns, ROOM_MIN_COLUMNS, ROOM_MAX_COLUMNS, RoomsTestId.ColumnsField)}</Grid>
      <Grid size={THIRD_WIDTH}>
        {numberField(RoomField.SeatsPerDesk, RoomReducerAction.SeatsPerDesk, state.seatsPerDesk, ROOM_MIN_SEATS_PER_DESK, ROOM_MAX_SEATS_PER_DESK, RoomsTestId.SeatsPerDeskField)}
      </Grid>
      <Grid size={12}>
        <TextField
          select
          fullWidth
          label={t('fields.aisles')}
          value={state.aislesAfterColumns}
          disabled={aisleOptions.length === 0}
          onChange={(event) => {
            const value = event.target.value as unknown as number[];
            dispatch({ type: RoomReducerAction.Aisles, payload: value });
          }}
          slotProps={{
            htmlInput: { 'data-qa': RoomsTestId.AislesField },
            select: {
              multiple: true,
              renderValue: (selected) => (selected as number[]).map((column) => toDisplayNumber(column)).join(', '),
            },
          }}>
          {aisleOptions.map((column) => (
            <MenuItem key={column} value={column}>
              <Checkbox checked={state.aislesAfterColumns.includes(column)} />
              <ListItemText primary={t('fields.aisleAfter', { column: toDisplayNumber(column) })} />
            </MenuItem>
          ))}
        </TextField>
      </Grid>
      <Grid size={HALF_WIDTH}>
        <TextField
          fullWidth
          type="number"
          label={t('fields.costOverride')}
          value={state.costOverride ?? ''}
          onChange={(event) => dispatch({ type: RoomReducerAction.CostOverride, payload: event.target.value === '' ? null : Number(event.target.value) })}
          error={Boolean(errors[RoomField.CostOverride])}
          helperText={errorText(errors[RoomField.CostOverride]) ?? t('fields.costOverrideHelp')}
          slotProps={{ htmlInput: { min: 0, step: ROOM_COST_STEP, 'data-qa': RoomsTestId.CostField } }}
        />
      </Grid>
      <Grid size={HALF_WIDTH}>
        <FormControlLabel
          control={
            <Checkbox
              checked={state.isActive}
              onChange={(event) => dispatch({ type: RoomReducerAction.IsActive, payload: event.target.checked })}
              slotProps={{ input: { 'data-qa': RoomsTestId.IsActiveField } as object }}
            />
          }
          label={t('fields.isActive')}
        />
      </Grid>
    </Grid>
  );
};

export default StepDetails;
