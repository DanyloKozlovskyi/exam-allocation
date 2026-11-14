import { FormControlLabel, Grid, MenuItem, Radio, RadioGroup, Switch, TextField, Typography, FormControl, FormLabel } from '@mui/material';
import type { Dispatch } from 'react';
import { useTranslation } from 'react-i18next';
import { RuleType, type RuleTypeInfoDto } from '../../../../../api/dtos';
import { RulesTestId } from '../../../../../enums/Rules';
import { I18nNamespace } from '../../../../../i18n';
import { RULE_MAX_WEIGHT, RULE_MIN_WEIGHT } from '../constants';
import { findTypeInfo } from '../parameters';
import { useFieldErrorText } from '../useFieldErrorText';
import { RuleField, RuleReducerAction, type RuleAction, type RuleState, type ValidationErrors } from '..';

interface Props {
  state: RuleState;
  dispatch: Dispatch<RuleAction>;
  errors: ValidationErrors;
  ruleTypes: RuleTypeInfoDto[];
  /** Type can't be changed when editing. */
  isTypeLocked: boolean;
}

enum RuleKind {
  Hard = 'hard',
  Soft = 'soft',
}

const HALF_WIDTH = 6;

const StepDetails = ({ state, dispatch, errors, ruleTypes, isTypeLocked }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const errorText = useFieldErrorText();
  const info = findTypeInfo(ruleTypes, state.type);
  const types = ruleTypes.length ? ruleTypes.map((item) => item.type).filter((type): type is RuleType => Boolean(type)) : Object.values(RuleType);

  return (
    <Grid container spacing={2}>
      <Grid size={12}>
        <TextField
          select
          fullWidth
          label={t('fields.type')}
          value={state.type}
          disabled={isTypeLocked}
          onChange={(event) => {
            const type = event.target.value as RuleType;
            dispatch({ type: RuleReducerAction.Type, payload: { type, info: findTypeInfo(ruleTypes, type) } });
          }}
          helperText={t(`ruleTypeDescription.${state.type}`)}
          slotProps={{ htmlInput: { 'data-qa': RulesTestId.TypeField } }}>
          {types.map((type) => (
            <MenuItem key={type} value={type}>
              {t(`ruleType.${type}`)}
            </MenuItem>
          ))}
        </TextField>
      </Grid>
      <Grid size={12}>
        <TextField
          fullWidth
          label={t('fields.displayName')}
          value={state.displayName}
          onChange={(event) => dispatch({ type: RuleReducerAction.DisplayName, payload: event.target.value })}
          error={Boolean(errors[RuleField.DisplayName])}
          helperText={errorText(errors[RuleField.DisplayName])}
          slotProps={{ htmlInput: { 'data-qa': RulesTestId.DisplayNameField } }}
        />
      </Grid>
      <Grid size={HALF_WIDTH}>
        <FormControl>
          <FormLabel>{t('fields.kind')}</FormLabel>
          <RadioGroup
            row
            value={state.isHard ? RuleKind.Hard : RuleKind.Soft}
            onChange={(event) => dispatch({ type: RuleReducerAction.IsHard, payload: event.target.value === RuleKind.Hard })}
            data-qa={RulesTestId.HardField}>
            <FormControlLabel value={RuleKind.Hard} control={<Radio />} label={t('fields.hard')} disabled={info?.allowsHard === false} />
            <FormControlLabel value={RuleKind.Soft} control={<Radio />} label={t('fields.soft')} disabled={info?.allowsSoft === false} />
          </RadioGroup>
        </FormControl>
      </Grid>
      <Grid size={HALF_WIDTH}>
        {state.isHard ? (
          <Typography color="text.secondary">{t('list.noWeight')}</Typography>
        ) : (
          <TextField
            fullWidth
            type="number"
            label={t('fields.weight')}
            value={Number.isNaN(state.weight) ? '' : state.weight}
            onChange={(event) => dispatch({ type: RuleReducerAction.Weight, payload: parseInt(event.target.value, 10) })}
            error={Boolean(errors[RuleField.Weight])}
            helperText={errorText(errors[RuleField.Weight]) ?? t('fields.weightHelp', { min: RULE_MIN_WEIGHT, max: RULE_MAX_WEIGHT })}
            slotProps={{ htmlInput: { min: RULE_MIN_WEIGHT, max: RULE_MAX_WEIGHT, 'data-qa': RulesTestId.WeightField } }}
          />
        )}
      </Grid>
      <Grid size={12}>
        <FormControlLabel
          control={
            <Switch
              checked={state.isEnabled}
              onChange={(event) => dispatch({ type: RuleReducerAction.IsEnabled, payload: event.target.checked })}
              slotProps={{ input: { 'data-qa': RulesTestId.EnabledField } as object }}
            />
          }
          label={t('fields.isEnabled')}
        />
      </Grid>
    </Grid>
  );
};

export default StepDetails;
