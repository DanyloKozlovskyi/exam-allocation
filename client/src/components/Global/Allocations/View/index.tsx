import { Alert, Box, Grid, List, ListItem, ListItemText, Paper, Skeleton, Stack, Tab, Tabs, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { AllocationCenterDto, SolveRunDto, TestCenterDto } from '../../../../api/dtos';
import { AllocationsTestId, AllocationViewTab } from '../../../../enums/Allocations';
import { I18nNamespace } from '../../../../i18n';
import { useAllocationQuery, useAllocationsByRunQuery } from '../../Queries/allocations';
import { useRulesListQuery } from '../../Queries/rules';
import { useSolveRunsListQuery } from '../../Queries/solveRuns';
import { formatNumber } from '../../SolveRuns/Common/formatters';
import { fillRatioColor } from '../Common/constants';
import AlternativeTabs from '../Common/components/AlternativeTabs';
import CentersMapPlaceholder from '../Common/components/CentersMapPlaceholder';
import ExportMenu from '../Common/components/ExportMenu';
import ParticipantsGrid from '../Common/components/ParticipantsGrid';
import PinAndRerun from '../Common/components/PinAndRerun';
import RoomsFillList from '../Common/components/RoomsFillList';
import { useAlternativeLabel } from '../Common/useAlternativeLabel';

interface Props {
  sessionId: string;
  allocationId: string;
  onSelectAllocation: (allocationId: string) => void;
  onOpenRoom: (roomId: string) => void;
  onRunStarted: (run: SolveRunDto) => void;
}

const SKELETON_HEIGHT_PX = 320;
const MAP_COLUMNS = 7;
const ROOMS_COLUMNS = 5;

const toCenter = (center: AllocationCenterDto): TestCenterDto => ({
  id: center.centerId,
  name: center.name,
  latitude: center.latitude,
  longitude: center.longitude,
  roomCount: center.rooms?.length ?? 0,
  capacity: center.rooms?.reduce((sum, room) => sum + (room.capacity ?? 0), 0) ?? 0,
});

const centerFill = (center: AllocationCenterDto) => {
  const used = (center.rooms ?? []).filter((room) => room.used);
  const capacity = used.reduce((sum, room) => sum + (room.capacity ?? 0), 0);
  const filled = used.reduce((sum, room) => sum + (room.filled ?? 0), 0);
  return capacity > 0 ? filled / capacity : 0;
};

const AllocationView = ({ sessionId, allocationId, onSelectAllocation, onOpenRoom, onRunStarted }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const alternativeLabel = useAlternativeLabel();
  const [tab, setTab] = useState(AllocationViewTab.Overview);
  const [selectedCenterId, setSelectedCenterId] = useState<string | null>(null);
  const { data: allocation, isLoading } = useAllocationQuery(allocationId);
  const { data: runs = [] } = useSolveRunsListQuery(sessionId);
  // AllocationDto has no runId (see change-requests): find the run that produced this allocation.
  const run = runs.find((item) => item.allocationIds?.includes(allocationId));
  const { data: alternatives = [] } = useAllocationsByRunQuery(run?.id);
  const { data: rules = [] } = useRulesListQuery(sessionId);

  const centers = useMemo(() => (allocation?.centers ?? []).map(toCenter), [allocation]);
  const fillById = useMemo(() => new Map((allocation?.centers ?? []).map((center) => [center.centerId, centerFill(center)])), [allocation]);

  if (isLoading || !allocation) {
    return <Skeleton variant="rectangular" height={SKELETON_HEIGHT_PX} />;
  }

  const penalties = Object.entries(allocation.objective?.penaltyByRule ?? {}).filter(([, penalty]) => penalty > 0);
  const ruleName = (ruleId: string) => {
    const rule = rules.find((item) => item.id === ruleId);
    return rule?.displayName || rule?.type || t('overview.unknownRule', { id: ruleId });
  };

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="h5">
          {t('title')} — {alternativeLabel(allocation.index)}
        </Typography>
        <Stack direction="row" spacing={1}>
          <PinAndRerun sessionId={sessionId} allocationId={allocationId} label={t('actions.pinAll')} testId={AllocationsTestId.PinAllButton} onRunStarted={onRunStarted} />
          <ExportMenu allocationId={allocationId} />
        </Stack>
      </Stack>

      {alternatives.length > 0 && <AlternativeTabs alternatives={alternatives} allocationId={allocationId} onSelect={onSelectAllocation} />}

      <Tabs value={tab} onChange={(_event, value: AllocationViewTab) => setTab(value)}>
        {Object.values(AllocationViewTab).map((value) => (
          <Tab key={value} value={value} label={t(`tabs.${value}`)} />
        ))}
      </Tabs>

      {tab === AllocationViewTab.Overview && (
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: MAP_COLUMNS }}>
            <Stack spacing={2}>
              <CentersMapPlaceholder
                centers={centers}
                selectedCenterId={selectedCenterId}
                onSelect={(centerId) => setSelectedCenterId((current) => (current === centerId ? null : centerId))}
                getMarkerColor={(center) => fillRatioColor(fillById.get(center.id ?? undefined) ?? 0)}
              />
              <Paper variant="outlined" sx={{ p: 2 }}>
                <Typography variant="subtitle1">{t('overview.penalties')}</Typography>
                {penalties.length === 0 ? (
                  <Alert severity="success">{t('overview.noPenalties')}</Alert>
                ) : (
                  <List dense>
                    {penalties.map(([ruleId, penalty]) => (
                      <ListItem key={ruleId}>
                        <ListItemText primary={t('overview.penaltyValue', { name: ruleName(ruleId), penalty: formatNumber(penalty) })} />
                      </ListItem>
                    ))}
                  </List>
                )}
              </Paper>
            </Stack>
          </Grid>
          <Grid size={{ xs: 12, md: ROOMS_COLUMNS }}>
            <Box>
              <Typography variant="subtitle1" gutterBottom>
                {t('overview.rooms')}
              </Typography>
              <RoomsFillList allocation={allocation} selectedCenterId={selectedCenterId} onOpenRoom={onOpenRoom} />
            </Box>
          </Grid>
        </Grid>
      )}

      {tab === AllocationViewTab.Participants && <ParticipantsGrid allocationId={allocationId} />}
    </Stack>
  );
};

export default AllocationView;
