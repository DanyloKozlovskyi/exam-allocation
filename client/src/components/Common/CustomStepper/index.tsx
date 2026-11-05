import { Box, Button, Grid, Step, StepLabel, Stepper } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { FORM_GRID_SIZE } from '../../../constants/ui';

export interface StepperStep {
  label: string;
  component: React.ReactNode;
}

interface Props {
  step: number;
  setStep: (step: number) => void;
  stepperSteps: StepperStep[];
  completeLabel: string;
  isNextDisabled?: boolean;
  /** Returns true when the step has validation errors. */
  validate?: (step: number) => boolean;
  handleComplete: () => void;
  gridSize?: number;
}

/** Replacement for pal-suite's blum CustomStepper: steps with Back / Next and a final complete action. */
const CustomStepper = ({ step, setStep, stepperSteps, completeLabel, isNextDisabled = false, validate, handleComplete, gridSize = FORM_GRID_SIZE }: Props) => {
  const { t } = useTranslation();
  const isLast = step === stepperSteps.length - 1;

  const handleNext = () => {
    if (validate?.(step)) {
      return;
    }

    if (isLast) {
      handleComplete();
    } else {
      setStep(step + 1);
    }
  };

  return (
    <Grid container spacing={3}>
      <Grid size={12}>
        <Stepper activeStep={step}>
          {stepperSteps.map((item) => (
            <Step key={item.label}>
              <StepLabel>{item.label}</StepLabel>
            </Step>
          ))}
        </Stepper>
      </Grid>
      <Grid size={gridSize}>{stepperSteps[step]?.component}</Grid>
      <Grid size={12}>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button disabled={step === 0} onClick={() => setStep(step - 1)}>
            {t('actions.back')}
          </Button>
          <Button variant="contained" disabled={isNextDisabled} onClick={handleNext}>
            {isLast ? completeLabel : t('actions.next')}
          </Button>
        </Box>
      </Grid>
    </Grid>
  );
};

export default CustomStepper;
