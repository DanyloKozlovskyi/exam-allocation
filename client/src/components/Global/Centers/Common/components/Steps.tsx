import { useReducer, useState } from 'react';
import { useTranslation } from 'react-i18next';
import CustomStepper, { type StepperStep } from '../../../../Common/CustomStepper';
import { CenterStep } from '../../../../../enums/Centers';
import { I18nNamespace } from '../../../../../i18n';
import { hasErrors } from '../../../Sessions/Common/formHelpers';
import { centerReducer, validateCenter, type CenterState, type CenterValidationErrors } from '..';
import StepDetails from './StepDetails';
import StepReview from './StepReview';

interface Props {
  initialState: CenterState;
  completeLabel: string;
  onComplete: (state: CenterState) => void;
}

/** Details → Summary stepper shared by Create and Edit. */
const Steps = ({ initialState, completeLabel, onComplete }: Props) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const [state, dispatch] = useReducer(centerReducer, initialState);
  const [step, setStep] = useState<number>(CenterStep.Details);
  const [errors, setErrors] = useState<CenterValidationErrors>({});

  const validate = (current: number) => {
    if (current !== CenterStep.Details) {
      return false;
    }
    const result = validateCenter(state);
    setErrors(result);
    return hasErrors(result);
  };

  const stepperSteps: StepperStep[] = [
    { label: t('form.steps.details'), component: <StepDetails state={state} dispatch={dispatch} errors={errors} /> },
    { label: t('form.steps.summary'), component: <StepReview state={state} setStep={setStep} /> },
  ];

  return <CustomStepper step={step} setStep={setStep} stepperSteps={stepperSteps} completeLabel={completeLabel} validate={validate} handleComplete={() => onComplete(state)} />;
};

export default Steps;
