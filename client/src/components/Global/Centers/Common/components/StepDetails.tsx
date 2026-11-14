import { FormControlLabel, Grid, Stack, Switch, TextField } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { CenterConstraint, CentersTestId } from '../../../../../enums/Centers';
import { I18nNamespace } from '../../../../../i18n';
import { HALF_GRID_SIZE } from '../../../Sessions/Common/constants';
import { CenterReducerAction, type CenterAction, type CenterState, type CenterValidationErrors } from '..';

const DECIMAL_INPUT_MODE = 'decimal';

interface Props {
  state: CenterState;
  dispatch: React.Dispatch<CenterAction>;
  errors: CenterValidationErrors;
}

const StepDetails = ({ state, dispatch, errors }: Props) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  return (
    <Stack spacing={2}>
      <TextField
        label={t('form.fields.name')}
        value={state.name}
        required
        onChange={(event) => dispatch({ type: CenterReducerAction.Name, payload: { name: event.target.value } })}
        error={Boolean(errors.name)}
        helperText={errors.name ? t(`validation.${errors.name}`, { max: CenterConstraint.NameMaxLength }) : undefined}
        slotProps={{ htmlInput: { 'data-qa': CentersTestId.NameField } }}
      />
      <TextField
        label={t('form.fields.address')}
        value={state.address}
        onChange={(event) => dispatch({ type: CenterReducerAction.Address, payload: { address: event.target.value } })}
        error={Boolean(errors.address)}
        helperText={errors.address ? t(`validation.${errors.address}`, { max: CenterConstraint.AddressMaxLength }) : undefined}
        slotProps={{ htmlInput: { 'data-qa': CentersTestId.AddressField } }}
      />
      <Grid container spacing={2}>
        <Grid size={HALF_GRID_SIZE}>
          <TextField
            fullWidth
            label={t('form.fields.latitude')}
            value={state.latitude}
            required
            onChange={(event) => dispatch({ type: CenterReducerAction.Latitude, payload: { latitude: event.target.value } })}
            error={Boolean(errors.latitude)}
            helperText={errors.latitude ? t(`validation.${errors.latitude}`) : undefined}
            slotProps={{ htmlInput: { 'data-qa': CentersTestId.LatitudeField, inputMode: DECIMAL_INPUT_MODE } }}
          />
        </Grid>
        <Grid size={HALF_GRID_SIZE}>
          <TextField
            fullWidth
            label={t('form.fields.longitude')}
            value={state.longitude}
            required
            onChange={(event) => dispatch({ type: CenterReducerAction.Longitude, payload: { longitude: event.target.value } })}
            error={Boolean(errors.longitude)}
            helperText={errors.longitude ? t(`validation.${errors.longitude}`) : undefined}
            slotProps={{ htmlInput: { 'data-qa': CentersTestId.LongitudeField, inputMode: DECIMAL_INPUT_MODE } }}
          />
        </Grid>
      </Grid>
      <TextField
        label={t('form.fields.costPerRoom')}
        value={state.costPerRoom}
        onChange={(event) => dispatch({ type: CenterReducerAction.CostPerRoom, payload: { costPerRoom: event.target.value } })}
        error={Boolean(errors.costPerRoom)}
        helperText={errors.costPerRoom ? t(`validation.${errors.costPerRoom}`) : t('form.fields.costHint')}
        slotProps={{ htmlInput: { 'data-qa': CentersTestId.CostField, inputMode: DECIMAL_INPUT_MODE } }}
      />
      <FormControlLabel
        control={
          <Switch
            checked={state.isActive}
            onChange={(event) => dispatch({ type: CenterReducerAction.IsActive, payload: { isActive: event.target.checked } })}
            slotProps={{ input: { 'data-qa': CentersTestId.ActiveField } as React.InputHTMLAttributes<HTMLInputElement> }}
          />
        }
        label={t('form.fields.isActive')}
      />
    </Stack>
  );
};

export default StepDetails;
