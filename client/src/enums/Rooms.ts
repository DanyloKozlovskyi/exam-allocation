/** Visual state of a seat rendered by `components/Common/RoomGrid`. */
export enum SeatState {
  Normal = 'Normal',
  Accessible = 'Accessible',
  Disabled = 'Disabled',
  Occupied = 'Occupied',
  Violation = 'Violation',
  Selected = 'Selected',
}

/** Steps of the room Create/Edit stepper. */
export enum RoomStep {
  Details = 0,
  Layout = 1,
  Summary = 2,
}

export enum RoomGridTestId {
  Grid = 'room-grid',
  Seat = 'room-grid-seat',
  Aisle = 'room-grid-aisle',
  Desk = 'room-grid-desk',
  AdjacencyEdge = 'room-grid-adjacency-edge',
}

export enum RoomsTestId {
  List = 'rooms-list',
  CreateButton = 'rooms-create-button',
  EditButton = 'rooms-edit-button',
  DeleteButton = 'rooms-delete-button',
  NameField = 'room-name-field',
  RowsField = 'room-rows-field',
  ColumnsField = 'room-columns-field',
  SeatsPerDeskField = 'room-seats-per-desk-field',
  AislesField = 'room-aisles-field',
  CostField = 'room-cost-field',
  IsActiveField = 'room-is-active-field',
  LayoutPreview = 'room-layout-preview',
  PreviewCapacity = 'room-preview-capacity',
  AdjacencyToggle = 'room-adjacency-toggle',
  DuplicateCountField = 'room-duplicate-count-field',
  Summary = 'room-summary',
}
