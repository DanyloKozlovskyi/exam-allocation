import type { TFunction } from 'i18next';
import type { StepperStep } from '../../../../Common/CustomStepper';
import type { ImportAction, ImportState, MappingValidationErrors } from '..';
import StepMapping from './StepMapping';
import StepMode from './StepMode';
import StepReview from './StepReview';
import StepSheet from './StepSheet';
import StepUpload from './StepUpload';
import StepValidation from './StepValidation';

interface Options {
  t: TFunction<'imports'>;
  state: ImportState;
  dispatch: React.Dispatch<ImportAction>;
  mappingErrors: MappingValidationErrors;
  setStep: (step: number) => void;
  onDownloadTemplate: () => void;
}

/** Upload → Sheet & header row → Mapping → Validation → Mode → Commit; order matches ImportWizardStep. */
export const buildImportSteps = ({ t, state, dispatch, mappingErrors, setStep, onDownloadTemplate }: Options): StepperStep[] => [
  { label: t('wizard.steps.upload'), component: <StepUpload state={state} dispatch={dispatch} onDownloadTemplate={onDownloadTemplate} /> },
  { label: t('wizard.steps.sheet'), component: <StepSheet state={state} dispatch={dispatch} /> },
  { label: t('wizard.steps.mapping'), component: <StepMapping state={state} dispatch={dispatch} errors={mappingErrors} /> },
  { label: t('wizard.steps.validation'), component: <StepValidation state={state} /> },
  { label: t('wizard.steps.mode'), component: <StepMode state={state} dispatch={dispatch} /> },
  { label: t('wizard.steps.commit'), component: <StepReview state={state} setStep={setStep} /> },
];
