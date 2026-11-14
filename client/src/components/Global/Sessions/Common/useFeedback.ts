import { useSnackbar } from 'notistack';
import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { processError } from '../../../../helpers/processError';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';

/** notistack wrappers shared by the WS-E features: success text from the caller, errors mapped by `processError`. */
export const useFeedback = () => {
  const { enqueueSnackbar } = useSnackbar();
  const { t } = useTranslation();

  const notifySuccess = useCallback((message: string) => enqueueSnackbar(message, { variant: SNACKBAR_SUCCESS }), [enqueueSnackbar]);
  const notifyError = useCallback((error: unknown) => processError(error, t, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })), [enqueueSnackbar, t]);
  const notifyErrorMessage = useCallback((message: string) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR }), [enqueueSnackbar]);

  return { notifySuccess, notifyError, notifyErrorMessage };
};
