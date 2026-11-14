// UI-only enums of the Centers feature (docs/10 §7).

/** Mirrors api/src/Domain/Centers/TestCenterConstraints.cs and GeoPoint limits. */
export enum CenterConstraint {
  NameMaxLength = 200,
  AddressMaxLength = 500,
  MinLatitude = -90,
  MaxLatitude = 90,
  MinLongitude = -180,
  MaxLongitude = 180,
  MinCost = 0,
}

export enum CenterStep {
  Details = 0,
  Summary = 1,
}

/** Fields requested from Places API (new) when a place is picked. */
export enum PlaceField {
  Id = 'id',
  DisplayName = 'displayName',
  FormattedAddress = 'formattedAddress',
  Location = 'location',
}

/** Google Maps JS API libraries used by the map. */
export enum MapsLibrary {
  Places = 'places',
}

export enum PlacesEvent {
  Select = 'gmp-select',
}

export enum CentersTestId {
  List = 'centers-list',
  CreateButton = 'centers-create-button',
  Map = 'centers-map',
  MapFallback = 'centers-map-fallback',
  FallbackLatitude = 'centers-map-fallback-latitude',
  FallbackLongitude = 'centers-map-fallback-longitude',
  FallbackAdd = 'centers-map-fallback-add',
  FallbackCenter = 'centers-map-fallback-center',
  PlaceSearch = 'centers-map-place-search',
  NameField = 'center-name-field',
  AddressField = 'center-address-field',
  LatitudeField = 'center-latitude-field',
  LongitudeField = 'center-longitude-field',
  CostField = 'center-cost-field',
  ActiveField = 'center-active-field',
  Review = 'center-review',
  RoomsLink = 'center-rooms-link',
}
