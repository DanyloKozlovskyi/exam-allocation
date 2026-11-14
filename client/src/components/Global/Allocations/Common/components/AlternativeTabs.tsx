import { Box, Tab, Tabs, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { AllocationSummaryDto } from '../../../../../api/dtos';
import { AllocationsTestId } from '../../../../../enums/Allocations';
import { I18nNamespace } from '../../../../../i18n';
import { formatNumber } from '../../../SolveRuns/Common/formatters';
import { useAlternativeLabel } from '../useAlternativeLabel';

interface Props {
  alternatives: AllocationSummaryDto[];
  allocationId: string;
  onSelect: (allocationId: string) => void;
}

/** One tab per alternative of the run (index 0 = best, -1 = manual), with an objective summary. */
const AlternativeTabs = ({ alternatives, allocationId, onSelect }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const label = useAlternativeLabel();
  const selected = alternatives.some((item) => item.id === allocationId) ? allocationId : false;

  return (
    <Tabs value={selected} onChange={(_event, value: string) => onSelect(value)} variant="scrollable" data-qa={AllocationsTestId.AlternativeTabs}>
      {alternatives.map((alternative) => (
        <Tab
          key={alternative.id}
          value={alternative.id}
          sx={{ textTransform: 'none', alignItems: 'flex-start' }}
          label={
            <Box sx={{ textAlign: 'left' }}>
              <Typography variant="subtitle2">{label(alternative.index)}</Typography>
              <Typography variant="caption" color="text.secondary">
                {t('alternative.summary', {
                  rooms: alternative.objective?.roomsUsed ?? 0,
                  cost: formatNumber(alternative.objective?.cost ?? 0),
                  penalty: formatNumber(alternative.objective?.softPenalty ?? 0),
                })}
              </Typography>
            </Box>
          }
        />
      ))}
    </Tabs>
  );
};

export default AlternativeTabs;
