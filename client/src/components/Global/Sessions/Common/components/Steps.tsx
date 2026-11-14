import { useReducer, useState } from 'react';
import { useTranslation } from 'react-i18next';
import CustomStepper, { type StepperStep } from '../../../../Common/CustomStepper';
import { SessionStep } from '../../../../../enums/Sessions';
import { I18nNamespace } from '../../../../../i18n';
import { hasErrors } from '../formHelpers';
import { sessionReducer, validateSession, type SessionState, type SessionValidationErrors } from '..';
import StepDetails from './StepDetails';
import StepReview from './StepReview';

interface Props {
  initialState: SessionState;
  completeLabel: string;
  onComplete: (state: SessionState) => void;
}

/** Details → Summary stepper shared by Create and Edit. */
const Steps = ({ initialState, completeLabel, onComplete }: Props) => {
  const { t } = useTranslation(I18nNamespace.Sessions);
  const [state, dispatch] = useReducer(sessionReducer, initialState);
  const [step, setStep] = useState<number>(SessionStep.Details);
  const [errors, setErrors] = useState<SessionValidationErrors>({});

  const validate = (current: number) => {
    if (current !== SessionStep.Details) {
      return false;
    }
    const result = validateSession(state);
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
