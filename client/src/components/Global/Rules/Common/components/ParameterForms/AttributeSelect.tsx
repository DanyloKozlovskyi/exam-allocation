import { MenuItem, TextField } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { RulesTestId } from '../../../../../../enums/Rules';
import { I18nNamespace } from '../../../../../../i18n';
import { RuleField } from '../..';
import { useFieldErrorText } from '../../useFieldErrorText';
import type { ParametersFormProps } from './types';
import { useAttributeLabel } from '../../useAttributeLabel';

const AttributeSelect = ({ parameters, onChange, attributes, errors }: ParametersFormProps) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const errorText = useFieldErrorText();
  const label = useAttributeLabel();
  const keys = attributes.map((attribute) => attribute.key ?? '').filter(Boolean);
  const options = parameters.attributeKey && !keys.includes(parameters.attributeKey) ? [parameters.attributeKey, ...keys] : keys;

  return (
    <TextField
      select
      fullWidth
      label={t('fields.attributeKey')}
      value={parameters.attributeKey}
      onChange={(event) => onChange({ attributeKey: event.target.value })}
      error={Boolean(errors[RuleField.AttributeKey])}
      helperText={errorText(errors[RuleField.AttributeKey])}
      slotProps={{ htmlInput: { 'data-qa': RulesTestId.AttributeField } }}>
      {options.map((key) => {
        const info = attributes.find((attribute) => attribute.key === key);
        return (
          <MenuItem key={key} value={key}>
            {info?.distinctValues !== undefined ? t('fields.attributeOption', { name: label(key), count: info.distinctValues }) : label(key)}
          </MenuItem>
        );
      })}
    </TextField>
  );
};

export default AttributeSelect;
