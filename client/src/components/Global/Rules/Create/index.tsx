import { Skeleton, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useEffect, useReducer, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { MutationKey } from '../../../../constants/mutationKeys';
import { queryKeys } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import CustomBackdrop from '../../../Common/CustomBackdrop';
import { useRuleTypesQuery, useSessionAttributesQuery } from '../../Queries/rules';
import { initialRuleState, ruleReducer, RuleReducerAction, toRuleDto } from '../Common';
import Steps from '../Common/components/Steps';
import { findTypeInfo } from '../Common/parameters';
import { createRule } from './mutations';

interface Props {
  sessionId: string;
  onFinish: () => void;
}

const SKELETON_HEIGHT_PX = 240;

const RuleCreate = ({ sessionId, onFinish }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rules);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { data: ruleTypes = [], isLoading } = useRuleTypesQuery();
  const { data: attributes = [] } = useSessionAttributesQuery(sessionId);
  const [state, dispatch] = useReducer(ruleReducer, initialRuleState);
  const initialized = useRef(false);

  // Apply the server defaults of the initial type once rule-types are loaded.
  useEffect(() => {
    if (!initialized.current && ruleTypes.length) {
      initialized.current = true;
      dispatch({ type: RuleReducerAction.Type, payload: { type: initialRuleState.type, info: findTypeInfo(ruleTypes, initialRuleState.type) } });
    }
  }, [ruleTypes]);

  const mutation = useMutation({
    mutationKey: [MutationKey.CreateRule],
    mutationFn: () => createRule(sessionId, toRuleDto(state, sessionId)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.rules.list(sessionId) });
      enqueueSnackbar(t('messages.created'), { variant: SNACKBAR_SUCCESS });
      onFinish();
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
  });

  return (
    <>
      <Typography variant="h5" gutterBottom>
        {t('createTitle')}
      </Typography>
      {isLoading ? (
        <Skeleton variant="rectangular" height={SKELETON_HEIGHT_PX} />
      ) : (
        <Steps state={state} dispatch={dispatch} ruleTypes={ruleTypes} attributes={attributes} completeLabel={t('actions.createRule')} handleComplete={() => mutation.mutate()} />
      )}
      {mutation.isPending && <CustomBackdrop label={t('messages.saving')} />}
    </>
  );
};

export default RuleCreate;
