import { useState, type Dispatch, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { RoomStep } from '../../../../../enums/Rooms';
import { I18nNamespace } from '../../../../../i18n';
import CustomStepper from '../../../../Common/CustomStepper';
import { hasErrors, validateDetails, validateLayout, type RoomAction, type RoomState, type ValidationErrors } from '..';
import StepDetails from './StepDetails';
import StepLayout from './StepLayout';
import StepReview from './StepReview';

interface Props {
  state: RoomState;
  dispatch: Dispatch<RoomAction>;
  completeLabel: string;
  handleComplete: () => void;
  summaryExtras?: ReactNode;
}

const FULL_WIDTH = 12;

const Steps = ({ state, dispatch, completeLabel, handleComplete, summaryExtras }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const [step, setStep] = useState<number>(RoomStep.Details);
  const [errors, setErrors] = useState<ValidationErrors>({});

  const validate = (current: number) => {
    const next = current === RoomStep.Details ? validateDetails(state) : current === RoomStep.Layout ? validateLayout(state) : {};
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
      gridSize={FULL_WIDTH}
      stepperSteps={[
        { label: t('steps.details'), component: <StepDetails state={state} dispatch={dispatch} errors={errors} /> },
        { label: t('steps.layout'), component: <StepLayout state={state} dispatch={dispatch} errors={errors} /> },
        {
          label: t('steps.summary'),
          component: (
            <StepReview state={state} setStep={changeStep}>
              {summaryExtras}
            </StepReview>
          ),
        },
      ]}
    />
  );
};

export default Steps;
