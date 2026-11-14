import type { TestCenterDto } from '../../../../api/dtos';

export interface MapPoint {
  latitude: number;
  longitude: number;
}

/** A place chosen via Places Autocomplete; used to prefill Create. */
export interface PickedPlace extends MapPoint {
  name: string;
  address: string;
  placeId: string | null;
}

export interface CentersMapProps {
  centers: TestCenterDto[];
  selectedCenterId?: string | null;
  onSelect?: (centerId: string) => void;
  onAddAtPoint?: (point: MapPoint) => void;
  onPlacePicked?: (place: PickedPlace) => void;
  /** Optional per-center marker colour (e.g. fill ratio in allocation overview). */
  getMarkerColor?: (center: TestCenterDto) => string | undefined;
}

/** Center of Ukraine — initial camera when there are no centers. */
export const DEFAULT_MAP_CENTER: MapPoint = { latitude: 49.0, longitude: 31.4 };
export const DEFAULT_MAP_ZOOM = 6;
export const CENTERS_MAP_ZOOM = 12;
export const MAP_HEIGHT_PX = 420;
