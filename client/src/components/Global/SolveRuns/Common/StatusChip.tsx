import { Chip, Stack } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { SolveRunStatus } from '../../../../api/dtos';
import { SolveRunsTestId } from '../../../../enums/SolveRuns';
import { I18nNamespace } from '../../../../i18n';
import { SOLVE_RUN_STATUS_COLOR } from './constants';

interface Props {
  status: SolveRunStatus | undefined;
  isStale?: boolean;
}

const StatusChip = ({ status = SolveRunStatus.Queued, isStale = false }: Props) => {
  const { t } = useTranslation(I18nNamespace.SolveRuns);
  return (
    <Stack direction="row" spacing={1}>
      <Chip size="small" color={SOLVE_RUN_STATUS_COLOR[status]} label={t(`status.${status}`)} data-qa={SolveRunsTestId.StatusChip} data-status={status} />
      {isStale && <Chip size="small" variant="outlined" color="warning" label={t('list.stale')} data-qa={SolveRunsTestId.StaleBadge} />}
    </Stack>
  );
};

export default StatusChip;
