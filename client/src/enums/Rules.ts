/** Steps of the rule Create/Edit stepper. */
export enum RuleStep {
  Details = 0,
  Parameters = 1,
  Summary = 2,
}

export enum RulesTestId {
  List = 'rules-list',
  CreateButton = 'rules-create-button',
  EditButton = 'rules-edit-button',
  DeleteButton = 'rules-delete-button',
  EnabledSwitch = 'rules-enabled-switch',
  PrecheckButton = 'rules-precheck-button',
  TypeField = 'rule-type-field',
  DisplayNameField = 'rule-display-name-field',
  HardField = 'rule-hard-field',
  WeightField = 'rule-weight-field',
  EnabledField = 'rule-enabled-field',
  AttributeField = 'rule-attribute-field',
  ScopeField = 'rule-scope-field',
  MaxPerScopeField = 'rule-max-per-scope-field',
  AdjacencyField = 'rule-adjacency-field',
  SeatLevelSideField = 'rule-seat-level-side-field',
  MinFillRatioField = 'rule-min-fill-ratio-field',
  NoParameters = 'rule-no-parameters',
  Summary = 'rule-summary',
}

/** Built-in attribute keys (mirror backend `AttributeKeys`). */
export enum BuiltInAttributeKey {
  Group = 'group',
  School = 'school',
}
