import { Alert, Box, Checkbox, FormControlLabel, FormGroup, Stack, Typography } from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useEffect, useMemo, useState, type Dispatch } from 'react';
import { useTranslation } from 'react-i18next';
import { AdjacencyKind } from '../../../../../api/dtos';
import { MutationKey } from '../../../../../constants/mutationKeys';
import { RoomsTestId } from '../../../../../enums/Rooms';
import { I18nNamespace } from '../../../../../i18n';
import RoomGrid from '../../../../Common/RoomGrid';
import { toAdjacencyOverlays } from '../../../../Common/RoomGrid/adjacency';
import { ROOM_PREVIEW_DEBOUNCE_MS } from '../constants';
import { previewRoomLayout } from '../mutations';
import { RoomField, RoomReducerAction, seatStateOf, toLayoutDto, type RoomAction, type RoomState, type ValidationErrors } from '..';
import { useFieldErrorText } from '../useFieldErrorText';

interface Props {
  state: RoomState;
  dispatch: Dispatch<RoomAction>;
  errors: ValidationErrors;
}

const StepLayout = ({ state, dispatch, errors }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const errorText = useFieldErrorText();
  const [kinds, setKinds] = useState<AdjacencyKind[]>([]);
  const layout = useMemo(() => toLayoutDto(state), [state]);
  const { mutate, data: preview, isPending } = useMutation({ mutationKey: [MutationKey.PreviewRoomLayout], mutationFn: previewRoomLayout });

  useEffect(() => {
    const timer = setTimeout(() => mutate(layout), ROOM_PREVIEW_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [layout, mutate]);

  const overlays = useMemo(() => (kinds.length ? toAdjacencyOverlays(preview, kinds) : []), [preview, kinds]);

  const toggleKind = (kind: AdjacencyKind) => setKinds((current) => (current.includes(kind) ? current.filter((item) => item !== kind) : [...current, kind]));

  return (
    <Stack spacing={2}>
      <Typography color="text.secondary">{t('layout.hint')}</Typography>
      {errors[RoomField.Layout] && <Alert severity="error">{errorText(errors[RoomField.Layout])}</Alert>}
      <Box sx={{ overflowX: 'auto' }} data-qa={RoomsTestId.LayoutPreview}>
        <RoomGrid
          layout={layout}
          getSeatState={(seat) => seatStateOf(state, seat.key)}
          onSeatClick={(seat) => dispatch({ type: RoomReducerAction.CycleSeat, payload: seat.key })}
          adjacency={overlays}
        />
      </Box>
      <Typography data-qa={RoomsTestId.PreviewCapacity}>
        {preview ? t('layout.previewCapacity', { capacity: preview.capacity ?? 0, accessible: preview.accessibleCapacity ?? 0 }) : t('layout.previewLoading')}
        {preview && isPending ? ` — ${t('layout.previewLoading')}` : ''}
      </Typography>
      <Box>
        <Typography variant="subtitle2">{t('actions.showAdjacency')}</Typography>
        <FormGroup row>
          {Object.values(AdjacencyKind).map((kind) => (
            <FormControlLabel
              key={kind}
              control={
                <Checkbox
                  checked={kinds.includes(kind)}
                  onChange={() => toggleKind(kind)}
                  slotProps={{ input: { 'data-qa': `${RoomsTestId.AdjacencyToggle}-${kind}` } as object }}
                />
              }
              label={t(`adjacencyKind.${kind}`)}
            />
          ))}
        </FormGroup>
      </Box>
    </Stack>
  );
};

export default StepLayout;
