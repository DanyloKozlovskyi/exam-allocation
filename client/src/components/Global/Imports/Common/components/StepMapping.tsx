import { Alert, Checkbox, Grid, Stack, TextField, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { ParticipantField } from '../../../../../api/dtos';
import { ImportsTestId } from '../../../../../enums/Imports';
import { I18nNamespace } from '../../../../../i18n';
import { HALF_GRID_SIZE } from '../../../Sessions/Common/constants';
import { ATTRIBUTE_KEY_MAX_LENGTH, attributeHeaders, ImportReducerAction, MappingError, type ImportAction, type ImportState, type MappingValidationErrors } from '..';

const SELECT_FULL_WIDTH = true;
const HEADER_LABEL_WIDTH_PX = 160;

interface Props {
  state: ImportState;
  dispatch: React.Dispatch<ImportAction>;
  errors: MappingValidationErrors;
}

const StepMapping = ({ state, dispatch, errors }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);
  const headers = state.preview?.headers ?? [];
  const freeHeaders = attributeHeaders(state);

  return (
    <Stack spacing={3}>
      {errors.general === MappingError.NamesRequired && <Alert severity="error">{t('mapping.namesRequired')}</Alert>}
      {errors.general === MappingError.DuplicateHeader && <Alert severity="error">{t('mapping.duplicateHeader', { header: errors.duplicateHeader })}</Alert>}

      <Stack spacing={2}>
        <Typography variant="h6">{t('mapping.fieldsTitle')}</Typography>
        <Grid container spacing={2}>
          {Object.values(ParticipantField).map((field) => (
            <Grid key={field} size={HALF_GRID_SIZE}>
              <TextField
                select
                fullWidth={SELECT_FULL_WIDTH}
                label={t(`mapping.fields.${field}`)}
                value={state.fields[field] ?? ''}
                onChange={(event) => dispatch({ type: ImportReducerAction.Field, payload: {}, field, value: event.target.value })}
                slotProps={{ select: { native: true }, inputLabel: { shrink: true }, htmlInput: { 'data-qa': `${ImportsTestId.MappingField}-${field}` } }}>
                <option value="">{t('mapping.notMapped')}</option>
                {headers.map((header) => (
                  <option key={header} value={header}>
                    {header}
                  </option>
                ))}
              </TextField>
            </Grid>
          ))}
        </Grid>
      </Stack>

      <Stack spacing={1}>
        <Typography variant="h6">{t('mapping.attributesTitle')}</Typography>
        <Typography color="text.secondary" variant="body2">
          {t('mapping.attributesHint')}
        </Typography>
        {freeHeaders.length === 0 && <Typography variant="body2">{t('mapping.noAttributes')}</Typography>}
        {freeHeaders.map((header) => {
          const attribute = state.attributes[header] ?? { key: '', enabled: false };
          const error = errors.attributeKeys[header];
          return (
            <Stack key={header} direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Checkbox
                checked={attribute.enabled}
                onChange={(event) => dispatch({ type: ImportReducerAction.Attribute, payload: {}, header, attribute: { enabled: event.target.checked } })}
                slotProps={{ input: { 'aria-label': header, 'data-qa': `${ImportsTestId.AttributeToggle}-${header}` } as React.InputHTMLAttributes<HTMLInputElement> }}
              />
              <Typography sx={{ minWidth: HEADER_LABEL_WIDTH_PX }}>{header}</Typography>
              <TextField
                size="small"
                label={t('mapping.attributeKey')}
                value={attribute.key}
                disabled={!attribute.enabled}
                onChange={(event) => dispatch({ type: ImportReducerAction.Attribute, payload: {}, header, attribute: { key: event.target.value } })}
                error={Boolean(error)}
                helperText={error ? t(`validation.${error}`, { max: ATTRIBUTE_KEY_MAX_LENGTH }) : undefined}
                slotProps={{ htmlInput: { 'data-qa': `${ImportsTestId.AttributeKeyField}-${header}` } }}
              />
            </Stack>
          );
        })}
      </Stack>
    </Stack>
  );
};

export default StepMapping;
