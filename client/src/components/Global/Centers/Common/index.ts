import type { TestCenterDto } from '../../../../api/dtos';
import { CenterConstraint } from '../../../../enums/Centers';
import type { FormFieldError } from '../../../../enums/Sessions';
import { emptyToNull, parseNumber, validateNumber, validateText } from '../../Sessions/Common/formHelpers';
import type { PickedPlace } from '../Map/types';

export interface CenterState {
  id: string | null;
  version: number | null;
  name: string;
  address: string;
  /** Numeric inputs are kept as typed text and parsed on save. */
  latitude: string;
  longitude: string;
  costPerRoom: string;
  googlePlaceId: string | null;
  isActive: boolean;
}

export interface CenterValidationErrors {
  name?: FormFieldError;
  address?: FormFieldError;
  latitude?: FormFieldError;
  longitude?: FormFieldError;
  costPerRoom?: FormFieldError;
}

export enum CenterReducerAction {
  Name = 'name',
  Address = 'address',
  Latitude = 'latitude',
  Longitude = 'longitude',
  CostPerRoom = 'costPerRoom',
  IsActive = 'isActive',
  Load = 'load',
}

export interface CenterAction {
  type: CenterReducerAction;
  payload: Partial<CenterState>;
}

export const initialCenterState: CenterState = {
  id: null,
  version: null,
  name: '',
  address: '',
  latitude: '',
  longitude: '',
  costPerRoom: '',
  googlePlaceId: null,
  isActive: true,
};

export const centerReducer = (state: CenterState, action: CenterAction): CenterState => {
  switch (action.type) {
    case CenterReducerAction.Load:
      return { ...initialCenterState, ...action.payload };
    case CenterReducerAction.Name:
    case CenterReducerAction.Address:
    case CenterReducerAction.Latitude:
    case CenterReducerAction.Longitude:
    case CenterReducerAction.CostPerRoom:
    case CenterReducerAction.IsActive:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};

export const validateCenter = (state: CenterState): CenterValidationErrors => ({
  name: validateText(state.name, CenterConstraint.NameMaxLength, true),
  address: validateText(state.address, CenterConstraint.AddressMaxLength, false),
  latitude: validateNumber(state.latitude, CenterConstraint.MinLatitude, CenterConstraint.MaxLatitude, true),
  longitude: validateNumber(state.longitude, CenterConstraint.MinLongitude, CenterConstraint.MaxLongitude, true),
  costPerRoom: validateNumber(state.costPerRoom, CenterConstraint.MinCost, Number.MAX_SAFE_INTEGER, false),
});

/** Prefill for Create (map click or Places pick), passed as router state. */
export const fromPrefill = (prefill?: Partial<PickedPlace> | null): CenterState => ({
  ...initialCenterState,
  name: prefill?.name ?? '',
  address: prefill?.address ?? '',
  latitude: prefill?.latitude === undefined ? '' : String(prefill.latitude),
  longitude: prefill?.longitude === undefined ? '' : String(prefill.longitude),
  googlePlaceId: prefill?.placeId ?? null,
});

export const fromCenterDto = (dto: TestCenterDto): CenterState => ({
  id: dto.id ?? null,
  version: dto.version ?? null,
  name: dto.name ?? '',
  address: dto.address ?? '',
  latitude: String(dto.latitude ?? ''),
  longitude: String(dto.longitude ?? ''),
  costPerRoom: dto.costPerRoom === null || dto.costPerRoom === undefined ? '' : String(dto.costPerRoom),
  googlePlaceId: dto.googlePlaceId ?? null,
  isActive: dto.isActive ?? true,
});

export const toCenterDto = (state: CenterState, sessionId: string): TestCenterDto => ({
  id: state.id,
  version: state.version,
  sessionId,
  name: state.name.trim(),
  address: emptyToNull(state.address) ?? '',
  latitude: parseNumber(state.latitude) ?? 0,
  longitude: parseNumber(state.longitude) ?? 0,
  costPerRoom: parseNumber(state.costPerRoom),
  googlePlaceId: state.googlePlaceId,
  isActive: state.isActive,
});
