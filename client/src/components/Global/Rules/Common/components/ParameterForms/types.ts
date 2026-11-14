import type { AttributeInfoDto } from '../../../../../../api/dtos';
import type { RuleParametersState } from '../../parameters';
import type { ValidationErrors } from '../..';

export interface ParametersFormProps {
  parameters: RuleParametersState;
  onChange: (patch: Partial<RuleParametersState>) => void;
  attributes: AttributeInfoDto[];
  errors: ValidationErrors;
}
