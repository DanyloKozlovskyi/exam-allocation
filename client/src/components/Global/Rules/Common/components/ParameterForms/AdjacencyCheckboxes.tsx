import { Checkbox, FormControl, FormControlLabel, FormGroup, FormHelperText, FormLabel, Stack } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { AdjacencyKind } from '../../../../../../api/dtos';
import { RulesTestId } from '../../../../../../enums/Rules';
import { I18nNamespace } from '../../../../../../i18n';
import { RuleField } from '../..';
import { useFieldErrorText } from '../../useFieldErrorText';
import AdjacencyIllustration from './AdjacencyIllustration';
import type { ParametersFormProps } from './types';

const AdjacencyCheckboxes = ({ parameters, onChange, errors }: ParametersFormProps) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const { t: tRooms } = useTranslation(I18nNamespace.Rooms);
  const errorText = useFieldErrorText();
  const toggle = (kind: AdjacencyKind) =>
    onChange({ adjacency: parameters.adjacency.includes(kind) ? parameters.adjacency.filter((item) => item !== kind) : [...parameters.adjacency, kind] });

  return (
    <FormControl error={Boolean(errors[RuleField.Adjacency])} component="fieldset" data-qa={RulesTestId.AdjacencyField}>
      <FormLabel component="legend">{t('fields.adjacency')}</FormLabel>
      <FormGroup>
        {Object.values(AdjacencyKind).map((kind) => (
          <FormControlLabel
            key={kind}
            control={<Checkbox checked={parameters.adjacency.includes(kind)} onChange={() => toggle(kind)} />}
            label={
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <AdjacencyIllustration kind={kind} />
                <span>{tRooms(`adjacencyKind.${kind}`)}</span>
              </Stack>
            }
          />
        ))}
      </FormGroup>
      {errors[RuleField.Adjacency] && <FormHelperText>{errorText(errors[RuleField.Adjacency])}</FormHelperText>}
    </FormControl>
  );
};

export default AdjacencyCheckboxes;
