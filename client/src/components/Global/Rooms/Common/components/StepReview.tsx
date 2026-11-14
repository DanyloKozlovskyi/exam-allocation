import { Stack } from '@mui/material';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { RoomsTestId } from '../../../../../enums/Rooms';
import { I18nNamespace } from '../../../../../i18n';
import ReadOnlyBlock from '../../../../Common/ReadOnlyBlock';
import RoomGrid from '../../../../Common/RoomGrid';
import { toDisplayNumber } from '../../../../Common/RoomGrid/seatKey';
import { enabledSeatCount, seatStateOf, toLayoutDto, type RoomState } from '..';

interface Props {
  state: RoomState;
  setStep: (step: number) => void;
  /** Extra controls (e.g. "duplicate to N rooms" on create). */
  children?: ReactNode;
}

const StepReview = ({ state, setStep, children }: Props) => {
  const { t } = useTranslation(I18nNamespace.Rooms);
  const accessible = state.accessibleSeats.filter((key) => !state.disabledSeats.includes(key)).length;
  return (
    <Stack spacing={2} data-qa={RoomsTestId.Summary}>
      <ReadOnlyBlock
        handleEdit={() => setStep(0)}
        items={[
          { label: t('fields.name'), value: state.name },
          { label: t('fields.size'), value: t('list.sizeValue', { rows: state.rows, columns: state.columns, seats: state.seatsPerDesk }) },
          { label: t('fields.aisles'), value: state.aislesAfterColumns.map(toDisplayNumber).join(', ') || '—' },
          { label: t('fields.capacity'), value: enabledSeatCount(state) },
          { label: t('fields.accessibleCapacity'), value: accessible },
          { label: t('fields.costOverride'), value: state.costOverride ?? t('list.defaultCost') },
          { label: t('fields.isActive'), value: state.isActive ? t('list.active') : t('list.inactive') },
        ]}
      />
      <RoomGrid layout={toLayoutDto(state)} getSeatState={(seat) => seatStateOf(state, seat.key)} />
      {children}
    </Stack>
  );
};

export default StepReview;
