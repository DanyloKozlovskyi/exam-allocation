import { LinearProgress, List, ListItemButton, ListItemText, ListSubheader, Paper, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { AllocationDto } from '../../../../../api/dtos';
import { AllocationsTestId } from '../../../../../enums/Allocations';
import { I18nNamespace } from '../../../../../i18n';
import { PERCENT_FACTOR } from '../constants';

interface Props {
  allocation: AllocationDto;
  selectedCenterId: string | null;
  onOpenRoom: (roomId: string) => void;
}

const fillPercent = (filled = 0, capacity = 0) => (capacity > 0 ? Math.min(PERCENT_FACTOR, (filled / capacity) * PERCENT_FACTOR) : 0);

/** Rooms of the allocation grouped by center, with fill bars; click opens the room seating. */
const RoomsFillList = ({ allocation, selectedCenterId, onOpenRoom }: Props) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  const centers = (allocation.centers ?? []).filter((center) => !selectedCenterId || center.centerId === selectedCenterId);

  return (
    <Paper variant="outlined" data-qa={AllocationsTestId.RoomsList}>
      <List dense disablePadding>
        {centers.map((center) => (
          <li key={center.centerId}>
            <ul style={{ padding: 0 }}>
              <ListSubheader>{center.name}</ListSubheader>
              {(center.rooms ?? []).map((room) => (
                <ListItemButton key={room.roomId} disabled={!room.used} onClick={() => room.roomId && onOpenRoom(room.roomId)}>
                  <ListItemText
                    primary={room.name}
                    secondary={
                      <Stack component="span" spacing={0.5}>
                        <LinearProgress
                          variant="determinate"
                          value={fillPercent(room.filled, room.capacity)}
                          data-qa={AllocationsTestId.RoomFillBar}
                          aria-label={`${room.name}: ${t('overview.roomFill', { filled: room.filled ?? 0, capacity: room.capacity ?? 0 })}`}
                        />
                        <Typography component="span" variant="caption">
                          {room.used ? t('overview.roomFill', { filled: room.filled ?? 0, capacity: room.capacity ?? 0 }) : t('overview.unused')}
                        </Typography>
                      </Stack>
                    }
                    slotProps={{ secondary: { component: 'span' } }}
                  />
                </ListItemButton>
              ))}
            </ul>
          </li>
        ))}
      </List>
    </Paper>
  );
};

export default RoomsFillList;
