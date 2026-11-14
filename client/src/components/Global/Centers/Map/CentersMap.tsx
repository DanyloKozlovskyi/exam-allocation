/// <reference types="google.maps" />
// The only place in the client that uses @vis.gl/react-google-maps (docs/06). Reused by the allocation overview (WS-F).
import { Box, Paper } from '@mui/material';
import { APIProvider, AdvancedMarker, Map, Pin, useMapsLibrary, type MapMouseEvent } from '@vis.gl/react-google-maps';
import { useEffect, useMemo, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import type { TestCenterDto } from '../../../../api/dtos';
import { appConfig } from '../../../../constants/config';
import { CentersTestId, MapsLibrary, PlaceField, PlacesEvent } from '../../../../enums/Centers';
import { I18nNamespace } from '../../../../i18n';
import CentersMapFallback from './CentersMapFallback';
import { CENTERS_MAP_ZOOM, DEFAULT_MAP_CENTER, DEFAULT_MAP_ZOOM, MAP_HEIGHT_PX, type CentersMapProps, type MapPoint, type PickedPlace } from './types';

/** Google's documented demo map id; Advanced Markers need some map id when none is configured. */
const DEMO_MAP_ID = 'DEMO_MAP_ID';
const GESTURE_HANDLING_GREEDY = 'greedy';
const SELECTED_PIN_COLOR = '#1976D2';
const INACTIVE_PIN_COLOR = '#9E9E9E';
const SELECTED_PIN_SCALE = 1.3;
const PLACE_SEARCH_MIN_WIDTH_PX = 320;

const centroid = (centers: TestCenterDto[]): MapPoint => {
  if (centers.length === 0) {
    return DEFAULT_MAP_CENTER;
  }
  const sum = centers.reduce<MapPoint>((acc, center) => ({ latitude: acc.latitude + (center.latitude ?? 0), longitude: acc.longitude + (center.longitude ?? 0) }), {
    latitude: 0,
    longitude: 0,
  });
  return { latitude: sum.latitude / centers.length, longitude: sum.longitude / centers.length };
};

interface PlaceSearchProps {
  onPlacePicked: (place: PickedPlace) => void;
}

/** Places Autocomplete (new Places API) web component mounted into a container. */
const PlaceSearch = ({ onPlacePicked }: PlaceSearchProps) => {
  const { t } = useTranslation(I18nNamespace.Centers);
  const places = useMapsLibrary(MapsLibrary.Places);
  const containerRef = useRef<HTMLDivElement>(null);
  const callbackRef = useRef(onPlacePicked);

  useEffect(() => {
    callbackRef.current = onPlacePicked;
  }, [onPlacePicked]);

  useEffect(() => {
    const container = containerRef.current;
    if (!places || !container) {
      return undefined;
    }
    const element = new places.PlaceAutocompleteElement({});
    element.setAttribute('placeholder', t('map.searchPlaceholder'));
    const handleSelect = async (event: Event) => {
      const place = (event as google.maps.places.PlacePredictionSelectEvent).placePrediction.toPlace();
      await place.fetchFields({ fields: Object.values(PlaceField) });
      if (!place.location) {
        return;
      }
      callbackRef.current({
        name: place.displayName ?? '',
        address: place.formattedAddress ?? '',
        latitude: place.location.lat(),
        longitude: place.location.lng(),
        placeId: place.id ?? null,
      });
    };
    const listener = (event: Event) => void handleSelect(event);
    element.addEventListener(PlacesEvent.Select, listener);
    container.appendChild(element);
    return () => {
      element.removeEventListener(PlacesEvent.Select, listener);
      element.remove();
    };
  }, [places, t]);

  return <Box ref={containerRef} sx={{ minWidth: PLACE_SEARCH_MIN_WIDTH_PX, mb: 1 }} data-qa={CentersTestId.PlaceSearch} />;
};

const GoogleCentersMap = ({ centers, selectedCenterId, onSelect, onAddAtPoint, onPlacePicked, getMarkerColor }: CentersMapProps) => {
  const center = useMemo(() => centroid(centers), [centers]);

  const handleClick = (event: MapMouseEvent) => {
    const latLng = event.detail.latLng;
    if (latLng && onAddAtPoint) {
      onAddAtPoint({ latitude: latLng.lat, longitude: latLng.lng });
    }
  };

  return (
    <APIProvider apiKey={appConfig.googleMapsApiKey}>
      <Paper variant="outlined" sx={{ p: 1 }} data-qa={CentersTestId.Map}>
        {onPlacePicked && <PlaceSearch onPlacePicked={onPlacePicked} />}
        <Box sx={{ height: MAP_HEIGHT_PX }}>
          <Map
            mapId={appConfig.googleMapsMapId || DEMO_MAP_ID}
            defaultCenter={{ lat: center.latitude, lng: center.longitude }}
            defaultZoom={centers.length > 0 ? CENTERS_MAP_ZOOM : DEFAULT_MAP_ZOOM}
            gestureHandling={GESTURE_HANDLING_GREEDY}
            onClick={handleClick}>
            {centers.map((item) => {
              const isSelected = item.id === selectedCenterId;
              const color = getMarkerColor?.(item) ?? (isSelected ? SELECTED_PIN_COLOR : item.isActive === false ? INACTIVE_PIN_COLOR : undefined);
              return (
                <AdvancedMarker
                  key={item.id}
                  position={{ lat: item.latitude ?? 0, lng: item.longitude ?? 0 }}
                  title={item.name}
                  onClick={onSelect && item.id ? () => onSelect(item.id ?? '') : undefined}>
                  <Pin background={color} borderColor={color} scale={isSelected ? SELECTED_PIN_SCALE : 1} />
                </AdvancedMarker>
              );
            })}
          </Map>
        </Box>
      </Paper>
    </APIProvider>
  );
};

/** Centers map: Google map with markers, click-to-add and Places search, or a coordinates form without an API key. */
const CentersMap = (props: CentersMapProps) => (appConfig.googleMapsApiKey ? <GoogleCentersMap {...props} /> : <CentersMapFallback {...props} />);

export default CentersMap;
