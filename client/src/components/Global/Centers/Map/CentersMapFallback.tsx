import AddLocationIcon from '@mui/icons-material/AddLocationAlt';
import PlaceIcon from '@mui/icons-material/Place';
import { Alert, Box, Button, Chip, Paper, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CenterConstraint, CentersTestId } from '../../../../enums/Centers';
import { I18nNamespace } from '../../../../i18n';
import { parseNumber, validateNumber } from '../../Sessions/Common/formHelpers';
import { DEFAULT_MAP_CENTER, type CentersMapProps } from './types';

/** Used instead of the Google map when `appConfig.googleMapsApiKey` is empty (and in tests). */
const CentersMapFallback = ({ centers, selectedCenterId, onSelect, onAddAtPoint, getMarkerColor }: CentersMapProps) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const [latitude, setLatitude] = useState(String(DEFAULT_MAP_CENTER.latitude));
  const [longitude, setLongitude] = useState(String(DEFAULT_MAP_CENTER.longitude));

  const latitudeError = validateNumber(latitude, CenterConstraint.MinLatitude, CenterConstraint.MaxLatitude, true);
  const longitudeError = validateNumber(longitude, CenterConstraint.MinLongitude, CenterConstraint.MaxLongitude, true);

  const handleAdd = () => {
    const lat = parseNumber(latitude);
    const lng = parseNumber(longitude);
    if (lat !== null && lng !== null && !latitudeError && !longitudeError) {
      onAddAtPoint?.({ latitude: lat, longitude: lng });
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 2 }} data-qa={CentersTestId.MapFallback}>
      <Stack spacing={2}>
        <Alert severity="info">
          <Typography variant="subtitle2">{t('map.fallbackTitle')}</Typography>
          {onAddAtPoint && t('map.fallbackHint')}
        </Alert>
        {onAddAtPoint && (
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'flex-start' }}>
            <TextField
              label={t('map.latitude')}
              value={latitude}
              size="small"
              onChange={(event) => setLatitude(event.target.value)}
              error={Boolean(latitudeError)}
              helperText={latitudeError ? t(`validation.${latitudeError}`) : undefined}
              slotProps={{ htmlInput: { 'data-qa': CentersTestId.FallbackLatitude, inputMode: 'decimal' } }}
            />
            <TextField
              label={t('map.longitude')}
              value={longitude}
              size="small"
              onChange={(event) => setLongitude(event.target.value)}
              error={Boolean(longitudeError)}
              helperText={longitudeError ? t(`validation.${longitudeError}`) : undefined}
              slotProps={{ htmlInput: { 'data-qa': CentersTestId.FallbackLongitude, inputMode: 'decimal' } }}
            />
            <Button variant="outlined" startIcon={<AddLocationIcon />} onClick={handleAdd} disabled={Boolean(latitudeError || longitudeError)} data-qa={CentersTestId.FallbackAdd}>
              {t('map.addHere')}
            </Button>
          </Stack>
        )}
        {centers.length > 0 && (
          <Box>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              {t('map.centersOnMap')}
            </Typography>
            <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
              {centers.map((center) => (
                <Chip
                  key={center.id}
                  icon={
                    getMarkerColor ? (
                      <Box component="span" sx={{ display: 'flex', color: getMarkerColor(center) }}>
                        <PlaceIcon fontSize="small" />
                      </Box>
                    ) : undefined
                  }
                  label={center.isActive === false ? `${center.name} (${t('map.inactive')})` : center.name}
                  color={center.id === selectedCenterId ? 'primary' : 'default'}
                  variant={center.id === selectedCenterId ? 'filled' : 'outlined'}
                  onClick={onSelect && center.id ? () => onSelect(center.id ?? '') : undefined}
                  data-qa={CentersTestId.FallbackCenter}
                />
              ))}
            </Stack>
          </Box>
        )}
      </Stack>
    </Paper>
  );
};

export default CentersMapFallback;
