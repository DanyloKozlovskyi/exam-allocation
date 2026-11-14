import { RuleType, type RuleDto, type RuleTypeInfoDto } from '../../../../api/dtos';
import {
  RULE_DEFAULT_WEIGHT,
  RULE_DISPLAY_NAME_MAX_LENGTH,
  RULE_MAX_FILL_PERCENT,
  RULE_MAX_WEIGHT,
  RULE_MIN_FILL_PERCENT,
  RULE_MIN_PER_SCOPE,
  RULE_MIN_WEIGHT,
  PERCENT_FACTOR,
} from './constants';
import { ADJACENCY_RULE_TYPES, ATTRIBUTE_RULE_TYPES, defaultParametersState, fromParametersDto, toParametersDto, type RuleParametersState } from './parameters';

export interface RuleState {
  id: string | null;
  version: number | null;
  type: RuleType;
  isHard: boolean;
  weight: number;
  isEnabled: boolean;
  displayName: string;
  parameters: RuleParametersState;
}

export enum RuleField {
  DisplayName = 'displayName',
  Weight = 'weight',
  AttributeKey = 'attributeKey',
  MaxPerScope = 'maxPerScope',
  Adjacency = 'adjacency',
  MinFillRatio = 'minFillRatio',
}

export enum RuleValidationError {
  AttributeRequired = 'attributeRequired',
  MaxPerScopeRange = 'maxPerScopeRange',
  AdjacencyRequired = 'adjacencyRequired',
  MinFillRatioRange = 'minFillRatioRange',
  WeightRange = 'weightRange',
  DisplayNameTooLong = 'displayNameTooLong',
}

export interface FieldError {
  error: RuleValidationError;
  min?: number;
  max?: number;
}

export type ValidationErrors = Partial<Record<RuleField, FieldError>>;

export enum RuleReducerAction {
  Init = 'Init',
  Type = 'Type',
  IsHard = 'IsHard',
  Weight = 'Weight',
  IsEnabled = 'IsEnabled',
  DisplayName = 'DisplayName',
  Parameters = 'Parameters',
}

export type RuleAction =
  | { type: RuleReducerAction.Init; payload: RuleState }
  | { type: RuleReducerAction.Type; payload: { type: RuleType; info?: RuleTypeInfoDto } }
  | { type: RuleReducerAction.IsHard | RuleReducerAction.IsEnabled; payload: boolean }
  | { type: RuleReducerAction.Weight; payload: number }
  | { type: RuleReducerAction.DisplayName; payload: string }
  | { type: RuleReducerAction.Parameters; payload: Partial<RuleParametersState> };

export const initialRuleState: RuleState = {
  id: null,
  version: null,
  type: RuleType.SeparateByAttribute,
  isHard: true,
  weight: RULE_DEFAULT_WEIGHT,
  isEnabled: true,
  displayName: '',
  parameters: defaultParametersState,
};

/** Keeps `isHard` within what the rule type allows (`rule-types`: allowsHard / allowsSoft). */
export const coerceHard = (isHard: boolean, info: RuleTypeInfoDto | undefined) => {
  if (info?.allowsHard === false) {
    return false;
  }
  if (info?.allowsSoft === false) {
    return true;
  }
  return isHard;
};

export const ruleReducer = (state: RuleState, action: RuleAction): RuleState => {
  switch (action.type) {
    case RuleReducerAction.Init:
      return action.payload;
    case RuleReducerAction.Type:
      return {
        ...state,
        type: action.payload.type,
        isHard: coerceHard(state.isHard, action.payload.info),
        parameters: fromParametersDto(action.payload.info?.defaultParameters, state.parameters),
      };
    case RuleReducerAction.IsHard:
      return { ...state, isHard: action.payload };
    case RuleReducerAction.IsEnabled:
      return { ...state, isEnabled: action.payload };
    case RuleReducerAction.Weight:
      return { ...state, weight: action.payload };
    case RuleReducerAction.DisplayName:
      return { ...state, displayName: action.payload };
    case RuleReducerAction.Parameters:
      return { ...state, parameters: { ...state.parameters, ...action.payload } };
    default:
      return state;
  }
};

export const fromRuleDto = (rule: RuleDto): RuleState => ({
  id: rule.id ?? null,
  version: rule.version ?? null,
  type: rule.type ?? RuleType.SeparateByAttribute,
  isHard: rule.isHard ?? true,
  weight: rule.weight ?? RULE_DEFAULT_WEIGHT,
  isEnabled: rule.isEnabled ?? true,
  displayName: rule.displayName ?? '',
  parameters: fromParametersDto(rule.parameters),
});

export const toRuleDto = (state: RuleState, sessionId: string): RuleDto => ({
  id: state.id,
  sessionId,
  type: state.type,
  isHard: state.isHard,
  weight: state.weight,
  isEnabled: state.isEnabled,
  displayName: state.displayName.trim() || null,
  parameters: toParametersDto(state.type, state.parameters),
  version: state.version,
});

export const validateDetails = (state: RuleState): ValidationErrors => {
  const errors: ValidationErrors = {};
  if (!state.isHard && !(Number.isInteger(state.weight) && state.weight >= RULE_MIN_WEIGHT && state.weight <= RULE_MAX_WEIGHT)) {
    errors[RuleField.Weight] = { error: RuleValidationError.WeightRange, min: RULE_MIN_WEIGHT, max: RULE_MAX_WEIGHT };
  }
  if (state.displayName.length > RULE_DISPLAY_NAME_MAX_LENGTH) {
    errors[RuleField.DisplayName] = { error: RuleValidationError.DisplayNameTooLong, max: RULE_DISPLAY_NAME_MAX_LENGTH };
  }
  return errors;
};

export const validateParameters = (state: RuleState): ValidationErrors => {
  const errors: ValidationErrors = {};
  const { parameters } = state;
  if (ATTRIBUTE_RULE_TYPES.includes(state.type) && !parameters.attributeKey.trim()) {
    errors[RuleField.AttributeKey] = { error: RuleValidationError.AttributeRequired };
  }
  if (state.type === RuleType.SeparateByAttribute && !(Number.isInteger(parameters.maxPerScope) && parameters.maxPerScope >= RULE_MIN_PER_SCOPE)) {
    errors[RuleField.MaxPerScope] = { error: RuleValidationError.MaxPerScopeRange, min: RULE_MIN_PER_SCOPE };
  }
  if (ADJACENCY_RULE_TYPES.includes(state.type) && parameters.adjacency.length === 0) {
    errors[RuleField.Adjacency] = { error: RuleValidationError.AdjacencyRequired };
  }
  const percent = parameters.minFillRatio * PERCENT_FACTOR;
  if (state.type === RuleType.MinRoomFill && !(percent >= RULE_MIN_FILL_PERCENT && percent <= RULE_MAX_FILL_PERCENT)) {
    errors[RuleField.MinFillRatio] = { error: RuleValidationError.MinFillRatioRange, min: RULE_MIN_FILL_PERCENT, max: RULE_MAX_FILL_PERCENT };
  }
  return errors;
};

export const hasErrors = (errors: ValidationErrors) => Object.keys(errors).length > 0;
