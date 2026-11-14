import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { Alert, Box, Button, Chip, FormControlLabel, List, ListItem, ListItemText, Paper, Skeleton, Stack, Switch, Typography } from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useSnackbar } from 'notistack';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { MoveDto, ParticipantBriefDto, SolveRunDto, ValidationReportDto, ViolationDto, ViolationKind } from '../../../../api/dtos';
import { MutationKey } from '../../../../constants/mutationKeys';
import { QueryKey } from '../../../../constants/queryKeys';
import { SNACKBAR_ERROR, SNACKBAR_SUCCESS } from '../../../../constants/ui';
import { AllocationsTestId, MoveModeStep } from '../../../../enums/Allocations';
import { SeatState } from '../../../../enums/Rooms';
import { processError } from '../../../../helpers/processError';
import { I18nNamespace } from '../../../../i18n';
import ConfirmDialog from '../../../Common/ConfirmDialog';
import RoomGrid from '../../../Common/RoomGrid';
import { defaultSeatState, type Box as SeatBox, type GridSeat } from '../../../Common/RoomGrid/geometry';
import { parseSeatKey, toDisplayNumber } from '../../../Common/RoomGrid/seatKey';
import { useRoomSeatingQuery } from '../../Queries/allocations';
import { formatNumber } from '../../SolveRuns/Common/formatters';
import { GROUP_BAR_HEIGHT_PX, groupColor, SEAT_TEXT_FONT_SIZE_PX, SEAT_TEXT_OFFSET_PX, SEATING_SEAT_WIDTH_PX } from '../Common/constants';
import ExportMenu from '../Common/components/ExportMenu';
import PinAndRerun from '../Common/components/PinAndRerun';
import { fullName, truncate } from '../Common/helpers';
import { applyMoves, validateMoves } from '../Common/mutations';

interface Props {
  sessionId: string;
  allocationId: string;
  roomId: string;
  onBack: () => void;
  /** Opens the same room in another allocation (the saved manual version). */
  onOpenAllocationRoom: (allocationId: string) => void;
  onRunStarted: (run: SolveRunDto) => void;
}

const SKELETON_HEIGHT_PX = 360;
const HALF = 2;
const TEXT_FILL = '#263238';

/** Adds or replaces the move of a participant; drops moves that return to the original seat. */
const upsertMove = (moves: MoveDto[], move: MoveDto, originalSeat: string | undefined) => {
  const rest = moves.filter((item) => item.participantId !== move.participantId);
  return move.seatKey === originalSeat ? rest : [...rest, move];
};

const RoomSeating = ({ sessionId, allocationId, roomId, onBack, onOpenAllocationRoom, onRunStarted }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const { t: tc } = useTranslation();
  const { enqueueSnackbar } = useSnackbar();
  const queryClient = useQueryClient();
  const { data: seating, isLoading } = useRoomSeatingQuery(allocationId, roomId);
  const [step, setStep] = useState(MoveModeStep.Off);
  const [selected, setSelected] = useState<string | null>(null);
  const [moves, setMoves] = useState<MoveDto[]>([]);
  const [report, setReport] = useState<ValidationReportDto | null>(null);
  const [confirmForce, setConfirmForce] = useState(false);

  const original = useMemo(() => {
    const seatOf = new Map<string, string>();
    const participants = new Map<string, ParticipantBriefDto>();
    (seating?.seats ?? []).forEach((seat) => {
      if (seat.participant?.id && seat.seatKey) {
        seatOf.set(seat.participant.id, seat.seatKey);
        participants.set(seat.participant.id, seat.participant);
      }
    });
    return { seatOf, participants };
  }, [seating]);

  const occupantBySeat = useMemo(() => {
    const placement = new Map(original.seatOf);
    moves.forEach((move) => move.participantId && move.seatKey && placement.set(move.participantId, move.seatKey));
    const bySeat = new Map<string, ParticipantBriefDto>();
    placement.forEach((seatKey, participantId) => bySeat.set(seatKey, original.participants.get(participantId)!));
    return bySeat;
  }, [original, moves]);

  const violationsBySeat = useMemo(() => {
    const source: ViolationDto[] = report
      ? (report.violations ?? []).filter((violation) => !violation.roomId || violation.roomId === roomId)
      : moves.length
        ? []
        : (seating?.seats ?? []).flatMap((seat) => seat.violations ?? []);
    const map = new Map<string, ViolationDto[]>();
    source.forEach((violation) => violation.seatKey && map.set(violation.seatKey, [...(map.get(violation.seatKey) ?? []), violation]));
    return map;
  }, [report, moves, seating, roomId]);

  const notifyError = (error: unknown) => processError(error, tc, (message) => enqueueSnackbar(message, { variant: SNACKBAR_ERROR }));

  const validation = useMutation({
    mutationKey: [MutationKey.ValidateMoves],
    mutationFn: () => validateMoves(allocationId, { moves }),
    onSuccess: (result) => {
      setReport(result);
      enqueueSnackbar(t('messages.validated'), { variant: SNACKBAR_SUCCESS });
    },
    onError: notifyError,
  });

  const apply = useMutation({
    mutationKey: [MutationKey.ApplyMoves],
    mutationFn: (force: boolean) => applyMoves(allocationId, { moves, force }),
    onSuccess: (manual) => {
      void queryClient.invalidateQueries({ queryKey: [QueryKey.Allocations] });
      void queryClient.invalidateQueries({ queryKey: [QueryKey.SolveRuns] });
      enqueueSnackbar(t('messages.applied'), { variant: SNACKBAR_SUCCESS });
      if (manual.id) onOpenAllocationRoom(manual.id);
    },
    onError: notifyError,
    onSettled: () => setConfirmForce(false),
  });

  if (isLoading || !seating?.room?.layout) {
    return <Skeleton variant="rectangular" height={SKELETON_HEIGHT_PX} />;
  }

  const { room } = seating;
  const layout = room.layout!;
  const isMoving = step !== MoveModeStep.Off;

  const seatText = (key: string) => {
    const position = parseSeatKey(key);
    return position ? t('seating.tooltipSeat', { row: toDisplayNumber(position.row), column: toDisplayNumber(position.column), seat: toDisplayNumber(position.seatIndex) }) : key;
  };

  const seatState = (seat: GridSeat) => {
    if (seat.key === selected) return SeatState.Selected;
    if (violationsBySeat.has(seat.key)) return SeatState.Violation;
    if (occupantBySeat.has(seat.key)) return SeatState.Occupied;
    return defaultSeatState(seat);
  };

  const handleSeatClick = (seat: GridSeat) => {
    if (seat.isDisabled) return;
    if (step === MoveModeStep.SelectSource) {
      if (occupantBySeat.has(seat.key)) {
        setSelected(seat.key);
        setStep(MoveModeStep.SelectTarget);
      }
      return;
    }
    if (step !== MoveModeStep.SelectTarget || !selected) return;
    if (seat.key !== selected) {
      const moving = occupantBySeat.get(selected)!;
      const target = occupantBySeat.get(seat.key);
      let next = upsertMove(moves, { participantId: moving.id, roomId, seatKey: seat.key }, original.seatOf.get(moving.id!));
      if (target) next = upsertMove(next, { participantId: target.id, roomId, seatKey: selected }, original.seatOf.get(target.id!));
      setMoves(next);
      setReport(null);
    }
    setSelected(null);
    setStep(MoveModeStep.SelectSource);
  };

  const toggleMoveMode = (on: boolean) => {
    setStep(on ? MoveModeStep.SelectSource : MoveModeStep.Off);
    setSelected(null);
    if (!on) {
      setMoves([]);
      setReport(null);
    }
  };

  const renderSeat = (seat: GridSeat, box: SeatBox) => {
    const occupant = occupantBySeat.get(seat.key);
    if (!occupant) return null;
    return (
      <>
        <rect x={box.x} y={box.y} width={box.width} height={GROUP_BAR_HEIGHT_PX} fill={groupColor(occupant.group)} pointerEvents="none" />
        <text
          x={box.x + box.width / HALF}
          y={box.y + (box.height + GROUP_BAR_HEIGHT_PX) / HALF}
          dx={0}
          dy={SEAT_TEXT_OFFSET_PX / HALF}
          textAnchor="middle"
          dominantBaseline="middle"
          fontSize={SEAT_TEXT_FONT_SIZE_PX}
          fill={TEXT_FILL}
          pointerEvents="none">
          {truncate(occupant.lastName ?? '')}
        </text>
      </>
    );
  };

  const tooltip = (seat: GridSeat) => {
    const occupant = occupantBySeat.get(seat.key);
    const violations = violationsBySeat.get(seat.key) ?? [];
    if (!occupant && !violations.length) return null;
    return (
      <Box>
        <Typography variant="body2">{seatText(seat.key)}</Typography>
        {occupant && (
          <>
            <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
              {fullName(occupant)}
            </Typography>
            {occupant.group && <Typography variant="caption">{t('seating.groupLabel', { group: occupant.group })}</Typography>}
            {occupant.school && (
              <Typography variant="caption" component="div">
                {t('seating.schoolLabel', { school: occupant.school })}
              </Typography>
            )}
          </>
        )}
        {violations.map((violation, index) => (
          <Typography key={`${violation.kind}-${index}`} variant="caption" component="div" color="error.light">
            {violation.kind ? t(`violationKind.${violation.kind}`) : ''}
          </Typography>
        ))}
      </Box>
    );
  };

  const filled = occupantBySeat.size;
  const groups = [...new Set([...occupantBySeat.values()].map((participant) => participant.group ?? ''))].sort();
  const violationKinds = [...new Set([...violationsBySeat.values()].flat().map((violation) => violation.kind))].filter((kind): kind is ViolationKind => Boolean(kind));
  const hardCount = (report?.violations ?? []).filter((violation) => violation.isHard).length;

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap' }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Button startIcon={<ArrowBackIcon />} onClick={onBack}>
            {t('actions.back')}
          </Button>
          <Typography variant="h5">{t('seating.title', { room: room.name })}</Typography>
          <Typography color="text.secondary">{t('seating.fill', { filled, capacity: room.capacity ?? 0 })}</Typography>
        </Stack>
        <Stack direction="row" spacing={1}>
          <PinAndRerun
            sessionId={sessionId}
            allocationId={allocationId}
            roomIds={[roomId]}
            label={t('actions.pinRoom')}
            testId={AllocationsTestId.PinRoomButton}
            onRunStarted={onRunStarted}
          />
          <ExportMenu allocationId={allocationId} roomId={roomId} />
        </Stack>
      </Stack>

      <FormControlLabel
        control={
          <Switch checked={isMoving} onChange={(event) => toggleMoveMode(event.target.checked)} slotProps={{ input: { 'data-qa': AllocationsTestId.MoveModeToggle } as object }} />
        }
        label={t('actions.moveMode')}
      />
      {isMoving && <Alert severity="info">{step === MoveModeStep.SelectTarget ? t('seating.moveHintTarget') : t('seating.moveHintSource')}</Alert>}

      <Box sx={{ overflowX: 'auto' }}>
        <RoomGrid
          layout={layout}
          seatWidth={SEATING_SEAT_WIDTH_PX}
          getSeatState={seatState}
          renderSeat={renderSeat}
          getSeatLabel={(seat) => t('seating.seatAria', { seat: seatText(seat.key), name: fullName(occupantBySeat.get(seat.key)) || t('seating.empty') })}
          getSeatTooltip={tooltip}
          onSeatClick={isMoving ? handleSeatClick : undefined}
          testId={AllocationsTestId.SeatingGrid}
          title={room.name}
        />
      </Box>

      <Stack direction="row" spacing={4} sx={{ flexWrap: 'wrap' }}>
        <Box>
          <Typography variant="subtitle2">{t('seating.groups')}</Typography>
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
            {groups.map((group) => (
              <Chip key={group} size="small" label={group || '—'} sx={{ bgcolor: groupColor(group) }} />
            ))}
          </Stack>
        </Box>
        <Box data-qa={AllocationsTestId.ViolationLegend}>
          <Typography variant="subtitle2">{t('seating.violations')}</Typography>
          {violationKinds.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('seating.noViolations')}
            </Typography>
          ) : (
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              {violationKinds.map((kind) => (
                <Chip key={kind} size="small" color="error" variant="outlined" label={t(`violationKind.${kind}`)} />
              ))}
            </Stack>
          )}
        </Box>
      </Stack>

      {isMoving && moves.length > 0 && (
        <Paper variant="outlined" sx={{ p: 2 }} data-qa={AllocationsTestId.PendingMoves}>
          <Typography variant="subtitle1">{t('seating.pendingMoves', { count: moves.length })}</Typography>
          <List dense>
            {moves.map((move) => (
              <ListItem key={move.participantId}>
                <ListItemText
                  primary={t('seating.moveItem', {
                    name: fullName(original.participants.get(move.participantId ?? '')),
                    from: seatText(original.seatOf.get(move.participantId ?? '') ?? ''),
                    to: seatText(move.seatKey ?? ''),
                  })}
                />
              </ListItem>
            ))}
          </List>
          {report && (
            <Alert severity={report.isValid ? 'success' : 'error'} data-qa={AllocationsTestId.ValidationReport} sx={{ mb: 1 }}>
              {report.isValid
                ? t('seating.reportValid', { penalty: formatNumber(report.softPenalty ?? 0) })
                : t('seating.reportInvalid', { count: hardCount, penalty: formatNumber(report.softPenalty ?? 0) })}
            </Alert>
          )}
          <Stack direction="row" spacing={1}>
            <Button onClick={() => validation.mutate()} loading={validation.isPending} data-qa={AllocationsTestId.ValidateMovesButton}>
              {t('actions.validateMoves')}
            </Button>
            <Button
              variant="contained"
              disabled={!report}
              loading={apply.isPending}
              onClick={() => (report?.isValid ? apply.mutate(false) : setConfirmForce(true))}
              data-qa={AllocationsTestId.ApplyMovesButton}>
              {t('actions.applyMoves')}
            </Button>
            <Button
              color="inherit"
              onClick={() => {
                setMoves([]);
                setReport(null);
              }}
              data-qa={AllocationsTestId.ResetMovesButton}>
              {t('actions.resetMoves')}
            </Button>
          </Stack>
        </Paper>
      )}

      <ConfirmDialog
        open={confirmForce}
        title={t('seating.forceTitle')}
        message={t('seating.forceMessage')}
        onConfirm={() => apply.mutate(true)}
        onCancel={() => setConfirmForce(false)}
      />
    </Stack>
  );
};

export default RoomSeating;
