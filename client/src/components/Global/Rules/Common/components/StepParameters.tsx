import type { Dispatch } from 'react';
import type { AttributeInfoDto } from '../../../../../api/dtos';
import { RuleReducerAction, type RuleAction, type RuleState, type ValidationErrors } from '..';
import { PARAMETER_FORMS } from './ParameterForms';

interface Props {
  state: RuleState;
  dispatch: Dispatch<RuleAction>;
  errors: ValidationErrors;
  attributes: AttributeInfoDto[];
}

const StepParameters = ({ state, dispatch, errors, attributes }: Props) => {
  const Form = PARAMETER_FORMS[state.type];
  return <Form parameters={state.parameters} onChange={(patch) => dispatch({ type: RuleReducerAction.Parameters, payload: patch })} attributes={attributes} errors={errors} />;
};

export default StepParameters;
