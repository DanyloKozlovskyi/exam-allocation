import { useState, type Dispatch } from 'react';
import { useTranslation } from 'react-i18next';
import type { AttributeInfoDto, RuleTypeInfoDto } from '../../../../../api/dtos';
import { RuleStep } from '../../../../../enums/Rules';
import { I18nNamespace } from '../../../../../i18n';
import CustomStepper from '../../../../Common/CustomStepper';
import { hasErrors, validateDetails, validateParameters, type RuleAction, type RuleState, type ValidationErrors } from '..';
import StepDetails from './StepDetails';
import StepParameters from './StepParameters';
import StepReview from './StepReview';

interface Props {
  state: RuleState;
  dispatch: Dispatch<RuleAction>;
  ruleTypes: RuleTypeInfoDto[];
  attributes: AttributeInfoDto[];
  isTypeLocked?: boolean;
  completeLabel: string;
  handleComplete: () => void;
}

const Steps = ({ state, dispatch, ruleTypes, attributes, isTypeLocked = false, completeLabel, handleComplete }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const [step, setStep] = useState<number>(RuleStep.Details);
  const [errors, setErrors] = useState<ValidationErrors>({});

  const validate = (current: number) => {
    const next = current === RuleStep.Details ? validateDetails(state) : current === RuleStep.Parameters ? validateParameters(state) : {};
    setErrors(next);
    return hasErrors(next);
  };

  const changeStep = (next: number) => {
    setErrors({});
    setStep(next);
  };

  return (
    <CustomStepper
      step={step}
      setStep={changeStep}
      validate={validate}
      completeLabel={completeLabel}
      handleComplete={handleComplete}
      stepperSteps={[
        { label: t('steps.details'), component: <StepDetails state={state} dispatch={dispatch} errors={errors} ruleTypes={ruleTypes} isTypeLocked={isTypeLocked} /> },
        { label: t('steps.parameters'), component: <StepParameters state={state} dispatch={dispatch} errors={errors} attributes={attributes} /> },
        { label: t('steps.summary'), component: <StepReview state={state} setStep={changeStep} /> },
      ]}
    />
  );
};

export default Steps;
