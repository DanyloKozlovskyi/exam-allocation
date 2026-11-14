import PushPinIcon from '@mui/icons-material/PushPin';
import { Button } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { SolveRunDto } from '../../../../../api/dtos';
import { MutationKey } from '../../../../../constants/mutationKeys';
import { queryKeys } from '../../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../../constants/ui';
import { processError } from '../../../../../helpers/processError';
import { I18nNamespace } from '../../../../../i18n';
import RunDialog from '../../../SolveRuns/RunDialog';
import { pinAll } from '../mutations';

interface Props {
  sessionId: string;
  allocationId: string;
  /** Pin only these rooms; all rooms when omitted. */
  roomIds?: string[];
  label: string;
  testId: string;
  onRunStarted: (run: SolveRunDto) => void;
}

/** Pin room / pin all → pins API → offers the re-run dialog (docs/06 Allocations). */
const PinAndRerun = ({ sessionId, allocationId, roomIds, label, testId, onRunStarted }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const [rerunOpen, setRerunOpen] = useState(false);

  const mutation = useMutation({
    mutationKey: [MutationKey.PinAll],
    mutationFn: () => pinAll(allocationId, { roomIds: roomIds ?? null }),
    onSuccess: (pins) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.pins.list(sessionId) });
      enqueueSnackbar(t('messages.pinned', { count: pins.length }), { variant: SNACKBAR_SUCCESS });
      setRerunOpen(true);
    },
    onError: (error) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR })),
  });

  return (
    <>
      <Button variant="outlined" startIcon={<PushPinIcon />} loading={mutation.isPending} onClick={() => mutation.mutate()} data-qa={testId}>
        {label}
      </Button>
      <RunDialog open={rerunOpen} sessionId={sessionId} onClose={() => setRerunOpen(false)} onStarted={onRunStarted} />
    </>
  );
};

export default PinAndRerun;
