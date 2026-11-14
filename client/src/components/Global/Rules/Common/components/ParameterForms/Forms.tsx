import { FormControlLabel, MenuItem, Stack, Switch, TextField, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { AdjacencyKind, RuleScope } from '../../../../../../api/dtos';
import { RulesTestId } from '../../../../../../enums/Rules';
import { I18nNamespace } from '../../../../../../i18n';
import { RuleField } from '../..';
import { PERCENT_FACTOR, RULE_MAX_FILL_PERCENT, RULE_MIN_FILL_PERCENT, RULE_MIN_PER_SCOPE } from '../../constants';
import { useFieldErrorText } from '../../useFieldErrorText';
import AdjacencyCheckboxes from './AdjacencyCheckboxes';
import AttributeSelect from './AttributeSelect';
import type { ParametersFormProps } from './types';

export const SeparateByAttributeForm = (props: ParametersFormProps) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const errorText = useFieldErrorText();
  const { parameters, onChange, errors } = props;
  return (
    <Stack spacing={2}>
      <AttributeSelect {...props} />
      <TextField
        select
        label={t('fields.scope')}
        value={parameters.scope}
        onChange={(event) => onChange({ scope: event.target.value as RuleScope })}
        slotProps={{ htmlInput: { 'data-qa': RulesTestId.ScopeField } }}>
        {Object.values(RuleScope).map((scope) => (
          <MenuItem key={scope} value={scope}>
            {t(`scope.${scope}`)}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        type="number"
        label={t('fields.maxPerScope')}
        value={Number.isNaN(parameters.maxPerScope) ? '' : parameters.maxPerScope}
        onChange={(event) => onChange({ maxPerScope: parseInt(event.target.value, 10) })}
        error={Boolean(errors[RuleField.MaxPerScope])}
        helperText={errorText(errors[RuleField.MaxPerScope])}
        slotProps={{ htmlInput: { min: RULE_MIN_PER_SCOPE, 'data-qa': RulesTestId.MaxPerScopeField } }}
      />
    </Stack>
  );
};

export const NoAdjacentByAttributeForm = (props: ParametersFormProps) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const { parameters, onChange } = props;
  return (
    <Stack spacing={2}>
      <AttributeSelect {...props} />
      <AdjacencyCheckboxes {...props} />
      {parameters.adjacency.includes(AdjacencyKind.Side) && (
        <FormControlLabel
          control={
            <Switch
              checked={parameters.seatLevelSide}
              onChange={(event) => onChange({ seatLevelSide: event.target.checked })}
              slotProps={{ input: { 'data-qa': RulesTestId.SeatLevelSideField } as object }}
            />
          }
          label={t('fields.seatLevelSide')}
        />
      )}
    </Stack>
  );
};

export const MinRoomFillForm = ({ parameters, onChange, errors }: ParametersFormProps) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const errorText = useFieldErrorText();
  const percent = Math.round(parameters.minFillRatio * PERCENT_FACTOR);
  return (
    <TextField
      type="number"
      label={t('fields.minFillRatio')}
      value={Number.isNaN(percent) ? '' : percent}
      onChange={(event) => onChange({ minFillRatio: parseFloat(event.target.value) / PERCENT_FACTOR })}
      error={Boolean(errors[RuleField.MinFillRatio])}
      helperText={errorText(errors[RuleField.MinFillRatio])}
      slotProps={{ htmlInput: { min: RULE_MIN_FILL_PERCENT, max: RULE_MAX_FILL_PERCENT, 'data-qa': RulesTestId.MinFillRatioField } }}
    />
  );
};

export const NoParametersForm = () => {
  const { t } = useTranslation(I18nNamespace.Rules);
  return (
    <Typography color="text.secondary" data-qa={RulesTestId.NoParameters}>
      {t('noParameters')}
    </Typography>
  );
};
