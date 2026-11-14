import { Stack, TextField } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { SessionConstraint, SessionsTestId } from '../../../../../enums/Sessions';
import { I18nNamespace } from '../../../../../i18n';
import { SessionReducerAction, type SessionAction, type SessionState, type SessionValidationErrors } from '..';

const DESCRIPTION_ROWS = 4;

interface Props {
  state: SessionState;
  dispatch: React.Dispatch<SessionAction>;
  errors: SessionValidationErrors;
}

const StepDetails = ({ state, dispatch, errors }: Props) => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  return (
    <Stack spacing={2}>
      <TextField
        label={t('form.fields.name')}
        value={state.name}
        required
        onChange={(event) => dispatch({ type: SessionReducerAction.Name, payload: { name: event.target.value } })}
        error={Boolean(errors.name)}
        helperText={errors.name ? t(`validation.${errors.name}`, { max: SessionConstraint.NameMaxLength }) : undefined}
        slotProps={{ htmlInput: { 'data-qa': SessionsTestId.NameField } }}
      />
      <TextField
        label={t('form.fields.examDate')}
        type="date"
        value={state.examDate}
        onChange={(event) => dispatch({ type: SessionReducerAction.ExamDate, payload: { examDate: event.target.value } })}
        slotProps={{ inputLabel: { shrink: true }, htmlInput: { 'data-qa': SessionsTestId.ExamDateField } }}
      />
      <TextField
        label={t('form.fields.description')}
        value={state.description}
        multiline
        minRows={DESCRIPTION_ROWS}
        onChange={(event) => dispatch({ type: SessionReducerAction.Description, payload: { description: event.target.value } })}
        error={Boolean(errors.description)}
        helperText={errors.description ? t(`validation.${errors.description}`, { max: SessionConstraint.DescriptionMaxLength }) : undefined}
        slotProps={{ htmlInput: { 'data-qa': SessionsTestId.DescriptionField } }}
      />
    </Stack>
  );
};

export default StepDetails;
