import PlaceIcon from '@mui/icons-material/Place';
import { Box, Chip, Paper, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { TestCenterDto } from '../../../../../api/dtos';
import { AllocationsTestId } from '../../../../../enums/Allocations';
import { I18nNamespace } from '../../../../../i18n';

/** Same props as WS-E's `components/Global/Centers/Map/CentersMap` (docs/06) + optional marker colour. */
export interface CentersMapProps {
  centers: TestCenterDto[];
  selectedCenterId?: string | null;
  onSelect?: (centerId: string) => void;
  onAddAtPoint?: (latitude: number, longitude: number) => void;
  onPlacePicked?: (center: TestCenterDto) => void;
  /** Fill-ratio colouring of markers (allocation overview). */
  getMarkerColor?: (center: TestCenterDto) => string;
}

const COORDINATE_DIGITS = 4;
const MAP_MIN_HEIGHT_PX = 160;

/**
 * Placeholder for the allocation overview map until `CentersMap` (WS-E) is merged — see docs/change-requests.md.
 * Renders centers as selectable markers without Google Maps.
 */
const CentersMapPlaceholder = ({ centers, selectedCenterId, onSelect, getMarkerColor }: CentersMapProps) => {
  const { t } = useTranslation(I18nNamespace.Allocations);
  return (
    <Paper variant="outlined" sx={{ p: 2, minHeight: MAP_MIN_HEIGHT_PX }} data-qa={AllocationsTestId.OverviewMap}>
      <Typography variant="subtitle1">{t('overview.map')}</Typography>
      <Typography variant="caption" color="text.secondary">
        {t('overview.mapPlaceholder')}
      </Typography>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mt: 1 }}>
        {centers.map((center) => (
          <Chip
            key={center.id}
            icon={
              <Box component="span" sx={{ display: 'flex', color: getMarkerColor?.(center) }}>
                <PlaceIcon fontSize="small" />
              </Box>
            }
            label={`${center.name} (${t('overview.coordinates', { lat: center.latitude?.toFixed(COORDINATE_DIGITS), lng: center.longitude?.toFixed(COORDINATE_DIGITS) })})`}
            color={center.id === selectedCenterId ? 'primary' : 'default'}
            variant={center.id === selectedCenterId ? 'filled' : 'outlined'}
            onClick={onSelect && center.id ? () => onSelect(center.id!) : undefined}
          />
        ))}
      </Stack>
    </Paper>
  );
};

export default CentersMapPlaceholder;
