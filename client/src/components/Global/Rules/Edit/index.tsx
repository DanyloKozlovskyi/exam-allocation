import { Skeleton, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useEffect, useReducer } from 'react';
import { useTranslation } from 'react-i18next';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { useRuleQuery, useRuleTypesQuery, useSessionAttributesQuery } from '../../Queries/rules';
import { fromRuleDto, initialRuleState, ruleReducer, RuleReducerAction, toRuleDto } from '../Common';
import Steps from '../Common/components/Steps';
import { editRule } from './mutations';
import { invalidateSessionInputs } from '../../Queries/invalidation';

interface Props {
  sessionId: string;
  ruleId: string;
  onFinish: () => void;
}

const SKELETON_HEIGHT_PX = 240;

const RuleEdit = ({ sessionId, ruleId, onFinish }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { data: rule } = useRuleQuery(sessionId, ruleId);
  const { data: ruleTypes = [] } = useRuleTypesQuery();
  const { data: attributes = [] } = useSessionAttributesQuery(sessionId);
  const [state, dispatch] = useReducer(ruleReducer, initialRuleState);

  useEffect(() => {
    if (rule) {
      dispatch({ type: RuleReducerAction.Init, payload: fromRuleDto(rule) });
    }
  }, [rule]);

  const mutation = useMutation({
    mutationKey: [MutationKey.EditRule],
    mutationFn: () => editRule(sessionId, toRuleDto(state, sessionId)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rules.list(sessionId) });
      void invalidateSessionInputs(queryClient);
      enqueueSnackbar(t('messages.saved'), { variant: SNACKBAR_SUCCESS });
      onFinish();
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('editTitle')}
      </Typography>
      {state.id ? (
        <Steps
          state={state}
          dispatch={dispatch}
          ruleTypes={ruleTypes}
          attributes={attributes}
          isTypeLocked
          completeLabel={t('actions.save')}
          handleComplete={() => mutation.mutate()}
        />
      ) : (
        <Skeleton variant="rectangular" height={SKELETON_HEIGHT_PX} />
      )}
      {mutation.isPending && <CustomBackdrop label={t('messages.saving')} />}
    </>
  );
};

export default RuleEdit;
