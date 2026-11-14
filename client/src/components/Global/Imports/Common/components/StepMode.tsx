import { Checkbox, FormControl, FormControlLabel, FormLabel, Radio, RadioGroup, Stack } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { DedupeStrategy, ImportMode } from '../../../../../api/dtos';
import { ImportsTestId } from '../../../../../enums/Imports';
import { I18nNamespace } from '../../../../../i18n';
import { ImportReducerAction, type ImportAction, type ImportState } from '..';

interface Props {
  state: ImportState;
  dispatch: React.Dispatch<ImportAction>;
}

const StepMode = ({ state, dispatch }: Props) => {
  const { t } = useTranslation(I18nNamespace.Imports);
  return (
    <Stack spacing={3}>
      <FormControl>
        <FormLabel>{t('mode.modeTitle')}</FormLabel>
        <RadioGroup
          value={state.mode}
          onChange={(event) => dispatch({ type: ImportReducerAction.Mode, payload: { mode: event.target.value as ImportMode } })}
          data-qa={ImportsTestId.ModeGroup}>
          {Object.values(ImportMode).map((mode) => (
            <FormControlLabel key={mode} value={mode} control={<Radio />} label={t(`mode.modes.${mode}`)} />
          ))}
        </RadioGroup>
      </FormControl>
      <FormControl>
        <FormLabel>{t('mode.dedupeTitle')}</FormLabel>
        <RadioGroup
          value={state.dedupeBy}
          onChange={(event) => dispatch({ type: ImportReducerAction.DedupeBy, payload: { dedupeBy: event.target.value as DedupeStrategy } })}
          data-qa={ImportsTestId.DedupeGroup}>
          {Object.values(DedupeStrategy).map((strategy) => (
            <FormControlLabel key={strategy} value={strategy} control={<Radio />} label={t(`mode.dedupe.${strategy}`)} />
          ))}
        </RadioGroup>
      </FormControl>
      <FormControlLabel
        control={
          <Checkbox
            checked={state.skipInvalid}
            onChange={(event) => dispatch({ type: ImportReducerAction.SkipInvalid, payload: { skipInvalid: event.target.checked } })}
            slotProps={{ input: { 'data-qa': ImportsTestId.SkipInvalid } as React.InputHTMLAttributes<HTMLInputElement> }}
          />
        }
        label={t('mode.skipInvalid')}
      />
    </Stack>
  );
};

export default StepMode;
