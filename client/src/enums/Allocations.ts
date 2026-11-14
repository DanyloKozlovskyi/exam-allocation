/** Inner tabs of the allocation view. */
export enum AllocationViewTab {
  Overview = 'Overview',
  Participants = 'Participants',
}

/** Manual move mode of the room seating view. */
export enum MoveModeStep {
  Off = 'Off',
  SelectSource = 'SelectSource',
  SelectTarget = 'SelectTarget',
}

export enum AllocationsTestId {
  AlternativeTabs = 'allocation-alternative-tabs',
  ExportButton = 'allocation-export-button',
  ExportMenuItem = 'allocation-export-menu-item',
  PinAllButton = 'allocation-pin-all-button',
  OverviewMap = 'allocation-overview-map',
  RoomsList = 'allocation-rooms-list',
  RoomFillBar = 'allocation-room-fill-bar',
  ParticipantsGrid = 'allocation-participants-grid',
  SearchField = 'allocation-search-field',
  SeatingGrid = 'allocation-seating-grid',
  MoveModeToggle = 'allocation-move-mode-toggle',
  PendingMoves = 'allocation-pending-moves',
  ValidateMovesButton = 'allocation-validate-moves-button',
  ApplyMovesButton = 'allocation-apply-moves-button',
  ResetMovesButton = 'allocation-reset-moves-button',
  ValidationReport = 'allocation-validation-report',
  PinRoomButton = 'allocation-pin-room-button',
  ViolationLegend = 'allocation-violation-legend',
}
